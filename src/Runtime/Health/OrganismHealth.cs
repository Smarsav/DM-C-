using System;
using System.Collections.Generic;
using DMToCSharp.Core;
using DMToCSharp.Runtime.Chemistry;

namespace DMToCSharp.Runtime.Health
{
    public enum BodyZone
    {
        Head,
        Chest,
        Groin,
        LeftArm,
        RightArm,
        LeftLeg,
        RightLeg
    }

    public enum DamageType
    {
        Brute,
        Burn,
        Toxin,
        Oxy,
        Brain,
        Stamina
    }

    public class BodyPart
    {
        public BodyZone Zone { get; set; }
        public string Name { get; set; }
        public double MaxDamage { get; set; }
        public double BruteDamage { get; set; }
        public double BurnDamage { get; set; }

        public BodyPart(BodyZone zone, string name, double maxDamage = 40.0)
        {
            Zone = zone;
            Name = name;
            MaxDamage = maxDamage;
            BruteDamage = 0;
            BurnDamage = 0;
        }

        public double TotalDamage
        {
            get { return BruteDamage + BurnDamage; }
        }

        public void ApplyDamage(DamageType type, double amount)
        {
            if (type == DamageType.Brute) BruteDamage += amount;
            else if (type == DamageType.Burn) BurnDamage += amount;
        }

        public void Heal(double brute, double burn)
        {
            BruteDamage = Math.Max(0.0, BruteDamage - brute);
            BurnDamage = Math.Max(0.0, BurnDamage - burn);
        }
    }

    public class OrganismHealth : DM_datum
    {
        public double MaxHealth { get; set; }
        public double ToxinDamage { get; set; }
        public double OxyDamage { get; set; }
        public double BrainDamage { get; set; }
        public double StaminaDamage { get; set; }

        public double BloodVolume { get; set; }
        public double MaxBloodVolume { get; set; }
        public double PulseRate { get; set; }
        public bool IsBleeding { get; set; }

        public ReagentContainer Bloodstream { get; private set; }
        public Dictionary<BodyZone, BodyPart> BodyParts { get; private set; }

        public OrganismHealth(double maxHealth = 100.0)
        {
            MaxHealth = maxHealth;
            BloodVolume = 560.0; // 560 mL standard human blood
            MaxBloodVolume = 560.0;
            PulseRate = 80.0;
            IsBleeding = false;
            Bloodstream = new ReagentContainer(100.0);

            BodyParts = new Dictionary<BodyZone, BodyPart>();
            BodyParts[BodyZone.Head] = new BodyPart(BodyZone.Head, "Head", 50.0);
            BodyParts[BodyZone.Chest] = new BodyPart(BodyZone.Chest, "Chest", 75.0);
            BodyParts[BodyZone.Groin] = new BodyPart(BodyZone.Groin, "Groin", 40.0);
            BodyParts[BodyZone.LeftArm] = new BodyPart(BodyZone.LeftArm, "Left Arm", 30.0);
            BodyParts[BodyZone.RightArm] = new BodyPart(BodyZone.RightArm, "Right Arm", 30.0);
            BodyParts[BodyZone.LeftLeg] = new BodyPart(BodyZone.LeftLeg, "Left Leg", 35.0);
            BodyParts[BodyZone.RightLeg] = new BodyPart(BodyZone.RightLeg, "Right Leg", 35.0);
        }

        public double TotalBrute
        {
            get
            {
                double total = 0;
                foreach (var part in BodyParts.Values) total += part.BruteDamage;
                return total;
            }
        }

        public double TotalBurn
        {
            get
            {
                double total = 0;
                foreach (var part in BodyParts.Values) total += part.BurnDamage;
                return total;
            }
        }

        public double TotalDamage
        {
            get
            {
                return TotalBrute + TotalBurn + ToxinDamage + OxyDamage + BrainDamage;
            }
        }

        public double CurrentHealth
        {
            get { return MaxHealth - TotalDamage; }
        }

        public string Status
        {
            get
            {
                if (CurrentHealth <= -100.0 || BloodVolume <= 150.0) return "Dead";
                if (CurrentHealth <= 0.0) return "Critical";
                if (StaminaDamage >= 100.0 || BrainDamage >= 60.0) return "Unconscious";
                return "Healthy";
            }
        }

        public void ApplyDamage(DamageType type, double amount, BodyZone zone = BodyZone.Chest)
        {
            if (amount <= 0) return;

            if (type == DamageType.Brute || type == DamageType.Burn)
            {
                BodyPart part;
                if (BodyParts.TryGetValue(zone, out part))
                {
                    part.ApplyDamage(type, amount);
                }
            }
            else if (type == DamageType.Toxin)
            {
                ToxinDamage += amount;
            }
            else if (type == DamageType.Oxy)
            {
                OxyDamage += amount;
            }
            else if (type == DamageType.Brain)
            {
                BrainDamage += amount;
            }
            else if (type == DamageType.Stamina)
            {
                StaminaDamage += amount;
            }
        }

        public void HealDamage(DamageType type, double amount, BodyZone zone = BodyZone.Chest)
        {
            if (amount <= 0) return;

            if (type == DamageType.Brute || type == DamageType.Burn)
            {
                BodyPart part;
                if (BodyParts.TryGetValue(zone, out part))
                {
                    part.Heal(type == DamageType.Brute ? amount : 0, type == DamageType.Burn ? amount : 0);
                }
            }
            else if (type == DamageType.Toxin)
            {
                ToxinDamage = Math.Max(0.0, ToxinDamage - amount);
            }
            else if (type == DamageType.Oxy)
            {
                OxyDamage = Math.Max(0.0, OxyDamage - amount);
            }
            else if (type == DamageType.Brain)
            {
                BrainDamage = Math.Max(0.0, BrainDamage - amount);
            }
            else if (type == DamageType.Stamina)
            {
                StaminaDamage = Math.Max(0.0, StaminaDamage - amount);
            }
        }

        public override string ToString()
        {
            return string.Format("Health [{0}]: {1:F0}/{2:F0} HP (Brute: {3:F0}, Burn: {4:F0}, Tox: {5:F0}, Oxy: {6:F0}, Blood: {7:F0}ml)",
                Status, CurrentHealth, MaxHealth, TotalBrute, TotalBurn, ToxinDamage, OxyDamage, BloodVolume);
        }
    }
}
