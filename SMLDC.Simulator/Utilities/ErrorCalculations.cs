using System;
using System.Collections.Generic;
using System.Text;
using SMLDC.Simulator.DiffEquations.Models;
using SMLDC.Simulator.Models;

namespace SMLDC.Simulator.Utilities
{



    public class ErrorCalculationSettings
    {
        public ErrorCalculationSettings(double pow, bool logspace, List<Tuple<int, int>> timeslots = null)
        {
            this.SSE_POWER_FOR_ERROR_CALC = pow;
            this.ErrorCalcInLogSpace = logspace;
            this.timeSlots = timeslots;

        }

        public List<Tuple<int, int>> timeSlots;
        // todo: efficient, binary search???
        public bool InTimeSlot(uint time)
        {
            if (timeSlots == null) { return true; }  //altijd goed :-)
            foreach(Tuple<int, int> timeslot in timeSlots)
            {
                if(time >= timeslot.Item1 && time < timeslot.Item2) { 
                    return true;
                }
                // aanname: op volgorde!-- early abort?
            }
            return false;
        }
        public static readonly int RMSE_noisy_after_carbs_per_bin_size = 15; //todo: param van maken? lage prio
        public static readonly int bovenMiddenonderMarge = 10;
        public bool USE_HOOGTEFACTOR_IN_RMSE = false;
        public double SSE_POWER_FOR_ERROR_CALC = 2;
        public bool ErrorCalcInLogSpace = false;
    }



    public class ErrorDataContainer
    {
        public ErrorData ml2Mmnts_TargetReal_rico;  // target: real;  voor errors van (noisy) measurements tov de echte (VIP, true) data.
        public ErrorData ml2Mmnts_TargetReal;  // target: real;  voor errors van (noisy) measurements tov de echte (VIP, true) data.
        public ErrorData ml2SmoothedMmnts_TargetSmoothed; // target: smoothed(mmnts);  errors t.o.v de smoothed noisy measurements


    //    public ErrorData ml2SmoothedMmnts_TargetReal; // target: smoothed(mmnts);  errors t.o.v de smoothed noisy measurements
    //    public ErrorData ml2Mmnts_TargetSmoothed;  // target: real;  voor errors van (noisy) measurements tov de echte (VIP, true) data.


        public ErrorDataContainer()
        {
            ml2Mmnts_TargetReal = new ErrorData();
            ml2Mmnts_TargetReal_rico = new ErrorData();
            ml2SmoothedMmnts_TargetSmoothed = new ErrorData();
       //     ml2SmoothedMmnts_TargetReal = new ErrorData();
       //     ml2Mmnts_TargetSmoothed = new ErrorData();
        }

        public ErrorDataContainer DeepCopy()
        {
            ErrorDataContainer that = new ErrorDataContainer();
            that.ml2Mmnts_TargetReal = this.ml2Mmnts_TargetReal.DeepCopy();
            that.ml2Mmnts_TargetReal_rico = this.ml2Mmnts_TargetReal_rico.DeepCopy();
            that.ml2SmoothedMmnts_TargetSmoothed = this.ml2SmoothedMmnts_TargetSmoothed.DeepCopy();
//            that.ml2SmoothedMmnts_TargetReal = this.ml2SmoothedMmnts_TargetReal.DeepCopy();
  //          that.ml2Mmnts_TargetSmoothed = this.ml2Mmnts_TargetSmoothed.DeepCopy();

            return that;
        }
    }



    public class ErrorData
    {
        // error vanaf de (smoothed) noisy mmnts tov true (underlying) virtual patient data.
        public double RMSE_Mmnts2Target;

        // error vanaf mmnts naar de ML.
        public double RMSE_ML2Mmnts;

        // error vanaf ML naar real (of smoothed) mmnts
        public double RMSE_ML2Target;

        public double Weighted_RMSE_ML2Mmnts_after_carbs_per_bin;
        public Dictionary<int, double> RMSE_ML2Mmnt_after_carbs_per_bin;

   //     private Dictionary<int, double> RMSE_noisy_after_carbs = new Dictionary<int, double>();
   //     private Dictionary<int, Dictionary<int, double>> RMSE_noisy_after_carbs_per_bin = new Dictionary<int, Dictionary<int, double>>();

