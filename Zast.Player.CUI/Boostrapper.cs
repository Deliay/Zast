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
        private readonly Status status;
        private readonly BiliLiveCrawler crawler;
        private readonly int roomId;

        public Boostrapper(BiliLiveCrawler biliLiveCrawler, int roomId)
        {
            this.status = AnsiConsole.Status();
            this.crawler = biliLiveCrawler;
            this.roomId = roomId;
        }

        private async Task RunDanmakuHandler(StatusContext ctx, CancellationToken cancellationToken = default)
        {
            ctx.Spinner = Spinner.Known.Weather;
            using var eventStreaming = new RoomEventStreaming(crawler, roomId);


            var online = 0;
            var lastEnter = "";
            var damakuSpeed = "N/A";
            int damakuCount = 0;
            DateTime start = DateTime.Now;
            await foreach (var @event in eventStreaming.RunAsync(cancellationToken))
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

        public async ValueTask RunAsync(CancellationToken cancellationToken = default)
        {
            await status.StartAsync("...", async ctx =>
            {
                var danmakuTask = RunDanmakuHandler(ctx, cancellationToken);
                //var whisperTask = RunWhisper(ctx, cancellationToken);

                await Task.WhenAll(danmakuTask);
            });
        }
    }
}
