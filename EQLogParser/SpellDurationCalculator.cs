using System;

namespace EQLogParser
{
    public class SpellDurationCalculator
    {
        private readonly PlayerLevelState _playerLevelState;

        public SpellDurationCalculator(PlayerLevelState playerLevelState)
        {
            _playerLevelState = playerLevelState;
        }

        public TimeSpan? GetDuration(Spell spell)
        {
            if (spell.DurationTicks == null)
            {
                return spell.Duration;
            }

            double ticks = spell.DurationTicks.Value;
            if (_playerLevelState.Level != null && spell.DurationFormula == 10)
            {
                ticks = Math.Min(ticks, _playerLevelState.Level.Value * 2);
            }

            return TimeSpan.FromSeconds(ticks * 6);
        }
    }
}
