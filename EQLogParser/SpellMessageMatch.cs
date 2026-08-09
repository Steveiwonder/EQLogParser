namespace EQLogParser
{
    public enum SpellMessageMatchType
    {
        You,
        Target,
        Ended
    }

    public class SpellMessageMatch
    {
        public Spell Spell { get; set; }
        public SpellMessageMatchType MatchType { get; set; }
        public string TargetName { get; set; }
    }
}
