using System;
using System.Collections.Generic;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime.Atmos
{
    public enum GasType
    {
        Oxygen = 0,
        Nitrogen = 1,
        CarbonDioxide = 2,
        Plasma = 3,
        WaterVapor = 4,
        NitrousOxide = 5,
        Tritium = 6,
        MaxGases = 7
    }

    public class GasMixture : DM_datum
    {
        public const double R_IDEAL_GAS_EQUATION = 8.314;
        public const double ONE_ATMOSPHERE = 101.325; // kPa
        public const double TCMB = 2.7; // Cosmic microwave background temperature
        public const double T0C = 273.15; // 0 Celsius in Kelvin
        public const double T20C = 293.15; // 20 Celsius standard station temperature

        // Specific heat capacities (J / (mol * K))
        private static readonly double[] SpecificHeatCapacity = new double[]
        {
            20.0, // Oxygen
            20.0, // Nitrogen
            30.0, // CarbonDioxide
            200.0, // Plasma
            40.0, // WaterVapor
            40.0, // NitrousOxide
            10.0  // Tritium
        };

        private readonly double[] _gases = new double[(int)GasType.MaxGases];

        public double Temperature { get; set; }
        public double Volume { get; set; }

        public GasMixture(double volume = 2500.0) // 2500 Liters default turf volume
        {
            Temperature = T20C;
            Volume = volume;
        }

        public double GetMoles(GasType gas)
        {
            return _gases[(int)gas];
        }

        public void SetMoles(GasType gas, double moles)
        {
            _gases[(int)gas] = Math.Max(0.0, moles);
        }

        public void AdjustMoles(GasType gas, double delta)
        {
            _gases[(int)gas] = Math.Max(0.0, _gases[(int)gas] + delta);
        }

        public double TotalMoles
        {
            get
            {
                double total = 0;
                for (int i = 0; i < _gases.Length; i++) total += _gases[i];
                return total;
            }
        }

        public double HeatCapacity
        {
            get
            {
                double capacity = 0;
                for (int i = 0; i < _gases.Length; i++)
                {
                    capacity += _gases[i] * SpecificHeatCapacity[i];
                }
                return Math.Max(0.0001, capacity);
            }
        }

        public double Pressure
        {
            get
            {
                if (Volume <= 0) return 0.0;
                return (TotalMoles * R_IDEAL_GAS_EQUATION * Temperature) / Volume;
            }
        }

        public double ThermalEnergy
        {
            get
            {
                return Temperature * HeatCapacity;
            }
        }

        public static GasMixture CreateStandardStationAir(double volume = 2500.0)
        {
            GasMixture mix = new GasMixture(volume);
            mix.Temperature = T20C;
            // ~101.3 kPa air mixture (21.84 mol O2, 82.16 mol N2)
            mix.SetMoles(GasType.Oxygen, 21.84);
            mix.SetMoles(GasType.Nitrogen, 82.16);
            return mix;
        }

        public void Equalize(GasMixture other)
        {
            if (other == null) return;

            double totalVolume = this.Volume + other.Volume;
            if (totalVolume <= 0) return;

            double combinedHeatCap = this.HeatCapacity + other.HeatCapacity;
            double combinedEnergy = this.ThermalEnergy + other.ThermalEnergy;
            double finalTemp = combinedHeatCap > 0 ? combinedEnergy / combinedHeatCap : T20C;

            for (int i = 0; i < (int)GasType.MaxGases; i++)
            {
                double totalMolesOfGas = this._gases[i] + other._gases[i];
                this._gases[i] = totalMolesOfGas * (this.Volume / totalVolume);
                other._gases[i] = totalMolesOfGas * (other.Volume / totalVolume);
            }

            this.Temperature = finalTemp;
            other.Temperature = finalTemp;
        }

        public GasMixture RemoveRatio(double ratio)
        {
            ratio = Math.Max(0.0, Math.Min(1.0, ratio));
            GasMixture extracted = new GasMixture(this.Volume * ratio);
            extracted.Temperature = this.Temperature;

            for (int i = 0; i < (int)GasType.MaxGases; i++)
            {
                double removed = this._gases[i] * ratio;
                extracted._gases[i] = removed;
                this._gases[i] -= removed;
            }

            return extracted;
        }

        public void Merge(GasMixture other)
        {
            if (other == null || other.TotalMoles <= 0) return;

            double combinedHeatCap = this.HeatCapacity + other.HeatCapacity;
            double combinedEnergy = this.ThermalEnergy + other.ThermalEnergy;
            double finalTemp = combinedHeatCap > 0 ? combinedEnergy / combinedHeatCap : this.Temperature;

            for (int i = 0; i < (int)GasType.MaxGases; i++)
            {
                this._gases[i] += other._gases[i];
                other._gases[i] = 0;
            }

            this.Temperature = finalTemp;
        }

        public override string ToString()
        {
            return string.Format("GasMix: {0:F1} kPa, {1:F1} K ({2:F1}°C), Moles: O2={3:F2}, N2={4:F2}, CO2={5:F2}, Plasma={6:F2}",
                Pressure, Temperature, Temperature - T0C,
                GetMoles(GasType.Oxygen), GetMoles(GasType.Nitrogen), GetMoles(GasType.CarbonDioxide), GetMoles(GasType.Plasma));
        }
    }
}
