using EmberKernel.Plugins.Components;
using HandyControl.Controls;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.WebsocketCrawler.Client;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands;
using Mikibot.Crawler.WebsocketCrawler.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmberKernel.Services.UI.Mvvm.ViewComponent.Window;

namespace Zast.UI.Crawler
{
    public class LiveEventCrawler : OptimizedObservableCollection<ICommandBase>, IComponent
    {
        private readonly BiliLiveCrawler crawler;
        private readonly WebsocketClient ws;
        private readonly CancellationTokenSource csc;
        private readonly IWindowManager wm;

        public LiveEventCrawler(IWindowManager wm)
        {
            this.crawler = new BiliLiveCrawler(new HttpClient());
            this.csc = new CancellationTokenSource();
            this.ws = new WebsocketClient();
            this.wm = wm;
        }

        public async ValueTask ConnectAsync()
        {
            var roomId = 11306;
            var realRoomId = await crawler.GetRealRoomId(roomId, csc.Token);
            var spectatorEndpoint = await crawler.GetLiveToken(realRoomId, csc.Token);
            var spectatorHost = spectatorEndpoint.Hosts[0];
            await ws.ConnectAsync(spectatorHost.Host, spectatorHost.WssPort, realRoomId, 0, spectatorEndpoint.Token, "wss", cancellationToken: csc.Token);

            _ = Consume(csc.Token);
        }

        private async Task Consume(CancellationToken token)
        {
            await foreach (var @event in ws.Events(csc.Token))
            {
                if (token.IsCancellationRequested) return;
                if (@event is Normal normal)
                {
                    var cmd = ICommandBase.Parse(normal.RawContent);
                    if (cmd != null)
                    {
                        switch (cmd.Command)
                        {
                            case KnownCommands.INTERACT_WORD:
                            case KnownCommands.DANMU_MSG:
                                await wm.BeginUIThreadScope(() => this.Insert(this.Count, cmd));
                                break;
                            default:
                                continue;
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            using var _csc = csc;
            using var _ws = ws;
            using var _crawler = crawler;
            GC.SuppressFinalize(this);
        }
    }
}