        //        public int nanDataTeller = 0;
        public int nietNanDataTeller = 0;
        public int bovenTelling = 0;
        public int middenTelling = 0;
        public int onderTelling = 0;
        public double bovenZwaartepunt = 0;
        public double middenZwaartepunt = 0;
        public double onderZwaartepunt = 0;


        //public double realLowerThanPredictedAtMinimum;
        //public double realHigherThanPredictedAtMinimum;
        //public double realLowerThanPredictedAtMaximum;
        //public double realHigherThanPredictedAtMaximum;

        public double max_LowerThanPredictedAtMinimum;
        public double max_HigherThanPredictedAtMinimum;
        public double max_LowerThanPredictedAtMaximum;
        public double max_HigherThanPredictedAtMaximum;

        public double rmse_LowerThanPredictedAtMinimum;
        public double rmse_HigherThanPredictedAtMinimum;
        public double rmse_LowerThanPredictedAtMaximum;
        public double rmse_HigherThanPredictedAtMaximum;



        public ErrorData DeepCopy()
        {
            ErrorData that = new ErrorData();
            //that.realLowerThanPredictedAtMaximum = this.realLowerThanPredictedAtMaximum;
            //that.realHigherThanPredictedAtMaximum = this.realHigherThanPredictedAtMaximum;
            //that.realLowerThanPredictedAtMinimum = this.realLowerThanPredictedAtMinimum;
            //that.realHigherThanPredictedAtMinimum = this.realHigherThanPredictedAtMinimum;

            that.max_LowerThanPredictedAtMaximum = this.max_LowerThanPredictedAtMaximum;
            that.max_HigherThanPredictedAtMaximum = this.max_HigherThanPredictedAtMaximum;
            that.max_LowerThanPredictedAtMinimum = this.max_LowerThanPredictedAtMinimum;
            that.max_HigherThanPredictedAtMinimum = this.max_HigherThanPredictedAtMinimum;

            that.rmse_LowerThanPredictedAtMaximum = this.rmse_LowerThanPredictedAtMaximum;
            that.rmse_HigherThanPredictedAtMaximum = this.rmse_HigherThanPredictedAtMaximum;
            that.rmse_LowerThanPredictedAtMinimum = this.rmse_LowerThanPredictedAtMinimum;
            that.rmse_HigherThanPredictedAtMinimum = this.rmse_HigherThanPredictedAtMinimum;

            that.RMSE_Mmnts2Target = this.RMSE_Mmnts2Target;
            that.RMSE_ML2Target = this.RMSE_ML2Target;
            that.RMSE_ML2Mmnts = this.RMSE_ML2Mmnts;
            that.Weighted_RMSE_ML2Mmnts_after_carbs_per_bin = this.Weighted_RMSE_ML2Mmnts_after_carbs_per_bin;
            that.RMSE_ML2Mmnt_after_carbs_per_bin = new Dictionary<int, double>();
            if (RMSE_ML2Mmnt_after_carbs_per_bin != null)
            {
                foreach (var key in this.RMSE_ML2Mmnt_after_carbs_per_bin.Keys)
                {
                    that.RMSE_ML2Mmnt_after_carbs_per_bin[key] = this.RMSE_ML2Mmnt_after_carbs_per_bin[key];
                }
            }

            that.nietNanDataTeller = this.nietNanDataTeller;
            that.bovenTelling = this.bovenTelling;
            that.middenTelling = this.middenTelling;
            that.onderTelling = this.onderTelling;
            that.bovenZwaartepunt = this.bovenZwaartepunt;
            that.middenZwaartepunt = this.middenZwaartepunt;
            that.onderZwaartepunt = this.onderZwaartepunt;
           
            return that;
        }

        public bool VooralBoven(double factor = 1)
        {
            return (bovenTelling > factor * middenTelling && bovenTelling > factor * onderTelling);
        }
        public bool VooralOnder(double factor = 1)
        {
            return (onderTelling > factor * middenTelling && onderTelling > factor * bovenTelling);
        }
        public bool BovenZitLinks()
        {
            return (bovenZwaartepunt < onderZwaartepunt && bovenZwaartepunt < middenZwaartepunt);
        }
        public bool OnderZitLinks()
        {
            return (onderZwaartepunt < middenZwaartepunt && onderZwaartepunt < bovenZwaartepunt);
        }
   
    
    
    //}


    //public class ErrorCalculations
    //{




