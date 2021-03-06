using CryptoExchange.Net.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security;
using System.Text;

namespace PyroNexusTradingAlertBot.Helpers
{
    public static class CryptoExchangeHelper
    {
        public class TextWriterILogger : TextWriter
        {
            public readonly LogLevel LogLevel;
            private readonly ILogger _logger;
            private readonly StringBuilder _builder = new StringBuilder();
            private int _buffer = 0;
            private static int MaxBuffer = 262144;
            private int _newlineIndex = 0;

            public TextWriterILogger(ILogger logger)
            {
                _logger = logger;
                LogLevel = GetLogLevel();
            }

            public override void Write(string value)
            {
                _logger.Log(LogLevel, value);
            }

            public override void Write(char value)
            {
                _builder.Append(value);
                _buffer += 1;
                if (_buffer + 1 > MaxBuffer)
                    Flush();
                else if (value == NewLine[_newlineIndex])
                {
                    _newlineIndex += 1;

                    if (_newlineIndex == NewLine.Length)
                        Flush();
                }
                else
                    _newlineIndex = 0;
            }

            public override void Flush()
            {
                Write(_builder.ToString());
                _builder.Clear();
                _buffer = 0;
                _newlineIndex = 0;
            }

            public override Encoding Encoding
            {
                get { return Encoding.Default; }
            }

            private LogLevel GetLogLevel()
            {
                foreach (LogLevel level in (LogLevel[])Enum.GetValues(typeof(LogLevel)))
                {
                    if (_logger.IsEnabled(level))
                    {
                        return level;
                    }
                }
                return LogLevel.Information;
            }
        }

        public static LogVerbosity GetLogVerbosity(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    return LogVerbosity.Debug;
                case LogLevel.Warning:
                    return LogVerbosity.Warning;
                case LogLevel.Error:
                case LogLevel.Critical:
                    return LogVerbosity.Error;
                case LogLevel.None:
                    return LogVerbosity.None;
                case LogLevel.Information:
                default:
                    return LogVerbosity.Info;
            }
        }
    }
}
