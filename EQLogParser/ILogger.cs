using System;

namespace EQLogParser
{
    public interface ILogger
    {
        void WriteLine(string message, ConsoleColor color, EverquestLogReader.LogType? logType = null);
    }
}