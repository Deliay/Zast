﻿using FFMpegCore.Pipes;
using FFMpegCore;
using Mikibot.Crawler.Http.Bilibili;
using SimpleHttpServer.Host;
using SimpleHttpServer.Pipeline;
using SimpleHttpServer.Pipeline.Middlewares;
using SimpleHttpServer.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FFMpegCore.Enums;
using System.IO;
using System.IO.Pipelines;
using System.IO.Pipes;
using Spectre.Console;
using System.Buffers;
using System.Diagnostics;
using Instances.Exceptions;

namespace Zast.Player.CUI.Bilibili
{
    public class RoomStreamStreamingProxy : IDisposable
    {
        private readonly SimpleHost host;
        private readonly BiliLiveCrawler crawler;
        private readonly long roomId;
        private static readonly HttpClient HttpClient = new();
        private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.80 Safari/537.36 Edg/98.0.1108.50";
        static RoomStreamStreamingProxy()
        {
            HttpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            HttpClient.DefaultRequestHeaders.Add("Referer", "https://live.bilibili.com/");
            HttpClient.DefaultRequestHeaders.Add("Origin", "https://live.bilibili.com");
        }
        public RoomStreamStreamingProxy(BiliLiveCrawler crawler, long roomId)
        {
            this.host = new SimpleHostBuilder()
                .ConfigureServer(server =>
                {
                    server.ListenLocalPort(11111);
                })
                .Build();
            this.crawler = crawler;
            this.roomId = roomId;
        }

        private readonly Random _random = new();
        private async ValueTask<string?> GetLiveStreamAddress(CancellationToken token)
        {
            var addressResult = await crawler.GetLiveStreamAddressV2(roomId, token);
            return addressResult.PlayUrlInfo.PlayUrl.Streams
                .SelectMany(c => c.Formats)
                .SelectMany(c => c.Codec)
                .Select(c => $"{c.UrlInfos.First().Host}{c.BaesUrl}{c.UrlInfos.First().Extra}")
                .FirstOrDefault();
        }

        private async Task OpenLiveStream(PipeWriter writer, CancellationToken token)
        {
            var url = await GetLiveStreamAddress(token);
            var res = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
            
            await res.Content.CopyToAsync(writer.AsStream(), token);
        }

        private async Task PeekLiveStream(PipeWriter writer, CancellationToken token)
        {
            var pipe = new Pipe();
            _ = OpenLiveStream(pipe.Writer, token);

            while (!token.IsCancellationRequested)
            {
                var result = await pipe.Reader.ReadAsync(token);
                while (!token.IsCancellationRequested && !result.IsCanceled && !result.IsCompleted)
                {
                    ReadOnlySequence<byte> buffer = result.Buffer;
                    Memory<byte> chunk = new byte[buffer.Length];
                    
                    foreach (var item in result.Buffer)
                    {
                        item.CopyTo(chunk);
                    }
                    await writer.WriteAsync(chunk, token);
                    await writer.FlushAsync(token);
                    pipe.Reader.AdvanceTo(buffer.End);
                    result = await pipe.Reader.ReadAsync(token);
                }
            }
        }

        private async Task WriteWaveStream(PipeWriter writer, CancellationToken token)
        {
            var pipe = new Pipe();
            _ = OpenLiveStream(pipe.Writer, token);
            var errorCount = 0;
            while (!token.IsCancellationRequested)
            {
                if (++errorCount > 3)
                {
                    AnsiConsole.MarkupLine("[grey]ff[/] [red]直播流重试次数过多，即将重新拉取新的直播流[/]");
                    return;
                }
                try
                {
                    Stopwatch sw = new();
                    AnsiConsole.MarkupLine($"[grey]ff[/] [lime]准备启动音频流[/]");
                    sw.Start();

                    await FFMpegArguments
                        .FromPipeInput(new StreamPipeSource(pipe.Reader.AsStream()), opt => opt
                            .WithCustomArgument("-fflags +discardcorrupt"))
                        .OutputToPipe(new StreamPipeSink(writer.AsStream()), opt => opt
                            .DisableChannel(Channel.Video)
                            .WithCustomArgument("-f wav"))
                        .CancellableThrough(token)
                        .ProcessAsynchronously(true, new() { Encoding = Encoding.UTF8 });
                    sw.Stop();
                    AnsiConsole.MarkupLine($"[grey]ff[/] [red]音频流终止，持续 {sw.Elapsed.TotalSeconds}s[/]");
                }
                catch (ObjectDisposedException)
                {
                    AnsiConsole.MarkupLine($"[grey]ff[/] [red]B站直播流已经断开[/]");
                    break;
                }
                catch (InstanceFileNotFoundException)
                {
                    AnsiConsole.MarkupLine($"[grey]ff[/] [red]未找到ffmpeg，请将其安装到环境变量中[/]");
                    throw;
                }
                catch (OperationCanceledException) {}
                catch (Exception e)
                {
                    AnsiConsole.WriteException(e);
                }
            }

        }
        public async ValueTask Route(RequestContext ctx, Func<ValueTask> next)
        {
            ctx.Http.Response.StatusCode = 200;
            ctx.Http.Response.ContentType = "audio/wave";

            try
            {
                while (!ctx.CancelToken.IsCancellationRequested)
                {
                    try
                    {
                        using var csc = CancellationTokenSource.CreateLinkedTokenSource(ctx.CancelToken);
                        var token = csc.Token;

                        var pipe = new Pipe();
                        _ = WriteWaveStream(pipe.Writer, token)
                            .ContinueWith((_) => csc.Cancel());

                        var result = await pipe.Reader.ReadAsync(token);
                        while (!token.IsCancellationRequested && !result.IsCanceled && !result.IsCompleted)
                        {
                            ReadOnlySequence<byte> buffer = result.Buffer;
                            foreach (var item in result.Buffer)
                            {
                                await ctx.Http.Response.OutputStream.WriteAsync(item, token);
                            }
                            pipe.Reader.AdvanceTo(buffer.End);
                            result = await pipe.Reader.ReadAsync(token);
                        }
                        AnsiConsole.MarkupLine($"[grey]系统[/] [red]直播流被意外中止，3秒后重启[/]");
                        await Task.Delay(TimeSpan.FromSeconds(3));
                    }
                    catch (OperationCanceledException) {}
                    catch (InstanceFileNotFoundException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        AnsiConsole.WriteException(e);
                    }
                }
            }
            finally
            {
                ctx.Http.Response.Close();
            }
            
        }

        public const string WaveEndpoint = "http://localhost:11111/streaming/wave";

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {

            host.AddHandlers(h => h.Use(RouterMiddleware.Route("/streaming/wave", r => r.Use(Route))));

            await host.Run(cancellationToken);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            using var _host = host;
        }
    }
}
