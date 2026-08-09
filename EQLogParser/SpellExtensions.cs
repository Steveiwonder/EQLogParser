using System;

namespace EQLogParser
{
    public static class SpellExtensions
    {
        public static Buff ToBuff(this Spell spell, DateTime landedTime, TimeSpan duration)
        {
            return new Buff()
            {
                Name = spell.Name,
                Expires = landedTime.Add(duration),
                Landed = landedTime,
                Duration = duration,
                IsDetrimental = spell.IsDetrimental
            };
        }
    }
}
