using System;
using System.Linq;
using EQLogParser.Contracts;

namespace EQLogParser
{
    public class ParserStatusFactory
    {
        private readonly IBuffManager _buffManager;
        private readonly CurrentSpellCast _currentSpellCast;
        private readonly StartupScanState _startupScanState;

        public ParserStatusFactory(
            IBuffManager buffManager,
            CurrentSpellCast currentSpellCast,
            StartupScanState startupScanState)
        {
            _buffManager = buffManager;
            _currentSpellCast = currentSpellCast;
            _startupScanState = startupScanState;
        }

        public ParserStatusUpdate Create()
        {
            return new ParserStatusUpdate()
            {
                UpdatedAt = DateTimeOffset.Now,
                StartupScan = _startupScanState.Current,
                CurrentCast = new CastStatus()
                {
                    IsCasting = _currentSpellCast.IsCasting,
                    Name = _currentSpellCast.Name ?? string.Empty,
                    LastCastFizzled = _currentSpellCast.LastCastFizzled,
                    LastCastInterrupted = _currentSpellCast.LastCastInterrupted,
                    LastCastDidNotTakeHold = _currentSpellCast.LastCastDidNotTakeHold
                },
                Players = _buffManager.GetPlayers()
                    .OrderBy(x => x.Name)
                    .Select(player => new PlayerStatus()
                    {
                        Name = player.Name,
                        Buffs = player.GetBuffs()
                            .OrderBy(x => x.Name)
                            .Select(buff => new BuffStatus()
                            {
                                Name = buff.Name,
                                Landed = buff.Landed,
                                Expires = buff.Expires,
                                DurationSeconds = buff.Duration.TotalSeconds,
                                TimeLeftSeconds = Math.Max(0, buff.TimeLeft.TotalSeconds),
                                Percent = Math.Clamp(buff.Percent, 0, 100),
                                IsDetrimental = buff.IsDetrimental,
                                IsExpired = buff.IsExpired
                            })
                            .ToArray()
                    })
                    .ToArray()
            };
        }
    }
}
