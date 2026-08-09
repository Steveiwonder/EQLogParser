using System;
using System.Collections.Generic;

namespace EQLogParser
{
    public interface IBuffManager
    {
        IEnumerable<Player> GetPlayers();
        void AddBuff(string playerName, Buff buff);
        void ExpireOrAddBuff(string playerName, Buff buff);
        void ExpireBuffs(string playerName, DateTime expiredAt, params string[] buffNames);
        void RemoveBuffs(string playerName, params string[] buffNames);
    }
}
