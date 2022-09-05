using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EQLogParser
{
    public class SpellParser
    {
        private readonly string _spellFilePath;

        public SpellParser(string spellFilePath)
        {
            if (string.IsNullOrEmpty(spellFilePath))
            {
                throw new ArgumentNullException(nameof(spellFilePath));
            }

            _spellFilePath = spellFilePath;
        }

        public IEnumerable<Spell> GetSpells()
        {
            List<Spell> Spells = new List<Spell>();
            using (FileStream fs = File.Open(_spellFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(fs))
                {
                    string s;
                    do
                    {
                        string str = s = reader.ReadLine();
                        Spell spell = ParseLine(str);
                        Spells.Add(spell);
                    } while (!reader.EndOfStream);
                }
            }

            return Spells;
        }

        private const int NAME_INDEX = 1;
        private const int CAST_TIME_INDEX = 13;
        private const int MANA_COST_INDEX = 19;
        private const int DURATION_INDEX = 17;
        private const int MESSAGE_YOU_INDEX = 6;
        private const int MESSAGE_TARGET_INDEX = 7;
        private const int MESSAGE_ENDED_INDEX = 8;
        private const int TAREGT_TYPE_INDEX = 86;
        //you target ended
        private Spell ParseLine(string line)
        {
            string[] parts = line.Split('^');

            string name = parts[NAME_INDEX];

            
            string castTime = parts[CAST_TIME_INDEX];
            string manaCost = parts[MANA_COST_INDEX];
            string duration = parts[DURATION_INDEX];
            return new Spell(parts)
            {
                CastTime = string.IsNullOrEmpty(castTime) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(double.Parse(castTime)),
                ManaCost = string.IsNullOrEmpty(manaCost) ? (int?)null : int.Parse(manaCost),
                Duration = string.IsNullOrEmpty(duration) ? (TimeSpan?)null : TimeSpan.FromSeconds(double.Parse(duration) * 6),
                MessageEnded = parts[MESSAGE_ENDED_INDEX],
                MessageYou = parts[MESSAGE_YOU_INDEX],
                MessageTarget = parts[MESSAGE_TARGET_INDEX],
                Name = name,
                TargetType = Enum.Parse<TargetTypes>(parts[TAREGT_TYPE_INDEX])
            };
        }


    }
}
