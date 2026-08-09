using System;
using System.Collections.Generic;
using System.Linq;
using EQLogParser.Contracts;

namespace EQLogParser
{
    public class DamageTracker
    {
        private static readonly TimeSpan Retention = TimeSpan.FromSeconds(75);
        private static readonly TimeSpan ChartWindow = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan CurrentDpsWindow = TimeSpan.FromSeconds(10);
        private readonly object _lock = new object();
        private readonly List<DamageEvent> _events = new List<DamageEvent>();

        public void AddDamage(string actorName, int damage, DateTime when)
        {
            if (string.IsNullOrWhiteSpace(actorName) || damage <= 0)
            {
                return;
            }

            lock (_lock)
            {
                _events.Add(new DamageEvent()
                {
                    ActorName = actorName,
                    Damage = damage,
                    When = when
                });

                Prune(DateTime.Now);
            }
        }

        public IReadOnlyList<DamageActorStatus> GetActors()
        {
            DateTime now = DateTime.Now;
            DateTime chartStart = now.Subtract(ChartWindow);
            DateTime currentStart = now.Subtract(CurrentDpsWindow);

            lock (_lock)
            {
                Prune(now);
                return _events
                    .GroupBy(x => x.ActorName)
                    .OrderBy(x => x.Key)
                    .Select(group =>
                    {
                        DamageEvent[] chartEvents = group
                            .Where(x => x.When >= chartStart)
                            .ToArray();

                        return new DamageActorStatus()
                        {
                            Name = group.Key,
                            DamageLastMinute = chartEvents.Sum(x => x.Damage),
                            CurrentDps = Math.Round(group
                                .Where(x => x.When >= currentStart)
                                .Sum(x => x.Damage) / CurrentDpsWindow.TotalSeconds, 1),
                            Samples = BuildSamples(chartEvents, chartStart, now)
                        };
                    })
                    .Where(x => x.DamageLastMinute > 0 || x.CurrentDps > 0)
                    .ToArray();
            }
        }

        private static IReadOnlyList<DpsSampleStatus> BuildSamples(DamageEvent[] events, DateTime chartStart, DateTime now)
        {
            List<DpsSampleStatus> samples = new List<DpsSampleStatus>();
            for (int i = 0; i < (int)ChartWindow.TotalSeconds; i++)
            {
                DateTime bucketStart = chartStart.AddSeconds(i);
                DateTime bucketEnd = bucketStart.AddSeconds(1);
                int damage = events
                    .Where(x => x.When >= bucketStart && x.When < bucketEnd)
                    .Sum(x => x.Damage);

                samples.Add(new DpsSampleStatus()
                {
                    At = bucketEnd,
                    Dps = damage
                });
            }

            return samples;
        }

        private void Prune(DateTime now)
        {
            DateTime cutoff = now.Subtract(Retention);
            _events.RemoveAll(x => x.When < cutoff);
        }

        private class DamageEvent
        {
            public string ActorName { get; set; }
            public int Damage { get; set; }
            public DateTime When { get; set; }
        }
    }
}
