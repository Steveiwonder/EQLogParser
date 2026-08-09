using System;
using System.Text.RegularExpressions;

namespace EQLogParser.Processors
{
    public class DamageDoneLogProcessor : ILogProcessor
    {
        private static readonly Regex DirectDamageRegex = new Regex(
            @"^(?<actor>You|[A-Z][A-Za-z]+) (?<verb>hit|hits|slash(?:es)?|pierce(?:s)?|cleave(?:s)?|crush(?:es)?|bash(?:es)?|kick(?:s)?|backstab(?:s)?|maul(?:s)?|punch(?:es)?|bite(?:s)?|claw(?:s)?) .+ for (?<damage>\d+) points of(?: [a-z-]+)? damage(?: by .*)?\.",
            RegexOptions.Compiled);

        private static readonly Regex YourDotDamageRegex = new Regex(
            @"^.+ has taken (?<damage>\d+) damage from your .+\.",
            RegexOptions.Compiled);

        private static readonly Regex YourDamageShieldRegex = new Regex(
            @"^.+ is .+ by YOUR .+ for (?<damage>\d+) points of non-melee damage\.",
            RegexOptions.Compiled);

        private static readonly Regex NamedDamageShieldRegex = new Regex(
            @"^.+ is .+ by (?<actor>[A-Z][A-Za-z]+)'s .+ for (?<damage>\d+) points of non-melee damage\.",
            RegexOptions.Compiled);

        private readonly DamageTracker _damageTracker;
        private DamageMatch _damageMatch;

        public EverquestLogReader.LogType LogType => EverquestLogReader.LogType.DamageDone;

        public DamageDoneLogProcessor(DamageTracker damageTracker)
        {
            _damageTracker = damageTracker;
        }

        public bool IsMatch(LogLine line)
        {
            return TryGetDamage(line.Message, out _damageMatch);
        }

        public void Process(LogLine line)
        {
            _damageTracker.AddDamage(_damageMatch.ActorName, _damageMatch.Damage, line.When);
            _damageMatch = null;
        }

        private static bool TryGetDamage(string message, out DamageMatch damageMatch)
        {
            Match match = DirectDamageRegex.Match(message);
            if (match.Success)
            {
                damageMatch = new DamageMatch()
                {
                    ActorName = NormalizeActorName(match.Groups["actor"].Value),
                    Damage = int.Parse(match.Groups["damage"].Value)
                };
                return true;
            }

            match = YourDotDamageRegex.Match(message);
            if (match.Success)
            {
                damageMatch = new DamageMatch()
                {
                    ActorName = "__YOU__",
                    Damage = int.Parse(match.Groups["damage"].Value)
                };
                return true;
            }

            match = YourDamageShieldRegex.Match(message);
            if (match.Success)
            {
                damageMatch = new DamageMatch()
                {
                    ActorName = "__YOU__",
                    Damage = int.Parse(match.Groups["damage"].Value)
                };
                return true;
            }

            match = NamedDamageShieldRegex.Match(message);
            if (match.Success)
            {
                damageMatch = new DamageMatch()
                {
                    ActorName = match.Groups["actor"].Value,
                    Damage = int.Parse(match.Groups["damage"].Value)
                };
                return true;
            }

            damageMatch = null;
            return false;
        }

        private static string NormalizeActorName(string actorName)
        {
            return string.Equals(actorName, "You", StringComparison.OrdinalIgnoreCase)
                ? "__YOU__"
                : actorName;
        }

        private class DamageMatch
        {
            public string ActorName { get; set; }
            public int Damage { get; set; }
        }
    }
}
