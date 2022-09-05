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

            return _buffs.Values.Where(x => !x.IsExpired);
        }

        public void ApplyBuff(Buff buff)
        {

            if (_buffs.ContainsKey(buff.Name))
            {
                _buffs.Remove(buff.Name);
            }
            _buffs.Add(buff.Name, buff);


            //remove expired buffs
            IEnumerable<Buff> expiredBuffs = _buffs.Values.Where(x => x.IsExpired).ToArray();
            foreach (var expiredBuff in expiredBuffs)
            {
                _buffs.Remove(expiredBuff.Name);
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