namespace EQLogParser.Processors
{
    public class SpellCastDidNotTakeHoldLogProcessor : ILogProcessor
    {
        private readonly CurrentSpellCast _currentSpellCast;
        public EverquestLogReader.LogType LogType => EverquestLogReader.LogType.SpellCastDidNotTakeHold;

        public SpellCastDidNotTakeHoldLogProcessor(CurrentSpellCast currentSpellCast)
        {
            _currentSpellCast = currentSpellCast;
        }
        public bool IsMatch(LogLine line)
        {
            return line.Message.Contains("Your spell did not take hold.");
        }

        public void Process(LogLine line)
        {
            _currentSpellCast.CastDidNotTakeHold();
        }
    }
}