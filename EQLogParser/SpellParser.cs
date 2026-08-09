using System;
using System.Collections.Generic;
using System.IO;

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
            List<Spell> spells = new List<Spell>();
            IDictionary<int, SpellMessages> spellMessages = GetSpellMessages();

            using (FileStream fs = File.Open(_spellFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(fs))
                {
                    while (!reader.EndOfStream)
                    {
                        string str = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(str))
                        {
                            continue;
                        }

                        Spell spell = ParseLine(str, spellMessages);
                        spells.Add(spell);
                    }
                }
            }

            return spells;
        }

        private const int NAME_INDEX = 1;
        private const int CAST_TIME_INDEX = 13;
        private const int MANA_COST_INDEX = 19;
        private const int DURATION_INDEX = 17;
        private const int MESSAGE_YOU_INDEX = 6;
        private const int MESSAGE_TARGET_INDEX = 7;
        private const int MESSAGE_ENDED_INDEX = 8;
        private const int TARGET_TYPE_INDEX = 86;

        private const int LEGENDS_CAST_TIME_INDEX = 8;
        private const int LEGENDS_MANA_COST_INDEX = 14;
        private const int LEGENDS_DURATION_INDEX = 12;

        private Spell ParseLine(string line, IDictionary<int, SpellMessages> spellMessages)
        {
            string[] parts = line.Split('^');
            int spellId = int.Parse(parts[0]);
            bool usesLegendsFormat = spellMessages.Count > 0;
            bool usesExternalMessages = spellMessages.TryGetValue(spellId, out SpellMessages messages);

            string castTime = parts[usesLegendsFormat ? LEGENDS_CAST_TIME_INDEX : CAST_TIME_INDEX];
            string manaCost = parts[usesLegendsFormat ? LEGENDS_MANA_COST_INDEX : MANA_COST_INDEX];
            string duration = parts[usesLegendsFormat ? LEGENDS_DURATION_INDEX : DURATION_INDEX];
            TargetTypes targetType = Enum.Parse<TargetTypes>(parts[TARGET_TYPE_INDEX]);

            return new Spell(parts)
            {
                CastTime = string.IsNullOrEmpty(castTime) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(double.Parse(castTime)),
                ManaCost = string.IsNullOrEmpty(manaCost) ? (int?)null : int.Parse(manaCost),
                Duration = string.IsNullOrEmpty(duration) ? (TimeSpan?)null : TimeSpan.FromSeconds(double.Parse(duration) * 6),
                MessageEnded = usesExternalMessages ? messages.MessageEnded : parts[MESSAGE_ENDED_INDEX],
                MessageYou = usesExternalMessages ? messages.MessageYou : parts[MESSAGE_YOU_INDEX],
                MessageTarget = usesExternalMessages ? messages.MessageTarget : parts[MESSAGE_TARGET_INDEX],
                Name = parts[NAME_INDEX],
                TargetType = targetType,
                IsDetrimental = usesLegendsFormat && IsLegendsDetrimentalTargetType(targetType)
            };
        }

        private static bool IsLegendsDetrimentalTargetType(TargetTypes targetType)
        {
            int targetTypeValue = (int)targetType;
            return targetTypeValue == 20 || targetTypeValue == 126;
        }

        private IDictionary<int, SpellMessages> GetSpellMessages()
        {
            Dictionary<int, SpellMessages> spellMessages = new Dictionary<int, SpellMessages>();
            string spellMessagesFilePath = Path.Combine(Path.GetDirectoryName(_spellFilePath), "spells_us_str.txt");

            if (!File.Exists(spellMessagesFilePath))
            {
                return spellMessages;
            }

            using (FileStream fs = File.Open(spellMessagesFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(fs))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        {
                            continue;
                        }

                        string[] parts = line.Split('^');
                        if (parts.Length < 6 || !int.TryParse(parts[0], out int spellId))
                        {
                            continue;
                        }

                        spellMessages[spellId] = new SpellMessages()
                        {
                            MessageYou = parts[3],
                            MessageTarget = parts[4],
                            MessageEnded = parts[5]
                        };
                    }
                }
            }

            return spellMessages;
        }

        private class SpellMessages
        {
            public string MessageYou { get; set; }
            public string MessageTarget { get; set; }
            public string MessageEnded { get; set; }
        }
    }
}
