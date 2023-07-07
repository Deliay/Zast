﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mikibot.Crawler.Http.Bilibili;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Spectre.Console.Cli;
using Zast.AyeRecorder.Config;
using Zast.AyeRecorder.Recording;
using Zast.AyeRecorder.Script;
using Zast.AyeRecorder.Script.Config;
using Zast.AyeRecorder.Scripts;
using Zast.BuildingBlocks.Scripts;
using Zast.BuildingBlocks.Util;

var arg = args.FirstOrDefault() ?? "";

IServiceCollection builder = new ServiceCollection();
builder.AddLogging((logger) =>
{
    var format = "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

    var conf = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .WriteTo.File(
            path: "logs\\logs_.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: format);

    if (arg != "config")
    {
        conf = conf.WriteTo.Console(
            theme: SystemConsoleTheme.Colored,
            outputTemplate: format);
    }

    Log.Logger = conf.CreateLogger();
    logger.AddSerilog(Log.Logger, true).AddDebug();
});
builder.AddSingleton<ScriptManager>();
builder.AddSingleton<RecordConfigRepository>();
builder.AddSingleton<ConfigScript>();
builder.AddAllSingleton<BitrateConfig, IMenuItem>();
builder.AddAllSingleton<PreferModeConfig, IMenuItem>();
builder.AddAllSingleton<ExitConfig, IMenuItem>();

builder.AddSingleton<RecordingScript>();
builder.AddTransient<RecordingMan>();
builder.AddSingleton<BiliLiveCrawler>();
builder.AddSingleton<BiliLiveStreamCrawler>();

var services = builder.BuildServiceProvider();

var csc = new CancellationTokenSource();
var scripts = services.GetRequiredService<ScriptManager>();

IScript script = arg switch
{
    "config" => services.GetRequiredService<ConfigScript>(),
    _ => services.GetRequiredService<RecordingScript>(),
};

await scripts.RunAsync(script, csc.Token);
