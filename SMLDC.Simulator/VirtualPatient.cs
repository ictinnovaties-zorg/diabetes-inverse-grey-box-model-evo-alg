using System.Collections.Generic;
using SMLDC.Simulator.Schedules;
using SMLDC.Simulator.Models.HeartRate;
using SMLDC.Simulator.Schedules.Events;
using System;
using SMLDC.Simulator.DiffEquations.Models;
using SMLDC.Simulator.DiffEquations.Solvers;
using static SMLDC.Simulator.Utilities.Enums;
using SMLDC.Simulator.Utilities;
using SMLDC.Simulator.Helpers;


namespace SMLDC.Simulator
{
    public class VirtualPatient
    {

        public bool LockMLtoNoisyMmnts = false; // alleen true voor ML patients

        public PatientSettings settings;
        public PatientSettings GetSettingsClone()
        {
            if (settings == null) { return null; }
            return new PatientSettings(settings);
        }

        //public VirtualPatient BestParticlePatient;


        // public bool isLast;

        private int _id;
        public int ID { get { return _id; } }

        public override string ToString()
        {
            return "Patient #" + ID + " (" + PatientType + ")";
        }


        public Schedule TrueSchedule;
        public Schedule NoisySchedule;
            

        public double GetHeartRate(uint time)
        {
            return NoisySchedule.GetHeartRate(time);
        }
        public double GetHrBaseEstimate(uint start, uint end)
        {
            return NoisySchedule.GetHrBaseEstimate(start, end);
        }
        public void SetHeartRateGenerator(AbstractHeartRateGenerator heartRateGenerator)
        {
            TrueSchedule.SetHeartRateGenerator(heartRateGenerator);
            if (NoisySchedule != null) { NoisySchedule.SetHeartRateGenerator(heartRateGenerator); }
        }

        public GlucoseInsulinSimulator simulator;
        public RandomStuff Random { get { return simulator.randomForSchedule; } }
        //construtor:
        public VirtualPatient(VirtualPatient vip)
                            : this(0, vip.Model, vip.settings, vip.TrueSchedule, null)
        {
            this.simulator = vip.simulator;
        }
        public VirtualPatient(VirtualPatient vip, Schedule schedule, double[] startvector)
                   : this(0, vip.Model, vip.settings, schedule, startvector)
        {
            this.simulator = vip.simulator;
        }
        public VirtualPatient(int id, BergmanAndBretonModel model, PatientSettings settings,
                                Schedule schedule)
                            : this(id, model, settings, schedule, null) { }
        public VirtualPatient(int id, BergmanAndBretonModel model, PatientSettings settings,
                                Schedule schedule, double[] startvector) {
            this._id = id;
            this.Model = new BergmanAndBretonModel(model);
            if (settings != null)
            {
                this.settings = new PatientSettings(settings);
            }
            else
            {
                // overbodig
                this.settings = null;
            }
            this.TrueSchedule = schedule;
            this.PatientType = PatientTypeEnum.STANDARD;

            if (startvector == null)
            {
                // TODO: wat als we niet op t=0 willen starten???
                startvector = this.Model.GenerateInitialValues();
            }
            this.StartTime = (uint)startvector[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN];
            this.GeneratedData = SolverResultFactory.CreateSolverResult(startvector);
        }


        public VirtualPatient(int id, Schedule schedule)
        {
            this._id = id;
            this.TrueSchedule = schedule;
            RealData = true;
            // een real patient genereert uiteraard geen virtuele data. De GeneratedData vullen met de 
            // echte gluc. mmnts (rest v/d code, zoals bv. logging, gaat uit van bestaan v/d generated data)
            double[] initvector = new double[BergmanAndBretonModel.GetNrOfParameters(true)];
            initvector[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN] = 0;
            initvector[BergmanAndBretonModel.G_Glucose_ODEindex_MG_per_DL] = schedule.GetMmntEventFromIndex(0).Glucose_TrueValue_in_MG_per_DL;
            PatientEvent lastmmntEvent = schedule.GetMmntEventFromIndex(schedule.GetMmntCount() - 1);
            uint aantal = lastmmntEvent.TrueStartTime + 1;
            this.GeneratedData = SolverResultFactory.CreateSolverResult(initvector, aantal );
            for(int i = 1; i < schedule.GetMmntCount(); i++)
            {
                uint time = schedule.GetMmntEventFromIndex(i).TrueStartTime;
                double[] data = new double[BergmanAndBretonModel.GetNrOfParameters(true)];
                data[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN] = time;
                data[BergmanAndBretonModel.G_Glucose_ODEindex_MG_per_DL] = schedule.GetMmntEventFromIndex(i).Glucose_TrueValue_in_MG_per_DL;
                this.GeneratedData.AddCopy(time, data);
            }
        }

