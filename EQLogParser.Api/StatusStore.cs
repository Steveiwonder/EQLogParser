using EQLogParser.Contracts;

namespace EQLogParser.Api
{
    public class StatusStore
    {
        private readonly object _lock = new object();
        private readonly HashSet<string> _dismissedBuffs = new HashSet<string>();
        private ParserStatusUpdate? _current;

        public ParserStatusUpdate? Current
        {
            get
            {
                lock (_lock)
                {
                    return _current;
                }
            }
        }

        public ParserStatusUpdate Set(ParserStatusUpdate status)
        {
            lock (_lock)
            {
                _current = ApplyDismissals(status);
                return _current;
            }
        }

        public ParserStatusUpdate? Dismiss(DismissBuffRequest request)
        {
            lock (_lock)
            {
                _dismissedBuffs.Add(GetDismissalKey(request.PlayerName, request.BuffName, request.Landed));
                if (_current != null)
                {
                    _current = ApplyDismissals(_current);
                }

                return _current;
            }
        }

        private ParserStatusUpdate ApplyDismissals(ParserStatusUpdate status)
        {
            return new ParserStatusUpdate()
            {
                UpdatedAt = status.UpdatedAt,
                StartupScan = status.StartupScan,
                CurrentCast = status.CurrentCast,
                Players = status.Players
                    .Select(player => new PlayerStatus()
                    {
                        Name = player.Name,
                        Buffs = player.Buffs
                            .Where(buff => !_dismissedBuffs.Contains(GetDismissalKey(player.Name, buff.Name, buff.Landed)))
                            .ToArray()
                    })
                    .Where(player => player.Buffs.Any())
                    .ToArray()
            };
        }

        private static string GetDismissalKey(string playerName, string buffName, DateTime landed)
        {
            return $"{playerName}|{buffName}|{landed.Ticks}";
        }
    }
}
