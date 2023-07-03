using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.WebsocketCrawler.Client;
using Mikibot.Crawler.WebsocketCrawler.Data;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Zast.Player.CUI.Bilibili
{
    public class RoomEventStreaming : IDisposable
    {
        private readonly int rawRoomId;
        private readonly BiliLiveCrawler crawler;
        private CancellationTokenSource? csc;

        public RoomEventStreaming(BiliLiveCrawler crawler, int rawRoomId)
        {
            this.rawRoomId = rawRoomId;
            this.crawler = crawler;
        }

        public void Dispose()
        {
            using var _csc = csc;
            GC.SuppressFinalize(this);
        }

        public async IAsyncEnumerable<ICommandBase> RunAsync(long roomId, long uid, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var _csc = csc = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var spectatorEndpoint = await crawler.GetLiveToken(roomId, _csc.Token);

            foreach (var spectatorHost in spectatorEndpoint.Hosts)
            {
                using var wsClient = new WebsocketClient();
                await wsClient.ConnectAsync(spectatorHost.Host, spectatorHost.WssPort, roomId, uid, spectatorEndpoint.Token, "wss", _csc.Token);

                await foreach (var @event in wsClient.Events(_csc.Token))
                {
                    if (@event is Normal normal)
                    {
                        var cmd = ICommandBase.Parse(normal.RawContent);
                        if (cmd != null)
                        {
                            yield return cmd;
                        }
                    }
                }
            }
        }
    }
}
