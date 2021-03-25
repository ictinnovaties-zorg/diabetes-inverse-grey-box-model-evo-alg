using System;
using System.Text;
using System.Collections.Generic;
using SMLDC.Simulator.Schedules.Events;
using SMLDC.Simulator.Schedules;
using SMLDC.Simulator.Utilities;
using SMLDC.Simulator;
using SMLDC.Simulator.DiffEquations.Solvers;
using SMLDC.Simulator.DiffEquations.Models;
using static SMLDC.Simulator.Utilities.Enums;
using SMLDC.MachineLearning.subpopulations;

namespace SMLDC.MachineLearning
{
    public class Particle : IComparable<Particle> // the 'hypothesis'
    {

        public int SolverInterval { get { return particleFilter.SolverInterval; } }


        ///  fields
        private int _prev_ID = -1;
        private int _ID;
        public int ID { get { return _ID; } }
        private static int ID_COUNTER = 1;
        private static readonly object _idCounterLockObject = new object();

        public override int GetHashCode()
        {
            return _ID;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) { return false; }
            try
            {
                return this.ID == ((Particle)obj).ID;
            }
            catch
            {
                return false;
            }
        }


        public double Weight;
        public double PreviousWeight { get; set; }



        public int CompareTo(Particle that)
        {
            // weight = error, so lower is better.
            return -this.Weight.CompareTo(that.Weight);
        }

        public static double[] RankBasedRelativeWeights(Particle[] particles, double boltzPow = 1)
        {
            // sorteren, slechtste heeft rw=1, eennaslechtste krijgt rw=2, ... daarna normeren,
            // en de pow erover om curve aan te passen.
            List<Particle> lijstje = new List<Particle>(particles);
            lijstje.Sort(); //eerste is slechtste mbt weight
            double[] relativeWeights = new double[particles.Length];
            for (int i = 0; i < lijstje.Count; i++)
            {
                // zoek lijstje[i] op in originele array, omdat de output die volgorde moet hebben
                bool gevonden = false;
                for (int ndx = 0; ndx < particles.Length; ndx++)
                {
                    if (particles[ndx] == lijstje[i])
                    {
                        //gevonden
                        gevonden = true;
                        relativeWeights[ndx] = (i + 1);
                    }
                }
                if (!gevonden)
                {
                    throw new ArgumentException("bug");
                }
            }
            double sum = 0;
            for (int i = 0; i < relativeWeights.Length; i++)
            {
                sum += relativeWeights[i];
            }
            //normeren
            for (int i = 0; i < relativeWeights.Length; i++)
            {
                relativeWeights[i] /= sum;
            }

            //pow:
            for (int i = 0; i < relativeWeights.Length; i++)
            {
                relativeWeights[i] = Math.Pow(relativeWeights[i], boltzPow);
            }
            return relativeWeights;
        }


        // met pow = 1 (default) is het gewone cumulative gewichten.
        public static double[] RelativeWeights(Particle[] particles, double boltzPow = 1)
        {
            //relatieve ranking
            // selectie op basis van *relatieve* ordering vd weights (=SSE)?
            double[] relativeWeights = new double[particles.Length];
            double minWeight = particles[particles.Length - 1].Weight;
            double maxWeight = particles[0].Weight;
            //herschalen naar range 0 --- 1
            if (maxWeight == minWeight) { minWeight -= 0.001; }
            double factor = 1 / (maxWeight - minWeight);
            for (int p = 0; p < particles.Length; p++)
            {
                double value = (particles[p].Weight - minWeight) * factor;
                double relW = Math.Pow(value, boltzPow);
                if (relW < 0 || double.IsNaN(relW) || double.IsInfinity(relW))
                {
                    relW = 0;
                }
                relativeWeights[p] = relW;
            }
            return relativeWeights;
        }

        // particle filter parameters:
        private ParticleFilter particleFilter;
        public SubPopulatie subPopulatie;
        public string SubPopulatieInfo { get; set; }
        private SolverResultBase GeneratedData;


        public bool IsOrphan() { return subPopulatie == null; }
        public void MakeOrphan()
        {
            this.SubPopulatieInfo = "SubPop#" + this.subPopulatie.ID + "@" + this.subPopulatie.exploratieTeller;
            this.GeneratedData = subPopulatie.BestPatientGeneratedData;
            this.subPopulatie = null;
        }



        public double ICR;
        public double ISF;

        public uint StartTime { set; get; }




        // HYPOTHESIS on parameters for modified model
        public BergmanAndBretonModel model;
        public BergmanAndBretonModel deltaModifiedModelParameters; // momentum ala NN.

      
        public double ActivityTauHeartRate { get { return model.TauGamma; } set { model.TauGamma = value; } }
      




        // als patient != null, copy dan zijn instellingen (model, startdata)
        public VirtualPatient BuildPatientFromParticle(Schedule scheduleUntilNow) //, bool useCarbHyp = true) //, Patient patient = null)
        {
            // has current settings, same as real (virtual) patient --> change! --- >>>>????? klopt dit nog?
            BergmanAndBretonModel pfModel = new BergmanAndBretonModel(this.model);

            // de 'ware' waarden door de hypotheses vervangen            
            Schedule schedule = scheduleUntilNow.DeepCopy();
        
            double[] startVector = CloneUtilities.CloneArray(modifiedModelStartVector_);
            VirtualPatient patient = new VirtualPatient(-this.ID, pfModel, particleFilter.ObservedPatient.GetSettingsClone(), schedule, startVector);
            patient.PatientType = PatientTypeEnum.PARTICLE;
            patient.StartTime = StartTime;
            patient.simulator = particleFilter.simulator;
            return patient;
        }






        // result of Hypothesis on internal parameters (G, X, I, etc...)
        // needed --> bootstrapping otherwise we would need to calc. the
        // entire trace: O(N). If using bootstrapping for the start vector, 
        // we just calc. the last c mmnts, so O(1) alg.

        private double[] modifiedModelStartVector_;
        public double[] modifiedModelStartVector
        {
            get { return modifiedModelStartVector_; }
            set
            {
                modifiedModelStartVector_ = value;
            }
        }


        public static int ID_to_break = 598036000;

        public Particle(Particle that)
        {
            this.subPopulatie = that.subPopulatie;
            this.particleFilter = that.particleFilter;
            this.SubPopulatieInfo = that.SubPopulatieInfo;
            this.GeneratedData = that.GeneratedData;
            this.modifiedModelStartVector_ = CloneUtilities.CloneArray(that.modifiedModelStartVector_);

            this._prev_ID = that.ID;
            _ID = particleFilter.NextParticleID;

            this.Weight = that.Weight;
            this.PreviousWeight = that.PreviousWeight;
            this.StartTime = that.StartTime;

            this.model = new BergmanAndBretonModel(that.model);
            this.deltaModifiedModelParameters = new BergmanAndBretonModel(that.deltaModifiedModelParameters);

            if (that.errorDataContainer != null)
            {
                this.errorDataContainer = that.errorDataContainer.DeepCopy();
            }

            this.WEIGHT_POWER = that.WEIGHT_POWER;
            this.THRESHOLD = that.THRESHOLD;

            // no deep copy of arrays etc:
        }


