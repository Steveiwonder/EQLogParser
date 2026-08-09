using System;
using System.Collections.Generic;
using System.Linq;

namespace EQLogParser
{
    public class Player
    {
        public string Name { get; set; }

        private readonly Dictionary<string, Buff> _buffs = new Dictionary<string, Buff>();

        public IEnumerable<Buff> GetBuffs()
        {
            return _buffs.Values;
        }

        public void PruneExpiredBefore(DateTime cutoff)
        {
            foreach (string buffName in _buffs
                .Where(x => x.Value.Expires < cutoff)
                .Select(x => x.Key)
                .ToArray())
            {
                _buffs.Remove(buffName);
            }
        }

        public void ApplyBuff(Buff buff)
        {
            if (_buffs.ContainsKey(buff.Name))
            {
                _buffs.Remove(buff.Name);
            }

            _buffs.Add(buff.Name, buff);
        }

        public void ExpireOrApplyBuff(Buff buff)
        {
            if (_buffs.ContainsKey(buff.Name))
            {
                _buffs[buff.Name].Expire(buff.Expires);
                return;
            }

            _buffs.Add(buff.Name, buff);
        }

        public void ExpireBuff(string buffName, DateTime expiredAt)
        {
            if (_buffs.ContainsKey(buffName))
            {
                _buffs[buffName].Expire(expiredAt);
            }
        }

        public void RemoveBuff(string buffName)
        {
            if (_buffs.ContainsKey(buffName))
            {
                _buffs.Remove(buffName);
            }
        }
    }
}
