using System;
using System.Text.RegularExpressions;

namespace EQLogParser.Processors
{
    public class SpellWornOffLogProcessor : ILogProcessor
    {
        private const string PetTargetName = "__PET__";
        private readonly Regex _targetWornOffRegex = new Regex(@"^Your (?<spell>.+) spell has worn off of (?<target>.+)\.$", RegexOptions.IgnoreCase);
        private readonly Regex _petWornOffRegex = new Regex(@"^Your pet's (?<spell>.+) spell has worn off\.$", RegexOptions.IgnoreCase);
        private readonly SpellCache _spellCache;
        private readonly IBuffManager _buffManager;
        private readonly SpellDurationCalculator _spellDurationCalculator;
        private Match _match;

        public EverquestLogReader.LogType LogType => EverquestLogReader.LogType.SpellWornOff;

        public SpellWornOffLogProcessor(
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
            _match = _targetWornOffRegex.Match(line.Message);
            if (_match.Success)
            {
                return true;
            }

            _match = _petWornOffRegex.Match(line.Message);
            return _match.Success;
        }

        public void Process(LogLine line)
        {
            string spellName = _match.Groups["spell"].Value.Trim();
            if (!_spellCache.TryGetSpellByName(spellName, out Spell spell))
            {
                _match = null;
                return;
            }

            TimeSpan? duration = _spellDurationCalculator.GetDuration(spell);
            if (duration == null || duration.Value.TotalMilliseconds <= 0)
            {
                _match = null;
                return;
            }

            string targetName = _match.Groups["target"].Success
                ? _match.Groups["target"].Value.Trim()
                : PetTargetName;

            _buffManager.ExpireOrAddBuff(targetName, spell.ToBuff(line.When, duration.Value));
            _match = null;
        }
    }
}
