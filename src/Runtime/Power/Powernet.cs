using System;
using System.Collections.Generic;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime.Power
{
    public enum PowerChannel
    {
        Equip = 0,
        Light = 1,
        Environ = 2
    }

    public class APC : DM_obj
    {
        public string AreaName { get; set; }
        public double BatteryCharge { get; set; }
        public double MaxBatteryCharge { get; set; }
        public bool Operating { get; set; }
        public bool MainBreaker { get; set; }

        public double EquipLoad { get; set; }
        public double LightLoad { get; set; }
        public double EnvironLoad { get; set; }

        public bool EquipChannelOn { get; set; }
        public bool LightChannelOn { get; set; }
        public bool EnvironChannelOn { get; set; }

        public APC(string areaName = "General Area", double capacity = 50000.0)
        {
            name = new DMValue(string.Format("APC ({0})", areaName));
            AreaName = areaName;
            BatteryCharge = capacity;
            MaxBatteryCharge = capacity;
            Operating = true;
            MainBreaker = true;

            EquipLoad = 1200.0; // Watts
            LightLoad = 400.0;
            EnvironLoad = 600.0;

            EquipChannelOn = true;
            LightChannelOn = true;
            EnvironChannelOn = true;
        }

        public double TotalLoad
        {
            get
            {
                double load = 0;
                if (EquipChannelOn) load += EquipLoad;
                if (LightChannelOn) load += LightLoad;
                if (EnvironChannelOn) load += EnvironLoad;
                return load;
            }
        }

        public void ProcessPowerTick(double deltaSeconds = 1.0)
        {
            if (!Operating || !MainBreaker) return;

            double energyUsedJoules = TotalLoad * deltaSeconds;
            if (BatteryCharge >= energyUsedJoules)
            {
                BatteryCharge -= energyUsedJoules;
            }
            else
            {
                // Brownout / Blackout
                BatteryCharge = 0;
                EquipChannelOn = false;
                LightChannelOn = false;
            }
        }

        public double ChargePercentage
        {
            get
            {
                return MaxBatteryCharge > 0 ? (BatteryCharge / MaxBatteryCharge) * 100.0 : 0.0;
            }
        }

        public override string ToString()
        {
            return string.Format("APC [{0}]: {1:F1}% Charge ({2:F0}/{3:F0} J), Load: {4:F0} W",
                AreaName, ChargePercentage, BatteryCharge, MaxBatteryCharge, TotalLoad);
        }
    }

    public class SMES : DM_obj
    {
        public double StoredEnergy { get; set; }
        public double Capacity { get; set; }
        public double InputRate { get; set; }
        public double OutputRate { get; set; }
        public bool InputAttempt { get; set; }
        public bool OutputAttempt { get; set; }

        public SMES(double capacity = 5000000.0) // 5 MJ default capacity
        {
            name = new DMValue("SMES Superconducting Magnetic Energy Storage");
            Capacity = capacity;
            StoredEnergy = capacity * 0.8; // 80% default charge
            InputRate = 200000.0; // 200 kW input
            OutputRate = 150000.0; // 150 kW output
            InputAttempt = true;
            OutputAttempt = true;
        }

        public double ChargePercentage
        {
            get { return Capacity > 0 ? (StoredEnergy / Capacity) * 100.0 : 0.0; }
        }

        public void Charge(double energyJoules)
        {
            StoredEnergy = Math.Min(Capacity, StoredEnergy + energyJoules);
        }

        public double Discharge(double requestedJoules)
        {
            double given = Math.Min(StoredEnergy, requestedJoules);
            StoredEnergy -= given;
            return given;
        }
    }
}