        // TODO: refactor zodat ErrorData methodes gebruikt worden voor _COUNT enz.?? ultra lage prio
        public static ErrorDataContainer CalculateRmseOnObservations(List<uint> carbTimes, List<uint> mmntTimes, List<double> mlPredictions, List<double> trueValues, List<double> mmnts, List<double> smoothedMmnts, ErrorCalculationSettings settings, bool Moet_ml2SmoothedMmnts_TargetSmoothed_BerekendWorden)
        {
            ErrorDataContainer errorDataContainer = new ErrorDataContainer();
            errorDataContainer.ml2Mmnts_TargetReal.CalculateRmseOnObservations(            carbTimes, mmntTimes, mlPredictions, trueValues,    mmnts,         settings);

            //// als 'reference' niet de true, maar de smoothed noisy nemen.
            //errorDataContainer.ml2Mmnts_TargetSmoothed.CalculateRmseOnObservations(        carbTimes, mmntTimes, mlPredictions, smoothedMmnts, mmnts,         settings);

            //// input: smoothed(mmnts), target: real
            // errorDataContainer.ml2SmoothedMmnts_TargetReal.CalculateRmseOnObservations(    carbTimes, mmntTimes, mlPredictions, trueValues,    smoothedMmnts, settings);

            if (Moet_ml2SmoothedMmnts_TargetSmoothed_BerekendWorden)
            {
                errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.CalculateRmseOnObservations(carbTimes, mmntTimes, mlPredictions, smoothedMmnts, smoothedMmnts, settings);
            }
            return errorDataContainer;
        }

