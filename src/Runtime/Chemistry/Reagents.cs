using System;
using System.Collections.Generic;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime.Chemistry
{
    public class Reagent : DM_datum
    {
        public string Id { get; set; }
        public string ReagentName { get; set; }
        public string ColorHex { get; set; }
        public double MetabolismRate { get; set; }
        public string TasteDescription { get; set; }

        public Reagent(string id, string name, string colorHex = "#ffffff", double metabolismRate = 0.4, string taste = "neutral")
        {
            Id = id;
            ReagentName = name;
            ColorHex = colorHex;
            MetabolismRate = metabolismRate;
            TasteDescription = taste;
        }

        public virtual void OnMobLife(DM_mob mob, double volume)
        {
            // Reagent effect on mob
        }
    }

    public class ReagentEntry
    {
        public Reagent Reagent { get; set; }
        public double Volume { get; set; }

        public ReagentEntry(Reagent reagent, double volume)
        {
            Reagent = reagent;
            Volume = volume;
        }
    }

    public class ReagentContainer : DM_datum
    {
        public double MaxVolume { get; set; }
        public double Temperature { get; set; }
        private readonly Dictionary<string, ReagentEntry> _reagents = new Dictionary<string, ReagentEntry>(StringComparer.OrdinalIgnoreCase);

        public ReagentContainer(double maxVolume = 100.0)
        {
            MaxVolume = maxVolume;
            Temperature = 293.15; // 20 C
        }

        public double TotalVolume
        {
            get
            {
                double total = 0;
                foreach (var entry in _reagents.Values) total += entry.Volume;
                return total;
            }
        }

        public double AvailableVolume
        {
            get { return Math.Max(0.0, MaxVolume - TotalVolume); }
        }

        public double GetReagentAmount(string id)
        {
            ReagentEntry entry;
            if (_reagents.TryGetValue(id, out entry))
            {
                return entry.Volume;
            }
            return 0.0;
        }

        public double AddReagent(string id, double amount, string name = null)
        {
            if (amount <= 0) return 0;
            double toAdd = Math.Min(amount, AvailableVolume);
            if (toAdd <= 0) return 0;

            ReagentEntry entry;
            if (_reagents.TryGetValue(id, out entry))
            {
                entry.Volume += toAdd;
            }
            else
            {
                var reagent = ChemistryRegistry.GetReagent(id) ?? new Reagent(id, name ?? id);
                _reagents[id] = new ReagentEntry(reagent, toAdd);
            }

            CheckReactions();
            return toAdd;
        }

        public double RemoveReagent(string id, double amount)
        {
            if (amount <= 0) return 0;
            ReagentEntry entry;
            if (_reagents.TryGetValue(id, out entry))
            {
                double removed = Math.Min(amount, entry.Volume);
                entry.Volume -= removed;
                if (entry.Volume <= 0.0001)
                {
                    _reagents.Remove(id);
                }
                return removed;
            }
            return 0;
        }

        public double TransTo(ReagentContainer target, double amount)
        {
            if (target == null || amount <= 0 || TotalVolume <= 0) return 0;

            double ratio = Math.Min(1.0, amount / TotalVolume);
            double actualTransferred = 0;

            List<string> keys = new List<string>(_reagents.Keys);
            foreach (var key in keys)
            {
                var entry = _reagents[key];
                double transVol = entry.Volume * ratio;
                double added = target.AddReagent(key, transVol);
                entry.Volume -= added;
                actualTransferred += added;

                if (entry.Volume <= 0.0001)
                {
                    _reagents.Remove(key);
                }
            }

            return actualTransferred;
        }

        public void CheckReactions()
        {
            // Example chemical synthesis reaction:
            // Welding fuel + Oxygen -> Heat + Carbon (Smoke)
            double fuel = GetReagentAmount("welding_fuel");
            double oxy = GetReagentAmount("oxygen");
            if (fuel >= 5.0 && oxy >= 5.0)
            {
                double reactionAmt = Math.Min(fuel, oxy);
                RemoveReagent("welding_fuel", reactionAmt);
                RemoveReagent("oxygen", reactionAmt);
                AddReagent("carbon", reactionAmt * 0.8, "Carbon Ash");
                Temperature += reactionAmt * 15.0; // Exothermic heat release
            }
        }

        public override string ToString()
        {
            List<string> parts = new List<string>();
            foreach (var entry in _reagents.Values)
            {
                parts.Add(string.Format("{0}: {1:F1}u", entry.Reagent.ReagentName, entry.Volume));
            }
            return string.Format("Container [{0:F1}/{1:F1}u, {2:F1}K]: {3}",
                TotalVolume, MaxVolume, Temperature, string.Join(", ", parts.ToArray()));
        }
    }

    public static class ChemistryRegistry
    {
        private static readonly Dictionary<string, Reagent> _registry = new Dictionary<string, Reagent>(StringComparer.OrdinalIgnoreCase);

        static ChemistryRegistry()
        {
            Register(new Reagent("water", "Water", "#3b82f6", 0.5, "pure water"));
            Register(new Reagent("oxygen", "Liquid Oxygen", "#93c5fd", 0.2, "metallic"));
            Register(new Reagent("carbon", "Carbon", "#1e293b", 0.1, "bitter ash"));
            Register(new Reagent("blood", "Blood", "#dc2626", 0.0, "metallic copper"));
            Register(new Reagent("epinephrine", "Epinephrine", "#10b981", 0.5, "sour chemical"));
            Register(new Reagent("morphine", "Morphine", "#8b5cf6", 0.3, "sweet medicinal"));
            Register(new Reagent("plasma", "Liquid Plasma", "#a855f7", 0.6, "toxic alien sludge"));
            Register(new Reagent("welding_fuel", "Welding Fuel", "#f59e0b", 0.4, "volatile petroleum"));
            Register(new Reagent("acid", "Sulfuric Acid", "#eab308", 0.8, "burning acid"));
        }

        public static void Register(Reagent reagent)
        {
            if (reagent != null) _registry[reagent.Id] = reagent;
        }

        public static Reagent GetReagent(string id)
        {
            Reagent r;
            _registry.TryGetValue(id, out r);
            return r;
        }
    }
}
