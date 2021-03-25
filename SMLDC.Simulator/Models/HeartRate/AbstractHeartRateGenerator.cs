using SMLDC.Simulator.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SMLDC.Simulator.Models.HeartRate
{
    public abstract class AbstractHeartRateGenerator
    {
        protected int[] heartRateScheme;
        protected uint offset = 0;


        // Get heart rate at given minute of calculation
        public int GetHeartRate(uint calculationMinute)
        {
            return heartRateScheme[(int)(calculationMinute + offset)];
        }

        // Heart rates will always be generated per minute
        public abstract void GenerateScheme(uint totalCalculationMinutes);
        public abstract AbstractHeartRateGenerator Copy(uint offset=0);

        public double GetHrBaseEstimate(uint start, uint end) { return GetHrBaseEstimate((int)start, (int)end); }
        public double GetHrBaseEstimate(int start = -1, int end = -1)
        {
            uint starttime = 0;
            if (start >= 0)
            {
                starttime = (uint)start;
            }
            uint endtime = 0;
            if (end >= 0)
            {
                endtime = (uint)end;
            }
            // alle HR uit hele trail opvragen:
            List<double> heartrates = new List<double>();
            double[] hrHistogram = new double[100];
            uint binBreedte = 5;
            uint[] hrBins = new uint[hrHistogram.Length];
            for (int i = 0; i < hrBins.Length; i++)
            {
                hrBins[i] = (uint)(i * binBreedte);
            }
            for (uint time = starttime; time <= endtime; time++)
            {
                double hr = GetHeartRate(time);
                heartrates.Add(hr);
                int binNr = (int)Math.Round(hr / binBreedte);
                hrHistogram[binNr]++;
            }
            hrHistogram = MyMath.MultiplyWithKernel(hrHistogram, new double[] { 1, 2, 4, 5, 4, 2, 1 }); // iets langer, om deoffset te corrigeren tov rico2. Rico3 lijkt wat vertraagd
            List<int> localOptimaHrHisto = MyMath.FindLocalOptima(hrBins, hrHistogram);
            int eerste_piek = -1;
            for (int i = 0; i < localOptimaHrHisto.Count; i++)
            {
                if (localOptimaHrHisto[i] > 0)
                {
                    eerste_piek = localOptimaHrHisto[i];
                    break;
                }
            }
            double estimatedHR = hrBins[eerste_piek] + binBreedte / 2.0;
            return estimatedHR;
        }
    }

}
