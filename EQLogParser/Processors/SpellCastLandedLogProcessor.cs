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
            if (!_currentSpellCast.CanMatchLandedMessage(line.When))
            {
                return false;
            }

            return _currentSpellCast.CastLandedMessages
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Any(castLandedMessage => line.Message.Contains(castLandedMessage, StringComparison.OrdinalIgnoreCase));
        }

        public void Process(LogLine line)
        {
            Spell spell = _spellCache.GetSpellByName(_currentSpellCast.Name);
            string playerName = GetTargetName(line.Message, spell);

            if (spell.Duration != null && spell.Duration.Value.TotalMilliseconds > 0)
            {
                _buffManager.AddBuff(playerName, spell.ToBuff(line.When));
            }

            _currentSpellCast.CastLanded(line.When);
        }

        private static string GetTargetName(string message, Spell spell)
        {
            if (IsSelfMessage(message, spell))
            {
                return "__YOU__";
            }

            if (!string.IsNullOrWhiteSpace(spell.MessageTarget))
            {
                int messageTargetIndex = message.IndexOf(spell.MessageTarget, StringComparison.OrdinalIgnoreCase);
                if (messageTargetIndex > 0)
                {
                    string targetName = message.Substring(0, messageTargetIndex).Trim();
                    if (targetName.EndsWith("'s", StringComparison.OrdinalIgnoreCase))
                    {
                        targetName = targetName.Substring(0, targetName.Length - 2).Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(targetName))
                    {
                        return targetName;
                    }
                }
            }

            string fallbackName = message.Split(' ')[0];
            return fallbackName.Replace("'s", string.Empty);
        }

        private static bool IsSelfMessage(string message, Spell spell)
        {
            return !string.IsNullOrWhiteSpace(spell.MessageYou)
                && string.Equals(message, spell.MessageYou, StringComparison.OrdinalIgnoreCase);
        }
    }
}
