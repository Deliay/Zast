using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Zast.AyeRecorder.Config;
using Zast.AyeRecorder.Script;
using Zast.AyeRecorder.Script.Config;
using Zast.BuildingBlocks.Scripts;
using Zast.BuildingBlocks.Util;

System.Console.WriteLine("hi");

IServiceCollection builder = new ServiceCollection();
builder.AddSingleton<ScriptManager>();
builder.AddSingleton<RecordConfigRepository>();
builder.AddSingleton<ConfigScript>();
builder.AddAllSingleton<BitrateConfig, IMenuItem>();
builder.AddAllSingleton<PreferRecordModeConfig, IMenuItem>();
builder.AddAllSingleton<ExitConfig, IMenuItem>();

var services = builder.BuildServiceProvider();

var arg = args.FirstOrDefault() ?? "";

var csc = new CancellationTokenSource();
var scripts = services.GetRequiredService<ScriptManager>();
var config = services.GetRequiredService<ConfigScript>();

await scripts.RunAsync(config, csc.Token);