        public bool RealData = false; // dummy
        public int RealDataPatientNumber
        {
            get
            {
                if (RealData)
                {
                    // tODO: dit lijkt wat omslachtig...kan dit elders?
                    return ((HeartRateReader)NoisySchedule.GetHeartRateGenerator()).RealDataPatientNumber;
                }
                else
                {
                    return -1;
                }
            }
        }


        //// compatible maken ///////////////////
        public PatientTypeEnum PatientType
        {
            get;
            set;
        }


        public BergmanAndBretonModel Model { get; set; }
        public bool UsesActivityModel { get { return Model.UseActivity; } }




        //todo:
        public double ICR = -9999;
        public double ISF = -9999;
        public double ForceCalculateISF(RandomStuff random)
        {
            ISF = ICR_ISF_calculation.CalculateISF(random, this);
            return ISF;
        }

        public double ForceCalculateICR(RandomStuff random)
        {
            ICR = ICR_ISF_calculation.CalculateICR(random, this);
            return ICR;
        }







        public uint StartTime { get; set; }






        /////////////////////////////////////////////  GENERATED DATA /////////////////////////////////////////////////////
        /////////////////////////////////////////////  GENERATED DATA /////////////////////////////////////////////////////
        /////////////////////////////////////////////  GENERATED DATA /////////////////////////////////////////////////////
        /////////////////////////////////////////////  GENERATED DATA /////////////////////////////////////////////////////
        /////////////////////////////////////////////  GENERATED DATA /////////////////////////////////////////////////////


        // todo: deze kan nog niet private zijn, omdat particle en subpop 'm gebruiken
        public SolverResultBase GeneratedData { get; set; }

        public void AddSolverResult(SolverResultBase result) { GeneratedData.AddCopy(result); }

        // deze wrappers maken het mogelijk om makkelijk van implementatie te switchen (zonder allerlei interface geneuzel :-)
        public double[] GetDataFromTime(uint time) { return GeneratedData.GetValuesFromTime(time); }
        public double[] GetDataFromIndex(int ndx) { return GeneratedData.GetValuesFromIndex(ndx); }
        public double[] GetStartData() { return GeneratedData.GetValuesFromIndex(0); }
        public void OverrideLastGeneratedData(double[] calculatedValuesToOverride) { GeneratedData.OverwriteLast(calculatedValuesToOverride); }
        public double[] GetValuesFromTime(uint time) { return GeneratedData.GetValuesFromTime(time); }
        public int GeneratedDataGetCount() { return GeneratedData.GetCount(); }
        public bool GeneratedDataIsNull() { return GeneratedData == null; }
        //public void ResetGeneratedData() { GeneratedData = new SolverResultBase(1); }
        public double[] GetLastData() { return GeneratedData.GetLastValues(); }
        public bool TryGetDataFromTime(uint time, out double[] values) { return GeneratedData.TryGetValuesFromTime(time, out values); }
     //   public bool TryGetDataFromTime_reuse(uint time, ref double[] reusable_array) { return GeneratedData.TryGetValuesFromTime_reusable(time, ref reusable_array); }
        public uint GetTimeFromIndex(int ndx) { return GeneratedData.GetTimeFromIndex(ndx); }


