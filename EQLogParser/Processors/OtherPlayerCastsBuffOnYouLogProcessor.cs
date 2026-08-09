using System;
using System.Collections.Generic;
using System.Linq;

namespace EQLogParser.Processors
{
    public class OtherPlayerCastsBuffOnYouLogProcessor : ILogProcessor
    {
        public EverquestLogReader.LogType LogType => EverquestLogReader.LogType.OtherPlayerCastsBuffOnYou;

        private readonly SpellCache _spellCache;
        private readonly IBuffManager _buffManager;
        private readonly SpellDurationCalculator _spellDurationCalculator;
        private IEnumerable<Spell> _spells;

        public OtherPlayerCastsBuffOnYouLogProcessor(SpellCache spellCache, IBuffManager buffManager, SpellDurationCalculator spellDurationCalculator)
        {
            _spellCache = spellCache;
            _buffManager = buffManager;
            _spellDurationCalculator = spellDurationCalculator;
        }

        public bool IsMatch(LogLine line)
        {
            IEnumerable<Spell> spells = _spellCache.GetSpellsByMessage(line.Message)
                .Where(x => x.Duration != null && x.Duration.Value.TotalMilliseconds > 0)
                .ToList();

            if (spells.Any() && spells.Count(x => x.MessageYou == line.Message) == 1)
            {
                _spells = spells;
                return true;
            }

            return false;
        }

        public void Process(LogLine line)
        {
            Spell spell = _spells.SingleOrDefault(x => x.MessageYou == line.Message);

            TimeSpan? duration = spell == null ? null : _spellDurationCalculator.GetDuration(spell);
            if (spell != null && duration != null)
            {
                _buffManager.AddBuff("__YOU__", spell.ToBuff(line.When, duration.Value));
            }

            _spells = null;
        }
    }
}
