using System.Collections.Generic;

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

        public void ApplyBuff(Buff buff)
        {
            if (_buffs.ContainsKey(buff.Name))
            {
                _buffs.Remove(buff.Name);
            }

            _buffs.Add(buff.Name, buff);
        }

        public void ExpireBuff(string buffName)
        {
            if (_buffs.ContainsKey(buffName))
            {
                _buffs[buffName].Expire();
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
