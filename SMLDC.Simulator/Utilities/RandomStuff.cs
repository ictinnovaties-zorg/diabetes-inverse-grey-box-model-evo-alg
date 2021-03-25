using System;
using System.Collections.Generic;
using System.Threading;
using MathNet.Numerics.Distributions;


namespace SMLDC.Simulator.Utilities
{
    public class RandomStuff
    {
        private int randomSeed = -1;//41046; // < 0: automatisch (dus geen predefined seed). >= 0: predefined.
        private Random randomGenerator;
        private Normal normalDistRandomGenerator; //call: randomGaussianValue = normalDist.Sample();


        public RandomStuff(int randSeed=-1)
        {
            if (randSeed >= 0)
            {
                randomSeed = randSeed;
            }
            _Init();
        }

        private void _Init()
        {
            if (randomGenerator == null)
            {
                if (randomSeed < 0)
                {
                    Console.WriteLine("... new random seed ...");
                    Thread.Sleep(10); // om te garanderen dat 2x new RandomStuff(-1) niet dezelfde rnd seed geeft.
                    randomSeed = (new Random(DateTime.Now.Millisecond)).Next(0, 1000000);
                }
                //else
                //{
                //    string txt = "*********************************** randomSeed is NIET random :-) ******************************************";
                //    for (int i = 0; i < 10; i++)
                //    {
                //        Console.WriteLine(txt);
                //    }
                //}
                randomGenerator = new Random(randomSeed);
                normalDistRandomGenerator = new Normal(0, 1, randomGenerator);
              //  Print();
            }
        }

        public int GetRandomSeed() { return randomSeed; }

        public void Print()
        {
            Console.WriteLine(RandomSeedToString());
        }

        public String RandomSeedToString()
        {
            return (">>>>> RandomStuff::randomSeed = " + randomSeed + " <<<<<");
        }

        //maak lijst (count = size) met random getallen [min, max>
        public List<int> RandomList(int size, int min, int max)
        {
            List<int> lijst = new List<int>(size);
            for (int s = 0; s < size; s++)
            {
                lijst.Add(NextInt(min, max));
            }
            return lijst;
        }



        //maak lijst (count = size) met unieke random getallen [min, max>
        public List<int> RandomUniqueList(uint min, uint max, int size = -1)
        {
            return RandomUniqueList((int)min, (int)max, size);
        }
        public List<int> RandomUniqueList(int min, int max, int size = -1)
        {
            if (size < 0)
            {
                size = max - min;
            }
            if (max - min < size)
            {
                throw new ArgumentException("RandomUniqueList(" + size + ", " + min + ", " + max + ") kan niet!");
            }
            HashSet<int> set = new HashSet<int>();
            List<int> lijst = new List<int>(size);
            while (true)
            {
                int rnd = NextInt(min, max);
                if (!set.Contains(rnd))
                {
                    lijst.Add(rnd);
                    set.Add(rnd);
                    if (lijst.Count == size)
                    {
                        return lijst;
                    }
                }
            }
        }


        public double GetRandomDoubleFromRange(double min, double max)
        {
            lock (normalDistRandomGenerator) { return randomGenerator.NextDouble() * (max - min) + min; ; }
        }

        public double NextDouble()
        {
            _Init();
            lock (normalDistRandomGenerator)
            {
                return randomGenerator.NextDouble();
            }
        }


        public bool NextBool(double trueProb = 0.5)
        {
            _Init();
            lock (normalDistRandomGenerator)
            {
                return (randomGenerator.NextDouble() <= trueProb);
            }
        }


        public int Next() { return NextInt(); }
        public int NextInt()
        {
            _Init();
            lock (normalDistRandomGenerator)
            {
                return randomGenerator.Next();
            }
        }

        public int Next(uint max) { return NextInt((int)max); }
        public int Next(int max) { return NextInt(max); }
        public int NextInt(uint max) { return NextInt((int)max); }
        public int NextInt(int max)
        {
            _Init();
            lock (normalDistRandomGenerator)
            {
                return randomGenerator.Next(max);
            }
        }

        public int Next(int min, int max) { return NextInt(min, max); }
        public int Next(uint min, uint max) { return NextInt((int)min, (int)max); }
        public int NextInt(uint min, uint max) { return NextInt((int)min, (int)max); }
        public int NextInt(int min, int max)
        {
            _Init();
            lock (normalDistRandomGenerator)
            {
                return randomGenerator.Next(min, max);
            }
        }

        public double Sample(double mean = 0, double stddev = 1) { return NormalDistributionSample(mean, stddev); }
        public double NormalDistributionSample(double mean = 0, double stddev = 1)
        {
            _Init();
            double rnd;
            lock (normalDistRandomGenerator)
            {
                rnd = mean + stddev * normalDistRandomGenerator.Sample(); // mean=0, std=1// mean=0, std=1
            }
            return rnd;
        }

        public double GetNormalDistributed(double mean, double standardDeviation, double sigma = Double.MaxValue)
        {
            return NormalDistributionSampleClipped(sigma, mean, standardDeviation);
        }


        public double NormalDistributionSampleClipped(double maxNrStddevs, double mean = 0, double stddev = 1)
        {
            _Init();
            double rnd;
            lock (normalDistRandomGenerator)
            {
                rnd = normalDistRandomGenerator.Sample();
            }
            rnd = Math.Max(-maxNrStddevs, Math.Min(rnd, maxNrStddevs));
            rnd = rnd * stddev;
            return mean + rnd; // mean=0, std=1// mean=0, std=1
        }



        // uniform verdeeld over het LOG-bereik:
        public double GetRandomValue_UniformInLogRange(double minValue, double maxValue)
        {
            if (minValue > maxValue) { throw new ArgumentException("minValue: " + minValue + " > maxValue: " + maxValue); }
            if (minValue == maxValue) { return minValue; } //er weinig keuze
            double rndValue = NextDouble();
            double logMinValue = Math.Log10(minValue);
            double logMaxValue = Math.Log10(maxValue);
            double logRange = logMaxValue - logMinValue;
            double rndInRange = logMinValue + logRange * rndValue;
            return Math.Pow(10, rndInRange);
        }

        public double GetRandomValue_UniformInRange(double minValue, double maxValue)
        {
            if (minValue >= maxValue) { throw new ArgumentException("minValue: " + minValue + " >= maxValue: " + maxValue); }
            double rndValue = NextDouble();
            return minValue + rndValue * (maxValue - minValue);
        }
    }
}
