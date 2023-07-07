using FFMpegCore;
using Microsoft.Extensions.Logging;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.Http.Bilibili.Model;
using System;
using System.Collections.Generic;
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
        private readonly CancellationTokenSource csc;
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
            this.csc = new CancellationTokenSource();
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

        private string PickAddress(RecordConfig config, LiveStreamAddressesV2 addresses)
        {
            int bitrate = config.Quality;
            var (protocol, format, codec) = ModeArgs(config.Mode);

            var sortedStreams = addresses.PlayUrlInfo.PlayUrl.Streams
                .SelectMany(s => s.Formats.SelectMany(f => f.Codec.Select(c => new Stream()
                {
                    Codec = c.CodecName,
                    Protocol = s.ProtocolName,
                    Format = f.FormatName,
                    Bitrate = c.Quality,
                    Url = $"{c.UrlInfos.First().Host}{c.BaesUrl}{c.UrlInfos.First().Extra}",
                })))
                .Order(this).ToList();

            var nearlyStreamGroup = sortedStreams.GroupBy(s => s.Bitrate)
                .Max(s => Math.Abs(s.Key - bitrate) * -1);
        }

        private async Task LoopingRecording()
        {
            while (!csc.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), csc.Token);
                var info = await crawler.GetLiveStreamAddressV2(roomId);
                var config = await configRepository.Load(csc.Token);
                if (info.LiveStatus == 0)
                {
                    continue;
                }
                var recCsc = CancellationTokenSource.CreateLinkedTokenSource(csc.Token);
                await FFMpegArguments.FromUrlInput()
            }
        }

        public async ValueTask Initialize(long roomId)
        {
            this.roomId = roomId;
            this.info = await crawler.GetLiveRoomInfo(roomId, csc.Token);
            
            loggerScope = this.logger.BeginScope($"{roomId}");

            _ = Task.Run(LoopingRecording, csc.Token);
        }

        public void Dispose()
        {
            using var _csc = this.csc;
            using var _s = loggerScope;
            GC.SuppressFinalize(this);
        }

        int IComparer<Stream>.Compare(Stream x, Stream y)
        {
            if (x.Bitrate > y.Bitrate) return 1;
            if (x.Protocol == "http_stream") return 1;
            if (x.Codec == "hevc") return 1;
            if (x.Format == "ts") return 1;

            return -1;
        }
    }
}
