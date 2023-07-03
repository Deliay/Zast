using Mikibot.Crawler.Http.Bilibili;
using Spectre.Console;
using Zast.Player.CUI.Bilibili;
using Zast.Player.CUI.Scripts;

namespace Zast.Player.CUI.Scripts.Scenes
{
    public class LoginScene : IMenuItem
    {
        private readonly CookieStore cookieStore;
        private readonly BiliBasicInfoCrawler crawler;

        public LoginScene(CookieStore cookieStore, BiliBasicInfoCrawler crawler)
        {
            this.cookieStore = cookieStore;
            this.crawler = crawler;
        }

        public string Name => "登录...";
        public Type Category => typeof(MainMenuScene);

        public async ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
        {
            Console.Clear();
            AnsiConsole.MarkupLine("[yellow]:warning:警告[/] Cookie将会存储在本地的cookie.json中，请妥善保管，不要将这个文件发送给其他人！");
            var cookie = AnsiConsole.Prompt(new TextPrompt<string>("请粘贴Cookie[grey](留空则清空登录信息)[/]").AllowEmpty());

            if (string.IsNullOrWhiteSpace(cookie))
            {
                await cookieStore.Save(default, cancellationToken);
                AnsiConsole.MarkupLine("已将登录信息清空，5秒后返回主菜单");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                return prev;
            }

            await AnsiConsole.Status().StartAsync("[olive]正在检测Cookie有效性...[/]", async (ctx) =>
            {
                crawler.SetCookie(cookie);
                var info = await crawler.GetNavInfo(cancellationToken);
                if (info.Mid > 0)
                {
                    await cookieStore.Save(new()
                    {
                        Cookie = cookie,
                        Uid = info.Mid,
                        Name = info.Name,
                    }, cancellationToken);
                    Console.Clear();
                    ctx.Status($"[teal]{info.Name}({info.Mid})[/]你好");
                    AnsiConsole.MarkupLine("[lime]:check_mark_button:Cookie有效！已将您的cookie存储到cookie.json中[/]");
                    AnsiConsole.MarkupLine("[yellow]:warning:警告[/][red]请勿将cookie.json转发给他人，否则会造成帐号泄漏！[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]无效的cookie，请尝试重新获取[/]");
                }
                AnsiConsole.MarkupLine("[grey]5秒后返回主菜单[/]");
                await Task.Delay(TimeSpan.FromSeconds(5));
            });

            return prev;
        }
    }
}