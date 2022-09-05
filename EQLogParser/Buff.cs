using System;
using System.Diagnostics;
using System.Dynamic;

namespace EQLogParser
{
    [DebuggerDisplay("{Name} {Expires}")]
    public class Buff
    {
        public string Name { get; set; }
        public DateTime Expires { get; set; }
        public DateTime Landed { get; set; }

        public TimeSpan TimeLeft => Expires - DateTime.Now;
        public TimeSpan Duration { get; set; }
        public bool IsExpired => TimeLeft.TotalMilliseconds < 0;
        public int Percent
        {
            get
            {
                double vv = (TimeLeft.TotalMilliseconds/Duration.TotalMilliseconds) * 100;

                return (int) (vv);
            }
        }
    }
}