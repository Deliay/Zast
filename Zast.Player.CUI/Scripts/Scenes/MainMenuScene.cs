using Mikibot.Crawler.Http.Bilibili;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zast.Player.CUI.Scripts.Scenes
{
    public class MainMenuScene : IScript
    {

        public MainMenuScene(IEnumerable<IMenuItem> menuItems)
        {
            MenuItems = menuItems;
        }

        public string Name => "Home";

        public IEnumerable<IMenuItem> MenuItems { get; }

        public ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
        {
            Console.Clear();
            AnsiConsole.Write(new FigletText("Zest CUI").Centered().Color(Color.Aqua));
            var sel = AnsiConsole.Prompt(new SelectionPrompt<IMenuItem>()
                .Title("做点什么...")
                .AddChoices(MenuItems)
                .UseConverter(arg => arg.Name));
            return ValueTask.FromResult(sel as IScript);
        }
    }
}
