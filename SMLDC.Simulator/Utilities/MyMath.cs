using System;
using System.Collections.Generic;


namespace SMLDC.Simulator.Utilities
{

    class RankingSortData<T2,T> : IComparable<RankingSortData<T2,T>>
    {


        public RankingSortData(T2 ndx, T data)
        {
            this.index = ndx;
            this.data = data;
        }

        public T2 index;
        public T data;

        int IComparable<RankingSortData<T2,T>>.CompareTo(RankingSortData<T2,T> that)
        {
            return Comparer<T>.Default.Compare(this.data, that.data); // Math.Sign( this.data - that.data);
        }
    }

    public class MyMath
    {


        public static double SignPow(double value, double pow)
        {
            if (double.IsNaN(value)) { return value; }
            return Math.Sign(value) * Math.Pow(Math.Abs(value), pow);
        }


        // geeft rank op basis van <T, double>
        public static Dictionary<T, double> GetRanking<T>(Dictionary<T, double> data, bool oplopend = true)
        {
            List<RankingSortData<T, double>> tosort = new List<RankingSortData<T, double>>();
            foreach(T key in data.Keys)
            {
                RankingSortData<T, double> element = new RankingSortData<T, double>(key, data[key]);
                tosort.Add(element);
            }
            tosort.Sort();
            
            Dictionary<T, double> ranking = new Dictionary<T, double>();
            for (int i = 0; i < tosort.Count; i++)
            {
                T ndx = tosort[i].index;
                double rank = (double) (oplopend ? i : tosort.Count - i - 1);
                ranking[tosort[i].index] = rank;
            }
            return ranking;
        }

        // dit kan vast slimmer ..
        public static List<int> GetRanking(List<double> data, bool oplopend=true)
        {
            List<RankingSortData<int, double>> tosort = new List<RankingSortData<int, double>>();
            for(int i  = 0; i < data.Count; i++)
            {
                RankingSortData<int, double> element = new RankingSortData<int, double>(i, data[i]);
                tosort.Add(element);
            }
            tosort.Sort();
            if(!oplopend)
            {
                tosort.Reverse();
            }
            List<int> indices = new List<int>();
            for(int i = 0; i < tosort.Count; i++)
            {
                indices.Add(tosort[i].index);
            }
            return indices;
        }
        





        public static readonly bool USE_ACCURATE_POW = false;
        // fast pow approximation
        // problem: does not seem to accurate. But is ok for rough calculations (it is only used in the gamma function for the Breton calculations)
        // https://martin.ankerl.com/2007/10/04/optimized-pow-approximation-for-java-and-c-c/
        public static double FastPow(double a, double b)
        {
            if (USE_ACCURATE_POW) { return Math.Pow(a, b); }
            int tmp = (int)(BitConverter.DoubleToInt64Bits(a) >> 32);
            int tmp2 = (int)(b * (tmp - 1072632447) + 1072632447);
            return BitConverter.Int64BitsToDouble(((long)tmp2) << 32);
        }



        // [start, end>
        public static double LeastSquaresLineFitSlope(List<double> yValues_full, int start_ndx, int end_ndx)
        {
            if(yValues_full == null ||  start_ndx < 0 || end_ndx > yValues_full.Count)
            {
                return double.NaN; // throw new ArgumentException();
            }
            List<double> xValues = new List<double>();
            List<double> yValues = new List<double>();
            for (int i = start_ndx; i < end_ndx; i++)
            {
                xValues.Add(i);
                yValues.Add(yValues_full[i]);
            }
            return LeastSquaresLineFitSlope(xValues, yValues);
        }


        // [start, end>
        public static double LeastSquaresLineFitSlope(SortedDictionary<uint,double> yValues_full, int start_ndx, int end_ndx)
        {
            if (yValues_full == null || start_ndx < 0 || end_ndx > yValues_full.Count)
            {
                return double.NaN; // throw new ArgumentException();
            }
            List<double> xValues = new List<double>();
            List<double> yValues = new List<double>();
            int i = 0;
            foreach (uint time in yValues_full.Keys)
            {
                if (i >= start_ndx && i < end_ndx)
                {
                    xValues.Add(time);
                    yValues.Add(yValues_full[time]);
                }
                i++;
            }
            return LeastSquaresLineFitSlope(xValues, yValues);
        }





