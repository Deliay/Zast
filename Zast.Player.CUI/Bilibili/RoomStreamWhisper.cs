using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using Mikibot.Crawler.Http.Bilibili;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Whisper.net;

namespace Zast.Player.CUI.Bilibili
{
    public class RoomStreamWhisper : IDisposable
    {
        private static readonly HttpClient HttpClient = new();
        private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.80 Safari/537.36 Edg/98.0.1108.50";
        static RoomStreamWhisper()
        {
            HttpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            HttpClient.DefaultRequestHeaders.Add("Referer", "https://live.bilibili.com/");
            HttpClient.DefaultRequestHeaders.Add("Origin", "https://live.bilibili.com");
        }
        private readonly BiliLiveCrawler crawler;
        private readonly long roomId;
        private readonly WhisperFactory whisperFactory;
        private readonly WhisperProcessor whisper;

        public RoomStreamWhisper(BiliLiveCrawler crawler, long roomId)
        {
            this.crawler = crawler;
            this.roomId = roomId;
            var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            this.whisperFactory = WhisperFactory.FromPath(Path.Combine(user, ".cache", "whisper", "ggml-medium.bin"));
            this.whisper = whisperFactory
                .CreateBuilder()
                .WithLogProbThreshold(0)
                .WithLanguage("chinese")
                .Build();
        }


        private Random _random = new();
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

            return await res.Content.ReadAsStreamAsync(token);

        }

        private async Task WriteWaveStream(Stream @out, CancellationToken token)
        {
            await FFMpegArguments
                .FromPipeInput(new StreamPipeSource(await OpenLiveStream(token)))
                .OutputToPipe(new StreamPipeSink(@out), opt => opt
                    .DisableChannel(Channel.Video)
                    .WithCustomArgument("-osr 16000 -f wav"))
                .ProcessAsynchronously();
            
        }
        private const int INTERVAL = 20;
        private async Task<MemoryStream> PartialStream(TimeSpan skip, MemoryStream @in, CancellationToken token)
        {
            var reader = new MemoryStream(@in.GetBuffer());
            var @out = new MemoryStream();
            await FFMpegArguments
                .FromPipeInput(new StreamPipeSource(reader), opt => opt.Seek(skip))
                .OutputToPipe(new StreamPipeSink(@out), opt => opt
                    .WithCustomArgument("-osr 16000 -f wav"))
                .ProcessAsynchronously();
                
            return @out;
        }

        public async IAsyncEnumerable<(SegmentData, SegmentData)> RunAsync([EnumeratorCancellation] CancellationToken token)
        {
            var @out = new MemoryStream();
            _ = WriteWaveStream(@out, token);


            TimeSpan time = TimeSpan.Zero;
            while (!token.IsCancellationRequested)
            {
                // peak 30 seconds
                await Task.Delay(TimeSpan.FromSeconds(INTERVAL), token);

                var stream = await PartialStream(time, @out, token);

                var tmp = $"{Path.GetTempFileName()}.wav";
                await File.WriteAllBytesAsync(tmp, stream.ToArray(), token);
                var info = await FFProbe.AnalyseAsync(tmp, cancellationToken: token);
                File.Delete(tmp);
                
                var reader = new MemoryStream(stream.ToArray());
                SegmentData last = null!;
                await foreach (var item in whisper.ProcessAsync(reader, token))
                {
                    yield return (item, last);
                    last = item;

                }

                time += info.Duration;
                Debug.WriteLine($"Duration: {time}");
            }
        }

        public void Dispose()
        {
            using var _f = whisperFactory;
            using var _w = whisper;
            GC.SuppressFinalize(this);
        }

    }
}
