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
    public class MainMenuScene : IScript
    {
        private readonly BiliLiveCrawler crawler;
        private readonly BiliBasicInfoCrawler basicInfoCrawler;
        private readonly CookieStore cookieStore;

        public MainMenuScene(
            IEnumerable<IMenuItem> menuItems,
            BiliLiveCrawler crawler,
            BiliBasicInfoCrawler basicInfoCrawler,
            CookieStore cookieStore)
        {
            MenuItems = menuItems.Where(i => i.Category == typeof(MainMenuScene));
            this.crawler = crawler;
            this.basicInfoCrawler = basicInfoCrawler;
            this.cookieStore = cookieStore;
        }

        public string Name => "Home";

        public IEnumerable<IMenuItem> MenuItems { get; }

        public async ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
        {

            Console.Clear();
            AnsiConsole.Write(new FigletText("Zest CUI").Centered().Color(Color.Aqua));

            var cookie = await cookieStore.Load(cancellationToken);
            if (!string.IsNullOrWhiteSpace(cookie.Cookie))
            {
                AnsiConsole.MarkupLine("[yellow]提醒[/] 你已经在cookie.json设置了登录状态，请妥善保管您的cookie.json，请勿将文件转发给他人");
                crawler.SetCookie(cookie.Cookie);
                basicInfoCrawler.SetCookie(cookie.Cookie);
                context.Set(cookie);
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]提醒[/] 游客状态下10分钟后B站会给用户名打码，请登录获得最佳体验");
            }

            return AnsiConsole.Prompt(new SelectionPrompt<IMenuItem>()
                .Title($"[teal]{cookie.Name ?? "游客"}[/]，做点什么...")
                .AddChoices(MenuItems)
                .UseConverter(arg => arg.Name));
        }
    }
}
