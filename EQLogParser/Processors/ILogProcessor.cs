namespace EQLogParser.Processors
{
    public interface ILogProcessor
    {
        EverquestLogReader.LogType LogType { get; }

        bool IsMatch(LogLine line);
        void Process(LogLine line);
    }
}