        public Tuple<List<uint>, List<double>> GetGeneratedDataForTimestamps(List<uint> times)
        {
            List<uint> mmnt_times = new List<uint>();
            List<double> mmnts = new List<double>();
            for (int i = 0; i < times.Count; i++)
            {
                uint time = times[i];
                if (GeneratedData.ContainsTime(time))
                {
                    double[] values = GeneratedData.GetValuesFromTime(time);
                    if (values != null)
                    {
                        mmnt_times.Add(time);
                        mmnts.Add(values[BergmanAndBretonModel.G_Glucose_ODEindex_MG_per_DL]);
                    }
                }
            }
            return new Tuple<List<uint>, List<double>>(mmnt_times, mmnts);
        }






        /////////////////////////////// events /////////////////////////
        /////////////////////////////// events /////////////////////////
        /////////////////////////////// events /////////////////////////
        /////////////////////////////// events /////////////////////////
        /////////////////////////////// events /////////////////////////
        /////////////////////////////// events /////////////////////////


        public void HandleEvent(RandomStuff random, PatientEvent evt)
        {
            if (evt.EventType == PatientEventType.INSULIN)
            {
                HandleAddInsulinEvent(evt);
            }
            else if (evt.EventType == PatientEventType.CARBS)
            {
                HandleMealEvent(evt);
            }
            else if (evt.EventType == PatientEventType.GLUCOSE_MASUREMENT)
            {
                HandleGlucoseMeasurement(evt);
            }
        }


        // gebruikt dit om de ML patient te locken op de (noisy, smoothed?) gluc. waardes
        private void HandleGlucoseMeasurement(PatientEvent evt)
        {
            double[] currentValues = GetLastData();
            currentValues[BergmanAndBretonModel.G_Glucose_ODEindex_MG_per_DL] = evt.TrueValue;
            OverrideLastGeneratedData(currentValues);
        }

        private void HandleAddInsulinEvent(PatientEvent evt)
        {
            if (evt.EventType != Enums.PatientEventType.INSULIN) { throw new ArgumentException("verkeerde event type!"); }

            BolusAdvice advice = BolusCalculations.CalculateBolus(simulator.randomForSchedule, this, evt);
            if (advice.Type == BolusAdviceType.INSULIN)
            {
                evt.Insulin_TrueValue_in_IU = advice.Quantity;
                //    evt.NoisyValue = advice.Quantity;
                if (evt.Insulin_TrueValue_in_IU != 0)
                {
                    double[] currentValues = this.GetLastData();
                    Model.HandleAddInsulinEvent(evt, currentValues);
                    OverrideLastGeneratedData(currentValues); //is misschien overbodig als dit geen copy is
                }
            }
            else
            {
                evt.TrueValue = 0;
                //     evt.NoisyValue = 0;
                // een slimme patient zou nu iets gaan eten!
                // .... 
            }

        }

        private void HandleMealEvent(PatientEvent evt)
        {
            if (evt.EventType != Enums.PatientEventType.CARBS) { throw new ArgumentException("verkeerde event type!"); }

            double[] currentValues = GetLastData();
            Model.HandleEatingEvent(evt, currentValues);
            OverrideLastGeneratedData(currentValues);
        }



        public double FoodForgetFactor
        {
            get { return settings.FoodForgetFactor; }
        }
        public double FoodNoiseFactor
        {
            get { return settings.CarbNoiseFactor; }
        }

        public static double foodNoiseIsPresentFromThisDay = -1.5; // na # dagen foodnoise aanzetten (indien >= 0)


      


        ///////////////////////////////////// GLUC mmnt noise ///////////////////////////////
        ///////////////////////////////////// GLUC mmnt noise ///////////////////////////////
        ///////////////////////////////////// GLUC mmnt noise ///////////////////////////////
        ///////////////////////////////////// GLUC mmnt noise ///////////////////////////////
        ///////////////////////////////////// GLUC mmnt noise ///////////////////////////////
        ///////////////////////////////////// GLUC mmnt noise ///////////////////////////////
        ///////////////////////////////////// GLUC mmnt noise ///////////////////////////////


        public bool SimulateNoisyGlucoseMmnts
        {
            get { return (GlucNoiseFactor > 0); }
        }
        public double GlucNoiseFactor
        {
            get { return settings.GlucNoiseFactor; }
        }

