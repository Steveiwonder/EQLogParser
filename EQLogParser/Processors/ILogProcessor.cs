namespace EQLogParser.Processors
{
    public interface ILogProcessor
    {
        EverquestLogReader.LogType LogType { get; }

        bool IsMatch(LogLine line);
        void Process(LogLine line);
    }

    public interface IStartupLogProcessor : ILogProcessor
    {
        bool IsStartupMatch(LogLine line);
    }
}
