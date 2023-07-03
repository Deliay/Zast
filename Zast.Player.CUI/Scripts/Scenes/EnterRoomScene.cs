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
    public class EnterRoomScene : IScript, IMenuItem
    {
        private readonly BiliLiveCrawler crawler;
        public EnterRoomScene(BiliLiveCrawler crawler)
        {
            this.crawler = crawler;

        }

        public string Name => "输入房间号...";

        public Type Category => typeof(MainMenuScene);

        public async ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
        {
            var roomId = AnsiConsole.Prompt(new TextPrompt<int>("[gray]请输入房间号：[/]")
                .PromptStyle("green")
                .Validate(_ => ValidationResult.Success()));

            await RoomHistory.Add(roomId, cancellationToken);

            var boostrapper = new DanmakuBoostrapper(crawler, roomId);
            await boostrapper.RunAsync(context, cancellationToken);

            return prev;
        }
    }
}
