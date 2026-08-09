using System.Text.RegularExpressions;

namespace EQLogParser.Processors
{
    public class PlayerLevelLogProcessor : ILogProcessor
    {
        private readonly PlayerLevelState _playerLevelState;
        private static readonly Regex LevelRegex = new Regex(@"Welcome to level (?<level>\d+)!", RegexOptions.IgnoreCase);

        public EverquestLogReader.LogType LogType => EverquestLogReader.LogType.PlayerLevel;

        public PlayerLevelLogProcessor(PlayerLevelState playerLevelState)
        {
            _playerLevelState = playerLevelState;
        }

        public bool IsMatch(LogLine line)
        {
            return LevelRegex.IsMatch(line.Message);
        }

        public void Process(LogLine line)
        {
            Match match = LevelRegex.Match(line.Message);
            if (int.TryParse(match.Groups["level"].Value, out int level))
            {
                _playerLevelState.SetLevel(level);
            }
        }
    }
}
