using Microsoft.Extensions.DependencyInjection;
using Mikibot.Crawler.Http.Bilibili;
using Spectre.Console;
using Zast.Player.CUI;
using Zast.Player.CUI.Scripts;
using Zast.Player.CUI.Scripts.Scenes;
using Zast.Player.CUI.Util;
using Zast.Player.CUI.Bilibili;

IServiceCollection builder = new ServiceCollection();

builder.AddSingleton<CookieStore>();
builder.AddSingleton<BiliLiveCrawler>();
builder.AddSingleton<BiliBasicInfoCrawler>();

builder.AddSingleton<ScriptManager>();
builder.AddSingleton<ZastCuiSettingRepository>();

builder.AddSingleton<MainMenuScene>();
builder.AddSingleton<InitializeScene>();
builder.AddAllSingleton<HistoryRoomScene, IMenuItem>();
builder.AddAllSingleton<EnterRoomScene, IMenuItem>();
builder.AddAllSingleton<LoginScene, IMenuItem>();
builder.AddAllSingleton<SettingScene, IMenuItem>();
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