        // nb. 'rico' is 'richtingscoefficient' (direction or slope of line).
        // this calculates a least square fit through the given data.
        public static double LeastSquaresLineFitSlope(List<double>xValues, List<double> yValues)
        {
            if(xValues == null || yValues == null ||  xValues.Count != yValues.Count) { throw new ArgumentException(); }
            // https://www.varsitytutors.com/hotmath/hotmath_help/topics/line-of-best-fit
            //  m = SUM[ ( x_i - X_mean ) (y_i - Y_mean ) ] / SUM[ (x_i - X_mean)^2 ] 
            // alternatieve formulering: https://www.mathsisfun.com/data/least-squares-regression.html 
            // nog niet getest of deze efficienter is.
            double x_mean = 0;
            double y_mean = 0;
            int teller = 0;
            for (int i = 0; i < xValues.Count; i++) 
            {
                if (!double.IsNaN(yValues[i]) && !double.IsInfinity(yValues[i]))
                {
                    x_mean += xValues[i];
                    y_mean += yValues[i];
                    teller++;
                }
            }

            if (teller > 1)
            {
                x_mean /= xValues.Count;
                y_mean /= xValues.Count;
            }
            else {
                return double.NaN;
            }
            
            double boven = 0;
            double onder = 0;
            for (int i = 0; i < xValues.Count; i++)
            {
                boven += ( (xValues[i] - x_mean) * (yValues[i] - y_mean) );
                onder += Math.Pow((xValues[i] - x_mean), 2);
            }


            double rico =  boven / onder;
            if(double.IsInfinity(rico))
            {
                rico = double.NaN;
            }
            return rico;
        }







        // tuple< a, b, c, top_x>
        public static Tuple<double, double, double, double> FindParabolaParameters(double[] x123, double[] y123)
        {
            return FindParabolaParameters(x123[0], x123[1], x123[2], y123[0], y123[1], y123[2]);
        }

        // tuple< a, b, c, top_x>
        public static Tuple<double, double, double, double> FindParabolaParameters(double x1, double x2, double x3, double y1, double y2, double y3)
        {
            //zie : https://sites.math.washington.edu/~conroy/m120-general/quadraticFunctionAlgebra.pdf
            // op papier doorgerekend tot deze formules (en in octave getest).  // werkt (uiteraard) niet als x'en niet uniek zijn
            if (x1 == x2 || x2 == x3) {
                //throw new ArgumentException("werkt niet!: x: " +  x1 + ",  " + x2 + ",  " + x3 ); 
                return null;
            }
            double boven = (y3 - y1) - (x3 - x1) * (y2 - y1) / (x2 - x1);
            double onder = (x3 * x3 - x1 * x1) - ((x3 - x1) * (x2 * x2 - x1 * x1) / (x2 - x1));
            double a = boven / onder;
            double b = ((y2 - y1) - a * (x2 * x2 - x1 * x1)) / (x2 - x1);
            double c = y1 - a * (x1 * x1) - b * x1;
            double minx = -b / (2 * a);
            if(double.IsNaN(minx) || double.IsInfinity(minx))
            {
                return null;
            }
            return new Tuple<double, double, double, double>(a, b, c, minx);
        }

        public static int FindIndexOfHighestValueLowerThanInput(List<uint> values, uint value) {
            if (values.Count == 0) { return -1; } // alles zit ervoor
            if(value < values[0]) { return -1; }
            if(value >= values[values.Count-1]) { return values.Count - 1; }
            int ndx = -1;
            for(int i = 0; i < values.Count; i++)
            {
                if(value < values[i])
                {
                    //we zijn 'm gepasseerd!
                    return i - 1;
                }
            }
            throw new ArgumentException("dit kan niet voorkomen! toch?....");
            return ndx;
        }



        public static double GetMinKey(SortedDictionary<double, double> dict)
        {
            double min_key = double.NaN;
            double min_value = Double.PositiveInfinity;
            foreach(double key in dict.Keys)
            {
                if(dict[key] < min_value)
                {
                    min_value = dict[key];
                    min_key = key;
                }
            }
            return min_key;
        }



