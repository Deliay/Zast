using Mikibot.Crawler.Http.Bilibili;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zast.BuildingBlocks.Scripts;
using Zast.Player.CUI.Bilibili;

namespace Zast.Player.CUI.Scripts.Scenes
{
    public class HistoryRoomScene : IMenuItem
    {
        private readonly BiliLiveCrawler crawler;

        public HistoryRoomScene(BiliLiveCrawler crawler)
        {
            this.crawler = crawler;
        }

        public string Name => "历史记录...";
        public Type Category => typeof(MainMenuScene);

        public async ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
        {
            var roomIds = await RoomHistory.GetHistory(cancellationToken);

            if (roomIds.Count == 0) 
            {
                AnsiConsole.MarkupLine($"[yellow]暂无历史[/]，3秒后返回");
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                return prev;
            }

            var roomId = AnsiConsole.Prompt(new SelectionPrompt<int>()
                .AddChoices(roomIds)
                .Title("选择要进入的房间："));


            var boostrapper = new DanmakuBootstrapper(crawler, roomId);
            await boostrapper.RunAsync(context, cancellationToken);

            return prev;
        }
    }
}
