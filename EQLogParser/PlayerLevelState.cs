namespace EQLogParser
{
    public class PlayerLevelState
    {
        public int? Level { get; private set; }

        public PlayerLevelState(ParserOptions options)
        {
            Level = options.PlayerLevel;
        }

        public void SetLevel(int level)
        {
            if (level > 0)
            {
                Level = level;
            }
        }
    }
}
