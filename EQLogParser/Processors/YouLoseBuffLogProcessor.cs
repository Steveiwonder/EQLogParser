using System.Collections.Generic;
using System.Linq;

namespace EQLogParser.Processors
{
    public class YouLoseBuffLogProcessor : ILogProcessor
    {
        private readonly SpellCache _spellCache;
        private readonly IBuffManager _buffManager;
        public EverquestLogReader.LogType LogType => EverquestLogReader.LogType.YouLoseBuff;
        private IEnumerable<Spell> _spell;
        public YouLoseBuffLogProcessor(SpellCache spellCache, IBuffManager buffManager)
        {
            _spellCache = spellCache;
            _buffManager = buffManager;
        }
        public bool IsMatch(LogLine line)
        {
            string message = line.Message.Substring(line.Message.IndexOf(']') + 1).Trim();
            IEnumerable<Spell> spells = _spellCache.GetSpellsByMessage(message);

            if (spells.Any(x => x.MessageEnded == message))
            {
                _spell = spells;
                return true;
            }

            return false;
        }

        public void Process(LogLine line)
        {
            _buffManager.RemoveBuffs("__YOU__", _spell.Select(x => x.Name).ToArray());
            _spell = null;
        }
    }
}