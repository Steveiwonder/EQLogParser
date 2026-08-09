using System;
using System.Linq;

namespace EQLogParser.Processors
{
    public class SpellCastLandedLogProcessor : ILogProcessor
    {
        private readonly CurrentSpellCast _currentSpellCast;
        private readonly IBuffManager _buffManager;
        private readonly SpellCache _spellCache;
        private readonly SpellDurationCalculator _spellDurationCalculator;
        private SpellMessageMatch _match;

        public EverquestLogReader.LogType LogType => EverquestLogReader.LogType.SpellCastLanded;

        public SpellCastLandedLogProcessor(
            CurrentSpellCast currentSpellCast,
            IBuffManager buffManager,
            SpellCache spellCache,
            SpellDurationCalculator spellDurationCalculator)
        {
            _currentSpellCast = currentSpellCast;
            _buffManager = buffManager;
            _spellCache = spellCache;
            _spellDurationCalculator = spellDurationCalculator;
        }

        public bool IsMatch(LogLine line)
        {
            _match = GetCurrentCastMatch(line) ?? GetDirectMessageMatch(line);
            return _match != null;
        }

        public void Process(LogLine line)
        {
            TimeSpan? duration = _match?.Spell == null ? null : _spellDurationCalculator.GetDuration(_match.Spell);
            if (duration != null && duration.Value.TotalMilliseconds > 0)
            {
                _buffManager.AddBuff(_match.TargetName, _match.Spell.ToBuff(line.When, duration.Value));
            }

            if (_currentSpellCast.CanMatchLandedMessage(line.When))
            {
                _currentSpellCast.CastLanded(line.When);
            }

            _match = null;
        }

        private SpellMessageMatch GetCurrentCastMatch(LogLine line)
        {
            if (!_currentSpellCast.CanMatchLandedMessage(line.When) || string.IsNullOrWhiteSpace(_currentSpellCast.Name))
            {
                return null;
            }

            Spell spell = _spellCache.GetSpellByName(_currentSpellCast.Name);
            if (!CurrentCastMessagesMatch(line.Message, spell))
            {
                return null;
            }

            return new SpellMessageMatch()
            {
                Spell = spell,
                MatchType = IsSelfMessage(line.Message, spell) ? SpellMessageMatchType.You : SpellMessageMatchType.Target,
                TargetName = GetTargetName(line.Message, spell)
            };
        }

        private SpellMessageMatch GetDirectMessageMatch(LogLine line)
        {
            return _spellCache.GetSpellMessageMatches(line.Message)
                .Where(x => x.MatchType != SpellMessageMatchType.Ended)
                .Where(x => x.Spell.Duration != null && x.Spell.Duration.Value.TotalMilliseconds > 0)
                .GroupBy(x => new { x.TargetName, x.Spell.Name })
                .Select(x => x.First())
                .FirstOrDefault();
        }

        private static bool CurrentCastMessagesMatch(string message, Spell spell)
        {
            return IsSelfMessage(message, spell)
                || (!string.IsNullOrWhiteSpace(spell.MessageTarget)
                    && message.Contains(spell.MessageTarget, StringComparison.OrdinalIgnoreCase));
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
