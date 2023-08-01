using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.Http.Bilibili.Model.LiveServer;
using Mikibot.Crawler.WebsocketCrawler.Client;
using Mikibot.Crawler.WebsocketCrawler.Data;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands;
using Spectre.Console;
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
        private readonly BiliLiveCrawler crawler;
        private CancellationTokenSource? csc;

        public RoomEventStreaming(BiliLiveCrawler crawler)
        {
            this.crawler = crawler;
        }

        public void Dispose()
        {
            using var _csc = csc;
            GC.SuppressFinalize(this);
        }

        private async IAsyncEnumerable<ICommandBase> RunAsync(LiveServerInfo spectatorHost, long roomId, long uid, string token, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var wsClient = new WebsocketClient();
            await wsClient.ConnectAsync(spectatorHost.Host, spectatorHost.WssPort, roomId, uid, token, "wss", cancellationToken);

            await foreach (var @event in wsClient.Events(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested) yield break;
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

        public async IAsyncEnumerable<ICommandBase> RunAsync(long roomId, long uid, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var _csc = csc = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var spectatorEndpoint = await crawler.GetLiveToken(roomId, _csc.Token);

            foreach (var spectatorHost in spectatorEndpoint.Hosts)
            {
                if (_csc.IsCancellationRequested) break;
                AnsiConsole.MarkupLine($"[grey]系统[/] 连接弹幕服务器 {spectatorHost.Host}");
                var enumerator = RunAsync(spectatorHost, roomId, uid, spectatorEndpoint.Token, cancellationToken)
                    .GetAsyncEnumerator(cancellationToken);

                bool hasNext;
                do
                {
                    try
                    {
                        hasNext = await enumerator.MoveNextAsync();
                    }
                    catch
                    {
                        AnsiConsole.MarkupLine($"[grey]系统[/] 连接失败 {spectatorHost.Host}");
                        break;
                    }
                    yield return enumerator.Current;
                } while (hasNext);
            }
        }
    }
}