        // voor RANDOM INIT param:
        public Particle(SubPopulatie sub, double[] svector=null)
        {
            subPopulatie = sub;
            SubPopulatieInfo = "SubPop#" + this.subPopulatie.ID;
            particleFilter = sub.particleFilter;

            modifiedModelStartVector_ = CloneUtilities.CloneArray(svector);

            RandomStuff random = sub.random;
            model = new BergmanAndBretonModel(particleFilter.UseActivityModel);
            model.SetParameters_RandomInLogRange(random);
            deltaModifiedModelParameters = new BergmanAndBretonModel(particleFilter.UseActivityModel);

            Weight = 1;
            _ID = particleFilter.NextParticleID;
        }







        // make new particle, close / closely related to its originator
        // aanmaker moet z'n eigen random meegeven, omdat we anders hier nondeterminisme krijgen
        // omdat moment van aanmaken af kan hangen van thread scheduling!
        public Particle CreateMutatedNewParticle(RandomStuff random)
        {
            Particle selectedParticle = this;
            Particle newParticle = new Particle(this);
            newParticle._prev_ID = this.ID;
            newParticle._ID = particleFilter.NextParticleID;

            double thisRMSE = selectedParticle.ErrorUsedForStagnationAndExploration();

            if (Double.IsNaN(thisRMSE))
            {
                thisRMSE = Double.PositiveInfinity;
            }
            double sigmaForRmse = 0.1 * CalculateSigmaForRmse(thisRMSE);

            //todo: hier ook compleet nieuwe rnd part. genereren?
            for (int paramNdx = 0; paramNdx < newParticle.model.GetNrOfParameters(); paramNdx++)
            {
                double oldvalue = newParticle.model.GetParameter(paramNdx);

                if (random.NextDouble() <= particleFilter.settingsForParticleFilter.FractionWhenChangeParam) // % kans om param te veranderen
                {
                    double rnd = random.NormalDistributionSample(0, sigmaForRmse);
                    // random step in logspace:
                    // bij lagere weigth grotere sprongen maken
                    double sigma = particleFilter.SIGMA_STEP_SIZES_logspace[paramNdx]; // std op basis van de log-stap van min naar max.
                    double randomstep = rnd * sigma;

                    if (random.NextDouble() <= particleFilter.settingsForParticleFilter.FractionMomentum)
                    {
                        randomstep += particleFilter.settingsForParticleFilter.GammaMomentum * newParticle.deltaModifiedModelParameters.GetParameter(paramNdx);
                    }
                    // in logschaal de random stap doen, zodat het effect evenredig is met de grootte v/d param.waarde
                    double logParamValue = Math.Log10(oldvalue);
                    double newvalue = Math.Pow(10, logParamValue + randomstep);
                    //aftoppen , binnen range blijven:
                    // daadwerkelijke delta gebruiken (kan afgekapt zijn door bbox)
                    newParticle.model.SetParameter(paramNdx, newvalue);
                    newParticle.deltaModifiedModelParameters.SetParameter(paramNdx, newParticle.model.GetParameter(paramNdx) - oldvalue, true);
                }
                else
                {
                    // stilstand in deze dimensie.
                    newParticle.deltaModifiedModelParameters.SetParameter(paramNdx, 0, true);
                }
            }
            return newParticle;
        }


 


        private static bool sentSigmaToLogger = false; //optimalisatie
        private double CalculateSigmaForRmse(double rmse)
        {
            // kleine stap bij kleine RMSE
            //(0.33./(1+exp(-0.1*(rmse-25))) + 0.000 ))
            if (false)
            {
                //double baase = 0;
                //double offset = 25; // positie v/d curve op rmse-as
                //double init = 0.5; // dit is de limiet (de max. grootte v/d sigma)
                //double factor = -0.1; // bepaalt de scherpte v/d curve
                //double sigmaFactor = init / (1 + Math.Exp(factor * (rmse - offset))) + baase;
                //if (!sentSigmaToLogger)
                //{
                //    sentSigmaToLogger = true;
                //    particleFilter.settingsLogger.Add("sigma(rmse) = " + baase + " + " + init + " / (1 + Exp( " + factor + " * (rmse-" + offset + ") ))");
                //}
                //return sigmaFactor;
            }
            else
            {
                // min(0.33, 0.05 + rmse * 0.01)
                double baase = 0.01;
                double init = 1; // dit is de limiet (de max. grootte v/d sigma)
                double factor = 0.0031; // bepaalt de scherpte v/d curve
                if (!sentSigmaToLogger)
                {
                    sentSigmaToLogger = true;
                    particleFilter.settingsLogger.Add("sigma(rmse) = Min(" + init + ", " + baase + " + rmse * " + factor + ")");
                }
                double sigmaFactor = 0.5 * Math.Min(init, baase + rmse * factor);
                return sigmaFactor;
            }
        }





        public string ModelParametersToCSV()
        {
            string txt = "" + OctaveStuff.MyFormat(model.Gb_in_MG_per_DL) + ",\t" + OctaveStuff.MyFormat(model.P1) + ",\t" + OctaveStuff.MyFormat(model.P2) + ",\t" +
                   OctaveStuff.MyFormat(model.P3) + ",\t" + OctaveStuff.MyFormat(model.P4) + ",\t" + OctaveStuff.MyFormat(model.Vi_in_L) + ",\t" +
                   OctaveStuff.MyFormat(model.Vb_in_L) + ",\t" +
                   OctaveStuff.MyFormat(model.DRate);
            //   OctaveStuff.MyFormat(Rutln);

            if (particleFilter.UseActivityModel)
            {
                txt += ",\t" + OctaveStuff.MyFormat(model.BaseHeartRate) + ",\t" +
                   OctaveStuff.MyFormat(model.tauZ) + ",\t" +
                   OctaveStuff.MyFormat(model.TauGamma) + ",\t" +
                   OctaveStuff.MyFormat(model.GammaFnPower) + ",\t" +
                   OctaveStuff.MyFormat(model.GammaFaFactor) + ",\t" +
                   OctaveStuff.MyFormat(model.alpha) + ",\t" +
                   OctaveStuff.MyFormat(model.beta);
            }
            return txt;
        }


        public string ToStringHeader() //kan niet static, al zou ik dat wel willen, omdat het afhangt van attributen... of ergens extra variabelen aanmaken
        {
            string parametersTxt = "Brg:";
            string[] paramNames = model.GetParameterNames();
            for (int i = 0; i < paramNames.Length; i++)
            {
                parametersTxt += OctaveStuff.MyFormat(paramNames[i], 7) + " ";
                //if (i > 0) 
                { parametersTxt += "|"; }
            }
            if (particleFilter.ObservedPatient.RealData)
            {
                parametersTxt += "\n REAL DATA";
            }
            else
            {
                parametersTxt += "\nreal";
                for (int i = 0; i < paramNames.Length; i++)
                {
                    parametersTxt += OctaveStuff.MyFormat(particleFilter.GetRealModelValue(i), 7) + " ";
                    //if (i > 0) 
                    { parametersTxt += "|"; }
                }
            }
            return parametersTxt;
        }


