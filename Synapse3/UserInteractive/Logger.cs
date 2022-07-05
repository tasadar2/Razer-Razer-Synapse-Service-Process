using System;
using NLog;

namespace Synapse3.UserInteractive
{
    public class Logger : ILogger
    {
        private NLog.Logger _logger;

        private static readonly Lazy<Logger> _lazy = new Lazy<Logger>(() => new Logger());

        public static Logger Instance => _lazy.Value;

        private Logger()
        {
            _logger = LogManager.GetCurrentClassLogger();
        }

        public void Debug(string message)
        {
            if (_logger.IsDebugEnabled)
            {
                Write(LogLevel.Debug, message);
            }
        }

        public void Error(string message)
        {
            if (_logger.IsErrorEnabled)
            {
                Write(LogLevel.Error, message);
            }
        }

        public void Fatal(string message)
        {
            if (_logger.IsFatalEnabled)
            {
                Write(LogLevel.Fatal, message);
            }
        }

        public void Info(string message)
        {
            if (_logger.IsInfoEnabled)
            {
                Write(LogLevel.Info, message);
            }
        }

        public void Trace(string message)
        {
            if (_logger.IsTraceEnabled)
            {
                Write(LogLevel.Debug, message);
            }
        }

        public void Warn(string message)
        {
            if (_logger.IsWarnEnabled)
            {
                Write(LogLevel.Warn, message);
            }
        }

        private void Write(LogLevel level, string message)
        {
            _logger.Log(typeof(Logger), new LogEventInfo(level, _logger.Name, message));
        }
    }
}
