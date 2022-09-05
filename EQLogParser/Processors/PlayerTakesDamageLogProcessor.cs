using System.Text.RegularExpressions;

namespace EQLogParser.Processors
{
    public class PlayerTakesDamageLogProcessor : ILogProcessor
    {
        private readonly ILogger _logger;
        public EverquestLogReader.LogType LogType => EverquestLogReader.LogType.PlayerTakesDamage;
        private const string HitPattern = @"^\[.*?\].*\bYOU for\b\s\d+\s\bpoints of damage\b\.";

        private Regex _hitRegex = new Regex(HitPattern, RegexOptions.IgnoreCase);

        public PlayerTakesDamageLogProcessor(ILogger logger)
        {
            _logger = logger;
        }
        public bool IsMatch(LogLine line)
        {
            return _hitRegex.IsMatch(line.Message);
        }

        public void Process(LogLine line)
        {
            // _logger.WriteLine(line, ConsoleColor.Red, LogType);
        }
    }
}