        private void CalculateRmseOnObservations(List<uint> carbTimes, List<uint> mmntTimes, List<double> mlPredictions, List<double> targetValues, List<double> measuredVaules, ErrorCalculationSettings settings)
        {
            //outputs:
            Dictionary<int, int> RMSE_noisy_after_carbs_COUNT = new Dictionary<int, int>();
            Dictionary<int, Dictionary<int, int>> RMSE_ML2Mmnt_after_carbs_per_bin_COUNT = new Dictionary<int, Dictionary<int, int>>();
            Dictionary<int, int> prevCarbTime = new Dictionary<int, int>();
            prevCarbTime[-1] = -1;

            this.RMSE_ML2Mmnt_after_carbs_per_bin = new Dictionary<int, double>();
            this.RMSE_ML2Mmnt_after_carbs_per_bin[-1] = 0;
            RMSE_noisy_after_carbs_COUNT[-1] = 1;

            Dictionary<int, Dictionary<int, double>> RMSE_ML2Mmnt_after_carbs_per_bin = new Dictionary<int, Dictionary<int, double>>();
            RMSE_ML2Mmnt_after_carbs_per_bin[-1] = new Dictionary<int, double>();
            RMSE_ML2Mmnt_after_carbs_per_bin_COUNT[-1] = new Dictionary<int, int>();
            List<uint> carbTimesUsedAsKeys = new List<uint>(); // een deel van de carb events uit schedule kunnen buiten gemeten gedeelte liggen. Uitsluiten
            int prev = -1;
            foreach (int key in carbTimes)
            {
                if( key < mmntTimes[0] || key > mmntTimes[mmntTimes.Count -1] )
                {
                    continue;
                }
                this.RMSE_ML2Mmnt_after_carbs_per_bin[key] = 0;
                RMSE_noisy_after_carbs_COUNT[key] = 0;
                carbTimesUsedAsKeys.Add((uint) key);
                // if (prev >= 0)
                {
                    prevCarbTime[key] = prev;
                    prev = key;
                }
                // _per_bin ook initialiseren?
                RMSE_ML2Mmnt_after_carbs_per_bin[key] = new Dictionary<int, double>();
                RMSE_ML2Mmnt_after_carbs_per_bin_COUNT[key] = new Dictionary<int, int>();
            }



            double bovenZwaartepunt_weight = 0;
            double middenZwaartepunt_weight = 0;
            double onderZwaartepunt_weight = 0;
            int nietNanDataTeller = 0;

            // afgeleide van MLcurve en Smoothed mmnts bepalen en RMSE daarop doen.
            // hoe? vantevoren? of bij 'doorlopen' v/d tijden?



           // double[] particleMmntPredictions_cached = null; // niet steeds nieuwe array aanmaken in SolverResult!
            for (int i = 0; i < mmntTimes.Count; i++)
            {
                uint time = mmntTimes[i];
                if(!settings.InTimeSlot(time))
                {
                    continue;
                }
                ////                bool found = predictionPatient.TryGetDataFromTime(time, out double[] particleMmntPredictions);
                //                bool found = predictionPatient.TryGetDataFromTime_reuse(time, ref particleMmntPredictions_cached);
                //                if (found == false || particleMmntPredictions_cached == null)
                //                {
                //                    continue;
                //                }
                //                double particleMmntPrediction = particleMmntPredictions_cached[BergmanAndBretonModel.G_Glucose_ODEindex_MG_per_DL];

                double particleMmntPrediction = mlPredictions[i];
                if (Double.IsNaN(particleMmntPrediction))
                {
                  //  this.RMSE_ML2Mmnts = double.MaxValue;
                  //  this.RMSE_ML2Target = double.MaxValue;
                    continue;
                }
                double gMmnt = measuredVaules[i];
                if (Double.IsNaN(gMmnt))
                {
                    break; // dit is nog niet gemeten. 
                }
                double diff;
                if(settings.ErrorCalcInLogSpace)
                {
                    diff = LogDiff(particleMmntPrediction, gMmnt);
                }
                else {
                    diff = particleMmntPrediction - gMmnt;
                }

                if (diff > ErrorCalculationSettings.bovenMiddenonderMarge)
                {
                    //boven
                    this.bovenTelling++;
                    this.bovenZwaartepunt += Math.Abs(diff) * time;
                    bovenZwaartepunt_weight += Math.Abs(diff);
                }
                else if (diff < -ErrorCalculationSettings.bovenMiddenonderMarge)
                {
                    //onder
                    this.onderTelling++;
                    this.onderZwaartepunt += Math.Abs(diff) * time;
                    onderZwaartepunt_weight += Math.Abs(diff);
                }
                else
                {
                    //midden
                    this.middenTelling++;
                    this.middenZwaartepunt += time;
                    middenZwaartepunt_weight += 1;
                }

                //double signFactor = 1;
               // if (diff > 0) { signFactor = settings.LobSidedErrorFactor; } // waar de gemeten waarde kleiner is dan de predictie: zwaarder afstraffen
                //double thisRMSE_ML2Mmnt = Math.Abs(Math.Pow(Math.Abs(diff), signFactor * settings.SSE_POWER_FOR_ERROR_CALC));
                double thisRMSE_ML2Mmnt = Math.Abs(Math.Pow(Math.Abs(diff), settings.SSE_POWER_FOR_ERROR_CALC));

                //TODO: hoogtefactor weer gaan gebruiken?
                double hoogteFactor = 1;
                if (settings.USE_HOOGTEFACTOR_IN_RMSE)
                {
                    hoogteFactor = 1 - (gMmnt - 175) / 250;
                    hoogteFactor = MyMath.Clip(hoogteFactor, 0, 1);
                }

                this.RMSE_ML2Mmnts += thisRMSE_ML2Mmnt * hoogteFactor;



                // zoek eerstVORIGE carb event

                int carb_start_time = -1;
                int ndx_before_time = MyMath.FindIndexOfHighestValueLowerThanInput(carbTimesUsedAsKeys, time);
                if (ndx_before_time >= 0)
                {
                    carb_start_time = (int)carbTimesUsedAsKeys[ndx_before_time];
                }
                this.RMSE_ML2Mmnt_after_carbs_per_bin[carb_start_time] += thisRMSE_ML2Mmnt * hoogteFactor;
                RMSE_noisy_after_carbs_COUNT[carb_start_time]++;
                // bepaal de juiste bin binnen deze carb curve:
                int bin_ndx = (int)(Math.Round(gMmnt)) / ErrorCalculationSettings.RMSE_noisy_after_carbs_per_bin_size;
                if (!RMSE_ML2Mmnt_after_carbs_per_bin[carb_start_time].ContainsKey(bin_ndx))
                {
                    RMSE_ML2Mmnt_after_carbs_per_bin[carb_start_time][bin_ndx] = 0;
                    RMSE_ML2Mmnt_after_carbs_per_bin_COUNT[carb_start_time][bin_ndx] = 0;
                }
                RMSE_ML2Mmnt_after_carbs_per_bin[carb_start_time][bin_ndx] += thisRMSE_ML2Mmnt;
                RMSE_ML2Mmnt_after_carbs_per_bin_COUNT[carb_start_time][bin_ndx]++;

                prevCarbTime.TryGetValue(carb_start_time, out int prev_key);
                if (prev_key >= 0) // huidige carb heeft OOK errors als gevolg van vorige
                {
                    this.RMSE_ML2Mmnt_after_carbs_per_bin[prev_key] += thisRMSE_ML2Mmnt * hoogteFactor * 0.5;
                    RMSE_noisy_after_carbs_COUNT[prev_key]++;
                }

                ///-------------- t.o.v. real ---------------------------

                if (targetValues != null)
                {
                    if (settings.ErrorCalcInLogSpace)
                    {
                        diff = LogDiff(particleMmntPrediction, targetValues[i]);
                    }
                    else
                    {
                        diff = particleMmntPrediction - targetValues[i];
                    }
                    this.RMSE_ML2Target += Math.Abs(Math.Pow(Math.Abs(diff), settings.SSE_POWER_FOR_ERROR_CALC));

                    ///----------- noisy mmnts tov de real data ------
                    if (settings.ErrorCalcInLogSpace)
                    {
                        diff = LogDiff(measuredVaules[i], targetValues[i]);
                    }
                    else
                    {
                        diff = measuredVaules[i] - targetValues[i];
                    }
                    this.RMSE_Mmnts2Target += Math.Abs(Math.Pow(Math.Abs(diff), settings.SSE_POWER_FOR_ERROR_CALC));
                }

                nietNanDataTeller++;
            }




            this.RMSE_Mmnts2Target = Math.Sqrt(this.RMSE_Mmnts2Target / nietNanDataTeller);
            this.RMSE_ML2Mmnts = Math.Sqrt(this.RMSE_ML2Mmnts / nietNanDataTeller);
            this.RMSE_ML2Target = Math.Sqrt(this.RMSE_ML2Target / nietNanDataTeller);

            double visuele_schalingsfactor = 500;
            if (settings.ErrorCalcInLogSpace)
            {
                this.RMSE_Mmnts2Target = visuele_schalingsfactor * RMSE_Mmnts2Target; // om redelijke schaling in grafiek te krijgen
                this.RMSE_ML2Mmnts = visuele_schalingsfactor * RMSE_ML2Mmnts; // om redelijke schaling in grafiek te krijgen
                this.RMSE_ML2Target = visuele_schalingsfactor * RMSE_ML2Target; // om redelijke schaling in grafiek te krijgen
                //this.RMSE_Mmnts2Target = Math.Pow(10, this.RMSE_Mmnts2Target);
                //this.RMSE_ML2Mmnts = Math.Pow(10, this.RMSE_ML2Mmnts);
                //this.RMSE_ML2Target = Math.Pow(10, this.RMSE_ML2Target);
            }
            //this.RMSE_Smooothed_Noisy_Mmnts = Math.Sqrt(this.RMSE_Smooothed_Noisy_Mmnts / nietNanDataTeller);
            this.nietNanDataTeller = nietNanDataTeller;

            this.bovenZwaartepunt /= bovenZwaartepunt_weight;
            this.middenZwaartepunt /= middenZwaartepunt_weight;
            this.onderZwaartepunt /= onderZwaartepunt_weight;

            this.Weighted_RMSE_ML2Mmnts_after_carbs_per_bin = 0;
            int teller = 0;
            foreach (int key in RMSE_noisy_after_carbs_COUNT.Keys)
            {
                //per bin 
                double rmse_per_bin = 0;
                foreach (int bin in RMSE_ML2Mmnt_after_carbs_per_bin[key].Keys)
                {
                    rmse_per_bin += (RMSE_ML2Mmnt_after_carbs_per_bin[key][bin] / RMSE_ML2Mmnt_after_carbs_per_bin_COUNT[key][bin]);
                }

                if (RMSE_ML2Mmnt_after_carbs_per_bin[key].Keys.Count > 0)
                {
                    this.RMSE_ML2Mmnt_after_carbs_per_bin[key] = Math.Sqrt(rmse_per_bin / RMSE_ML2Mmnt_after_carbs_per_bin[key].Keys.Count);
                    if(settings.ErrorCalcInLogSpace)
                    {
//                        this.RMSE_ML2Mmnt_after_carbs_per_bin[key] = Math.Pow(10, this.RMSE_ML2Mmnt_after_carbs_per_bin[key]);
                        this.RMSE_ML2Mmnt_after_carbs_per_bin[key] = visuele_schalingsfactor *  this.RMSE_ML2Mmnt_after_carbs_per_bin[key]; // visuele schaling, meer niet
                    }
                }
                else
                {
                    this.RMSE_ML2Mmnt_after_carbs_per_bin[key] = 0;
                }
                this.Weighted_RMSE_ML2Mmnts_after_carbs_per_bin += this.RMSE_ML2Mmnt_after_carbs_per_bin[key];
                
                teller++;
            }
            this.Weighted_RMSE_ML2Mmnts_after_carbs_per_bin = this.Weighted_RMSE_ML2Mmnts_after_carbs_per_bin / teller;


            if (Double.IsNaN(this.RMSE_ML2Mmnts) || Double.IsNaN(this.RMSE_ML2Target) || Double.IsNaN(this.Weighted_RMSE_ML2Mmnts_after_carbs_per_bin))
            {
                this.RMSE_ML2Mmnts = double.PositiveInfinity;
                this.RMSE_Mmnts2Target = double.PositiveInfinity;
                this.RMSE_ML2Target = double.PositiveInfinity;
                this.Weighted_RMSE_ML2Mmnts_after_carbs_per_bin = double.PositiveInfinity;
            }

        }




