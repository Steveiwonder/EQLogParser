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
            //_logger.WriteLine(line, ConsoleColor.Gray, LogType);
            Match matches = _regex.Match(line.Message);

            var spellName = matches.Groups["spell"].Value.Trim();
            
            Spell spell = _spellCache.GetSpellByName(spellName);

            _currentSpellCast.BeginCast(spellName, new []{ spell.MessageYou, spell.MessageTarget});
        }
    }
}