        public void CreateNoisySchedule()
        {
            if (this.RealData)
            {
               Console.WriteLine("Patient met real data! Berekent geen bolus adviezen!");
                return;
            }
            NoisySchedule = new Schedule();
            CopyInsulinEventsToNoisy();
            // TODO: hier ook carb noisy, zie ScheduleGenerator. Is voorlopig daar omdat het (ongeveer) per dag moet
            // alternatief: uitzoeken of de noise ongeveer zelfde stddev heeft ongeacht grootte v/d maaltijd, dan kan het makkelijk hier
            // anders moet het hier in blokken van een dag bepaald worden.
            CopyCarbEventsToNoisyOrForget();

            IntroduceTimingNoise();
            GenereerGlucoseMmntsAndNoise();
            NoisySchedule.SetHeartRateGenerator(TrueSchedule.GetHeartRateGenerator());

        }


        private void CopyCarbEventsToNoisyOrForget()
        {
            //// TODO: op dit moment is noisy al gevuld (in scheduleGEnerator) Refactor!
            //// dus alleen carbs weghalen
            if (this.RealData)
            {
                throw new ArgumentException("Patient met real data! Berekent geen bolus adviezen!");
            }

            //    return;
            for (int i = 0; i < TrueSchedule.GetEventCount(); i++)
            {
                PatientEvent evt = TrueSchedule.GetEventFromIndex(i);
                if (evt.EventType == PatientEventType.CARBS)
                {
                    if (!simulator.randomForSchedule.NextBool(settings.FoodForgetFactor))
                    {
                        // uit eerder onderzoek: stddev <= 17.3 (wisselt per maaltijd, eigenlijk curve gebruiken, en ook offset nog meenemen!!!)
                        double sigma = settings.CarbNoiseFactor * 17.3;
                        double noisyCarbs = simulator.randomForSchedule.NormalDistributionSampleClipped(Globals.maxSigma, evt.Carb_TrueValue_in_gram, sigma);
                        noisyCarbs = Math.Max(1, noisyCarbs);
                        NoisySchedule.AddUniqueEventWithoutSorting(PatientEventType.CARBS, evt.TrueStartTime, noisyCarbs);
                    }
                }
            }
            NoisySchedule.SortSchedule();
        }


        
        private void CopyInsulinEventsToNoisy()
        {
            if (this.RealData)
            {
                throw new ArgumentException("Patient met real data! Berekent geen bolus adviezen!");
            }
            for (int i = 0; i < TrueSchedule.GetEventCount(); i++)
            {
                PatientEvent evt = TrueSchedule.GetEventFromIndex(i);
                if(evt.EventType == PatientEventType.INSULIN)
                {
                    NoisySchedule.AddUniqueEventWithoutSorting(PatientEventType.INSULIN, evt.TrueStartTime, evt.Insulin_TrueValue_in_IU);
                }
            }
            NoisySchedule.SortSchedule();
        }

        private void IntroduceTimingNoise()
        {
            //// noise op de 'toedieningstijd':
            /// dit doen we hier pas, omdat ....????
            if (this.settings.CarbTimeNoiseSigma > 0)
            {
                for (int e = 0; e < NoisySchedule.GetEventCount(); e++)
                {
                    PatientEvent noisyEvent = NoisySchedule.GetEventFromIndex(e);
                    if(noisyEvent.EventType != PatientEventType.CARBS) { continue; }
                    uint newTime = 0;
                    do
                    {
                        double rndTime = simulator.randomForSchedule.NormalDistributionSampleClipped(Globals.maxSigma, noisyEvent.TrueStartTime, this.settings.CarbTimeNoiseSigma);
                        newTime = (uint)rndTime; // Math.Max(0, Math.Min(rndTime, 23 * 60 + 59)); // altijd binnen de dag, omdat we hier per dag genereren!
                        if (newTime == noisyEvent.TrueStartTime)
                        { // ook ok
                            break;
                        }
                    } while (NoisySchedule.ContainsTime(newTime));
                    noisyEvent.TrueStartTime = newTime;
                    NoisySchedule.SortSchedule(); //inefficient, maar schedules zijn klein genoeg dat dit toch bijna instantaan is.
                }
            }
            NoisySchedule.IntegrityCheck();
        }
          

