using System;
using System.Collections.Generic;
using Serilog;
using System.IO;

namespace FGTableSearch {

    public enum LogLogEnum {
        Warning,
        Information,
        Verbose,
        Debug,
        Error,
        Fatal
    }

    public class LogLog {

        public Dictionary<LogLogEnum, Action<string>> Logger { get; } = new Dictionary<LogLogEnum, Action<string>> {
            { LogLogEnum.Warning,s=> Log.Warning(s) },
            { LogLogEnum.Information,s=> Log.Information(s) },
            { LogLogEnum.Verbose,s=> Log.Verbose(s) },
            { LogLogEnum.Debug,s=> Log.Debug(s) },
            { LogLogEnum.Error,s=> Log.Error(s) },
            { LogLogEnum.Fatal,s=> Log.Fatal(s) }
        };

        private static readonly Lazy<LogLog> lazy = new Lazy<LogLog>(() => new LogLog());

        public static LogLog Instance { get { return lazy.Value; } }

        public LogLog() {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.RollingFile(Path.Combine(Path.GetTempPath(), $"FGSearch_{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.log"))
                .CreateLogger();
        }

        public void Close() {
            Log.CloseAndFlush();
        }
    }
}
