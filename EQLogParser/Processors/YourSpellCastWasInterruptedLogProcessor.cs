using System;

namespace EQLogParser.Processors
{
    public class YourSpellCastWasInterruptedLogProcessor : ILogProcessor
    {
        private readonly CurrentSpellCast _currentSpellCast;
        public EverquestLogReader.LogType LogType => EverquestLogReader.LogType.YourSpellCastWasInterrupted;

        public YourSpellCastWasInterruptedLogProcessor(CurrentSpellCast currentSpellCast)
        {
            _currentSpellCast = currentSpellCast;
        }
        public bool IsMatch(LogLine line)
        {
            return line.Message.Contains("Your spell is interrupted.", StringComparison.OrdinalIgnoreCase);
        }

        public void Process(LogLine line)
        {
            _currentSpellCast.CastInterrupted();
        }
    }
}