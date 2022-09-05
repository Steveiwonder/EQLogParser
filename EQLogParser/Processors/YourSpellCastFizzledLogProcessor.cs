using System;

namespace EQLogParser.Processors
{
    public class YourSpellCastFizzledLogProcessor : ILogProcessor
    {
        private readonly CurrentSpellCast _currentSpellCast;
        public EverquestLogReader.LogType LogType => EverquestLogReader.LogType.YourSpellCastFizzled;

        public YourSpellCastFizzledLogProcessor(CurrentSpellCast currentSpellCast)
        {
            _currentSpellCast = currentSpellCast;
        }
        public bool IsMatch(LogLine line)
        {
            return line.Message.Contains("Your spell fizzles!", StringComparison.OrdinalIgnoreCase);
        }

        public void Process(LogLine line)
        {
            _currentSpellCast.CastFizzled();
        }
    }
}