using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Channels;

namespace Zast.Core
{
    public class LoggerSink : ILogEventSink, IDisposable
    {
        private readonly Channel<string> channel = Channel.CreateUnbounded<string>();
        private readonly MessageTemplateTextFormatter formatter = new("[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
        private static readonly AsyncLocal<LoggerSink> LocalInstance = new();

        public LoggerSink()
        {
        }

        public IAsyncEnumerable<string> allEvents()
        {
            return channel.Reader.ReadAllAsync();
        }

        public static LoggerSink Instance
        {
            get
            {
                LocalInstance.Value ??= new LoggerSink();

                return LocalInstance.Value;
            }
        }

        public void Dispose()
        {
        }

        public void Emit(LogEvent logEvent)
        {
            var sw = new StringWriter();
            formatter.Format(logEvent, sw);
            channel.Writer.TryWrite(sw.ToString());
        }
    }
}