        public override string ToString()
        {
            //string particleTxt = "Particle (#" + _prev_ID + " --> " + _ID + " [sub=" + subPopulatie.ID + "], w=" + Weight.ToString("0.00") + ")"
            //    + "\tE:_n=" + OctaveStuff.MyFormat(RMSE_Noisy_Mmnts)
            //    + "\tE:_sn=" + OctaveStuff.MyFormat(RMSE_Smooothed_Noisy_Mmnts)
            //    + ",\t_r=" + OctaveStuff.MyFormat(RMSE_Real_Mmnts)
            //    + "\t " + Get_undershoot_overshoot_txt();
            string idtxt = "";
            if (subPopulatie == null)
            {
              //  idtxt += OctaveStuff.MyFormat(SubPopulatieInfo, 13);
            }
            else
            {
              //  idtxt += OctaveStuff.MyFormat("#" + _prev_ID + "-->" + _ID, 15);
            }
            string particleTxt = idtxt + " w=" + OctaveStuff.MyFormat(Weight, 7) + " "
                + "En:" + OctaveStuff.MyFormat(RMSE_ML2Mmnts, 7)
                + "|w" + OctaveStuff.MyFormat(this.Weighted_RMSE_ML2Mmnts_after_carbs_per_bin, 7)
                //  + "|" + OctaveStuff.MyFormat(RMSE_ML2Smooothed, 7)
                //  + "|" + OctaveStuff.MyFormat(this.Weighted_RMSE_ML2SmoothedMmnts_after_carbs_per_bin, 7)
                + "|r " + OctaveStuff.MyFormat(RMSE_ML2Real_ALLEEN_VOOR_REFERENTIE, 7)
                + "|ri " + OctaveStuff.MyFormat(errorDataContainer.ml2Mmnts_TargetReal_rico.RMSE_ML2Mmnts, 7)
                
             //   + "|" + OctaveStuff.MyFormat(RMSE_Noisy_tov_real, 7)  // is vrijwel altijd hetzelfde. TODO: elders tonen?
             //   + "  " + Get_undershoot_overshoot_txt()
             //    + " [mg/dL]> " 
             ;

            StringBuilder parametersTxt = new StringBuilder();
            if (subPopulatie == null)
            {
                parametersTxt.Append("HIST##:");
            }
            else
            {
                parametersTxt.Append("s" + OctaveStuff.MyFormat("" + subPopulatie.ID, 2) + ":");
            }
            string[] paramNames = model.GetParameterNames();
            for (int i = 0; i < paramNames.Length; i++)
            {
                string signTxt = ".";
                double delta = deltaModifiedModelParameters.GetParameter(i);
                if (delta > 0) { signTxt = "+"; }
                else if (delta < 0) { signTxt = "-"; }

                //parametersTxt += particleFilter.ParamNames[i] + " = " + OctaveStuff.MyFormat(particleFilter.GetRealModelValue(i)) + " | " + OctaveStuff.MyFormat(modifiedModelParameters[i]) + "(" + signTxt + "),";
                //                parametersTxt += OctaveStuff.MyFormat(modifiedModelParameters[i]) + "" + signTxt + "";
                parametersTxt.Append(OctaveStuff.MyFormat(model.GetParameter(i)) + "" + signTxt + "");
                //if (i > 0) 
                { parametersTxt.Append("|"); }
            }
            //parametersTxt += "}";

            return parametersTxt.ToString() + " " + particleTxt;

        }






