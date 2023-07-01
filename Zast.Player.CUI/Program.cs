using Microsoft.Extensions.DependencyInjection;
using Mikibot.Crawler.Http.Bilibili;
using Spectre.Console;
using Spectre.Console.Cli;
using Zast.Player.CUI;

var builder = new ServiceCollection();

builder.AddSingleton<BiliLiveCrawler>();

builder.AddSingleton(async (ctx) =>
{
    var crawler = ctx.GetRequiredService<BiliLiveCrawler>();
    var roomId = AnsiConsole.Prompt(new TextPrompt<int>("[gray]请输入房间号(tab可补全)：[/]")
        .PromptStyle("green")
        .AddChoices(await RoomHistory.GetHistory()));

    await RoomHistory.Add(roomId);
    return new Boostrapper(crawler, roomId);
});

var services = builder.BuildServiceProvider();
var rootCancellationSource = new CancellationTokenSource();

Console.CancelKeyPress += Console_CancelKeyPress;

void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    rootCancellationSource.Cancel();
    Environment.Exit(0);
}

await (await services.GetRequiredService<Task<Boostrapper>>())
    .RunAsync(rootCancellationSource.Token);