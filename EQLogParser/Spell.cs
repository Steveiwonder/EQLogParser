using System;
using System.Diagnostics;

namespace EQLogParser
{
    [DebuggerDisplay("{Name} {CastTime} {ManaCost} {Duration}")]
    public class Spell
    {
        public string[] Parts { get; private set; }
        public string Name { get; set; }
        public TimeSpan? CastTime { get; set; }
        public int? ManaCost { get; set; }
        public TimeSpan? Duration { get; set; }

        public string MessageYou { get; set; }
        public string MessageTarget { get; set; }
        public string MessageEnded { get; set; }
        public TargetTypes TargetType { get; set; }

        public Spell(string[] parts)
        {
            Parts = parts;
        }
    }
}