        private static double LogDiff(double x, double y)
        {
//            return Math.Max(Math.Log10(Math.Abs(x - y) + 1), 0);
            return Math.Log10(x) - Math.Log10(y);
        }


        public static Tuple<double[], double[]> CalcUndershootOvershoot(List<uint> times, List<double> values, VirtualPatient patient1, double GLUC_THRESHOLD_FOR_UNDERSHOOT)
        {
            uint starttime = times[0];
            uint endtime = times[times.Count - 1];
            Tuple<List<uint>, List<double>> timesAndValues = new Tuple<List<uint>, List<double>>(times, values);
            //Tuple <List<int>, List<double>> referenceMeasurementsInRange = patient1.GetTimeTrueMeasurementTupleInRange(starttime, endtime);
            Tuple<List<uint>, List<double>> otherMeasurementsInRange = patient1.GetGeneratedDataForTimestamps(times);

            Tuple<double[], double[]> res1Tuple = CalcDiffAtLocalMinima(timesAndValues, otherMeasurementsInRange, GLUC_THRESHOLD_FOR_UNDERSHOOT);
            Tuple<double[], double[]> res2Tuple = CalcDiffAtLocalMinima(otherMeasurementsInRange, timesAndValues, GLUC_THRESHOLD_FOR_UNDERSHOOT);

            //orig: ALLEEN maximale over/undershoot:
            //at min:
            double max_realLowerThanPredictedAtMinimum = -Math.Max(res1Tuple.Item1[0], res2Tuple.Item1[1]); // real < predicted op minimum --> risk, want er lijkt niks aan de hand, maar echte Gluc is veel te laag.
            double max_realHigherThanPredictedAtMinimum = Math.Max(res1Tuple.Item1[1], res2Tuple.Item1[0]); // real > predicted op minimum --> ok
            // at max:
            double max_realLowerThanPredictedAtMaximum = -Math.Max(res1Tuple.Item1[2], res2Tuple.Item1[3]); // real < predicted op MAXimum --> ok
            double max_realHigherThanPredictedAtMaximum = Math.Max(res1Tuple.Item1[3], res2Tuple.Item1[2]); // real > predicted op MAXimum --> risk: echte Gluc veel hoger dan berekend. (is minder risk, maar wel interessant)

            //VERBTERING: rmse over alle under/overshoots
            double rmse_realLowerThanPredictedAtMinimum = -Math.Max(res1Tuple.Item2[0], res2Tuple.Item2[1]); // real < predicted op minimum --> risk, want er lijkt niks aan de hand, maar echte Gluc is veel te laag.
            double rmse_realHigherThanPredictedAtMinimum = Math.Max(res1Tuple.Item2[1], res2Tuple.Item2[0]); // real > predicted op minimum --> ok
            // at max:
            double rmse_realLowerThanPredictedAtMaximum = -Math.Max(res1Tuple.Item2[2], res2Tuple.Item2[3]); // real < predicted op MAXimum --> ok
            double rmse_realHigherThanPredictedAtMaximum = Math.Max(res1Tuple.Item2[3], res2Tuple.Item2[2]); // real > predicted op MAXimum --> risk: echte Gluc veel hoger dan berekend. (is minder risk, maar wel interessant)



            return new Tuple<double[], double[]>(
                new double[] { max_realLowerThanPredictedAtMinimum, max_realHigherThanPredictedAtMinimum, max_realLowerThanPredictedAtMaximum, max_realHigherThanPredictedAtMaximum },
                new double[] { rmse_realLowerThanPredictedAtMinimum, rmse_realHigherThanPredictedAtMinimum, rmse_realLowerThanPredictedAtMaximum, rmse_realHigherThanPredictedAtMaximum }
            );
        }