        public void CalculateErrorOnObservations(ErrorCalculationSettings errorCalculationSettings, int begin = -1, int end = -1)
        {
            if (begin < 0) { begin = (int)StartTime; }
            if (end < 0) { end = (int)this.GetStopTime(); }
            VirtualPatient patient = this.ParticlePatient;

            Tuple< List<uint>, List<double>,List<double>, List<double>, Dictionary<uint, double>> tup = particleFilter.GetSmoothedNoisyMmnts(begin, end - 1, particleFilter.settingsForParticleFilter.SMOOTHING_RANGE );
            List<uint> times = tup.Item1; //gesorteerd?
            List<double> trueValues = tup.Item2;
            List<double> noisyValues = tup.Item3;
            List<double> smoothedNoisyValues = tup.Item4;

            // vraag alle huidige (in trail) carb tijden op:
            List<uint> carbTimes = patient.TrueSchedule.GetCarbTimes((uint)begin, (uint)end);

            if (times.Count > 0 && carbTimes.Count > 0)
            {

                List<double> mlPredictions = new List<double>(times.Count);
                double[] particleMmntPredictions_cached = null; // niet steeds nieuwe array aanmaken in SolverResult!
                for (int i = 0; i < times.Count; i++)
                {
                    bool found = patient.TryGetDataFromTime/*_reuse*/(times[i], /*ref*/ out particleMmntPredictions_cached);
                    if (found == false || particleMmntPredictions_cached == null)
                    {
                        mlPredictions.Add(double.NaN);
                    }
                    else
                    {
                        double particleMmntPrediction = particleMmntPredictions_cached[BergmanAndBretonModel.G_Glucose_ODEindex_MG_per_DL];
                        mlPredictions.Add(particleMmntPrediction);
                    }
                }
                // use the slope of the ML predictions, and compare this to the actual slope.
                int halveRange = 1;
                List<double> mlSlopePredictions = new List<double>(times.Count);
                for (int i = 0; i < times.Count; i++)
                {
                    double thisSlope = MyMath.LeastSquaresLineFitSlope(mlPredictions, i - halveRange, i + halveRange);
                    thisSlope = MyMath.SignPow(thisSlope, 0.5);
                    mlSlopePredictions.Add( thisSlope);
                }
                // afgeleide van VIP waardes (nb: TODO: niet elke keer opnieuw berekenen!!!)
                List<double> smoothedNoisyRico = new List<double>(times.Count);
                for (int i = 0; i < times.Count; i++)
                {
                    double dezeRico = MyMath.LeastSquaresLineFitSlope(smoothedNoisyValues, i - halveRange, i + halveRange);
                    dezeRico = MyMath.SignPow(dezeRico, 0.5);
                    smoothedNoisyRico.Add(dezeRico);
                }

                errorDataContainer = ErrorData.CalculateRmseOnObservations(carbTimes, times, mlPredictions, trueValues, noisyValues, smoothedNoisyValues, errorCalculationSettings, Moet_ml2SmoothedMmnts_TargetSmoothed_BerekendWorden()); // (gewogen, hogere gluc telt minder mee) RMSE op dit stuk data
                ///*hack*/  //              errorDataContainer.ml2Mmnts_TargetReal_rico = errorDataContainer.ml2Mmnts_TargetReal;
                ErrorCalculationSettings errorCalculationSettingsForSlope = new ErrorCalculationSettings(errorCalculationSettings.SSE_POWER_FOR_ERROR_CALC, false, errorCalculationSettings.timeSlots);
                ErrorDataContainer errorDataContainerRico = ErrorData.CalculateRmseOnObservations(carbTimes, times, mlSlopePredictions, smoothedNoisyRico, smoothedNoisyRico, smoothedNoisyRico, errorCalculationSettingsForSlope, Moet_ml2SmoothedMmnts_TargetSmoothed_BerekendWorden()); // (gewogen, hogere gluc telt minder mee) RMSE op dit stuk data
                errorDataContainer.ml2Mmnts_TargetReal_rico = errorDataContainerRico.ml2Mmnts_TargetReal;


                if (trueValues != null && !Double.IsNaN(errorDataContainer.ml2Mmnts_TargetReal.RMSE_ML2Mmnts) && errorDataContainer.ml2Mmnts_TargetReal.RMSE_ML2Mmnts < 1e6)
                {

                    //uitschieters op het moment dat 'de andere curve' een max|min heeft.
                    // dit is op de REAL, dus dit is alleen voor ***EVALUATIE*** in SIM, want NIET beschikbaar voor echte patient!
                    // dus nooit te gebruiken als leer-signaal!!!!
                    Tuple<double[], double[]> res = CalcUndershootOvershoot(times, trueValues, patient); //Dit is goede maat voor performance t.o.v. echte patient
                    errorDataContainer.ml2Mmnts_TargetReal.max_LowerThanPredictedAtMinimum = res.Item1[0]; // real < predicted op minimum --> risk, want er lijkt niks aan de hand, maar echte Gluc is veel te laag.
                    errorDataContainer.ml2Mmnts_TargetReal.max_HigherThanPredictedAtMinimum = res.Item1[1]; // real > predicted op minimum --> ok
                                                                                                            // at max:
                    errorDataContainer.ml2Mmnts_TargetReal.max_LowerThanPredictedAtMaximum = res.Item1[2]; // real < predicted op MAXimum --> ok
                    errorDataContainer.ml2Mmnts_TargetReal.max_HigherThanPredictedAtMaximum = res.Item1[3]; // real > predicted op MAXimum --> risk: echte Gluc veel hoger dan berekend. (is minder risk, maar wel interessant)

                    errorDataContainer.ml2Mmnts_TargetReal.rmse_LowerThanPredictedAtMinimum = res.Item2[0]; // noisy < predicted op minimum --> risk, want er lijkt niks aan de hand, maar echte Gluc is veel te laag.
                    errorDataContainer.ml2Mmnts_TargetReal.rmse_HigherThanPredictedAtMinimum = res.Item2[1]; // noisy > predicted op minimum --> ok
                                                                                                             // at max:
                    errorDataContainer.ml2Mmnts_TargetReal.rmse_LowerThanPredictedAtMaximum = res.Item2[2]; // noisy < predicted op MAXimum --> ok
                    errorDataContainer.ml2Mmnts_TargetReal.rmse_HigherThanPredictedAtMaximum = res.Item2[3]; // noisy > predicted op MAXimum --> risk: echte Gluc veel hoger dan berekend. (is minder risk, maar wel interessant)



                    if (double.IsNaN(errorDataContainer.ml2Mmnts_TargetReal.max_LowerThanPredictedAtMaximum) || errorDataContainer.ml2Mmnts_TargetReal.max_LowerThanPredictedAtMaximum > 1e6
                        || double.IsNaN(errorDataContainer.ml2Mmnts_TargetReal.max_HigherThanPredictedAtMinimum) || errorDataContainer.ml2Mmnts_TargetReal.max_HigherThanPredictedAtMinimum > 1e6
                        || double.IsNaN(errorDataContainer.ml2Mmnts_TargetReal.max_LowerThanPredictedAtMaximum) || errorDataContainer.ml2Mmnts_TargetReal.max_LowerThanPredictedAtMaximum > 1e6
                        || double.IsNaN(errorDataContainer.ml2Mmnts_TargetReal.max_HigherThanPredictedAtMaximum) || errorDataContainer.ml2Mmnts_TargetReal.max_HigherThanPredictedAtMaximum > 1e6)
                    {
                        errorDataContainer.ml2Mmnts_TargetReal.max_LowerThanPredictedAtMinimum = Double.PositiveInfinity;
                        errorDataContainer.ml2Mmnts_TargetReal.max_HigherThanPredictedAtMinimum = Double.PositiveInfinity;
                        errorDataContainer.ml2Mmnts_TargetReal.max_LowerThanPredictedAtMaximum = Double.PositiveInfinity;
                        errorDataContainer.ml2Mmnts_TargetReal.max_HigherThanPredictedAtMaximum = Double.PositiveInfinity;
                        errorDataContainer.ml2Mmnts_TargetReal.rmse_LowerThanPredictedAtMinimum = Double.PositiveInfinity;
                        errorDataContainer.ml2Mmnts_TargetReal.rmse_HigherThanPredictedAtMinimum = Double.PositiveInfinity;
                        errorDataContainer.ml2Mmnts_TargetReal.rmse_LowerThanPredictedAtMaximum = Double.PositiveInfinity;
                        errorDataContainer.ml2Mmnts_TargetReal.rmse_HigherThanPredictedAtMaximum = Double.PositiveInfinity;

                        errorDataContainer.ml2Mmnts_TargetReal.Weighted_RMSE_ML2Mmnts_after_carbs_per_bin = double.PositiveInfinity;

                    }
                }

                if (Moet_ml2SmoothedMmnts_TargetSmoothed_BerekendWorden())
                {

                    // deze kan voor sturing van pf gebruikt worden.
                    Tuple<double[], double[]> res = CalcUndershootOvershoot(times, smoothedNoisyValues, patient);
                    errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.max_LowerThanPredictedAtMinimum = res.Item1[0]; // noisy < predicted op minimum --> risk, want er lijkt niks aan de hand, maar echte Gluc is veel te laag.
                    errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.max_HigherThanPredictedAtMinimum = res.Item1[1]; // noisy > predicted op minimum --> ok
                                                                                                                        // at max:
                    errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.max_LowerThanPredictedAtMaximum = res.Item1[2]; // noisy < predicted op MAXimum --> ok
                    errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.max_HigherThanPredictedAtMaximum = res.Item1[3]; // noisy > predicted op MAXimum --> risk: echte Gluc veel hoger dan berekend. (is minder risk, maar wel interessant)

                    errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.rmse_LowerThanPredictedAtMinimum = res.Item2[0]; // noisy < predicted op minimum --> risk, want er lijkt niks aan de hand, maar echte Gluc is veel te laag.
                    errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.rmse_HigherThanPredictedAtMinimum = res.Item2[1]; // noisy > predicted op minimum --> ok
                                                                                                                         // at max:
                    errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.rmse_LowerThanPredictedAtMaximum = res.Item2[2]; // noisy < predicted op MAXimum --> ok
                    errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.rmse_HigherThanPredictedAtMaximum = res.Item2[3]; // noisy > predicted op MAXimum --> risk: echte Gluc veel hoger dan berekend. (is minder risk, maar wel interessant)



                    if ( double.IsNaN(errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.max_LowerThanPredictedAtMaximum) || errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.max_LowerThanPredictedAtMaximum > 1e6
                        || double.IsNaN(errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.max_HigherThanPredictedAtMinimum) || errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.max_HigherThanPredictedAtMinimum > 1e6
                        || double.IsNaN(errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.max_LowerThanPredictedAtMaximum) || errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.max_LowerThanPredictedAtMaximum > 1e6
                        || double.IsNaN(errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.max_HigherThanPredictedAtMaximum) || errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.max_HigherThanPredictedAtMaximum > 1e6)
                    {

                        errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.max_LowerThanPredictedAtMinimum = Double.PositiveInfinity;
                        errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.max_HigherThanPredictedAtMinimum = Double.PositiveInfinity;
                        errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.max_LowerThanPredictedAtMaximum = Double.PositiveInfinity;
                        errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.max_HigherThanPredictedAtMaximum = Double.PositiveInfinity;
                        errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.rmse_LowerThanPredictedAtMinimum = Double.PositiveInfinity;
                        errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.rmse_HigherThanPredictedAtMinimum = Double.PositiveInfinity;
                        errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.rmse_LowerThanPredictedAtMaximum = Double.PositiveInfinity;
                        errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.rmse_HigherThanPredictedAtMaximum = Double.PositiveInfinity;

                        errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.Weighted_RMSE_ML2Mmnts_after_carbs_per_bin = double.PositiveInfinity;
                    }
                }
                

                CalculateWeight(); //  RMSE --> weight //TODO: ook overshoots(op smoothed noise) erin betrekken
            }
        }



