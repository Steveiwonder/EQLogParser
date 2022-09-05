using System.Collections.Generic;
using System.Linq;

namespace EQLogParser
{
    public class BuffManager : IBuffManager
    {
        private Dictionary<string, Player> Players { get; set; } = new Dictionary<string, Player>();


        public void AddBuff(string playerName, Buff buff)
        {
            GetPlayer(playerName).ApplyBuff(buff);
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
            foreach (var s in buffNames)
            {
                player.RemoveBuff(s);
            }
            
        }

        public IEnumerable<Player> GetPlayers()
        {
            return Players.Values.Where(x => x.GetBuffs().Any());
        }
    }
}