        //TODO: efficientie??? error handling als evt niet in 1 vd 2 lijsten zit of niet matcht?
        // TODO: tuple returnen?
        private static Tuple<double[], double[]> CalcDiffAtLocalMinima(
                                                Tuple<List<uint>, List<double>> referenceMeasurementsInRange,
                                                Tuple<List<uint>, List<double>> targetMeasurementsInRange,
                                                double GLUC_THRESHOLD_FOR_UNDERSHOOT
                                               )

        {
            int aantal = Math.Min(targetMeasurementsInRange.Item1.Count, referenceMeasurementsInRange.Item1.Count);
            List<uint> tijden = new List<uint>(aantal);
            List<double> mmnts = new List<double>(aantal);
            for (int i = 0; i < aantal; i++)
            {
                tijden.Add(referenceMeasurementsInRange.Item1[i]);
                mmnts.Add(referenceMeasurementsInRange.Item2[i]);
            }

            List<int> localOptimaNdx = MyMath.FindLocalOptima(tijden, mmnts);
            // elk optimum en punten midden ertussen op x-as ..
            // todo; midden ertussen op y-as ... beste punt zoeken
            List<int> interestingPointsList = new List<int>();
            for(int i = 0; i < localOptimaNdx.Count; i++)
            {
                int ndx = localOptimaNdx[i];
                interestingPointsList.Add(ndx);
                //if(i < localOptimaNdx.Count - 1)
                //{
                //    int mid = (Math.Abs(localOptimaNdx[i + 1]) + Math.Abs(ndx)) / 2;
                //    int first = (mid + Math.Abs(ndx)) / 2;
                //    int third = (Math.Abs(localOptimaNdx[i + 1]) + mid) / 2;                    // midpoint telt als max en min mee:
                //    interestingPointsList.Add(first);
                //    interestingPointsList.Add(-first);
                //    interestingPointsList.Add(mid);
                //    interestingPointsList.Add(-mid);
                //    interestingPointsList.Add(third);
                //    interestingPointsList.Add(-third);
                //}
            }

            double[] results = new double[4];
            
            int[] teller = new int[4]; // zodat we niet 1 waarde, maar de som/err etc kunnen bepalen over alle (local) optima
            double[] somKwadraten = new double[4];

            foreach (int opt in interestingPointsList)
            {
                int t = Math.Abs(opt);
                double refValue = referenceMeasurementsInRange.Item2[t];
                double targetValue = targetMeasurementsInRange.Item2[t];

                double diff = Math.Abs(refValue - targetValue);
                int index = -1;
                if (opt < 0)
                {
                    //local min:
                    if (refValue < GLUC_THRESHOLD_FOR_UNDERSHOOT || targetValue < GLUC_THRESHOLD_FOR_UNDERSHOOT)
                    {
                        if (refValue < targetValue)
                        {
                            index = 0;
                        }
                        else
                        {
                            index = 1;
                        }
                    }
                }
                else
                {
                    //local max:
                    if (refValue < targetValue)
                    {
                        index = 2;
                    }
                    else
                    {
                        index = 3;
                    }
                }
                if (index >= 0)
                {
                    results[index] = Math.Max(results[index], diff);
                    somKwadraten[index] += diff * diff;
                    teller[index]++;
                }
            }
            
            for(int i = 0; i < teller.Length; i++)
            {
                if (teller[i] > 0)
                {
                    somKwadraten[i] = Math.Sqrt(somKwadraten[i] / teller[i]);
                }
                else
                {
                    somKwadraten[i] = 0;
                }
            }
            return new Tuple<double[], double[]>(results, somKwadraten);
        }

    }


}
