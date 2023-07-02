using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Whisper.net;

namespace Zast.Player.CUI
{
    public class Boostrapper
    {
        private static readonly HttpClient client = new();
        private readonly BiliLiveCrawler crawler;
        private readonly int roomId;

        public Boostrapper(BiliLiveCrawler biliLiveCrawler, int roomId)
        {
            this.crawler = biliLiveCrawler;
            this.roomId = roomId;
        }

        private async Task PrintLiveStatus(long roomId, CancellationToken cancellationToken)
        {

            var basic = await crawler.GetLiveRoomInfo(roomId, cancellationToken);

            using var stream = await client.GetStreamAsync(basic.UserCover, cancellationToken);

            var image = new CanvasImage(stream)
            {
                MaxWidth = 32
            };

            var status = basic.LiveStatus == 0 ? "[grey]休息中[/]" : $"[lime]直播中[/] {basic.Title}";

            AnsiConsole.Write(new Panel(image)
            {
                Header = new(status)
                {
                    Justification = Justify.Center
                },
            });
        }

        private async Task RunDanmakuHandler(CancellationToken cancellationToken = default)
        {
            Console.Clear();

            using var eventStreaming = new RoomEventStreaming(crawler, roomId);

            var online = 0;
            var lastEnter = "";
            var damakuSpeed = "N/A";
            int damakuCount = 0;
            DateTime start = DateTime.Now;

            var info = await crawler.GetRealRoomInfo(roomId, cancellationToken);

            await PrintLiveStatus(info.RoomId, cancellationToken);

            AnsiConsole.MarkupLine("[yellow]Esc退出当前直播间[/]");
            await AnsiConsole.Status().StartAsync("连接中...", async (ctx) =>
            {
                await foreach (var @event in eventStreaming.RunAsync(info.RoomId, cancellationToken))
                {
                    if (@event is CommandBase<OnlineRankCount> ranking)
                    {
                        online = ranking.Data.Count;
                    }
                    else if (@event is CommandBase<InteractWord> interact)
                    {
                        lastEnter = interact.Data.UserName.EscapeMarkup();
                    }
                    else
                    {
                        await @event.Write();
                    }
                    if (@event is CommandBase<DanmuMsg>)
                    {
                        var now = DateTime.Now;
                        var duration = (now - start).TotalMinutes;
                        damakuSpeed = $"{((++damakuCount) / duration):F}";

                        if (now - start > TimeSpan.FromMinutes(1))
                        {
                            start = now;
                            damakuCount = 0;
                        }
                    }
                    ctx.Status($"弹幕 [purple]{damakuSpeed}[/]条/分 ({damakuCount}) | 在线 [green]{online}[/] 人 | [grey]{lastEnter} 进入直播间[/]");
                }
            });
        }

        private async Task RunWhisper(StatusContext ctx, CancellationToken cancellationToken = default)
        {
            using var rmtpStreaming = new RoomStreamWhisper(crawler, roomId);
            ctx.Refresh();
            await foreach (var (curr, last) in rmtpStreaming.RunAsync(cancellationToken))
            {
                if (last is not null)
                    await Task.Delay((curr.End - last.Start) * 0.25, cancellationToken);
                AnsiConsole.MarkupLine($"[grey]直播[/] [silver]{curr.Text.EscapeMarkup()}[/]");
            }
        }

        private static async Task Quit(CancellationTokenSource csc, CancellationToken cancellationToken)
        {
            while (Console.ReadKey().Key != ConsoleKey.Escape)
            {
                if (cancellationToken.IsCancellationRequested) { break; }
                await Task.Delay(1, cancellationToken);
            }
            csc.Cancel();
        }

        public async ValueTask RunAsync(CancellationToken cancellationToken = default)
        {
            using var csc = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = csc.Token;

            try
            {
                await Task.WhenAll(RunDanmakuHandler(token), Quit(csc, token));
            }
            catch
            {

            }
        }
    }
}
