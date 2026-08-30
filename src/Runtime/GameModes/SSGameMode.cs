using System;
using System.Collections.Generic;
using DMToCSharp.Core;
using DMToCSharp.Runtime.MC;

namespace DMToCSharp.Runtime.GameModes
{
    public class Objective : DM_datum
    {
        public string Description { get; set; }
        public bool Completed { get; set; }
        public string TargetName { get; set; }

        public Objective(string desc, string target = "")
        {
            Description = desc;
            TargetName = target;
            Completed = false;
        }

        public override string ToString()
        {
            return string.Format("[{0}] {1}", Completed ? "SUCCESS" : "PENDING", Description);
        }
    }

    public class Antagonist : DM_datum
    {
        public string CKey { get; set; }
        public string CharacterName { get; set; }
        public string Role { get; set; }
        public List<Objective> Objectives { get; private set; }
        public int Telecrystals { get; set; }

        public Antagonist(string ckey, string charName, string role = "Traitor")
        {
            CKey = ckey;
            CharacterName = charName;
            Role = role;
            Objectives = new List<Objective>();
            Telecrystals = 20; // Standard Syndicate Uplink TC
        }

        public void AddObjective(string desc, string target = "")
        {
            Objectives.Add(new Objective(desc, target));
        }

        public bool AllObjectivesComplete()
        {
            if (Objectives.Count == 0) return true;
            for (int i = 0; i < Objectives.Count; i++)
            {
                if (!Objectives[i].Completed) return false;
            }
            return true;
        }
    }

    public enum RoundStage
    {
        Pregame,
        InProgress,
        Ended
    }

    public class SSGameMode : DMSubsystem
    {
        public static readonly SSGameMode Instance = new SSGameMode();

        public string ModeName { get; set; }
        public RoundStage Stage { get; set; }
        public int RoundTimeSeconds { get; private set; }
        public List<Antagonist> Antagonists { get; private set; }

        public SSGameMode()
        {
            SubsystemName = "Game Mode & Objectives";
            Priority = 20;
            WaitMilliseconds = 1000;
            ModeName = "Secret (Traitor)";
            Stage = RoundStage.InProgress;
            Antagonists = new List<Antagonist>();

            // Setup default traitor
            var traitor = new Antagonist("syndie_agent", "Agent Smith", "Syndicate Infiltrator");
            traitor.AddObjective("Assassinate the Research Director", "Dr. H. Aris");
            traitor.AddObjective("Steal the Captain's Antique Laser Gun", "Antique Laser Gun");
            traitor.AddObjective("Escape alive on the emergency shuttle.");
            Antagonists.Add(traitor);
        }

        public override DMValue Fire(bool resumed = false)
        {
            base.Fire(resumed);
            if (Stage == RoundStage.InProgress)
            {
                RoundTimeSeconds++;
            }
            return DMValue.Null;
        }

        public string GenerateRoundEndReport()
        {
            List<string> report = new List<string>();
            report.Add(string.Format("=== ROUND END REPORT - {0} ===", ModeName.ToUpperInvariant()));
            report.Add(string.Format("Round Duration: {0:D2}:{1:D2}", RoundTimeSeconds / 60, RoundTimeSeconds % 60));
            report.Add(string.Format("Antagonists Count: {0}", Antagonists.Count));

            for (int i = 0; i < Antagonists.Count; i++)
            {
                var a = Antagonists[i];
                report.Add(string.Format("\nAntagonist: {0} ({1}) - Role: {2}", a.CharacterName, a.CKey, a.Role));
                for (int j = 0; j < a.Objectives.Count; j++)
                {
                    report.Add(string.Format("  - {0}", a.Objectives[j].ToString()));
                }
                report.Add(string.Format("Outcome: {0}", a.AllObjectivesComplete() ? "SYNDICATE VICTORY" : "SYNDICATE DEFEATED"));
            }

            return string.Join("\n", report.ToArray());
        }

        public override string StatEntry()
        {
            return string.Format("{0}: {1}ms (Mode: {2}, Elapsed: {3}s)", SubsystemName, Cost.ToString("F2"), ModeName, RoundTimeSeconds);
        }
    }
}