        public static int GetMinIndex(List<double> values)
        {
            int min_ndx = 0;
            double min_value = values[0];
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i] < min_value)
                {
                    min_value = values[i];
                    min_ndx = i;
                }
            }
            return min_ndx;
        }

        public static int GetMinIndex(double[] values)
        {
            int min_ndx = 0;
            double min_value = values[0];
            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] < min_value)
                {
                    min_value = values[i];
                    min_ndx = i;
                }
            }
            return min_ndx;
        }

        public static int GetMaxIndex(List<double> values)
        {
            int max_ndx = 0;
            double max_value = values[0];
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i] > max_value)
                {
                    max_value = values[i];
                    max_ndx = i;
                }
            }
            return max_ndx;
        }

        public static int GetMaxIndex(double[] values)
        {
            int max_ndx = 0;
            double max_value = values[0];
            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] > max_value)
                {
                    max_value = values[i];
                    max_ndx = i;
                }
            }
            return max_ndx;
        }
        public static int GetMinIndex(List<int> values)
        {
            int min_ndx = 0;
            int min_value = values[0];
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i] < min_value)
                {
                    min_value = values[i];
                    min_ndx = i;
                }
            }
            return min_ndx;
        }
        public static int GetMaxIndex(List<int> values)
        {
            int max_ndx = 0;
            int max_value = values[0];
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i] > max_value)
                {
                    max_value = values[i];
                    max_ndx = i;
                }
            }
            return max_ndx;
        }


        public static double[] GetCumulativeWeights(double[] relativeWeights)
        {
            // bepaal totaal gewicht (gewogen mbv Math.pow(value, boltzmann) ):
            double[] cumulativeWeights = new double[relativeWeights.Length];
            for (int p = 0; p < relativeWeights.Length; p++)
            {
                cumulativeWeights[p] = relativeWeights[p] + (p > 0 ? cumulativeWeights[p - 1] : 0);
            }
            return cumulativeWeights;
        }

        public static int[] GetIndicesInCumulativeArray(RandomStuff random, double[] relativeWeights, int aantal)
        {
            double[] cumulativeWeights = MyMath.GetCumulativeWeights(relativeWeights);
            double totalWeight = cumulativeWeights[cumulativeWeights.Length - 1];
            int[] indices = new int[aantal];

            for(int i = 0; i < aantal; i++)
            {
                double rndValue = totalWeight * random.NextDouble() * 0.99999999;
                int rndNdx = MyMath.GetIndexInCumulativeArray(cumulativeWeights, rndValue);
                indices[i] = rndNdx;
            }
            return indices;
        }


        // TODO: binary search, ALS dit een cpu-bottleneck is!
        public static int GetIndexInCumulativeArray(double[] cumulative, double value)
        {
            for (int p = 0; p < cumulative.Length; p++)
            {
                if (value <= cumulative[p])
                {
                    return p;
                }
            }
            //    throw new ArgumentException("value = " + value + ", en dat is hoger dan laatste: " + cumulative[cumulative.Length - 1]);
            return -1;
        }


        public static double Clip(double value, double min, double max)
        {
            if (value < min) { return min; }
            else if (value > max) { return max; }
            return value;
        }
        public static int Clip(int value, int min, int max)
        {
            if (value < min) { return min; }
            else if (value > max) { return max; }
            return value;
        }

        public static double[] Multiply(double[] array, double value)
        {
            double[] res = new double[array.Length];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = array[i] * value;
            }
            return res;
        }
        public static double[] Plus(double[] array1, double[] array2)
        {
            double[] res = new double[array1.Length];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = array1[i] + array2[i];
            }
            return res;
        }

        public static double[] Minus(double[] array1, double[] array2)
        {
            double[] res = new double[array1.Length];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = array1[i] - array2[i];
            }
            return res;
        }
        // range: [start, stop> optioneel
        public static double Sum(List<double> values, int startNdx = 0, int stopNdx = -1)
        {
            if (stopNdx < 0 || stopNdx > values.Count)
            {
                stopNdx = values.Count;
            }
            if (startNdx < 0)
            {
                startNdx = 0;
            }

            double sum = 0;
            for (int i = startNdx; i < stopNdx; i++)
            {
                sum += values[i];
            }
            return sum;
        }

        public static List<double> Diffs(List<double> values)
        {
            if (values.Count == 0) { return new List<double>(); }
            List<double> diffs = new List<double>(values.Count - 1);
            for (int i = 0; i < values.Count - 1; i++)
            {
                diffs.Add(values[i + 1] - values[i]);
            }
            return diffs;
        }

        public static SortedDictionary<T, double> MultiplyWithKernel<T>(SortedDictionary<T, double> dictValues, double[] kernel_orig, bool normalize = true)
        {
            List<double> values = new List<double>(dictValues.Count);
            foreach(T key in dictValues.Keys)
            {
                values.Add(dictValues[key]);
            }
            List<double>results = MultiplyWithKernel(values, kernel_orig, normalize);
            SortedDictionary<T, double> resultsDict = new SortedDictionary<T, double>();
            int teller = 0;
            foreach (T key in dictValues.Keys)
            {
                resultsDict[key] = results[teller];
                teller++;
            }
            return resultsDict;
        }


        public static List<double> MultiplyWithKernel(List<double> values, double[] kernel_orig, bool normalize = true)
        {
            double[] arr = MultiplyWithKernel(values.ToArray(), kernel_orig, normalize);
            return new List<double>(arr);
            //TODO; the above is inefficient, but it is not used much, so no problem (yet).
        }



        public static double[] MultiplyWithKernel(double[] values, double[] kernel_orig, bool normalize = true)
        {
            double[] kernel = new double[kernel_orig.Length];
            if (normalize)
            {
                double kernelsum = 0;
                for (int i = 0; i < kernel_orig.Length; i++)
                {
                    kernelsum += kernel_orig[i];
                }
                for (int i = 0; i < kernel_orig.Length; i++)
                {
                    kernel[i] = kernel_orig[i] / kernelsum;
                }
            }
            else
            {
                Array.Copy(kernel_orig, kernel, kernel_orig.Length);
            }
            uint range = (uint)kernel.Length;
            uint rangeLinks = range / 2;
            uint rangeRechts = range / 2;
            if (rangeLinks + rangeRechts < range - 1)
            {
                rangeLinks++;
            }
            double[] smoothedValues = new double[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                double sum = 0;
                int teller = 0;
                for (int j = Math.Max(0, i - (int)rangeLinks); j < Math.Min(i + rangeRechts, values.Length); j++)
                {
                    sum += values[j] * kernel[teller];
                    teller++;
                }
                smoothedValues[i] = sum;
            }
            return smoothedValues;
        }



        public static List<double> Smoothing(List<double> values, uint range)
        {
            //smoothing
            uint rangeLinks = range / 2;
            uint rangeRechts = range / 2;
            if(rangeLinks + rangeRechts < range - 1)
            {
                rangeLinks++;
            }
            List<double> smoothedValues = new List<double>();
            for (int i = 0; i < values.Count; i++)
            {
                double sum = 0;
                int teller = 0;
                for (int j = Math.Max(0, i - (int)rangeLinks); j < Math.Min(i + rangeRechts + 1, values.Count); j++)
                {
                    teller++;
                    sum += values[j];
                }
                smoothedValues.Add(sum / teller);
            }
            return smoothedValues;
        }

        public static void AddOffsetToItem1(List<Tuple<int, double>> list, int offset)
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = new Tuple<int, double>(list[i].Item1 + offset, list[i].Item2);
            }
        }

        // returns list met ints. De ints zijn de INDICES van de (lokale) optima.
        // een minimum wordt met een negatieve index gecodeerd.
        public static List<int> FindLocalOptima(uint[] tijden, double[] outputs)
        {
            return FindLocalOptima(new List<uint>(tijden), new List<double>(outputs));
        }
        public static List<int> FindLocalOptima(List<uint> tijden, List<double> outputs)
        {
            if (tijden.Count != outputs.Count)
            {
                throw new ArgumentException("FindLocalOptima: Counts ongelijk!");
            }
            if(tijden.Count == 0) { return new List<int>(); }
            double prev = outputs[0];
            int prevDir = 0;
            List<int> optima = new List<int>();
            int plateau_teller = 0;

            for (int g = 0; g < tijden.Count; g++)
            {

                double current = outputs[g];
                if (double.IsInfinity(current) || double.IsNaN(current))
                {
                    break;
                }
               // if (Double.IsNaN(prev) || double.IsInfinity(prev))
               // {
               //     dir = 0;
               //     prev = current;
               // }
                else
                {

                    int curDir = Math.Sign(current - prev);
                    if(curDir == 0)
                    {
                        // plateau: zelfde waarde als vorige.
                        plateau_teller++;
                        // niet de prev updaten, die houden we op de richting voordat we het plateau tegenkwamen
                    }
                    else
                    {
                        if (curDir != prevDir)  // sign flip in afgeleide
                        {
                            int piek = current < prev ? 1 : -1; // dal als -index, piek als +index opslaan
                                                                // en vergeet niet om het punt VOOR index g te gebruiken, want g zelf is NA het min. of max.!
                            optima.Add((g - plateau_teller/2 - 1) * piek);
                        }
                        prevDir = curDir;
                        plateau_teller = 0;
                    }
                }
                prev = current;
            }

            return optima;
        }

    }

}
