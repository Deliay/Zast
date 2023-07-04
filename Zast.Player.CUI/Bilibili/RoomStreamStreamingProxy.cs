using FFMpegCore.Pipes;
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
            var realRoomid = await crawler.GetRealRoomId(roomId, token);
            var allAddresses = await crawler.GetLiveStreamAddress(realRoomid, token);
            if (allAddresses.Count <= 0) return default;

            return allAddresses[_random.Next(0, allAddresses.Count - 1)].Url;
        }

        private async Task<Stream> OpenLiveStream(CancellationToken token)
        {
            var url = await GetLiveStreamAddress(token);

            var res = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);

            var stream = await res.Content.ReadAsStreamAsync(token);

            await Task.Delay(TimeSpan.FromSeconds(2), token);

            return stream;
        }
        private readonly Stream _stream = new MemoryStream();
        private async Task WriteWaveStream(PipeWriter writer, CancellationToken token)
        {
            var pipe = new Pipe();
            var rawStream = await OpenLiveStream(token);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    Stopwatch sw = new();
                    AnsiConsole.MarkupLine($"[grey]系统[/] [lime]准备启动音频流[/]");
                    sw.Start();
                    await FFMpegArguments
                        .FromPipeInput(new StreamPipeSource(rawStream))
                        .OutputToPipe(new StreamPipeSink(writer.AsStream()), opt => opt
                            .DisableChannel(Channel.Video)
                            .WithCustomArgument("-f wav"))
                        .WithLogLevel(FFMpegLogLevel.Error)
                        .CancellableThrough(token)
                        .NotifyOnOutput((progress) => AnsiConsole.MarkupLine($"[grey]ff[/] {progress.EscapeMarkup()}"))
                        .NotifyOnError((progress) => AnsiConsole.MarkupLine($"[grey]ff[/] [red]{progress.EscapeMarkup()}[/]"))
                        .ProcessAsynchronously();
                    sw.Stop();
                    AnsiConsole.MarkupLine($"[grey]系统[/] [red]音频流终止，持续 {sw.Elapsed.TotalSeconds}s[/]");
                }
                catch (ObjectDisposedException)
                {
                    AnsiConsole.MarkupLine($"[grey]系统[/] [red]B站直播流已经断开[/]");
                    break;
                }
                catch (OperationCanceledException) {}
                catch (Exception e)
                {
                    AnsiConsole.WriteException(e);
                }
                finally
                {
                    
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
            using var __Stream = _stream;
            using var _host = host;
        }
    }
}
