using Mikibot.Crawler.Http.Bilibili;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Zast.BuildingBlocks.Scripts;
using Zast.Player.CUI.Bilibili;

namespace Zast.Player.CUI.Scripts.Scenes
{
    public class MainMenuScene : IScript
    {
        private readonly IScript initializeScene;

        public MainMenuScene(IEnumerable<IMenuItem> menuItems, InitializeScene initializeScene)
        {
            MenuItems = menuItems.Where(i => i.Category == typeof(MainMenuScene));
            this.initializeScene = initializeScene;
        }

        public string Name => "Home";

        public IEnumerable<IMenuItem> MenuItems { get; }

        public ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
        {

            Console.Clear();
            AnsiConsole.Write(new FigletText("Zest CUI").Centered().Color(Color.Aqua));

            if (!context.TryGet<ZastCuiSetting>(out var setting))
            {
                return ValueTask.FromResult(initializeScene);
            }

            context.TryGet<CookieInfo>(out var cookieInfo);

            if (!string.IsNullOrWhiteSpace(cookieInfo.Cookie))
            {
                AnsiConsole.MarkupLine("[yellow]提醒[/] 你已经在cookie.json设置了登录状态，请妥善保管您的cookie.json，请勿将文件转发给他人");
            }

            var next = (IScript)AnsiConsole.Prompt(new SelectionPrompt<IMenuItem>()
                .Title($"[teal]{cookieInfo.Name ?? "游客"}[/]，做点什么...")
                .AddChoices(MenuItems)
                .UseConverter(arg => arg.Name));

            return ValueTask.FromResult(next);
        }
    }
}
