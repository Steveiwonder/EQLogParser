using System;

namespace EQLogParser
{
    public static class SpellExtensions
    {
        public static Buff ToBuff(this Spell spell, DateTime landedTime)
        {
            if (spell.Duration == null)
            {
                throw new Exception($"Spell {spell.Name} duration was null");
            }
            return new Buff()
            {
                Name = spell.Name,
                Expires = landedTime.Add(spell.Duration.Value),
                Landed = landedTime,
                Duration = spell.Duration.Value
            };
        }
    }
}