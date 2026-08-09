using System;
using System.Collections.Generic;
using System.Linq;

namespace EQLogParser
{
    public class BuffManager : IBuffManager
    {
        private static readonly TimeSpan ExpiredBuffRetention = TimeSpan.FromMinutes(5);
        private Dictionary<string, Player> Players { get; set; } = new Dictionary<string, Player>();

        public void AddBuff(string playerName, Buff buff)
        {
            GetPlayer(playerName).ApplyBuff(buff);
        }

        public void ExpireOrAddBuff(string playerName, Buff buff)
        {
            GetPlayer(playerName).ExpireOrApplyBuff(buff);
        }

        private Player GetPlayer(string playerName)
        {
            if (Players.ContainsKey(playerName))
            {
                return Players[playerName];
            }

            Player player = new Player()
            {
                Name = playerName,
            };

            Players.Add(playerName, player);
            return player;
        }

        public void RemoveBuffs(string playerName, params string[] buffNames)
        {
            Player player = GetPlayer(playerName);
            foreach (string buffName in buffNames)
            {
                player.RemoveBuff(buffName);
            }
        }

        public void ExpireBuffs(string playerName, params string[] buffNames)
        {
            Player player = GetPlayer(playerName);
            foreach (string buffName in buffNames)
            {
                player.ExpireBuff(buffName);
            }
        }

        public IEnumerable<Player> GetPlayers()
        {
            DateTime expiredCutoff = DateTime.Now.Subtract(ExpiredBuffRetention);
            foreach (Player player in Players.Values)
            {
                player.PruneExpiredBefore(expiredCutoff);
            }

            return Players.Values.Where(x => x.GetBuffs().Any());
        }
    }
}
