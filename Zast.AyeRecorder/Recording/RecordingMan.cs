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
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zast.AyeRecorder.Config;
using Zast.AyeRecorder.Script.Config;

namespace Zast.AyeRecorder.Recording
{
    public class RecordingMan : IDisposable, IComparer<RecordingMan.Stream>
    {
        private readonly BiliLiveCrawler crawler;
        private readonly BiliLiveStreamCrawler streamCrawler;
        private readonly ILogger<RecordingMan> logger;
        private readonly RecordConfigRepository configRepository;
        private IDisposable? loggerScope;
        private readonly CancellationTokenSource cts;
        private long roomId;
        private LiveRoomInfo info;

        public RecordingMan(
            BiliLiveCrawler crawler,
            BiliLiveStreamCrawler streamCrawler,
            ILogger<RecordingMan> logger,
            RecordConfigRepository configRepository
            )
        {
            this.crawler = crawler;
            this.streamCrawler = streamCrawler;
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
                .Where(s => s.Codec != "hevc")
                .OrderDescending(this).ToList();

            foreach (var item in streams)
            {
                logger.LogInformation($"可用录制源 {item.Protocol} {item.Format} in {item.Codec} 画质 {item.Bitrate} ({BitrateConfig.Convert(item.Bitrate)}) ");
            }

            logger.LogInformation($"码率偏好 {bitrate} {protocol} | {format} | {codec}");

            var stream = streams
                .GroupBy(s => s.Bitrate)
                .MaxBy(s => Math.Abs(s.Key - bitrate) * -1)
                ?.OrderDescending(this)
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

        private Task FFmpegRecording(Stream stream, PipeWriter writer, CancellationToken cancellationToken)
        {
            logger.LogInformation("使用 ffmpeg 进行输出");
            var output = writer.AsStream();
            return FFMpegArguments
                .FromUrlInput(new Uri(stream.Url))
                .OutputToPipe(new StreamPipeSink(output), opt => opt.CopyChannel(Channel.All).ForceFormat("flv"))
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously(true);
        }

        private Task DirectRecording(Stream stream, PipeWriter writer, CancellationToken cancellationToken)
        {
            logger.LogInformation("使用 B站flv 进行输出");
            var @out = writer.AsStream();
            return streamCrawler.OpenLiveStream(stream.Url, @out, cancellationToken);
        }

        private Task Recording(Stream stream, PipeWriter writer, CancellationToken cancellationToken)
        {
            return stream.Protocol switch
            {
                "http_stream" => DirectRecording(stream, writer, cancellationToken),
                _ => FFmpegRecording(stream, writer, cancellationToken),
            };
        }

        private async Task WriteFile(string path, PipeReader reader, CancellationToken cancellationToken)
        {
            using var fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            logger.LogInformation($"输出文件 {path}");
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await reader.ReadAtLeastAsync(1, cancellationToken);
                if (result.IsCompleted)
                {
                    continue;
                }
                while (!cancellationToken.IsCancellationRequested && !result.IsCanceled && !result.IsCompleted)
                {
                    ReadOnlySequence<byte> buffer = result.Buffer;
                    foreach (var item in result.Buffer)
                    {
                        await fs.WriteAsync(item, cancellationToken);
                    }
                    reader.AdvanceTo(buffer.End);
                    result = await reader.ReadAsync(cancellationToken);
                }
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

                    logger.LogInformation($"开始录制 {roomId} 源格式 {liveStream.Protocol} - {liveStream.Codec} {liveStream.Format} 码率 {liveStream.Bitrate}");

                    Pipe pipe = new();

                    Task recordingTask = Recording(liveStream, pipe.Writer, cancellationToken);

                    Task fileWriteTask = WriteFile(path, pipe.Reader, cancellationToken);

                    await Task.WhenAny(recordingTask, fileWriteTask);
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
            if (x.Bitrate > y.Bitrate) return 1;
            else if (x.Protocol == "http_stream") return -1;
            else if (y.Protocol != "http_stream") return 1;
            if (x.Codec == "avc") return 1;
            else if (y.Codec == "avc") return -1;
            if (x.Codec == "hevc") return 1;
            else if (y.Codec == "hevc") return -1;
            if (x.Format == "ts") return 1;
            else if (x.Format == "ts") return -1;

            return -1;
        }
    }
}
