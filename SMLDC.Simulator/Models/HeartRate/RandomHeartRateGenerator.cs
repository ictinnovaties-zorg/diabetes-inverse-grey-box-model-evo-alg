using System;
using System.Collections.Generic;
using System.Text;

using SMLDC.Simulator.Helpers;
using SMLDC.Simulator.Utilities;

namespace SMLDC.Simulator.Models.HeartRate
{
    /* first proof-of-concept of HR generator, built by students. FSMHeartRateSimulator is a more accurate version.
     * Now it is only used for generating a flat HR during binary search etc...
     * TODO: remove?
     */
    public class RandomHeartRateGenerator : AbstractHeartRateGenerator
    {
        private static int minutesInADay = 1440;
        private readonly int _baseHr;
        private int _lastHeartRate;
        private RandomStuff random;
        public RandomHeartRateGenerator(RandomStuff random, int baseHr)
        {
            this.random = random;
            this._baseHr = baseHr;
            this._lastHeartRate = (int)(baseHr * activityIntensityFactors[(int)ActivityIntensity.Sleeping]);
        }

        // Intensity factors for generating heart rates. These factors can be used to generate activity heart rates based on resting heart rate
        // The DeclaredVariables Enums.ActivityIntensity can be used as Index
        public static double[] activityIntensityFactors =
        {
            0.9, // Sleeping
            1.25, // LightActivity
            1.6, // LightlyIntense
            2.05, // ModeratelyIntense
            2.55, // HeavilyIntense
            2.85 // ExtremelyIntense
        };


        public enum ActivityIntensity
        {
            Sleeping = 0,
            LightActivity = 1, // Getting coffee, short walks, cooking food...
            LightlyIntense = 2, // Biking, Walking, Weightlifting...
            ModeratelyIntense = 3, // Running, race cycling ...
            HeavilyIntense = 4, // Sprinting, competitive sports matches...
            ExtremelyIntense = 5 // Running for your life?
        }

        public void UpdateHeartrateScheme(int hr, uint duration) 
        {
            //heartRateScheme.Clear();
            heartRateScheme = new int[duration];
            for (int i = 0; i < duration; i++)
            {
                heartRateScheme[i] = hr;
            }
        }

        public override AbstractHeartRateGenerator Copy(uint offset=0)
        {
            RandomHeartRateGenerator rhr = new RandomHeartRateGenerator(random, this._baseHr);
            rhr.heartRateScheme = this.heartRateScheme;
            rhr.offset = offset;
            return rhr;
        }




        public override void GenerateScheme(uint totalCalculationMinutes)
        {
            GenerateHeartRateScheme(totalCalculationMinutes);
            RandomActivityGenerator();
        }

        public void GenerateHeartRateScheme(uint duration)
        {
            // eindtijd is ook belangrijk (voor solver enz)
            // TODO: fixen, maar nu gewoon grote overloop:
            List<int> hrs = new List<int>();
            for (uint time = 0; time < duration + 24*60*10; time++)
            {
               GenerateHeartRate(hrs, time);
            }
        }

        private void GenerateHeartRate(List<int>hrs, uint currentCalculation)
        {
            // Assuming calculations are always per minute
            int time = (int)currentCalculation % minutesInADay;

            // sleep
            if (time < 420 || time > 1380)
            {
                _lastHeartRate = random.Next((int)(_baseHr * 0.85), (int)(_baseHr * 0.95));
            }

            // Wake up out of bed
            if (time > 420 && time < 435)
            {
                _lastHeartRate = GenerateDeviatedHeartRate(random.GetRandomDoubleFromRange(1.2, 1.3), _lastHeartRate);
            }

            // Awake
            if (time >= 435 && time < 1380)
            {

                _lastHeartRate = GenerateDeviatedHeartRate(random.GetRandomDoubleFromRange(1, 1.1), _lastHeartRate);
            }

            hrs.Add(_lastHeartRate);
        }

        public void RandomActivityGenerator()
        {
            int totalDays = heartRateScheme.Length / minutesInADay;

            // Random activities for each day
            for (int day = 0; day < totalDays; day++)
            {
                int daysAdder = day * minutesInADay;

                // Random walks/activities
                for (int i = 0; i < random.GetNormalDistributed(15, 5, Globals.maxSigma); i++)
                {
                    AddHeartRateActivity(random.Next(450, 960) + daysAdder, random.GetRandomDoubleFromRange(1.5, 2.25), random.Next(3, 20));
                }

                // Dinner
                AddHeartRateActivity(random.Next(990, 1080) + daysAdder, 1.25, 30);

                // Random afternoon Gym Session
                if (random.NextDouble() < 0.3)
                {
                    AddHeartRateActivity(random.Next(900, 960) + daysAdder, random.GetRandomDoubleFromRange(1.65, 2.75), random.Next(60, 90));
                }

                // Random evening activities
                for (int i = 0; i < random.GetNormalDistributed(3, 1, Globals.maxSigma); i++)
                {
                    AddHeartRateActivity(random.Next(1140, 1350) + daysAdder, random.GetRandomDoubleFromRange(1.25, 2.2), random.Next(3, 15));
                }
            }
        }

        private void AddHeartRateActivity(int time, double intensityFactor, int duration)
        {
            for (int i = time; i < time + duration; i++)
            {
                this.heartRateScheme[i] = GenerateDeviatedHeartRate(intensityFactor, this.heartRateScheme[i - 1]);
            }
        }

        // Generates a fluctuating heart rate deviated from last heart rate, based on resting heart rate and with given intensity factor
        private int GenerateDeviatedHeartRate(double intensityFactor, int lastHeartRate) 
        {
            // Creates small deviations between heart rates
            int newRandomHeartRate = (int)random.GetRandomDoubleFromRange(_baseHr * (intensityFactor), _baseHr * (intensityFactor + 0.17));

            // to deviate from last heartrate and prevent huge instant heart rate spikes
            return (newRandomHeartRate * lastHeartRate) / (Math.Min(newRandomHeartRate, lastHeartRate) + (Math.Abs(newRandomHeartRate - lastHeartRate) / 4));
        }
    }
}
