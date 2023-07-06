using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

System.Console.WriteLine("hi");

IServiceCollection builder = new ServiceCollection();

var services = builder.BuildServiceProvider();

var arg = args.FirstOrDefault() ?? "";