        // bepaal:
        // [0] undershoot (real < predicted op een low)
        // [1] safe undershoot (real > predicted op low)
        // [2] overshoot (real > predicted op high)
        // [3] safe overshoot (real < predicted at high)
        // gebruik de mmnts, NIET de gegenereerde data (want die heb je niet bij een echte menselijke patient)
        // nb dit slaat alleen ergens op in een continue meter situatie
        //
        // alles bepaald op de true values
        private Tuple<double[], double[]> CalcUndershootOvershoot(List<uint> times, List<double> values, VirtualPatient patient = null)
        {
            if (patient == null) { patient = this.ParticlePatient; }
            return ErrorData.CalcUndershootOvershoot(times, values, patient, GLUC_THRESHOLD_FOR_UNDERSHOOT);
        }







        public ErrorDataContainer errorDataContainer;
        public double RMSE_ML2Real_ALLEEN_VOOR_REFERENTIE { get { return errorDataContainer.ml2Mmnts_TargetReal.RMSE_ML2Target; } }

        // de ml2Mmnts kan zowel uit de ml2Mmnts_TargetReal als uit de ml2Mmnts_TargetSmoothed gehaald worden. Is dezelfde berekening
        // die NIKS met de target te maken heeft.

        /// 1e orde afgeleide/rico gebruiken
        public double RMSE_ML2Mmnts_en_RMSE_ML2Mmnts_slope {
            get { 
                return errorDataContainer.ml2Mmnts_TargetReal.RMSE_ML2Mmnts * (1 + Math.Pow( errorDataContainer.ml2Mmnts_TargetReal_rico.RMSE_ML2Mmnts, 2) ); 
            } 
        }
        public double Weighted_RMSE_ML2Mmnts_en_RMSE_ML2Mmnts_slope
        {
            get
            {
                return errorDataContainer.ml2Mmnts_TargetReal.Weighted_RMSE_ML2Mmnts_after_carbs_per_bin * (1 + Math.Pow(errorDataContainer.ml2Mmnts_TargetReal_rico.RMSE_ML2Mmnts, 2));
            }
        }
        public double RMSE_ML2Mmnts { get { return errorDataContainer.ml2Mmnts_TargetReal.RMSE_ML2Mmnts; } }
        public double Weighted_RMSE_ML2Mmnts_after_carbs_per_bin { get { return errorDataContainer.ml2Mmnts_TargetReal.Weighted_RMSE_ML2Mmnts_after_carbs_per_bin; } }

        public double RMSE_ML2Smooothed { get { return errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.RMSE_ML2Target; } }
        public double Weighted_RMSE_ML2SmoothedMmnts_after_carbs_per_bin { get { return errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.Weighted_RMSE_ML2Mmnts_after_carbs_per_bin; } }


        public double ErrorUsedForStagnationAndExploration()
        {
            return ErrorToUseForWeight(); // RMSE_Smooothed_Noisy_Mmnts;
        }


        public double ErrorToUseForWeight()
        {
            // todo: enum?
            double sse_over_alles = 0;
            switch (particleFilter.settingsForParticleFilter.ErrorForWeights)
            {
                case 0:
                    { sse_over_alles = RMSE_ML2Mmnts; break; }
                case 1:
                    { sse_over_alles = Weighted_RMSE_ML2Mmnts_after_carbs_per_bin; break; }
                case 2:
                    { break; }
                case 3:
                    { sse_over_alles = RMSE_ML2Smooothed; break; }
                case 4:
                    { sse_over_alles = Weighted_RMSE_ML2SmoothedMmnts_after_carbs_per_bin; break; }
                case 5:
                    { sse_over_alles = RMSE_ML2Mmnts_en_RMSE_ML2Mmnts_slope; break; }
                case 6:
                    { sse_over_alles = Weighted_RMSE_ML2Mmnts_en_RMSE_ML2Mmnts_slope; break; }
                default:
                    {
                        throw new ArgumentException("onbekende ErrorForWeights = " + particleFilter.settingsForParticleFilter.ErrorForWeights);
                    }
            }
            if (particleFilter.settingsForParticleFilter.USE_OVERSHOOTS_IN_WEIGHT)
            {
                //  TODO: alle fouten weer aanzetten
                double totalweighting = 0;
                sse_over_alles = Math.Pow(sse_over_alles, 2); totalweighting = 1;
                sse_over_alles += noisyLowerThanPredictedAtMaximumFactor * Math.Pow(errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.rmse_LowerThanPredictedAtMaximum, 2); //  ^/\ don't care
                sse_over_alles += noisyHigherThanPredictedAtMaximumFactor * Math.Pow(errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.rmse_HigherThanPredictedAtMaximum, 2); //  /\^ lange termijn probleem
                sse_over_alles += noisyLowerThanPredictedAtMinimumFactor * Math.Pow(errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.rmse_LowerThanPredictedAtMinimum, 2); //  \/v gevaarlijk
                sse_over_alles += noisyHigherThanPredictedAtMinimumFactor * Math.Pow(errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.rmse_HigherThanPredictedAtMinimum, 2); //  v\/ relevant voor fine tuning
                totalweighting += (noisyLowerThanPredictedAtMaximumFactor + noisyHigherThanPredictedAtMaximumFactor + noisyLowerThanPredictedAtMinimumFactor + noisyHigherThanPredictedAtMinimumFactor);
                sse_over_alles = Math.Sqrt(sse_over_alles / totalweighting);
            }

            return sse_over_alles; // Math.Sqrt(sse_over_alles);
        }


