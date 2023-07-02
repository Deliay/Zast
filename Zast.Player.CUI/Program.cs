using Microsoft.Extensions.DependencyInjection;
using Mikibot.Crawler.Http.Bilibili;
using Spectre.Console;
using Zast.Player.CUI;
using Zast.Player.CUI.Scripts;
using Zast.Player.CUI.Scripts.Scenes;
using Zast.Player.CUI.Util;

IServiceCollection builder = new ServiceCollection();

builder.AddSingleton<BiliLiveCrawler>();

builder.AddSingleton<ScriptManager>();

builder.AddSingleton<MainMenuScene>();
builder.AddAllSingleton<HistoryRoomScene, IMenuItem>();
builder.AddAllSingleton<EnterRoomScene, IMenuItem>();
builder.AddAllSingleton<ExitScene, IMenuItem>();


var services = builder.BuildServiceProvider();
var rootCancellationSource = new CancellationTokenSource();

Console.CancelKeyPress += Console_CancelKeyPress;

void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    rootCancellationSource.Cancel();
    Environment.Exit(0);
}

var manager = services.GetRequiredService<ScriptManager>();
var main = services.GetRequiredService<MainMenuScene>();

await manager.RunAsync(main, rootCancellationSource.Token);
