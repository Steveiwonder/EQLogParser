using System;

namespace EQLogParser
{
    public class ConsoleLogger : ILogger
    {
        public void WriteLine(string message, ConsoleColor color, EverquestLogReader.LogType? logType = null)
        {

            Console.ForegroundColor = color;
            string format = (logType != null ? $"[{logType.ToString()}]" : "") + $"{message}";
            Console.WriteLine(format);
        }
    }
}