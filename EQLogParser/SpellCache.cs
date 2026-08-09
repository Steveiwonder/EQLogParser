using System;
using System.Collections.Generic;
using System.Linq;

namespace EQLogParser
{
    public class SpellCache
    {
        private readonly IDictionary<string, List<Spell>> _spells = new Dictionary<string, List<Spell>>();
        private readonly ILookup<string, Spell> _spellsByEffectMessage;
        private readonly Spell[] _spellsWithTargetMessages;

        public SpellCache(IEnumerable<Spell> spells)
        {
            Spell[] spellList = spells.ToArray();
            foreach (Spell spell in spellList)
            {
                if (_spells.ContainsKey(spell.Name))
                {
                    _spells[spell.Name].Add(spell);
                }
                else
                {
                    _spells.Add(spell.Name, new List<Spell>() { spell });
                }
            }

            _spellsByEffectMessage = spellList.Select((s, i) => new
            {
                messageEnded = new { spell = s, message = s.MessageEnded },
                messageYou = new { spell = s, message = s.MessageYou },
                messageTarget = new { spell = s, message = s.MessageTarget }
            }).SelectMany(x => new[] { x.messageEnded, x.messageTarget, x.messageYou })
                .Where(x => !string.IsNullOrWhiteSpace(x.message))
                .ToLookup(x => x.message, x => x.spell);

            _spellsWithTargetMessages = spellList
                .Where(x => !string.IsNullOrWhiteSpace(x.MessageTarget))
                .ToArray();
        }

        public Spell GetSpellByName(string spellName)
        {
            return _spells[spellName].First();
        }

        public IEnumerable<Spell> GetSpellsByMessage(string message)
        {
            return _spellsByEffectMessage[message];
        }

        public IEnumerable<SpellMessageMatch> GetSpellMessageMatches(string message)
        {
            foreach (Spell spell in _spellsByEffectMessage[message])
            {
                if (string.Equals(spell.MessageYou, message, StringComparison.OrdinalIgnoreCase))
                {
                    yield return new SpellMessageMatch()
                    {
                        Spell = spell,
                        MatchType = SpellMessageMatchType.You,
                        TargetName = "__YOU__"
                    };
                }

                if (string.Equals(spell.MessageEnded, message, StringComparison.OrdinalIgnoreCase))
                {
                    yield return new SpellMessageMatch()
                    {
                        Spell = spell,
                        MatchType = SpellMessageMatchType.Ended,
                        TargetName = "__YOU__"
                    };
                }
            }

            foreach (Spell spell in _spellsWithTargetMessages)
            {
                int targetMessageIndex = message.IndexOf(spell.MessageTarget, StringComparison.OrdinalIgnoreCase);
                if (targetMessageIndex <= 0)
                {
                    continue;
                }

                string targetName = message.Substring(0, targetMessageIndex).Trim();
                if (targetName.EndsWith("'s", StringComparison.OrdinalIgnoreCase))
                {
                    targetName = targetName.Substring(0, targetName.Length - 2).Trim();
                }

                if (string.IsNullOrWhiteSpace(targetName))
                {
                    continue;
                }

                yield return new SpellMessageMatch()
                {
                    Spell = spell,
                    MatchType = SpellMessageMatchType.Target,
                    TargetName = targetName
                };
            }
        }
    }
}
