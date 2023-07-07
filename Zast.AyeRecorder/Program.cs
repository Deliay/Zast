using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Zast.AyeRecorder.Config;
using Zast.AyeRecorder.Recording;
using Zast.AyeRecorder.Script;
using Zast.AyeRecorder.Script.Config;
using Zast.AyeRecorder.Scripts;
using Zast.BuildingBlocks.Scripts;
using Zast.BuildingBlocks.Util;

IServiceCollection builder = new ServiceCollection();
builder.AddSingleton<ScriptManager>();
builder.AddSingleton<RecordConfigRepository>();
builder.AddSingleton<ConfigScript>();
builder.AddAllSingleton<BitrateConfig, IMenuItem>();
builder.AddAllSingleton<PreferModeConfig, IMenuItem>();
builder.AddAllSingleton<ExitConfig, IMenuItem>();

builder.AddTransient<RecordingMan>();

var services = builder.BuildServiceProvider();

var arg = args.FirstOrDefault() ?? "";

var csc = new CancellationTokenSource();
var scripts = services.GetRequiredService<ScriptManager>();

IScript script = arg switch
{
    "config" => services.GetRequiredService<ConfigScript>(),
    _ => services.GetRequiredService<RecordingScript>(),
};

await scripts.RunAsync(script, csc.Token);
