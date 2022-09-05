using System.Text.RegularExpressions;

namespace EQLogParser.Processors
{
    public class NpcMissedYouLogProcessor : ILogProcessor
    {
        private readonly ILogger _logger;
        private const string MissPattern = @"^\[.*?\].*\bYou, but misses";
        private Regex _missRegex = new Regex(MissPattern, RegexOptions.IgnoreCase);
        public EverquestLogReader.LogType LogType => EverquestLogReader.LogType.NpcMissedYou;

        public NpcMissedYouLogProcessor(ILogger logger)
        {
            _logger = logger;
        }
        public bool IsMatch(LogLine line)
        {
            return _missRegex.IsMatch(line.Message);
        }

        public void Process(LogLine line)
        {
            //_logger.WriteLine(line, ConsoleColor.Gray, LogType);
        }
    }
}