        public bool Moet_ml2SmoothedMmnts_TargetSmoothed_BerekendWorden()
        {
            return USE_OVERSHOOTS_IN_WEIGHT || particleFilter.settingsForParticleFilter.ErrorForWeights == 2;
        }
        public string Get_undershoot_overshoot_txt()
        {
            string undershoot_overshoot_txt = "";
            if (Moet_ml2SmoothedMmnts_TargetSmoothed_BerekendWorden())
            {
                undershoot_overshoot_txt +=
                    "r<>p:{\\/ " + OctaveStuff.MyFormat(errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.rmse_LowerThanPredictedAtMinimum) // gevaarlijk, hypo/low
                    + ", " + OctaveStuff.MyFormat(errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.rmse_HigherThanPredictedAtMinimum)
                    + " /\\ " + OctaveStuff.MyFormat(errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.rmse_LowerThanPredictedAtMaximum) //lange termijn probleem, hyper(?)
                    + ", " + OctaveStuff.MyFormat(errorDataContainer.ml2SmoothedMmnts_TargetSmoothed.rmse_HigherThanPredictedAtMaximum)
                + "}";
            }
            else
            {
                undershoot_overshoot_txt +=
                "r<>p:{\\/ " + OctaveStuff.MyFormat(errorDataContainer.ml2Mmnts_TargetReal.rmse_LowerThanPredictedAtMinimum) // gevaarlijk, hypo/low
                + ", " + OctaveStuff.MyFormat(errorDataContainer.ml2Mmnts_TargetReal.rmse_HigherThanPredictedAtMinimum)
                + " /\\ " + OctaveStuff.MyFormat(errorDataContainer.ml2Mmnts_TargetReal.rmse_LowerThanPredictedAtMaximum) //lange termijn probleem, hyper(?)
                + ", " + OctaveStuff.MyFormat(errorDataContainer.ml2Mmnts_TargetReal.rmse_HigherThanPredictedAtMaximum)
                + "}";
            }
            return undershoot_overshoot_txt;
        }



        // todo: slimmere weights functie maken
        private double WEIGHT_POWER = 0.1;
        private double THRESHOLD = 1;
        private bool USE_OVERSHOOTS_IN_WEIGHT { get { return particleFilter.settingsForParticleFilter.USE_OVERSHOOTS_IN_WEIGHT; } }
        private double GLUC_THRESHOLD_FOR_UNDERSHOOT { get { return particleFilter.settingsForParticleFilter.GLUC_THRESHOLD_FOR_UNDERSHOOT; } }
        private double noisyLowerThanPredictedAtMaximumFactor { get { return particleFilter.settingsForParticleFilter.noisyLowerThanPredictedAtMaximumFactor; } }
        private double noisyHigherThanPredictedAtMaximumFactor { get { return particleFilter.settingsForParticleFilter.noisyHigherThanPredictedAtMaximumFactor; } }
        private double noisyLowerThanPredictedAtMinimumFactor { get { return particleFilter.settingsForParticleFilter.noisyLowerThanPredictedAtMinimumFactor; } }
        private double noisyHigherThanPredictedAtMinimumFactor { get { return particleFilter.settingsForParticleFilter.noisyHigherThanPredictedAtMinimumFactor; } }



        // weight gebruikt voor 'sturen' naar betere oplossingen.
        private void CalculateWeight()
        {
            double sse_over_alles = ErrorToUseForWeight();
            Weight = sse_over_alles;
        }



        public uint GetStopTime()
        {
            return this.particleFilter.GetCurrentTime();
        }








        private static uint minimaleTijdErna = (uint)(60 * 1.5);

        // List< <time, estimate, offset, learningrate> >
        public List<Tuple<uint, double, uint, double>> Evaluate_with_local_search(ErrorCalculationSettings errorCalculationSettings, Schedule scheduleToUse_orig, uint starttime, uint stopTime)
        {
            Schedule scheduleToUse = scheduleToUse_orig.DeepCopy();
            List<Tuple<uint, double, uint, double>> carbHyps = new List<Tuple<uint, double, uint, double>>();

            SetModifiedModelStartVector(null, starttime);

            double[] orig_startVector = CloneUtilities.CloneArray(modifiedModelStartVector_);

            Evaluate_simple(errorCalculationSettings, scheduleToUse, starttime, stopTime, orig_startVector);

            List<Tuple<PatientEvent, uint>> pieken = scheduleToUse.GetCarbPieken(starttime, stopTime, minimaleTijdErna);
            foreach (Tuple<PatientEvent, uint> piek in pieken)
            {
                PatientEvent thisCarbEvent = piek.Item1;
                uint actualStopTime = piek.Item2;
                uint currentCarbInputTime = thisCarbEvent.TrueStartTime;

                // elke piek is gekoppeld aan een carb event
                uint startTimeForFraction = particleFilter.GetCarbTrailStartSlopeTime();
                uint stopTimeForFraction = particleFilter.GetCarbTrailEndSlopeTime();

                double fractie = ((int)currentCarbInputTime - (int)startTimeForFraction) / (double)(stopTimeForFraction - startTimeForFraction);
                fractie = MyMath.Clip(fractie, 0, 1);
                fractie = Math.Pow(fractie, particleFilter.settingsForParticleFilter.local_search_learning_rate_power);

                if (fractie > 0)
                {
                    // zoektocht naar optimale carbs voor dit event
                    Tuple<double, uint> estAndOffset = SearchForBestCarbHypothessisFit(errorCalculationSettings, scheduleToUse, thisCarbEvent, actualStopTime);
                    double carbHyp = estAndOffset.Item1;
                    uint carbTijd = estAndOffset.Item2;

                    double thisLr = fractie * particleFilter.settingsForParticleFilter.local_search_learning_rate_base;
                    carbHyps.Add(new Tuple<uint, double, uint, double>(currentCarbInputTime, carbHyp, carbTijd, thisLr));

                    if (particleFilter.settingsForParticleFilter.UpdateCarbHypDuringSearch)
                    {
                        particleFilter.UpdateCarbEstimation(this, thisLr, currentCarbInputTime, carbTijd, carbHyp);
                    }
                 

                    // niet vergeten schedule te updaten, omdat anders de volgende iteratie nog de oude onjuiste carbs heeft
                    // double update = carbHyp * thisLr + thisCarbEvent.Carb_TrueValue_in_gram * (1 - thisLr);
                    scheduleToUse.UpdateEventAtTime(thisCarbEvent.TrueStartTime, carbHyp);
                    scheduleToUse.MoveEventAtTimeToTime(thisCarbEvent.TrueStartTime, carbTijd);

                    // recentste stukje evalueren met de meest recente carb estimate:
                    uint rerun_start_time = (uint) Math.Max(starttime, (int)currentCarbInputTime - 30);
                    double[] initialVector = this.ParticlePatient.GetDataFromTime(rerun_start_time);
                    this.Evaluate_simple(errorCalculationSettings, scheduleToUse, rerun_start_time, actualStopTime, initialVector);
                }

            }
            // nog eenmaal, om nieuwe RMSE voor totaal te bepalen met gevonden beste carbhyp
            Evaluate_simple(errorCalculationSettings, scheduleToUse, starttime, stopTime, orig_startVector);
            this.GeneratedData = this.ParticlePatient.GeneratedData;

            return carbHyps;
        }




