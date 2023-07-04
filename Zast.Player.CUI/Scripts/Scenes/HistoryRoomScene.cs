using Mikibot.Crawler.Http.Bilibili;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var roomId = AnsiConsole.Prompt(new SelectionPrompt<int>()
                .AddChoices(await RoomHistory.GetHistory(cancellationToken))
                .Title("选择要进入的房间："));


            var boostrapper = new DanmakuBootstrapper(crawler, roomId);
            await boostrapper.RunAsync(context, cancellationToken);

            return prev;
        }
    }
}
