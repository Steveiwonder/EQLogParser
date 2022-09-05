using System.Collections.Generic;

namespace EQLogParser
{
    public interface IBuffManager
    {
        IEnumerable<Player> GetPlayers();
        void AddBuff(string playerName, Buff buff);
        void RemoveBuffs(string playerName, params string[] buffNames);
    }
}