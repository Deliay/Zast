using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmberCore.Extensions
{
    public static class SerilogBuilderExtension
    {
        public static ILoggingBuilder AddSerilog(this ILoggingBuilder builder, Func<LoggerConfiguration, LoggerConfiguration> rootBuilder)
        {
            var conf = new LoggerConfiguration();
            rootBuilder(conf);
            Log.Logger = conf.CreateLogger();

            builder.AddSerilog(logger: Log.Logger, dispose: true);
            return builder;
        }
    }
}