        // LET OP DAT DE OFFSETS NIET ZO GROOT (NEGATIEF) WORDEN, DAT ZE GROTER WORDEN DAN VerwijderCarbHypsDichterBijDanMarge 

           private static int[] offsets = {12,  5,  0, -5, -12 }; //goed met parabolic, 6 staps
//        private static int[] offsets = { 40, 25, 15, 5, 0, -5, -15, -25, -40 };  //werkt goed
        // <estimate, time>
        private Tuple<double, uint> SearchForBestCarbHypothessisFit(ErrorCalculationSettings errorCalculationSettings_orig, Schedule scheduleToUse_orig, PatientEvent pEventOrig, uint stopTime)
        {
            ErrorCalculationSettings errorCalculationSettings = new ErrorCalculationSettings(errorCalculationSettings_orig.SSE_POWER_FOR_ERROR_CALC, errorCalculationSettings_orig.ErrorCalcInLogSpace);
            errorCalculationSettings.USE_HOOGTEFACTOR_IN_RMSE = false;

            // zoeken door steeds 1 stapje te zetten in een richting (carbs of tijd) en op basis van delta error
            // zo'n stap ene of andere kant op te zetten.
            uint beste_tijd = 0;
            double beste_carb = -1;
            double laagste_rmse = double.PositiveInfinity;

            double orig_estimate = pEventOrig.Carb_TrueValue_in_gram;
            uint startTimeForSearch = (uint)Math.Max(0, (int)pEventOrig.TrueStartTime - 2 * particleFilter.settingsForParticleFilter.VerwijderCarbHypsDichterBijDanMarge);

            for (int offset_ndx = 0; offset_ndx < offsets.Length; offset_ndx++)
            {
                int offset_stap = offsets[offset_ndx];
                Schedule scheduleToUse = scheduleToUse_orig.DeepCopy();
                if(pEventOrig.TrueStartTime + offset_stap > stopTime)
                {
                    offset_stap = (int) stopTime - (int) pEventOrig.TrueStartTime;
                }
                PatientEvent pEvent = scheduleToUse.MoveEventAtTime(pEventOrig.TrueStartTime, offset_stap); // het kan zijn dat de move niet perfect gaat
                // de ParabolicSearchForBestCarbHypothessisFit gebruikt de waarde die particle patient in solverresult heeft opgeslagen, dus we MOETEN
                //
                // Eerst de verder in de toekomst-varianten doorrekenen.
                // Want als we eerst de kleinste (i.e. verst naar verleden) offset doen, dan is de solverresults 'vervuild' met die vroege piek.
                // als je daarna een latere offset neemt, dan wordt vervuiling uit die eerdere offset als startvector gebruikt.

                //
                // nog een probleem: als deze event door de move stuivertje-wisselt met andere event (omdat er door missing detectie 2 dicth bij elkaar zitten)
                // dan komt die andere event VOOR deze, waardoor ie meegenomen wordt in simulatie, terwijl het eerst de tweede event was van de
                // twee die dicht op elkaar staan!
                if (pEvent.TrueStartTime < stopTime)
                {
                    // alle carb events NA pEvent op 0 zetten! Want we willen weten hoe goed deze event de data erna kan verklaren.
                    // uiteraard eventuele events die in het stukje ervoor zitten, NIET weghalen.
                    // ... maar dit heeft weer als probleem, dat de huidige event probert het gemiddelde te worden van de huidige carb en de volgende
                    // als die vlakbij is, waardoor de volgende een lage estimate krijgt en offset gaat schuiven en hij op een gegeven
                    // moment zo dicht bij de ander zit dat ie opgeheven wordt...
                    
                    scheduleToUse.UpdateCarbEventsToValue(0, pEvent.TrueStartTime + 1, stopTime);

                    Tuple<double, double> bestEstimate;
                    if(particleFilter.settingsForParticleFilter.CarbSearchLinear)
                    {
                        bestEstimate = LinearSearchForBestCarbHypothessisFit(errorCalculationSettings, scheduleToUse, startTimeForSearch, stopTime, pEvent.TrueStartTime, orig_estimate);
                    }
                    else
                    {
                        bestEstimate = ParabolicSearchForBestCarbHypothessisFit(errorCalculationSettings, scheduleToUse, startTimeForSearch, stopTime, pEvent.TrueStartTime, orig_estimate);
                    }

                    if (bestEstimate.Item2 < laagste_rmse)
                    {
                        laagste_rmse = bestEstimate.Item2;
                        beste_tijd = pEvent.TrueStartTime;
                        beste_carb = bestEstimate.Item1;
                    }
                }
            }

            return new Tuple<double, uint>(beste_carb, beste_tijd);
        }


        private static double[] carbSteps = {-25, -10, -5, -2, 0, 2, 5, 10, 25}; //redelijk bij ML-hack
        // <estimate, rmse>
        private Tuple<double, double> LinearSearchForBestCarbHypothessisFit(ErrorCalculationSettings errorCalculationSettings, Schedule scheduleToUse, uint starttime, uint stopTime, uint eventTime, double orig_estimate)
        {
            if(orig_estimate == 0)
            {
                orig_estimate = 25;
            }
            double[] initialVector = this.ParticlePatient.GetDataFromTime(starttime); // deze data zit nog niet in pf!
            Schedule baseTestSchedule = scheduleToUse.CropSchedule2Copy(starttime, stopTime, false);

            double lowest_error = double.PositiveInfinity;
            double best_carb = 0;
            for (int carb_step_ndx = 0; carb_step_ndx < carbSteps.Length; carb_step_ndx++)
            {
                double carbhyp = orig_estimate + carbSteps[carb_step_ndx];
                if(carbhyp <= 0) { continue; }
                Schedule testSchedule = baseTestSchedule.DeepCopy();
                testSchedule.UpdateEventAtTime(eventTime, carbhyp);
                // Create a new patient with the same parameters as the patient
                Particle SearchParticle = new Particle(this); //dit doet GEEN deep copy van particlePatient
                                                                       // dit maakt intern een nieuwe patient aan, dus de oude particlePatient (van de input patient) blijft onveranderd
                SearchParticle.Evaluate_simple(errorCalculationSettings, testSchedule, starttime, stopTime, initialVector); //, false);

                // Retrieve the SSE and calc. new search step & direction
                double error = SearchParticle.RMSE_ML2Mmnts; // .Weighted_RMSE_ML2Mmnts_after_carbs_per_bin; //of op smoothed?
                if(error < lowest_error)
                {
                    lowest_error = error;
                    best_carb = carbhyp;
                }
            }
            return new Tuple<double, double>(best_carb, lowest_error);
        }


