using DotLiquid;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using Microsoft.Extensions.Logging;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.Http.Bilibili.Model;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zast.AyeRecorder.Config;
using Zast.AyeRecorder.Script.Config;

namespace Zast.AyeRecorder.Recording
{
    public class RecordingMan : IDisposable, IComparer<RecordingMan.Stream>
    {
        private readonly BiliLiveCrawler crawler;
        private readonly ILogger<RecordingMan> logger;
        private readonly RecordConfigRepository configRepository;
        private IDisposable? loggerScope;
        private readonly CancellationTokenSource cts;
        private long roomId;
        private LiveRoomInfo info;

        public RecordingMan(
            BiliLiveCrawler crawler,
            ILogger<RecordingMan> logger,
            RecordConfigRepository configRepository
            )
        {
            this.crawler = crawler;
            this.logger = logger;
            this.configRepository = configRepository;
            this.cts = new CancellationTokenSource();
        }

        public (string, string, string) ModeArgs(Mode mode)
        {
            return mode switch
            {
                Mode.streaming => ("http_stream", "flv", "avc"),
                Mode.m3u8_hls_ts_h265 => ("http_hls", "ts", "hevc"),
                Mode.m3u8_hls_ts_h264 => ("http_hls", "ts", "avc"),
                Mode.m3u8_hls_fmp4_h265 => ("http_hls", "fmp4", "hevc"),
                Mode.m3u8_hls_fmp4_h264 => ("http_hls", "fmp4", "avc"),
                _ => throw new NotImplementedException(),
            };
        }

        private struct Stream
        {
            public string Url { get; set; }
            public string Protocol { get; set; }
            public string Format { get; set; }
            public string Codec { get; set; }
            public int Bitrate { get; set; }
        }

        private Stream PickStream(RecordConfig config, LiveStreamAddressesV2 addresses)
        {
            int bitrate = config.Quality;
            var (protocol, format, codec) = ModeArgs(config.Mode);

            var streams = addresses.PlayUrlInfo.PlayUrl.Streams
                .SelectMany(s => s.Formats.SelectMany(f => f.Codec.Select(c => new Stream()
                {
                    Codec = c.CodecName,
                    Protocol = s.ProtocolName,
                    Format = f.FormatName,
                    Bitrate = c.Quality,
                    Url = $"{c.UrlInfos.First().Host}{c.BaesUrl}{c.UrlInfos.First().Extra}",
                })))
                .Order(this).ToList();

            foreach (var item in streams)
            {
                logger.LogInformation($"可用录制源 {item.Protocol} {item.Format} in {item.Codec} 画质 {item.Bitrate} ({BitrateConfig.Convert(item.Bitrate)}) ");
            }

            logger.LogInformation($"码率偏好 {bitrate} {protocol} | {format} | {codec}");

            var stream = streams
                .GroupBy(s => s.Bitrate)
                .MaxBy(s => Math.Abs(s.Key - bitrate) * -1)
                ?.Order(this)
                ?.FirstOrDefault();

            return stream ?? throw new InvalidDataException();
        }

        private async ValueTask<string> GetFileName(RecordConfig config, Stream stream)
        {
            var roomInfo = await crawler.GetLiveRoomInfo(roomId, cts.Token);

            var variable = new
            {
                roomId,
                recordingAt = $"{DateTime.Now:yyyyMMdd-HHmmss-fff}",
                title = roomInfo.Title,
                quality = BitrateConfig.Convert(stream.Bitrate),
            };

            try
            {
                Template template = Template.Parse(config.RecordFileNameFormat ?? RecordConfig.Default().RecordFileNameFormat);
                return template.Render(Hash.FromAnonymousObject(variable));
            }
            catch (Exception e)
            {
                logger.LogError(e, $"文件名格式 {config.RecordFileNameFormat} 无法渲染");
                return $"{roomId}-{variable.recordingAt:yyyyMMdd-HHmmss-fff}-{variable.title}-{variable.quality}";
            }
        }

        private async Task LoopingRecording(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var info = await crawler.GetLiveStreamAddressV2(roomId, cancellationToken);
                    var config = await configRepository.Load(cancellationToken) ?? RecordConfig.Default();
                    if (info.LiveStatus == 0 || info.PlayUrlInfo is null)
                    {
                        continue;
                    }

                    var liveStream = PickStream(config, info);

                    var fileName = await GetFileName(config, liveStream);

                    if (!Directory.Exists($"{roomId}"))
                    {
                        Directory.CreateDirectory($"{roomId}");
                    }

                    var path = Path.Combine(Environment.CurrentDirectory, $"{roomId}", $"{fileName}.flv");

                    var index = 0;
                    while (File.Exists(path))
                    {
                        path = Path.Combine(Environment.CurrentDirectory, $"{roomId}", $"{fileName}_{index}.flv");
                    }

                    logger.LogInformation($"开始录制 {roomId} 源格式 {liveStream.Protocol} - {liveStream.Codec} {liveStream.Format} 码率 {liveStream.Bitrate} 目标文件 {path}");

                    Pipe pipe = new();

                    using var output = pipe.Writer.AsStream();
                    Task ffmpegTask = FFMpegArguments
                        .FromUrlInput(new Uri(liveStream.Url))
                        .OutputToPipe(new StreamPipeSink(output), opt => opt.CopyChannel(Channel.All).ForceFormat("flv"))
                        .CancellableThrough(cancellationToken)
                        .ProcessAsynchronously(true);


                    Task fileWriteTask = Task.Run(async () =>
                    {
                        using var fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            var result = await pipe.Reader.ReadAsync(cancellationToken);
                            while (!cancellationToken.IsCancellationRequested && !result.IsCanceled && !result.IsCompleted)
                            {
                                ReadOnlySequence<byte> buffer = result.Buffer;
                                foreach (var item in result.Buffer)
                                {
                                    await fs.WriteAsync(item, cancellationToken);
                                }
                                pipe.Reader.AdvanceTo(buffer.End);
                                result = await pipe.Reader.ReadAsync(cancellationToken);
                            }
                        }
                    }, cancellationToken);

                    await Task.WhenAny(ffmpegTask, fileWriteTask);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "监听房间出错");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
                }
            }
        }

        public async ValueTask Initialize(long roomId)
        {
            this.roomId = roomId;

            this.info = await crawler.GetLiveRoomInfo(roomId, cts.Token);
            
            loggerScope = this.logger.BeginScope($"{roomId}");
            _ = LoopingRecording(cts.Token);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.cts?.Cancel();
            this.cts?.Dispose();
            this.loggerScope?.Dispose();
        }

        int IComparer<Stream>.Compare(Stream x, Stream y)
        {
            if (x.Bitrate > y.Bitrate) return 6;
            else if (x.Protocol == "http_stream") return 5;
            else if (x.Codec == "hevc") return 2;
            else if (x.Format == "ts") return 1;

            return -1;
        }
    }
}
