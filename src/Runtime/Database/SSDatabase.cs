using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DMToCSharp.Core;
using DMToCSharp.Runtime.MC;

namespace DMToCSharp.Runtime.Database
{
    public class PlayerRecord
    {
        public string CKey { get; set; }
        public string CharacterName { get; set; }
        public string PreferredJob { get; set; }
        public int RoundsPlayed { get; set; }
        public int Karma { get; set; }
        public string AdminRank { get; set; }

        public PlayerRecord(string ckey, string charName = "John Doe", string job = "Assistant")
        {
            CKey = ckey.ToLowerInvariant();
            CharacterName = charName;
            PreferredJob = job;
            RoundsPlayed = 0;
            Karma = 0;
            AdminRank = "Player";
        }
    }

    public class SSDatabase : DMSubsystem
    {
        public static readonly SSDatabase Instance = new SSDatabase();

        private readonly Dictionary<string, PlayerRecord> _players = new Dictionary<string, PlayerRecord>(StringComparer.OrdinalIgnoreCase);
        public int TotalQueries { get; private set; }

        public SSDatabase()
        {
            SubsystemName = "Database & Persistence";
            Priority = 10;
            WaitMilliseconds = 2000;

            // Seed default player records
            RegisterPlayer(new PlayerRecord("admin", "Station Administrator", "Captain") { AdminRank = "Host", Karma = 100, RoundsPlayed = 42 });
            RegisterPlayer(new PlayerRecord("doctor_who", "Dr. Gregory House", "Chief Medical Officer") { Karma = 15, RoundsPlayed = 18 });
            RegisterPlayer(new PlayerRecord("syndie_agent", "Agent Smith", "Security Officer") { Karma = 5, RoundsPlayed = 9 });
        }

        public void RegisterPlayer(PlayerRecord player)
        {
            if (player != null && !string.IsNullOrEmpty(player.CKey))
            {
                lock (_players)
                {
                    _players[player.CKey] = player;
                }
                TotalQueries++;
            }
        }

        public PlayerRecord GetPlayer(string ckey)
        {
            TotalQueries++;
            if (string.IsNullOrEmpty(ckey)) return null;

            lock (_players)
            {
                PlayerRecord p;
                if (_players.TryGetValue(ckey, out p))
                {
                    return p;
                }
                // Auto create new guest record
                p = new PlayerRecord(ckey, "Spaceman", "Assistant");
                _players[ckey] = p;
                return p;
            }
        }

        public List<PlayerRecord> GetAllPlayers()
        {
            lock (_players)
            {
                return new List<PlayerRecord>(_players.Values);
            }
        }

        public override string StatEntry()
        {
            return string.Format("{0}: {1}ms (Registered Profiles: {2}, Queries: {3})",
                SubsystemName, Cost.ToString("F2"), _players.Count, TotalQueries);
        }
    }
}
