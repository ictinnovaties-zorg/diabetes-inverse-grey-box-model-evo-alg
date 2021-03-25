
using SMLDC.Simulator.Utilities;
using System;
using System.Collections.Generic;

namespace SMLDC.Simulator.Models.HeartRate.HrFsm
{
    public class HrFsmSettings
    {

        public HrFsmSettings(RandomStuff random, string heartRateGeneratorOption, string realDataBaseFolder,  int realPatientIndex, Dictionary<string, double[]> patientParameters)
        {
            this.realDataBaseFolder = realDataBaseFolder;
           // this.heartRateFileName = heartRateFileName;
            this.heartRateGeneratorOption = heartRateGeneratorOption;
            this.realPatientIndex = realPatientIndex;

            // hier worden de random ranges gebruike die in de ini stonden:
            Dictionary<string, double> patientValues = new Dictionary<string, double>(patientParameters.Count);
            foreach (KeyValuePair<string, double[]> dataField in patientParameters)
            {
                //random range:
                double value = random.GetNormalDistributed(dataField.Value[0], dataField.Value[1], Globals.maxSigma);
                patientValues.Add(dataField.Key, value);
            }
            ParseParameters(patientValues);
        }


        private void ParseParameters(Dictionary<string, double> patientValues)
        {
            // parse de inhoud van de ini file op patient settings.

            // uint, maar door stddev omlaag kan het onder 0 komen (en dus overflow naar gigahoge int) daarom eerst maxen.
            this.maxAantalKeerSportPerDag = Max0AndSetting(patientValues, "maxAantalKeerSportPerDag");
            this.maxAantalKeerExtreemPerHardlopen = Max0AndSetting(patientValues, "maxAantalKeerExtreemPerHardlopen");
            this.maxDuurExtreemSport = Max0AndSetting(patientValues, "maxDuurExtreemSport");
            this.maxDuurFietsen = Max0AndSetting(patientValues, "maxDuurFietsen");
            this.maxDuurHardlopen = Max0AndSetting(patientValues, "maxDuurHardlopen");
           // this.maxDuurWandelen = Max0AndSetting(patientValues, "maxDuurWandelen");
            this.maxTijdTotWakker = Max0AndSetting(patientValues, "maxTijdTotWakker");
            this.maxTijdVanZittenNaarSlapen = Max0AndSetting(patientValues, "maxTijdVanZittenNaarSlapen");
            this.maxTijdVanZittenNaarSlapenAlsJeMoetGaanSlapen = Max0AndSetting(patientValues, "maxTijdVanZittenNaarSlapenAlsJeMoetGaanSlapen");
            this.maxTijdVanStaanNaarSlapenAlsJeMoetGaanSlapen = Max0AndSetting(patientValues, "maxTijdVanStaanNaarSlapenAlsJeMoetGaanSlapen");
            this.minTijdTussenSporten = Max0AndSetting(patientValues, "minTijdTussenSporten");

            this.kansOpLopen = patientValues["kansOpLopen"];
            this.kansOpSporten = patientValues["kansOpSporten"];
            this.kansOpFietsenVsHardlopen = patientValues["kansOpFietsenVsHardlopen"];
            this.kasnOpZitten = patientValues["kasnOpZitten"];

            this.factorSlapen = patientValues["factorSlapen"];
            this.factorZitten = patientValues["factorZitten"];
            this.factorEten = patientValues["factorEten"];
            this.factorStaan = patientValues["factorStaan"];
            this.factorLopen = patientValues["factorLopen"];
            this.factorFietsen = patientValues["factorFietsen"];
            this.factorHardlopen = patientValues["factorHardlopen"];
            this.factorExtreem = patientValues["factorExtreem"];
            this.factorTraplopen = patientValues["factorTraplopen"];

        }

        private uint Max0AndSetting(Dictionary<string, double> patientValues, string txt)
        {
            return (uint)Math.Max(0, Math.Round(patientValues[txt]));

        }

        public string realDataBaseFolder;
        //public string heartRateFileName;
        public string heartRateGeneratorOption;
        public int realPatientIndex;

        public double factorSlapen;
        public double factorZitten;
        public double factorEten;
        public double factorStaan;
        public double factorLopen;
        public double factorFietsen;
        public double factorHardlopen;
        public double factorTraplopen;
        public double factorExtreem;

        public uint maxAantalKeerSportPerDag;
        public uint maxAantalKeerExtreemPerHardlopen;
        public uint maxDuurExtreemSport;
        public uint maxDuurFietsen;
        public uint maxDuurHardlopen;
      //  public uint maxDuurWandelen;
        public uint maxTijdTotWakker;
        public uint maxTijdVanZittenNaarSlapen;
        public uint maxTijdVanZittenNaarSlapenAlsJeMoetGaanSlapen;
        public uint maxTijdVanStaanNaarSlapenAlsJeMoetGaanSlapen;
        public uint minTijdTussenSporten;

        public double kansOpSporten;
        public double kansOpLopen;
        public double kansOpFietsenVsHardlopen;
        public double kasnOpZitten;
    }
}
