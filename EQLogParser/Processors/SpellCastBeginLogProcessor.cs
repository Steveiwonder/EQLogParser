using System.Linq;
using System.Text.RegularExpressions;

namespace EQLogParser.Processors
{
    public class SpellCastBeginLogProcessor : ILogProcessor
    {
        private readonly ILogger _logger;
        private readonly CurrentSpellCast _currentSpellCast;
        private readonly SpellCache _spellCache;
        public EverquestLogReader.LogType LogType => EverquestLogReader.LogType.SpellCastBegin;
        private const string Pattern = @"(\byou begin casting\b)(?<spell>.*)\.";
        private readonly Regex _regex = new Regex(Pattern, RegexOptions.IgnoreCase);

        public SpellCastBeginLogProcessor(ILogger logger, CurrentSpellCast currentSpellCast, SpellCache spellCache)
        {
            _logger = logger;
            _currentSpellCast = currentSpellCast;
            _spellCache = spellCache;
        }

        public bool IsMatch(LogLine line)
        {
            return _regex.IsMatch(line.Message);
        }

        public void Process(LogLine line)
        {
            Match matches = _regex.Match(line.Message);
            string spellName = matches.Groups["spell"].Value.Trim();

            Spell spell = _spellCache.GetSpellByName(spellName);
            string[] landedMessages = new[] { spell.MessageYou, spell.MessageTarget }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();

            _currentSpellCast.BeginCast(spellName, landedMessages);
        }
    }
}
