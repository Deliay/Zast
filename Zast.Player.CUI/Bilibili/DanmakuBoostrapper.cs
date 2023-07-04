using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.Http.Bilibili.Model;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Whisper.net;
using Zast.Player.CUI.Scripts;
using Zast.Player.CUI.Util;

namespace Zast.Player.CUI.Bilibili
{
    public class DanmakuBoostrapper
    {
        private static readonly HttpClient client = new();
        private readonly BiliLiveCrawler liveCrawler;
        private readonly long roomId;

        public DanmakuBoostrapper(BiliLiveCrawler biliLiveCrawler, long roomId)
        {
            this.liveCrawler = biliLiveCrawler;
            this.roomId = roomId;
        }

        private async Task PrintLiveStatus(long roomId, CancellationToken cancellationToken)
        {

            using var stream = await client.GetStreamAsync(roomInfo.UserCover, cancellationToken);

            var image = new CanvasImage(stream)
            {
                MaxWidth = 16
            };

            var status = roomInfo.LiveStatus == 0 ? "[grey]休息中[/]" : $"[lime]直播中[/] {roomInfo.Title}";

            AnsiConsole.Write(new Panel(image)
            {
                Header = new(status)
                {
                    Justification = Justify.Center
                },
            });
        }

        private async Task RunDanmakuHandler(ScriptContext context, CancellationToken cancellationToken = default)
        {
            Console.Clear();

            using var eventStreaming = new RoomEventStreaming(liveCrawler);

            var online = 0;
            var lastEnter = "";
            var damakuSpeed = "N/A";
            int damakuCount = 0;
            DateTime start = DateTime.Now;

            AnsiConsole.MarkupLine("[yellow]Esc退出当前直播间[/]");
            AnsiConsole.MarkupLine("[grey]正在获得房间地址...[/]");
            var info = await liveCrawler.GetRealRoomInfo(roomId, cancellationToken);

            await PrintLiveStatus(info.RoomId, cancellationToken);

            if (context.TryGet(out CookieInfo cookie))
            {
                AnsiConsole.MarkupLine($"[lime]检测倒你已经登录，将以 [teal]{cookie.Name}[/] 的身份进入直播间[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]你还没有登录，B站将在游客观看10分钟后屏蔽所有弹幕用户名称。[/]");
            }

            await AnsiConsole.Status().StartAsync("连接中...", async (ctx) =>
            {
                await foreach (var @event in eventStreaming.RunAsync(info.RoomId, cookie.Uid, cancellationToken))
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
                        damakuSpeed = $"{(++damakuCount) / duration:F}";

                        if (now - start > TimeSpan.FromMinutes(1))
                        {
                            start = now;
                            damakuCount = 0;
                        }
                    }
                    ctx.Status($"弹幕 [purple]{damakuSpeed}[/]条/分 ({damakuCount}) | 在线 [green]{online}[/] 人 | [grey]{lastEnter} 进入直播间[/]{_voiceCache}");
                }
            });
        }

        private async Task RunWhisper(StatusContext ctx, CancellationToken cancellationToken = default)
        {
            using var rmtpStreaming = new RoomStreamWhisper(liveCrawler, roomId);
            ctx.Refresh();
            await foreach (var (curr, last) in rmtpStreaming.RunAsync(cancellationToken))
            {
                if (last is not null)
                    await Task.Delay((curr.End - last.Start) * 0.25, cancellationToken);
                AnsiConsole.MarkupLine($"[grey]直播[/] [silver]{curr.Text.EscapeMarkup()}[/]");
            }
        }

        private string _voiceCache = "";

        private async Task RunLiveStream(ScriptContext ctx, CancellationToken cancellationToken = default)
        {
            if (!ctx.TryGet<ZastCuiSetting>(out var setting)) throw new InvalidCastException();

            if (!setting.EnabledAudio)
            {
                return;
            }
            if (roomInfo.LiveStatus == 0)
            {
                return;
            }

            AnsiConsole.MarkupLine($"[yellow]提示[/] 你开启了声音播放，即将播放直播流中的声音");
            using var streamProxy = new RoomStreamStreamingProxy(liveCrawler, roomId);

            Task web = streamProxy.RunAsync(cancellationToken);

            using var bass = new UrlBassPlayer();
            bass.LoadingProgressUpdated += (size) => _voiceCache = $" | {size / 1024}kb";

            bass.Play(RoomStreamStreamingProxy.WaveEndpoint, cancellationToken);

            var tcs = new TaskCompletionSource();
            cancellationToken.Register(() => tcs.SetResult());
            await tcs.Task;
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

        private LiveRoomInfo roomInfo;

        public async ValueTask RunAsync(ScriptContext context, CancellationToken cancellationToken = default)
        {
            using var csc = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = csc.Token;

            try
            {
                roomInfo = await liveCrawler.GetLiveRoomInfo(roomId, cancellationToken);
                await Task.WhenAny(
                    RunDanmakuHandler(context, token),
                    RunLiveStream(context, token),
                    Quit(csc, token));
                
                csc.Cancel();
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLine($"[red]发生错误{e.Message}[/]");
            }
        }
    }
}