        // <estimate, rmse>
        private Tuple<double, double> ParabolicSearchForBestCarbHypothessisFit(ErrorCalculationSettings errorCalculationSettings, Schedule scheduleToUse, uint starttime, uint stopTime, uint eventTime, double orig_estimate)
        {
            double MIN_CARB_DELTA = 2; //gr carb
            double MIN_CARB = 0;
            double MID_CARB = 70;  // gr. carb
            double MAX_CARB = 250; //gr carb

            double[] initialVector = this.ParticlePatient.GetDataFromTime(starttime); // deze data zit nog niet in pf!
            Schedule baseTestSchedule = scheduleToUse.CropSchedule2Copy(starttime, stopTime, false);
            double[] carbs123 = new double[] { MIN_CARB, MID_CARB, MAX_CARB };
            double[] errors123 = new double[3];

            SortedDictionary<double, double> carb2errorMap = new SortedDictionary<double, double>();

            // steeds 3 punten pakken, en de PARABOOL er doorheen, en dan het dal bepalen.
            // het dal en twee oude punten gebruiken voor nieuwe dal bepaling, 
            // etc..
            int teller = 0;
            double prevBestCarb = carbs123[1]; //mid
            while (teller <= particleFilter.settingsForParticleFilter.MaxNrStepsInParabolicSearch)
            {
                for (int i = 0; i < 3; i++)
                {
                    double carbHyp = carbs123[i];
                    if (!carb2errorMap.ContainsKey(carbHyp))
                    {
                        teller++;
                        Schedule testSchedule = baseTestSchedule.DeepCopy();
                        testSchedule.UpdateEventAtTime(eventTime, carbHyp);
                        // Create a new patient with the same parameters as the patient
                        Particle parabolicSearchParticle = new Particle(this); //dit doet GEEN deep copy van particlePatient
                                                                               // dit maakt intern een nieuwe patient aan, dus de oude particlePatient (van de input patient) blijft onveranderd
                        parabolicSearchParticle.Evaluate_simple(errorCalculationSettings, testSchedule, starttime, stopTime, initialVector); //, false);

                        // Retrieve the SSE and calc. new search step & direction
                        carb2errorMap[carbHyp] = parabolicSearchParticle.RMSE_ML2Mmnts; // .Weighted_RMSE_ML2Mmnts_after_carbs_per_bin; //of op smoothed?
                    }
                    errors123[i] = carb2errorMap[carbHyp];
                }
                // parabola bepalen:
                Tuple<double, double, double, double> parabola = MyMath.FindParabolaParameters(carbs123, errors123);
                if (parabola == null)
                {
                    break;
                }


                if (parabola.Item1 < 0)
                {
                    // bergparabool... ??? wat nu? -- todo: zie subpopulatie parabolic search...
                    //   Console.WriteLine("bergparabool!!!???");
                    break;
                }
                //vervang carb met grootste error:
                int ndx = MyMath.GetMaxIndex(errors123);
                if (parabola.Item4 < 0) // || parabola.Item4 > MAX_CARB)
                {
                    // te klein / groot  ... 
                    // pak punt tussen 2  laagste / hoogste waardes
                    if (parabola.Item4 < 0)
                    {
                        ndx = MyMath.GetMaxIndex(carbs123);

                    }
                    else
                    {
                       // slechtste eruit halen
                        ndx = MyMath.GetMaxIndex(carbs123);

                    }
                    int ndx0, ndx1;
                    if (ndx == 0) { ndx0 = 1; ndx1 = 2; }
                    else if (ndx == 1) { ndx0 = 0; ndx1 = 2; }
                    else { ndx0 = 0; ndx1 = 1; }
                    carbs123[ndx] = (carbs123[ndx0] + carbs123[ndx1]) / 2;
                }
                else
                {
                    // pak de piek
                    carbs123[ndx] = parabola.Item4; // nieuwe is laagste punt vd parabola
                }

                // zijn we klaar?
                if (Math.Abs(carbs123[ndx] - prevBestCarb) < MIN_CARB_DELTA)
                {
                    break;
                }
                prevBestCarb = carbs123[ndx];
            }


            int minndx = MyMath.GetMinIndex(errors123);
            return new Tuple<double, double>( carbs123[minndx], errors123[minndx]);
        }





        public VirtualPatient ParticlePatient { get; set; }




        public double[] GetCopyOfModifiedModelStartVector(uint time)
        {
            double[] startvector = null;
            if (!IsOrphan())
            {
                if (subPopulatie.BestPatientGeneratedData != null)
                {
                    startvector = subPopulatie.BestPatientGeneratedData.GetValuesFromTime(time);
                }
                else
                {
                    //legitiem, als input null is
                    Console.WriteLine("ERROR?!?!!?");
                }
            }

            if (startvector == null)
            {
                // dan maar aan hub vragen
                particleFilter.BestPatientGeneratedData.TryGetValuesFromTime(time, out startvector);
            }

            if (startvector == null)
            {
                //throw new ArgumentException("mag niet voorkomen!");
                return null;
            }
            // voor zekerheid altijd een copy zodat we de originele databron niet vervuilen bij volgende solver actie
            return CloneUtilities.CloneArray(startvector);
        }


        private void SetModifiedModelStartVector(double[] startvector, uint starttime)
        {
            if (startvector == null)
            {
                // Geeft een copy terug
                startvector = GetCopyOfModifiedModelStartVector(starttime);
            }
            if (startvector == null)
            {
                throw new ArgumentException("startvector == null! Dat mag niet voorkomen!");
            }
            modifiedModelStartVector_ = startvector;

            uint tm = (uint)Math.Round(this.modifiedModelStartVector[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]);
            if (tm != starttime)
            {
                Console.WriteLine("oeps!!!!");
                //throw new ArgumentException("oeps@!");
                Console.WriteLine("tm = " + tm);
                Console.WriteLine("starttime = " + StartTime);
            }
        }

        // scheduleToUse heeft bij true/noisy value hetzelfde, namelijk de NOISY value die de ML ziet.
        // dus particle kan gewoon de 'true' value gebruiken want dat is al geen echte ground thruth data meer
        //
        // Evaluate_Simple is bevat geen random mommenten
        public void Evaluate_simple(ErrorCalculationSettings errorCalculationSettings, Schedule scheduleToUse, uint starttime, uint stopTime, double[] startvector = null)//, bool useCarbHyp = true)
        {
            this.StartTime = starttime;
            SetModifiedModelStartVector(startvector, starttime);


            // patient van Particle maken:
            ParticlePatient = BuildPatientFromParticle(scheduleToUse);//, useCarbHyp);


            //// simulate the patient.
            RandomStuff random = particleFilter.random; // is dit verstandig?
            if (subPopulatie != null) { random = subPopulatie.random; }
            particleFilter.simulator.RunOnePatient(random, ParticlePatient, starttime, (int) stopTime, SolverInterval);

            this.CalculateErrorOnObservations(errorCalculationSettings, (int)starttime, (int)stopTime);
        }






    }

}
