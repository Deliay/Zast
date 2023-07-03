using Mikibot.Crawler.Http.Bilibili;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zast.Player.CUI.Bilibili.Streaming.PipeUtil;

namespace Zast.Player.CUI.Bilibili.Streaming
{
    public class RoomStreamSupplier : IDisposable
    {
        private readonly BiliLiveCrawler liveCrawler;
        private readonly BiliLiveStreamCrawler streamCrawler;
        private readonly CancellationTokenSource csc;

        public RoomStreamSupplier(BiliLiveCrawler liveCrawler, BiliLiveStreamCrawler streamCrawler)
        {
            this.liveCrawler = liveCrawler;
            this.streamCrawler = streamCrawler;
            csc = new CancellationTokenSource();
        }

        private Random _random = new();
        private async ValueTask<string?> GetLiveStreamAddress(long roomId, CancellationToken cancellationToken)
        {
            var realRoomid = await liveCrawler.GetRealRoomId(roomId, cancellationToken);
            var allAddresses = await liveCrawler.GetLiveStreamAddress(realRoomid, cancellationToken);
            if (allAddresses.Count <= 0) return default;

            return allAddresses[_random.Next(0, allAddresses.Count - 1)].Url;
        }

        private readonly List<Func<CancellationToken, Task<PipeWriter>>> writers = new();

        public RoomStreamSupplier To(Func<CancellationToken, Task<PipeWriter>> writer)
        {
            writers.Add(writer);
            return this;
        }

        public async Task RunAsync(long roomId, CancellationToken cancellationToken)
        {
            using var runCsc = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, csc.Token);
            var token = runCsc.Token;

            var url = await GetLiveStreamAddress(roomId, token);
            if (url is null)
            {
                throw new InvalidDataException();
            }

            var sourceStream = await streamCrawler.OpenLiveStream(url, token);

            using var mulPipe = new PipeMulticaster(sourceStream);
            foreach (var writer in writers)
            {
                mulPipe.To(writer);
            }
            await mulPipe.Start(cancellationToken);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            writers.Clear();
        }
    }
}