        private void GenereerGlucoseMmntsAndNoise()
        {
            if (this.RealData)
            {
                throw new ArgumentException("Patient met real data! Berekent geen bolus adviezen!");
            }
            // vul true schedule met mmnts, en  noisy schedule met geruisde mmnts.
            for (int i = 0; i < TrueSchedule.GetMmntCount(); i++)
            {
                PatientEvent evt = TrueSchedule.GetMmntEventFromIndex(i);
                evt.TrueValue = this.GeneratedData.GetValuesFromTime(evt.TrueStartTime)[BergmanAndBretonModel.G_Glucose_ODEindex_MG_per_DL];
                PatientEvent noisyEvent = new PatientEvent(evt);
                double noise = CreateGlucoseMeasurementNoise(simulator.randomForSchedule, noisyEvent.Glucose_TrueValue_in_MG_per_DL);
                noisyEvent.Glucose_TrueValue_in_MG_per_DL = evt.Glucose_TrueValue_in_MG_per_DL + noise;
                NoisySchedule.AddUniqueEventWithoutSorting(noisyEvent);
            }
            NoisySchedule.SortSchedule();
            NoisySchedule.AddStopEvent();
        }


    




        // noise: BG < 100 --> errrange = +-0.83*18 = 14.94 mg/dl (95% v/d tijd, dus 2stddev naar links en naar rechts)
        // Bg > 100 --> 15% (95% vd tijd, dus 2 stddev)
        public double CreateGlucoseMeasurementNoise(RandomStuff random, double glucMmnt)
        {
            if (GlucNoiseFactor > 0)
            {
                double rnd = 0;
                double stddev = 7.47; // 14.94 * 0.5; //14.94 = 0.83 * 18,  is 2stdev
                if (glucMmnt > 100)
                {
                    stddev = 0.15 * glucMmnt * 0.5; // want 15% is 2 stddev
                }
                stddev = stddev * GlucNoiseFactor;

                rnd = random.Sample(); // mean=0, std=1.
                rnd = Math.Min(2.5, Math.Max(rnd, -2.5)); //aftoppen voor extremen op X stddev.
                rnd = rnd * stddev;
                //rnd zodanig kiezen dat gluc niet < 0 wordt
                if (glucMmnt + rnd < 0)
                {
                    rnd = -glucMmnt;
                }
                return rnd; // Math.Max(0, glucMmnt + rnd);
            }
            return 0;
        }




        ///////////////////////////////////// GLUC mmnt events ///////////////////////////////
        ///////////////////////////////////// GLUC mmnt events ///////////////////////////////
        ///////////////////////////////////// GLUC mmnt events ///////////////////////////////
        ///////////////////////////////////// GLUC mmnt events ///////////////////////////////
        ///////////////////////////////////// GLUC mmnt events ///////////////////////////////
        ///////////////////////////////////// GLUC mmnt events ///////////////////////////////
        ///////////////////////////////////// GLUC mmnt events ///////////////////////////////
        ///////////////////////////////////// GLUC mmnt events ///////////////////////////////

   
        public Tuple<List<uint>, List<double>, List<double>> GetMeasurementEventsInRange(int start, int stop)
        {
            List<uint> times = new List<uint>();
            List<double> trueValues = new List<double>();
            List<double> noisyValues = new List<double>();

            for (int i = 0; i < NoisySchedule.GetMmntCount(); i++)
            {
                PatientEvent evt = NoisySchedule.GetMmntEventFromIndex(i);
                uint time = evt.TrueStartTime;
                if (time < start)
                {
                    continue;
                }
                if (time > stop)
                {
                    // klaar
                    break;
                }
                times.Add(time);
                noisyValues.Add(evt.TrueValue);

                if (TrueSchedule != null)
                {
                    // true opvragen, zou in andere schema op exact zelfde tijd moeten zitten
                    TrueSchedule.TryGetMmntEvent(time, out PatientEvent trueEvent);
                    trueValues.Add(trueEvent.Glucose_TrueValue_in_MG_per_DL);
                }
            }
            if(trueValues.Count == 0) { trueValues = null; }
            return new Tuple<List<uint>, List<double>, List<double>>(times, trueValues, noisyValues);
        }


        
    }
}