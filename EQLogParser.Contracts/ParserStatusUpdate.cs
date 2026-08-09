using System;
using System.Collections.Generic;

namespace EQLogParser.Contracts
{
    public class ParserStatusUpdate
    {
        public DateTimeOffset UpdatedAt { get; set; }
        public CastStatus CurrentCast { get; set; } = new CastStatus();
        public IReadOnlyList<PlayerStatus> Players { get; set; } = Array.Empty<PlayerStatus>();
    }

    public class CastStatus
    {
        public bool IsCasting { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool LastCastFizzled { get; set; }
        public bool LastCastInterrupted { get; set; }
        public bool LastCastDidNotTakeHold { get; set; }
    }

    public class PlayerStatus
    {
        public string Name { get; set; } = string.Empty;
        public IReadOnlyList<BuffStatus> Buffs { get; set; } = Array.Empty<BuffStatus>();
    }

    public class BuffStatus
    {
        public string Name { get; set; } = string.Empty;
        public DateTime Landed { get; set; }
        public DateTime Expires { get; set; }
        public double DurationSeconds { get; set; }
        public double TimeLeftSeconds { get; set; }
        public int Percent { get; set; }
        public bool IsDetrimental { get; set; }
        public bool IsExpired { get; set; }
    }
}
