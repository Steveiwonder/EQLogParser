using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    public class SpellCache
    {
        
        private readonly IDictionary<string, List<Spell>> _spells = new Dictionary<string, List<Spell>>();
        private ILookup<string, Spell> _spellsByEffectMessage;
        public SpellCache(IEnumerable<Spell> spells)
        {
            
            foreach (Spell spell in spells)
            {
                if (_spells.ContainsKey(spell.Name))
                {
                    _spells[spell.Name].Add(spell);
                }
                else
                {
                    _spells.Add(spell.Name, new List<Spell>(){ spell });
                }
            }

            var duplicateSpells = _spells.Where(x => x.Value.Count > 1).SelectMany(x=>x.Value);

            _spellsByEffectMessage = spells.Select((s, i) => new
            {
                messageEnded = new {spell = s, message = s.MessageEnded},
                messageYou = new {spell = s, message = s.MessageYou},
                messageTarget = new {spell = s, message = s.MessageTarget}
            }).SelectMany(x => new[] {x.messageEnded, x.messageTarget, x.messageYou})
                .ToLookup(x=>x.message, x => x.spell);
            
            


        }

        public Spell GetSpellByName(string spellName)
        {

            return _spells[spellName].First();
        }

        public IEnumerable<Spell> GetSpellsByMessage(string message)
        {
            return _spellsByEffectMessage[message];

        }
    }
}