using System;
using System.Collections.Generic;
using System.Linq;

namespace EQLogParser.Processors
{
    public class YouLoseBuffLogProcessor : ILogProcessor
    {
        private readonly SpellCache _spellCache;
        private readonly IBuffManager _buffManager;
        private readonly SpellDurationCalculator _spellDurationCalculator;
        private IEnumerable<Spell> _spell;

        public EverquestLogReader.LogType LogType => EverquestLogReader.LogType.YouLoseBuff;

        public YouLoseBuffLogProcessor(
            SpellCache spellCache,
            IBuffManager buffManager,
            SpellDurationCalculator spellDurationCalculator)
        {
            _spellCache = spellCache;
            _buffManager = buffManager;
            _spellDurationCalculator = spellDurationCalculator;
        }

        public bool IsMatch(LogLine line)
        {
            string message = line.Message.Substring(line.Message.IndexOf(']') + 1).Trim();
            IEnumerable<Spell> spells = _spellCache.GetSpellsByMessage(message)
                .Where(x => string.Equals(x.MessageEnded, message, StringComparison.OrdinalIgnoreCase));

            if (spells.Any())
            {
                _spell = spells.ToArray();
                return true;
            }

            return false;
        }

        public void Process(LogLine line)
        {
            Spell[] spells = _spell.GroupBy(x => x.Name).Select(x => x.First()).ToArray();
            _buffManager.ExpireBuffs("__YOU__", spells.Select(x => x.Name).ToArray());

            if (spells.Length != 1)
            {
                _spell = null;
                return;
            }

            Spell spell = spells[0];
            TimeSpan? duration = _spellDurationCalculator.GetDuration(spell);
            if (duration != null && duration.Value.TotalMilliseconds > 0)
            {
                _buffManager.ExpireOrAddBuff("__YOU__", spell.ToBuff(line.When, duration.Value));
            }

            _spell = null;
        }
    }
}
