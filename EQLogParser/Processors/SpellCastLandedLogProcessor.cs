using System;
using System.Linq;

namespace EQLogParser.Processors
{
    public class SpellCastLandedLogProcessor : ILogProcessor
    {
        private readonly CurrentSpellCast _currentSpellCast;
        private readonly IBuffManager _buffManager;
        private readonly SpellCache _spellCache;
        public EverquestLogReader.LogType LogType => EverquestLogReader.LogType.SpellCastLanded;

        public SpellCastLandedLogProcessor(CurrentSpellCast currentSpellCast, IBuffManager buffManager, SpellCache spellCache)
        {
            _currentSpellCast = currentSpellCast;
            _buffManager = buffManager;
            _spellCache = spellCache;
        }
        public bool IsMatch(LogLine line)
        {
            return _currentSpellCast.CastLandedMessages.Any(castLandedMessage => line.Message.Contains(castLandedMessage, StringComparison.OrdinalIgnoreCase));
        }

        public void Process(LogLine line)
        {
            Spell spell = _spellCache.GetSpellByName(_currentSpellCast.Name);

           
            string playerName = line.Message.Split(' ')[0];


            if (playerName.Contains("'s"))
            {
                playerName = playerName.Replace("'s", "");
            }

            if (line.Message.Contains("You", StringComparison.OrdinalIgnoreCase) || line.Message.Contains("Your", StringComparison.OrdinalIgnoreCase))
            {
                playerName = "__YOU__";
            }

            if (spell.Duration != null && spell.Duration.Value.TotalMilliseconds > 0)
            {
                
                _buffManager.AddBuff(playerName, spell.ToBuff(line.When));
            }

            _currentSpellCast.CastLanded();

        }
    }
}