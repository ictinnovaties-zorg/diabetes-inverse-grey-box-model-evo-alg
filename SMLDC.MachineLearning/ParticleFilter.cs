using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using SMLDC.Simulator;
using SMLDC.Simulator.DiffEquations.Models;
using SMLDC.Simulator.DiffEquations.Solvers;
using SMLDC.Simulator.Schedules;
using SMLDC.Simulator.Schedules.Events;
using SMLDC.Simulator.Utilities;
using static SMLDC.Simulator.Utilities.Enums;
using System.Diagnostics;
using SMLDC.MachineLearning.subpopulations;


/*
 *  TODO: translate and update this description!!!!
 *  
 *  
 * 
 * Deze ParticleFiltering class (samen met de Particle class) is de kern van de machine learning.
 * 
 * De particle filter wordt vanuit PatientController::Run aangeroepen.
 * 
 * De PF doet het volgende: Bij elke (relevante) event (zoals food, gluc.mmnt, etc) wordt een lading hypotheses (particles)
 * aangemaakt die allemaal een iets verschillende hypothetische patient beschrijven. Die hypothetische patienten worden
 * ook gerund (niet hun hele 'leven' vanaf tijd = 0 toen originele patient begon, maar bv. vanaf 10 events terug [een trail] ).
 * De particles worden dus gerund, en kunnen daarna vergeleken worden (w.b.t. gemeten gluc.) met de originele te obesrveren patient.
 * De particles die de beste voorspelllingen hebben gedaan voor het hele traject, hebben een grotere kans om naar de nieuwe ronde
 * (bij de volgende event) door te gaan. Ze worden dan (willekeurig) wat gemuteerd, en er worden nieuwe particles aangemaakt
 * om de slechtste te vervangen. Dit gebeurt random, waarbij betere particles hogere kans hebben om geselecteerd te worden als basis 
 * voor de nieuwe.
 *
 * De manier waarop de particle hypotheses en de echte patient data vergeleken worden, staat beschreven in deliverable 4 (analysis). 
 * Idem voor het particle filtering proces.
 * 
 * opmerking; Deze simulatie tool is begonnen als studentenproject. Zij hebben het programma opgezet op een generieke wijze, omdat toen
 * nog niet volledig duidelijk was op welke manier er met hun product zou worden verder gegaan (en omdat het een eis van het desbetreffende
 * vak was). Vandaar dat de 'oudere' delen van de code opgezet zijn om gebruik te maken van meerdere fysieke patient modellen (bergman, extended
 * bergman, etc). Maar het particle filter onderzoek is alleen gedaan op het extended (bergman) model. Om dit snel werkend te krijgen,
 * wordt hier en daar de aanname gedaan dat het in de simulatie gebruikte model het extended model is, en wordt het naar dat model gecast.
 * Alternatieve modellen worden nu niet gebruikt. Wanneer dat in de toekomst wel het geval zou zijn, dan moet de particle filter code
 * generieker gemaakt worden, zodat ze niet afhangt van het extended bergman model. Voor dit onderzoek was het niet nodig, en de particle
 * filtering is dus in zekere zin later in 'gehackt' :-)  omdat generaliseren op het moment van onderzoek doen simpelweg te lastig is omdat 
 * je nog niet weet hoe het onderzoek zal verlopen, en het dan bijna niet te doen is om daar bij het ontwerp rekening mee te houden. Een 
 * refactoring slag zou gemaakt moeten worden om de model specifieke 'hacks' om te zetten naar iets generieks, zodat deze simulator
 * geschikt kan worden gemaakt voor andere modellen. 
 * 
 * Het is voor het proof-of-concept van Machine Learning met een virtuele patient gelukkig niet nodig, en het direct gebruiken van het 
 * extended model, ipv alles via interfaces en generiek, maakt de structuur wel duidelijker (minder tussenlagen)
 * 
 * opmerking: de originele unit tests (gemaakt door de studentengroep, voor de originele simulatie) zijn niet aangepast 
 * aan de machine learning. Het test project is daarom verwijderd uit het project.
 * 
 * Wilco Moerman
 * cwj.moerman@windesheim.nl
 * 
 */



namespace SMLDC.MachineLearning
{


    // deze class is eigenlijk een verzameling particle filters (de subpop's zijn de echte pf) en niet slechts 1 pf.
    public class ParticleFilter
    {
        public bool DISABLED_WARNING = true;

        // als true, dan krijgen ALLE particles de echte VIP waardes (en zouden errors 0 moeten zijn).        
        // oa handig om te checken of alles goed gaat met noisy mmnts en offsets enzo
        public void GiveParticleTheRealParameters_Hack(Particle p)
        {
            if (!settingsForParticleFilter.ML_PERFECT_HACK) { return; }

            BergmanAndBretonModel rModel = ObservedPatient.Model;
            p.model.Gb_in_MG_per_DL = rModel.Gb_in_MG_per_DL;
            p.model.Vb_in_L = rModel.Vb_in_L;
            p.model.Vi_in_L = rModel.Vi_in_L;
            p.model.P1 = rModel.P1;
            p.model.P2 = rModel.P2;
            p.model.P3 = rModel.P3;
            p.model.P4 = rModel.P4;
            p.model.DRate = rModel.DRate;
            p.model.Carbs2Gluc = rModel.Carbs2Gluc;

            if (UseActivityModel)
            {
                p.model.BaseHeartRate = rModel.BaseHeartRate;
                p.model.GammaFaFactor = rModel.GammaFaFactor;
                p.model.alpha = rModel.alpha;
                p.model.beta = rModel.beta;
                p.model.GammaFnPower = rModel.GammaFnPower;
                p.model.TauGamma = rModel.TauGamma;
                p.model.tauZ = rModel.tauZ;
            }
        }



        /////////////////////////// carbhypothesis ////////////////////////
        /////////////////////////// carbhypothesis ////////////////////////
        /////////////////////////// carbhypothesis ////////////////////////
        /////////////////////////// carbhypothesis ////////////////////////
        public CarbHypothesis learningCarbHypothesis;

        public void UpdateCarbEstimation(Particle particle, double learningrate, uint time, uint nieuwetijd, double estCarbs)
        {
            learningCarbHypothesis.UpdateCarbEstimation(particle, learningrate, time, nieuwetijd, estCarbs);
        }



        ////////////////////////////////// Particle filter subpopulaties ////////////////////////////
        ////////////////////////////////// Particle filter subpopulaties ////////////////////////////
        ////////////////////////////////// Particle filter subpopulaties ////////////////////////////
        ////////////////////////////////// Particle filter subpopulaties ////////////////////////////
        ////////////////////////////////// Particle filter subpopulaties ////////////////////////////
        public List<SubPopulatie> subPopuluaties;
        public int subPopulatieTeller { get { return subPopuluaties.Count; } }


        private static readonly object particle_counter_lock_object = new object();
        private static int particle_counter = 0;
        public int NextParticleID
        {
            get
            {
                lock (particle_counter_lock_object)
                {
                    particle_counter++;
                    return particle_counter;
                }
            }
        }

        public Particle BestParticleWithRealCarbs { get; set; }
        public Particle BestParticle { get; set; }

        private SolverResultBase bestPatientWithPerfectCarbsGeneratedData;
        private SolverResultBase bestPatientGeneratedData_met_NaN_gaten;
        private SolverResultBase bestPatientGeneratedData;
        public SolverResultBase BestPatientGeneratedData
        {
            get
            {
                if (bestPatientGeneratedData == null || bestPatientGeneratedData.GetCount() == 0) { return null; }
                else { return bestPatientGeneratedData; }
            }
        }
        public uint MAX_oudeSubpopBestParticles_LENGTH { get { return this.settingsForParticleFilter.MaximumHistoryQueueLength; } }
        private List<Particle> historySubpopBestParticlesList = new List<Particle>(); // als een subpop verstart, dan hierin de op dat moment beste particle bewaren voor later gebruikk


        // subs moeten hun eigen random meegeven, omdat we anders hier nondeterminisme krijgen, omdat moment van registeren afhangt van 
        // thread scheduling.
        public bool IsBezet(Particle particle) { return nearestParticleHashing.IsBezet(particle); }
        private NearestParticleHashing nearestParticleHashing;
        public BergmanAndBretonModel GetRandomSample(RandomStuff random, Particle particle) { return nearestParticleHashing.GetRandomSample(random, particle); }


        private int stagnatieTeller = 0;
        private StringBuilder stagnatieLog = new StringBuilder();
        private static readonly object _registreerStagnatie_lockObject = new object();
     

        public Particle RegistreerStagnatie(RandomStuff random, Particle particle, bool forceer = false)
        {
            lock (_registreerStagnatie_lockObject)
            {
                stagnatieTeller++;
                if (particle != null)
                {
                    stagnatieLog.Append(particle.subPopulatie.ID + " exploreert, ");
                    Particle cloneParticle = new Particle(particle);
                    historySubpopBestParticlesList.Add(cloneParticle);
                    cloneParticle.MakeOrphan();
                }
                if (forceer || (historySubpopBestParticlesList.Count >= 1 && random.NextDouble() <= settingsForParticleFilter.ProbabilityOfSamplingFromBestHistoryToNewSubpopulation))
                {
                    //oude oudeSubpopBestParticles terug als nieuwe seed
                    //todo: voorkeur geven midden uit queue ?
                    int rnd_ndx = (int)Math.Abs(random.NormalDistributionSample( historySubpopBestParticlesList.Count * 0.5, historySubpopBestParticlesList.Count * 0.25 ));
                    rnd_ndx = rnd_ndx % historySubpopBestParticlesList.Count;
                    Particle seed = new Particle(historySubpopBestParticlesList[rnd_ndx]);
                    return seed;
                }
                else
                {
                    if (particle == null)
                    {
                        return new Particle(historySubpopBestParticlesList[0]);
                    }
                    BergmanAndBretonModel newRandomModel = GetRandomSample(random, particle);
                    Particle newSample = new Particle(particle);
                    newSample.model = newRandomModel;
                    return newSample;
                }
            }
        }

        ///////////////////////////////////////// Constructor, attributen /////////////////////////////////////
        ///////////////////////////////////////// Constructor, attributen /////////////////////////////////////
        ///////////////////////////////////////// Constructor, attributen /////////////////////////////////////
        ///////////////////////////////////////// Constructor, attributen /////////////////////////////////////
        ///////////////////////////////////////// Constructor, attributen /////////////////////////////////////
        ///////////////////////////////////////// Constructor, attributen /////////////////////////////////////
        ///////////////////////////////////////// Constructor, attributen /////////////////////////////////////

        public int RealDataPatientNumber
        {
            get { return ObservedPatient.RealDataPatientNumber; }
        }

        public double[] ObservedPatientOriginalStartVector { get; }


        public VirtualPatient ObservedPatient { get; set; } // observed/real/groundtruth patient
        public Schedule RealOriginalSchedule { get; set; }


        private ParticleFilterDataLogger pfDataLogger; //voor latere logging:
        public ParticleFilterSettings settingsForParticleFilter;
        public SettingsLogger settingsLogger;

        public int PATIENT_INDEX { get { return ObservedPatient.ID; } }

        public GlucoseInsulinSimulator simulator;
        public string ConfigIniPath { get { return simulator.ConfigIniPath; } }


        public MidpointSolver MyMidpointSolver;
        public RandomStuff random;
        public string ID;
        private static int ID_COUNTER = 0;


        private string DateString;
        public string GetDateString()
        {
            return DateString;
        }


        //constructor
        public ParticleFilter(GlucoseInsulinSimulator simulator, VirtualPatient _patient, ParticleFilterSettings _settingsForParticleFilter, string dateStringForLogging = null)
        {
            if (dateStringForLogging == null)
            {
                DateString = DateTime.Now.ToString(@"yyyy\-MM\-dd~~HH\h\-mm\m\-ss\s-") + DateTime.Now.Millisecond + "ms"; // exporter.GetCurrentDateString();
            }
            else
            {
                DateString = dateStringForLogging;
            }
            if (!Globals.IsVirtual())
            {
                System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
            }
            PowerUtilities.PreventPowerSave();
            this.simulator = simulator;
            MyMidpointSolver = simulator.MyMidpointSolver;

            settingsForParticleFilter = _settingsForParticleFilter;
            if (Globals.SeedForParticleFilter < 0)
            {
                random = new RandomStuff();
                Globals.SeedForParticleFilter = random.GetRandomSeed();
            }
            else
            {
                random = new RandomStuff(Globals.SeedForParticleFilter + ID_COUNTER);
                Globals.SeedForParticleFilter = random.GetRandomSeed() - ID_COUNTER;
            }
            settingsForParticleFilter.seedForParticleFilter = random.GetRandomSeed();

            MyMidpointSolver.ResetNrOfSolverSteps(); // de stappen om de VIP te maken, tellen uiteraard niet mee!

            ObservedPatient = _patient;
            this.ID = "#" + ID_COUNTER + "_patient" + PATIENT_INDEX;
            ID_COUNTER++;


            if (ObservedPatient.RealData)
            {
                //   throw new ArgumentException("noise op 0 oid ???");
                this.ObservedPatientOriginalStartVector = BergmanAndBretonModel.GenerateInitialValues(true /*aanname: altijd activity*/);
            }
            else
            {
                // TODO: in nieuwe sim implementeren
                // for (uint u = 2; u < 20; u++)
                {
                    //uint u = 7;
                    //ICR_ISF_calculation.TestDuration_in_min = u * 60;
                    //Console.WriteLine("duur = " + u + " uur");
                    double icr = ObservedPatient.ForceCalculateISF(random);
                    double isf = ObservedPatient.ForceCalculateICR(random);
                    Console.WriteLine("icr = " + icr);
                    Console.WriteLine("isf = " + isf);
                    if (icr < settingsForParticleFilter.ICR_lower_bound || icr > settingsForParticleFilter.ICR_upper_bound
                        || isf < settingsForParticleFilter.ISF_lower_bound || isf > settingsForParticleFilter.ISF_upper_bound)
                    {
                        throw new ArgumentException("patient heeft onjuiste ICR of ISF!");
                    }
                }
                this.ObservedPatientOriginalStartVector = CloneUtilities.CloneArray(ObservedPatient.GetStartData());
            }
            RealOriginalSchedule = ObservedPatient.TrueSchedule.DeepCopy();

            bestPatientGeneratedData = SolverResultFactory.CreateSolverResult(this.ObservedPatientOriginalStartVector);
            bestPatientGeneratedData_met_NaN_gaten = SolverResultFactory.CreateSolverResult(this.ObservedPatientOriginalStartVector);
            bestPatientWithPerfectCarbsGeneratedData = SolverResultFactory.CreateSolverResult(this.ObservedPatientOriginalStartVector);

            //loggers etc:
            settingsLogger = new SettingsLogger(this);


            // set sigmaStepSizes, gebaseerd op min-max (log)range:
            SIGMA_STEP_SIZES_logspace = new double[LOWER_HIGHER_BOUNDS.GetLength(0)];
            for (int paramNdx = 0; paramNdx < SIGMA_STEP_SIZES_logspace.Length; paramNdx++)
            {
                if (LOWER_HIGHER_BOUNDS[paramNdx, 1] <= LOWER_HIGHER_BOUNDS[paramNdx, 0])
                {
                    SIGMA_STEP_SIZES_logspace[paramNdx] = 0; //als de range 0 is (precies een waarde)
                }
                else
                {
                    SIGMA_STEP_SIZES_logspace[paramNdx] = Math.Log10(LOWER_HIGHER_BOUNDS[paramNdx, 1] / LOWER_HIGHER_BOUNDS[paramNdx, 0]);
                }
            }

            // maak alle subpopulaties
            subPopuluaties = new List<SubPopulatie>();
            for (int pop = 0; pop < this.NumberOfSubPopulations; pop++)
            {
                SubPopulatie sub;
                switch (settingsForParticleFilter.SearchInSubPopulationType)
                {
                    case SubPopulatieType.NORMAAL:
                        {
                            sub = new SubPopulatie(this);
                            break;
                        }
                    case SubPopulatieType.SEARCH_REPEATED_OVER_RANGE:
                        {
                            sub = new SubPopulatie_repeatedSearch(this);
                            break;
                        }
                    default:
                        {
                            throw new ArgumentException("onbekend type!");
                        }
                }
                subPopuluaties.Add(sub);
            }

            learningCarbHypothesis = new CarbHypothesis(this);


            if (ObservedPatient.RealData)
            {
                // we weten de TrueSchedule niet eens! Wat we ingelezen hebben, is al noisy
                ObservedPatient.NoisySchedule = ObservedPatient.TrueSchedule;
                ObservedPatient.TrueSchedule = null;
            }
            //else
            //{
                // CreateParticleFilterCommand first makes Simulation and ParticleFilter objects,
                // then runs the simulation, and only then calls the particleFilter.run()
                // so creating the 'noisy schedule' can only be done in pf.run()
            //}

            nearestParticleHashing = new NearestParticleHashing(this);

            pfDataLogger = new ParticleFilterDataLogger(this);
            pfDataLogger.Init();

            stopwatch.Start();
        }// ParticleFilter constructor





        private int mmntCounter_Evaluate = 0;
        public int MmntCounter_Evaluate { get { return mmntCounter_Evaluate; } }
        List<Tuple<int, int>> insulinTimeSlots;
        List<Tuple<PatientEvent, uint>> insulinEventsList;
        public void Run()
        {
            uint pfStartTime = ObservedPatient.StartTime;
            uint pfEndTime = ObservedPatient.NoisySchedule.GetLastTime();

            insulinEventsList = ObservedPatient.NoisySchedule.GetInsulineEvents();

            // insulinEventsList --> lijst met timeslots voor de errror calculation
            insulinTimeSlots = new List<Tuple<int, int>>();
            for (int pk = 0; pk < insulinEventsList.Count; pk++)
            {
                Tuple<PatientEvent, uint> piek = insulinEventsList[pk];
                PatientEvent insulineEvent = piek.Item1;
                int this_run_start = (int)piek.Item1.TrueStartTime - 1 + +(int)settingsForParticleFilter.LockPredictionsOnMmnts_Start_TimeWindow_in_min;
                int this_run_end = this_run_start + (int)settingsForParticleFilter.LockPredictionsOnMmnts_End_TimeWindow_in_min;
                // eerste timeslot v/a 0 laten lopen, anders gaat evalueren de eerste paar keer mis (en dit is makkelijkste fix)
                insulinTimeSlots.Add(new Tuple<int, int>( pk == 0 ? 0 : this_run_start, this_run_end));
            }
            insulinTimeSlots = null; //werkt niet!  hack hack hack


            // eerst maar eens de mmnts noisen, en de daadwerkelijke insuline waardes overnemen van true naar noisy
            // DAT KAN HIER PAS, in RUN, (vanwege hoe het in CreateParticleFilterCommand gaat, want daar
            // wordt eerst SIM en PF gemaakt en daarna pas de patient SIM gerund, en daarna PF.run()
            // dus als we noisy schedule willen maken in PF constructor, dan is de sim nog niet gerund en is er geen data.
            //ObservedPatient.CreateNoisySchedule();


            // en dan kan de ML beginnen:
            for (uint currentTime = pfStartTime; currentTime < pfEndTime; currentTime += settingsForParticleFilter.evaluateEveryNMinutes)
            {
                double simSecPerSec = 60000 * (settingsForParticleFilter.evaluateEveryNMinutes / (double)stopwatch.ElapsedMilliseconds);
                stopwatch.Restart();
                Console.WriteLine("======>>> run PF [" + ID + "]: @ t = " + currentTime + " (" + OctaveStuff.MyFormat(MyMidpointSolver.GetNrOfSolverSteps()) + " # [" + OctaveStuff.MyFormat(MyMidpointSolver.SolverStepsPerSecond()) + "/sec, " + OctaveStuff.MyFormat(simSecPerSec) + " sim/real]) <<<========");

                SetTrailStartTimes(currentTime);


                if (this.GetLongTrainTrailStartTime() < 0)
                {
                    if (!DISABLED_WARNING) { Console.WriteLine("Evauate::SKIPPING"); }
                }
                else {  
                     EvalueerEnResample(currentTime);

                    if (mmntCounter_Evaluate % Globals.logEveryNEvaluations == 0)
                    {
                        if (BestParticle != null)
                        {
                            pfDataLogger.LogToFile();
                        }
                    }

                    mmntCounter_Evaluate++;
                }
            }
            Console.WriteLine("PF finished");
            Finish();
        }




        //////////////////////////////// SETTINGS /////////////////////////////////////////
        //////////////////////////////// SETTINGS /////////////////////////////////////////
        //////////////////////////////// SETTINGS /////////////////////////////////////////
        //////////////////////////////// SETTINGS /////////////////////////////////////////

        public int SolverInterval { get { return settingsForParticleFilter.SolverInterval; } }

        public bool UseActivityModel
        {
            get
            {
                return settingsForParticleFilter.MLUseBretonActivityModel; // ObservedPatient.UsesActivityModel;
            }
        }
        public uint TrailLengthInMinutes { get { return settingsForParticleFilter.TrailLengthInMinutes; } }
        public uint NumberOfParticlesToKeep { get { return settingsForParticleFilter.NumberOfParticlesToKeep; } }
        public uint NumberOfSubPopulations { get { return settingsForParticleFilter.NumberOfSubPopulations; } }



        /////////////////////////////////////// FREE PARAMETERS (BERGMAN, BRETON, etc) ////////////////////////////////////////////
        /////////////////////////////////////// FREE PARAMETERS (BERGMAN, BRETON, etc) ////////////////////////////////////////////
        /////////////////////////////////////// FREE PARAMETERS (BERGMAN, BRETON, etc) ////////////////////////////////////////////
        /////////////////////////////////////// FREE PARAMETERS (BERGMAN, BRETON, etc) ////////////////////////////////////////////
        public double GetRealModelValue(int ndx) { return ObservedPatient.Model.GetParameter(ndx); }
        public double[,] LOWER_HIGHER_BOUNDS { get { return BergmanAndBretonModel.Get_LOWER_HIGHER_BOUNDS(); } }

        public double[] SIGMA_STEP_SIZES_logspace;


        

        // de diverse subpop zijn de 'diversity'.
        private Tuple<SortedDictionary<int, double>, SortedDictionary<int, double>> GetSubPopulationRMSEs()
        {
            SortedDictionary<int, double> rsme_ml2mmnts_Dict = new SortedDictionary<int, double>();
            SortedDictionary<int, double> rmse_ml2real_dict = new SortedDictionary<int, double>();
            for (int s = 0; s < subPopuluaties.Count; s++)
            {
                double value = Double.NaN;
                double value2 = Double.NaN;
                if (!subPopuluaties[s].IsInExplorePhase)
                {
                    value = MyMath.Clip(subPopuluaties[s].BestParticle.RMSE_ML2Mmnts, 0, 10000); // suffe overflow etc in octave::gnuplot
                    value2 = MyMath.Clip(subPopuluaties[s].BestParticle.RMSE_ML2Real_ALLEEN_VOOR_REFERENTIE, 0, 10000); // suffe overflow etc in octave::gnuplot
                }
                rsme_ml2mmnts_Dict[s] = value; //ml2noisy
                rmse_ml2real_dict[s] = value2; //ml2real
            }
            return new Tuple<SortedDictionary<int, double>, SortedDictionary<int, double>>(rsme_ml2mmnts_Dict, rmse_ml2real_dict);
        }








        private uint _currentStopTime_Evaluate_STORED;
        public uint GetCurrentTime() { return _currentStopTime_Evaluate_STORED; }//voor oa de particles.


        // carb trail alleen in de longtrain
        public uint GetCarbTrailStartSlopeTime()
        {
            return (uint)Math.Max(0, _currentStopTime_Evaluate_STORED - settingsForParticleFilter.NrXTrailForCarbSlopeStart * TrailLengthInMinutes);
        }
        public uint GetCarbTrailEndSlopeTime()
        {
            return (uint)Math.Max(0, _currentStopTime_Evaluate_STORED - settingsForParticleFilter.NrXTrailForCarbSlopeEnd * TrailLengthInMinutes);
        }




        public uint GetTrainTrailStopTime()
        {
            return (uint)Math.Max(0, _currentStopTime_Evaluate_STORED - settingsForParticleFilter.TrainTrailSkipFirstFraction * TrailLengthInMinutes);
        }


        // probleem: de oude best particle heeft voor (geschatte) interne waarden zoals X gezorgd.
        // die gebruiken we nu (bootstrappend) voor de volgende run. 
        // MAAR: wat als de bergman-param heel anders zijn? Stel dat de gevoeligheid voor X in G 
        // omhoog is gegaan. Gebruiken van oude data van curve gegenereerd met lagere gevoeligheid
        // is dan nu veel te hoog.
        // Is niet te voorkomen, maar misschien minimaliseren door als startpunt altijd een punt
        // te pakken dat ver van de laatse ins/carb events ligt?
        // dus: hier terugzoeken vanaf starpunt als starttijd te dicht op carb/ins ligt.
        // In dat geval iets verder terug beginnen, vlak voor carb/ins, zo dicht mogelijk op evenwichtstand (rond Gb en X = 0 etc)
        //
        // nb dit probleem is ook bij gewone korte evaluatie!!!! TODO;;;;



        public void SetTrailStartTimes(uint curtime)
        {
            _currentStopTime_Evaluate_STORED = curtime; //opslaan voor gebruik elders (tostring etc)
            SetTrainTrailStartTimes();
            if (_train_trail_starttime < 0) {
                _evaluatie_trail_starttime = -1;
                return;
            }

            Schedule scheduleToUse = this.ObservedPatient.NoisySchedule;

            _evaluatie_trail_starttime = (int) Math.Max(0, _long_train_trail_starttime - settingsForParticleFilter.TrailForEvaluationInMinutes);
            PatientEvent prev_carb_event = scheduleToUse.GetLastCarbEventBeforeTime(_evaluatie_trail_starttime);
            PatientEvent prev_ins_event = scheduleToUse.GetLastInsulinEventBeforeTime(_evaluatie_trail_starttime);
            int prev_time = (int)Math.Max((prev_carb_event != null ? prev_carb_event.TrueStartTime : 0), (prev_ins_event != null ? prev_ins_event.TrueStartTime : 0));
            while (Math.Abs(_evaluatie_trail_starttime - prev_time) < 120 && prev_time > 0 && !(prev_carb_event == null && prev_ins_event == null))
            {
                _evaluatie_trail_starttime = prev_time - 1;
                prev_carb_event = scheduleToUse.GetLastCarbEventBeforeTime(_evaluatie_trail_starttime);
                prev_ins_event = scheduleToUse.GetLastInsulinEventBeforeTime(_evaluatie_trail_starttime);
                prev_time = (int) Math.Max((prev_carb_event != null ? prev_carb_event.TrueStartTime : 0), (prev_ins_event != null ? prev_ins_event.TrueStartTime : 0));
            }
        }

        private int _evaluatie_trail_starttime = 0;
        private int _train_trail_starttime = 0;
        private int _long_train_trail_starttime = 0; // tegen overfitting

        public int GetEvaluatieTrailStartTime()
        {
            return _evaluatie_trail_starttime;
        }
        public int GetTrainTrailStartTime()
        {
            return _train_trail_starttime;
        }
        public int GetLongTrainTrailStartTime()
        {
            return _long_train_trail_starttime;
        }
        public void SetLongTrailStartTimeToZero()
        {
            _long_train_trail_starttime = 0;
        }
        public void SetTrainTrailStartTimes()
        {
            _long_train_trail_starttime = (int)_currentStopTime_Evaluate_STORED - (int)(settingsForParticleFilter.TrailLengthInMinutes);
            if (_long_train_trail_starttime < 0)
            {
                _train_trail_starttime = -1;
                return;
            }
            Schedule scheduleToUse = this.ObservedPatient.NoisySchedule;
            PatientEvent prev_carb_event = scheduleToUse.GetLastCarbEventBeforeTime(_long_train_trail_starttime);
            PatientEvent prev_ins_event = scheduleToUse.GetLastInsulinEventBeforeTime(_long_train_trail_starttime);

            uint prev_time = Math.Max((prev_carb_event != null ? prev_carb_event.TrueStartTime : 0), (prev_ins_event != null ? prev_ins_event.TrueStartTime : 0));
            while (Math.Abs(_long_train_trail_starttime - prev_time) < 120 && prev_time > 0 && !(prev_carb_event == null && prev_ins_event == null))
            {
                _long_train_trail_starttime = (int)prev_time + 1;
                prev_carb_event = scheduleToUse.GetFirstCarbEventAfterTime(_long_train_trail_starttime);
                prev_ins_event = scheduleToUse.GetFirstInsulinEventAfterTime(_long_train_trail_starttime);
                prev_time = Math.Max((prev_carb_event != null ? prev_carb_event.TrueStartTime : 0), (prev_ins_event != null ? prev_ins_event.TrueStartTime : 0));
            }

            _train_trail_starttime = (int)_currentStopTime_Evaluate_STORED - (int)(settingsForParticleFilter.TrailLengthInMinutes * settingsForParticleFilter.TrainTrailFase1Fraction);
            if (_train_trail_starttime < 0)
            {
                _long_train_trail_starttime = -1;
                return;
            }

            prev_carb_event = scheduleToUse.GetLastCarbEventBeforeTime(_train_trail_starttime);
            prev_ins_event = scheduleToUse.GetLastInsulinEventBeforeTime(_train_trail_starttime);
            prev_time = Math.Max((prev_carb_event != null ? prev_carb_event.TrueStartTime : 0), (prev_ins_event != null ? prev_ins_event.TrueStartTime : 0));
            while (Math.Abs(_train_trail_starttime - prev_time) < 120 && prev_time > 0 && !(prev_carb_event == null && prev_ins_event == null))
            {
                _train_trail_starttime = (int)prev_time + 1;
                prev_carb_event = scheduleToUse.GetFirstCarbEventAfterTime(_train_trail_starttime);
                prev_ins_event = scheduleToUse.GetFirstInsulinEventAfterTime(_train_trail_starttime);
                prev_time = Math.Max((prev_carb_event != null ? prev_carb_event.TrueStartTime : 0), (prev_ins_event != null ? prev_ins_event.TrueStartTime : 0));
            }


            // dit is nodig omdat we anders geen startvector kunnen bepalen. Dit is alleen de eerste keer!
            if (BestPatientGeneratedData.GetValuesFromTime((uint)GetLongTrainTrailStartTime()) == null)
            {
                _long_train_trail_starttime = 0;
            }

        }


        private Dictionary<Tuple<int, int>, Tuple<List<uint>, List<double>, List<double>, List<double>, Dictionary<uint, double>>> SmoothedNoisyMmntsCache = new Dictionary<Tuple<int, int>, Tuple<List<uint>, List<double>, List<double>, List<double>, Dictionary<uint, double>>>();
        private Queue<Tuple<int, int>> SmoothedNoisyMmntsCacheKeyQueue = new Queue<Tuple<int, int>>();
        public Tuple<List<uint>, List<double>, List<double>, List<double>, Dictionary<uint, double>> GetSmoothedNoisyMmnts(int begin, int end, uint smoothingRange)
        {
            lock (SmoothedNoisyMmntsCache)
            {
                while (SmoothedNoisyMmntsCacheKeyQueue.Count > 20)
                {
                    Tuple<int, int> deq_tup = SmoothedNoisyMmntsCacheKeyQueue.Dequeue();
                    SmoothedNoisyMmntsCache.Remove(deq_tup);
                }

                Tuple<int, int> key = new Tuple<int, int>(begin, end);
                if (SmoothedNoisyMmntsCache.ContainsKey(key))
                {
                    // al aanwezig, hergebruiken:
                    return SmoothedNoisyMmntsCache[key];
                }

                Tuple<List<uint>, List<double>, List<double>> lists = this.ObservedPatient.GetMeasurementEventsInRange(begin, end);

                List<uint> times = lists.Item1; //gesorteerd?
                List<double> trueValues = lists.Item2;
                List<double> noisyValues = lists.Item3;
                double[] kernel;
                if(smoothingRange < 3 || smoothingRange % 2 == 0)
                {
                    throw new ArgumentException("GetSmoothedNoisyMmnts(..., smoothingRange = " + smoothingRange + ")");
                }
                else if (smoothingRange == 3)
                {
                    kernel = new double[] { 1,  2,  1 };
                }
                else if (smoothingRange == 5)
                {
                    kernel = new double[] { 1,  2,  4,  2,  1 };
                }
                else if (smoothingRange == 7)
                {
                    kernel = new double[] { 1,  2,  4,  5,  4,  2,  1 };
                }
                else if (smoothingRange == 9)
                {
                    kernel = new double[] { 1,  2,  4,  4.5,  5,  4.5,  4,  2,  1 };
                }
                else
                {
                    throw new ArgumentException();
                }
                List<double> smoothedNoisyValues = MyMath.MultiplyWithKernel(noisyValues, kernel);

                Dictionary<uint, double> smoothedNoisyValuesDict = new Dictionary<uint, double>();
                for (int i = 0; i < times.Count; i++)
                {
                    uint time = times[i];
                    smoothedNoisyValuesDict[time] = smoothedNoisyValues[i];
                }
                Tuple<List<uint>, List<double>, List<double>, List<double>, Dictionary<uint, double>> tup = new Tuple<List<uint>, List<double>, List<double>, List<double>, Dictionary<uint, double>>(times, trueValues, noisyValues, smoothedNoisyValues, smoothedNoisyValuesDict);
                SmoothedNoisyMmntsCache[key] = tup;
                SmoothedNoisyMmntsCacheKeyQueue.Enqueue(key);
                return tup;
            }
        }


        public SortedDictionary<uint, double> slope1_FOR_LOGGING = new SortedDictionary<uint, double>();
        public SortedDictionary<uint, double> slope2_FOR_LOGGING = new SortedDictionary<uint, double>();
        public SortedDictionary<uint, double> slope3_FOR_LOGGING = new SortedDictionary<uint, double>();
        public SortedDictionary<uint, double> slope2_smoothed_FOR_LOGGING = new SortedDictionary<uint, double>();
        public SortedDictionary<uint, double> slope3_smoothed_FOR_LOGGING = new SortedDictionary<uint, double>();
        public SortedDictionary<uint, double> product2times3 = new SortedDictionary<uint, double>();
        private uint laatste_piek_gedetecteerd_tijd = 0;


        // todo: dit moet helemaal omgegooid: juist basiss in carbhyp nemen, en daarmee schedule vullen,
        // ipv schedule als basis, en carbs bij events opvragen. Want als een carb niet gelogd is, is er geen event
        // maar hooguit een detectie
        private Schedule PrepareScheduleInsertCarbHypotheses(Schedule scheduleToUse_orig, Particle particle)
        {
            return PrepareScheduleInsertCarbHypotheses(scheduleToUse_orig, particle.subPopulatie);
        }
        private Schedule PrepareScheduleInsertCarbHypotheses(Schedule scheduleToUse_orig, SubPopulatie sub)
        {
            Schedule totalSchedule = new Schedule();
            totalSchedule.AddAllInsulinEventsFromSchedule(scheduleToUse_orig);
            Schedule carbSchedule = learningCarbHypothesis.GetCarbHypothesisSchedule(sub, scheduleToUse_orig.GetLastTime());
            totalSchedule.AddAllCarbEventsFromSchedule(carbSchedule);
            totalSchedule.IntegrityCheck();
            totalSchedule.SetHeartRateGenerator(scheduleToUse_orig.GetHeartRateGenerator());
            return totalSchedule;
        }


        private Stopwatch stopwatch = new Stopwatch();
        private bool eerstekeerAddCarbToLearningHypothesis = true;
        public void EvalueerEnResample(uint _currentPatientTime)
        {
            // run simulations --> evaluate blood measurement results at all gluc-mmnt moments

            // make a schedule with the events up until now by copying original and 
            // removing everything in the future of the current event (no time traveling/looking into the future kind of cheating allowed!)   
            Schedule noisyFoodScheduleClippedToTrail = ObservedPatient.NoisySchedule.CropSchedule2Copy(0, _currentPatientTime, false);


            // eerst bij elkaar harken wat we aan info van de patient gekregen hebben, en dat toevoegen:
            int vanafTijd = (int)GetCurrentTime() - (int)this.settingsForParticleFilter.evaluateEveryNMinutes;
            if (eerstekeerAddCarbToLearningHypothesis)
            {
                // anders missen we de eerste paar carbs!
                vanafTijd = 0;
            }
            learningCarbHypothesis.AddNewCarbHypothesisGroupsFromSchedule(noisyFoodScheduleClippedToTrail, vanafTijd);

            // detecteren DAT er vreemde gluc stijging is zonder (geregistreerde) carb event?                
            // vanwege FoodForgetFactor

            // alle (recente?) gluc Mmnts ophalen t/m _currentPatientTime
            // smoothen, en daarin abrupte stijgingen detecteren
            Tuple<List<uint>, List<double>, List<double>, List<double>, Dictionary<uint, double>> tup = GetSmoothedNoisyMmnts(0, (int)_currentPatientTime, this.settingsForParticleFilter.SMOOTHING_RANGE_FOR_FORGET_DETECTION);
            List<uint> times = tup.Item1;
            List<double> smoothedNoisyValues = tup.Item4;
            Dictionary<uint, double> times2SmoothedNoisy = tup.Item5;


            int halveRange = (int)this.settingsForParticleFilter.SMOOTHING_RANGE_FOR_FORGET_DETECTION / 2;
            for (int i = halveRange; i < times.Count - halveRange - 1; i++)
            {
                if (times[i] >= vanafTijd - (int)this.settingsForParticleFilter.evaluateEveryNMinutes/2)
                {
                    double dezeRico = MyMath.LeastSquaresLineFitSlope(smoothedNoisyValues, i - halveRange, i + halveRange);
                    slope1_FOR_LOGGING[times[i]] = dezeRico;
                }
            }
            // 2e orde afgeleide nemen: rico van ricos. maar rico is al smooth, dus diff is voldoende?
            halveRange = 2;
            int j = halveRange;
            foreach (uint time in slope1_FOR_LOGGING.Keys)
            {
                if (j >= halveRange && j < slope1_FOR_LOGGING.Count - halveRange - 1)
                {
                    double dezeRico = MyMath.LeastSquaresLineFitSlope(slope1_FOR_LOGGING, j - halveRange, j + halveRange);
                    slope2_FOR_LOGGING[time] = dezeRico;
                }
                j++;
            }
            // 3e afgeleide:
            halveRange = 2;
            j = halveRange;
            foreach (uint time in slope2_FOR_LOGGING.Keys)
            {
                if (j >= halveRange && j < slope2_FOR_LOGGING.Count - halveRange - 1)
                {
                    double thisSlope = MyMath.LeastSquaresLineFitSlope(slope2_FOR_LOGGING, j - halveRange, j + halveRange);
                    slope3_FOR_LOGGING[time] = thisSlope;
                }
                j++;
            }


            slope2_smoothed_FOR_LOGGING = MyMath.MultiplyWithKernel<uint>(slope2_FOR_LOGGING, new double[] { 1, 2, 4, 5, 4, 2, 1 });
            slope3_smoothed_FOR_LOGGING = MyMath.MultiplyWithKernel<uint>(slope3_FOR_LOGGING, new double[] { 1, 1, 1, 2, 4, 5, 4, 2, 1 }); // iets langer, om deoffset te corrigeren tov rico2. Rico3 lijkt wat vertraagd

            // nog geen pass, nu op basis van rico2*rico3. Het patroon is: diep_dal-piekRond0-diep_dal... dus paren zoeken?

            product2times3 = new SortedDictionary<uint, double>();
            foreach (uint time in slope2_smoothed_FOR_LOGGING.Keys)
            {
                if (slope3_smoothed_FOR_LOGGING.ContainsKey(time) && time > vanafTijd - settingsForParticleFilter.evaluateEveryNMinutes / 2)
                {
                    double prod = slope2_smoothed_FOR_LOGGING[time] * slope3_smoothed_FOR_LOGGING[time];
                    if (Math.Abs(prod) < 3e-5) { prod = 0; }
                    product2times3[time] = prod;
                }
            }
            product2times3 = MyMath.MultiplyWithKernel<uint>(product2times3, new double[] { 1, 3, 4, 3, 1 });  //{ 1, 2, 4, 5, 4, 2, 1 });

            List<double> product2maal3_waardes_lijst = new List<double>(product2times3.Values);
            List<uint> product2maal3_tijden = new List<uint>(product2times3.Keys);



            List<int> localOptimaNdx = MyMath.FindLocalOptima(product2maal3_tijden, product2maal3_waardes_lijst);
            for (int ndx = 0; ndx < localOptimaNdx.Count; ndx++)
            {
                int index_dal1 = localOptimaNdx[ndx];
                if (index_dal1 < 0)
                {
                    uint time = product2maal3_tijden[-index_dal1];
                    double dalwaarde = product2maal3_waardes_lijst[-index_dal1];
                    if (time < laatste_piek_gedetecteerd_tijd)
                    {
                        continue;
                    }
                    // een dal in product. Kijk of er een piek in rico2 is.
                    if (
                         //dalwaarde < this.settingsForParticleFilter.MargingProd2x3  &&
                         slope3_smoothed_FOR_LOGGING[time] < 0 &&
                         slope2_smoothed_FOR_LOGGING[time] > settingsForParticleFilter.MarginSlope2
                        )
                    {
                        double initialEstimate = 0;
                        if (settingsForParticleFilter.BaseInitialCarbEstimationOnGlucoseCurve && (eerstekeerAddCarbToLearningHypothesis || time > settingsForParticleFilter.TrailForEvaluationInMinutes))
                        {
                            // initialEstimate = MyMath.Clip(-300000 * dalwaarde, 0, 200);
                            initialEstimate = MyMath.Clip(100 * slope2_smoothed_FOR_LOGGING[time], 0, 200);
                        }
                        learningCarbHypothesis.AddNewCarbHypothesisGroup(time, false, initialEstimate, (int)settingsForParticleFilter.VoegCarbHypToeAlsVerderwegDanMarg);
                        laatste_piek_gedetecteerd_tijd = (uint)Math.Max(0, (int)time - settingsForParticleFilter.evaluateEveryNMinutes * 2);
                    }
                }
            }


            learningCarbHypothesis.Opschonen();
            eerstekeerAddCarbToLearningHypothesis = false;


            // estimate base HR on learning trail, use it to 'box' the ML hr values
            double estimatedHR = ObservedPatient.GetHrBaseEstimate((uint)this.GetTrainTrailStartTime()/*is >=0 op dit punt in code*/, this.GetCurrentTime());
            Console.WriteLine("ESTIMATE FOR HR base: " + estimatedHR);
            BergmanAndBretonModel.Initialize_boundingRanges(BergmanAndBretonModel.parameter_index_HRb, estimatedHR - 5, estimatedHR + 10);


            // strip helemaal van carbs:
            noisyFoodScheduleClippedToTrail.RemoveCarbEvents();

            // run/eval. particles;

            // resamplen bevat veel random calls, dus KAN NIET PARALLEL. Maar is toch heel kort.
            foreach (SubPopulatie sub in subPopuluaties)
            {
                sub.Resample();
                if (settingsForParticleFilter.UseLobsidednessSignalFromCarbHypInResample)
                {
                    sub.CarbHypGBaseFeedback(gBaseSignal);
                }
            }

            // wat post processing (stagnatie detectie enz)
            StringBuilder[] postResultaten = new StringBuilder[subPopuluaties.Count]; // zodat we parallel kunnen runnen.


            bool UseParallelProcessingOnSubPopulaties = this.settingsForParticleFilter.UseParallelProcessingOnSubPopulaties;  // nb dit had mooier met #if #else gekund via preprocessor statements, maar dan kun je tijdens debuggen de parallel niet aan/uit zetten. Met een bool wel.
            bool UseParallelProcessingParticleEvaluaties = this.settingsForParticleFilter.UseParallelProcessingParticleEvaluaties;  // nb dit had mooier met #if #else gekund via preprocessor statements, maar dan kun je tijdens debuggen de parallel niet aan/uit zetten. Met een bool wel.


            Dictionary<SubPopulatie, Schedule> schedules_for_sub = new Dictionary<SubPopulatie, Schedule>();
            foreach (SubPopulatie sub in subPopuluaties)
            {
                // hier schedule per sub brouwen, omdat we het anders op meerdere plekken in sub moeten regelen en rekening houden met subsub die het weer niet mag doen, etc..
                schedules_for_sub[sub] = PrepareScheduleInsertCarbHypotheses(noisyFoodScheduleClippedToTrail, sub);
            }
            ErrorCalculationSettings errorCalculationSettings = new ErrorCalculationSettings(2, settingsForParticleFilter.ErrorCalcInLogSpace, insulinTimeSlots);
            errorCalculationSettings.USE_HOOGTEFACTOR_IN_RMSE = true;

            if (UseParallelProcessingOnSubPopulaties)
            {
                // https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/how-to-write-a-simple-parallel-foreach-loop
                Parallel.ForEach(subPopuluaties, (sub) =>
                {
                    postResultaten[sub.ID] = sub.EvalueerNaResample(errorCalculationSettings, schedules_for_sub[sub], (uint)GetLongTrainTrailStartTime(), (uint)GetTrainTrailStartTime(), GetTrainTrailStopTime(), UseParallelProcessingParticleEvaluaties);

                });
            }
            else
            {
                foreach (SubPopulatie sub in subPopuluaties)
                {
                    // hier schedule per sub brouwen, omdat we het anders op meerdere plekken in sub moeten regelen en rekening houden met subsub die het weer niet mag doen, etc..
                    postResultaten[sub.ID] = sub.EvalueerNaResample(errorCalculationSettings, schedules_for_sub[sub], (uint)GetLongTrainTrailStartTime(), (uint)GetTrainTrailStartTime(), GetTrainTrailStopTime(), UseParallelProcessingParticleEvaluaties);
                }
            }







            List<Particle> bestSubParticles = new List<Particle>();
            int nrExplorerend = 0;
            foreach (SubPopulatie sub in subPopuluaties)
            {
                if (sub.IsInExplorePhase)
                {
                    nrExplorerend++;
                }
                else
                {
                    if (!double.IsNaN(sub.ICR) && !double.IsInfinity(sub.ICR) && !double.IsNaN(sub.ISF) && !double.IsInfinity(sub.ISF))
                    {
                        bestSubParticles.Add(sub.BestParticle);
                    }
                }
            }

            bestSubParticles.Sort();  // Laag naar hoog gewicht. 
                                      // De beste in bovenstaande loop onthouden is iets sneller in theorie, want: in loop is O(N) versus sorteren O(N log(N)) maar het gaat om zulke
                                      // minuscule aantallen dat de code zo cleaner is met waarschijnlijk nog geen miliseconde verlies. Dit gaat past een rol spelen als er 
                                      // honderden subpopulaties zijn die elke keer gesotereerd moeten worden.
            if (bestSubParticles.Count > 0)
            {
                BestParticle = bestSubParticles[bestSubParticles.Count - 1];
                // scenario: toevallig is deze sub aan het stagneren, en is er bijna.
                // volgende iteratie is ie toevallig 2e en gaat in exploratie...
                // en de dan 1e was een toevalstreffer. Dan zijn we de beste kwijt.
                BestParticle.subPopulatie.ResetStagnatie();
            }
            else
            {
                // de oude gebruiken??? 
                // kies maar wat
                BestParticle = subPopuluaties[0].BestParticle;
            }


            // print de outputs
            if (BestParticle != null)
            {
                Console.WriteLine("======================== " + GetDayEtcTxt() + " ========================\r\n");
                Console.WriteLine(BestParticle.ToStringHeader());
            }
            if (!Globals.RunParallelSimulations)
            {
                for (int i = 0; i < postResultaten.Length; i++)
                {
                    Console.WriteLine(postResultaten[i]);
                }
                //nogmaals header/real voor leesbaarheid
                if (BestParticle != null)
                {
                    Console.WriteLine(BestParticle.ToStringHeader());
                }
            }
            Console.WriteLine("Best:\n" + BestParticle);
            if(BestParticle != null && _currentPatientTime > this.settingsForParticleFilter.nearestParticleHashing_FactorUseAfterTime * (this.settingsForParticleFilter.TrailLengthInMinutes) )
            {
                //Console.WriteLine("~~~~~~~~~~~~~~~~~~~~ nearestParticleHashing ~~~~~~~~~~~~~~~~~~~~~");
                foreach(SubPopulatie sub in this.subPopuluaties)
                {
                   if(!sub.IsInExplorePhase)
                   {
                        int[] bin_indices = nearestParticleHashing.GetBinIndices(sub.BestParticle);
                        bool nieuw = false;
                        if (sub.bin_indices == null)
                        {
                            nieuw = true;
                        }
                        else
                        {
                            // vergelijk of ie naar nieuwe bin verplaatst is!
                            for(int dim = 0; dim < bin_indices.Length; dim++)
                            {
                                if(sub.bin_indices[dim] != bin_indices[dim])
                                {
                                    nieuw = true;
                                    break;
                                }
                            }
                        }
                        if (nieuw) 
                        { 
                            nearestParticleHashing.RegistreerParticle(sub.BestParticle);
                            sub.bin_indices = bin_indices;
                        }

                   }
                }

                nearestParticleHashing.Print();

            }



            if (historySubpopBestParticlesList.Count > 0)
            {
                Dictionary<Particle, Schedule> schedules_for_particle = new Dictionary<Particle, Schedule>();
                foreach (Particle particle in historySubpopBestParticlesList)
                {
                    // hier schedule per sub brouwen, omdat we het anders op meerdere plekken in sub moeten regelen en rekening houden met subsub die het weer niet mag doen, etc..
                    schedules_for_particle[particle] = PrepareScheduleInsertCarbHypotheses(noisyFoodScheduleClippedToTrail, particle);
                }


                //https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/how-to-write-a-simple-parallel-foreach-loop
                if (UseParallelProcessingParticleEvaluaties || UseParallelProcessingOnSubPopulaties)
                {
                    // https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/how-to-write-a-simple-parallel-foreach-loop
                    Parallel.ForEach(historySubpopBestParticlesList, (particle) =>
                    {
                        particle.Evaluate_simple(errorCalculationSettings, schedules_for_particle[particle], (uint)GetLongTrainTrailStartTime(), _currentPatientTime);

                    });
                }
                else
                {
                    foreach (Particle particle in historySubpopBestParticlesList)
                    {
                        particle.Evaluate_simple(errorCalculationSettings, schedules_for_particle[particle], (uint)GetLongTrainTrailStartTime(), _currentPatientTime);
                    }
                }

                historySubpopBestParticlesList.Sort(); //van laag naar hoog
                historySubpopBestParticlesList.Reverse(); //eerste is beste
                while (historySubpopBestParticlesList.Count > MAX_oudeSubpopBestParticles_LENGTH)
                {
                    // slechtste weghalen
                    historySubpopBestParticlesList.RemoveAt(historySubpopBestParticlesList.Count - 1);
                }



                //if (historySubpopBestParticlesList[0].CompareTo(BestParticle) > 0) //let op: weight laag is beter (want weight ~ error)
                //{
                //    Console.WriteLine("   >>> history --> BestParticle >>>");
                //    BestParticle = historySubpopBestParticlesList[0];
                //}
            }




            Console.WriteLine("\nAantal STAGNATIE-RESAMPLE: " + stagnatieTeller + " / " + this.subPopuluaties.Count + " = (" + OctaveStuff.MyFormat(100 * stagnatieTeller / (double)this.subPopuluaties.Count) + ")%\n");
            stagnatieTeller = 0;

            if (!Globals.RunParallelSimulations)
                if (!Globals.RunParallelSimulations)
            {
                // we mogen verbose zijn!
                Console.WriteLine("Beste van alles (hist + subpop):\n" + BestParticle);
                if (BestParticle.subPopulatie != null)
                {
                    //    ObservedPatient.ForceCalculateICR();
                    //   ObservedPatient.ForceCalculateISF();
                    Console.WriteLine("REAL - ICR = " + OctaveStuff.MyFormat(ObservedPatient.ICR) + ", ISF = " + OctaveStuff.MyFormat(ObservedPatient.ISF) + " (" + OctaveStuff.MyFormat(ObservedPatient.ISF * 18) + ")");
                    Console.WriteLine("best - ICR = " + OctaveStuff.MyFormat(BestParticle.subPopulatie.ICR) + ", ISF = " + OctaveStuff.MyFormat(BestParticle.subPopulatie.ISF) + " (" + OctaveStuff.MyFormat(BestParticle.subPopulatie.ISF * 18) + ")");
                }
                else
                {
                    Console.WriteLine("geen best particle.subpolicy");
                }
            }




                // evaluatie, logging, etc....
                // eval. settings: gewoon de RMSE, geen rare fratsen!
            
            ErrorCalculationSettings errorCalculationSettingsForEvaluation = new ErrorCalculationSettings(2, false, insulinTimeSlots);
            // op hele trail lange runnen, zodat we de curve tot aan nu kunnen tonen.
            Schedule noisyFoodScheduleClippedToTrail_forBest = PrepareScheduleInsertCarbHypotheses(noisyFoodScheduleClippedToTrail, BestParticle);
            BestParticle.Evaluate_simple(errorCalculationSettingsForEvaluation, noisyFoodScheduleClippedToTrail_forBest, (uint)GetEvaluatieTrailStartTime(), _currentPatientTime);
            // trail updaten:
            bestPatientGeneratedData.AddCopyOverwrite(BestParticle.ParticlePatient.GeneratedData);

            // experimenteel, ivm locken: ook een lock curve kunnen tekenen.
            bestPatientGeneratedData_met_NaN_gaten.AddCopyOverwrite(BestParticle.ParticlePatient.GeneratedData.DeepCopy());
            // maar error NIET op gedeelte waar carb est nog aangepast worden en/of waar op geselecteerd wordt.
            // op deze manier blijft de evaluatie ONAFHANKELIJK van de trail waarop geleerd wordt....
            BestParticle.CalculateErrorOnObservations(errorCalculationSettingsForEvaluation, (int)GetEvaluatieTrailStartTime(), GetLongTrainTrailStartTime());
            Console.WriteLine("PF: BestParticle  evaluatietrail t=<" + GetEvaluatieTrailStartTime() + "," + GetLongTrainTrailStartTime() + ">:\n" + BestParticle + "\n");


            // beste ML nu ook op schema met daarin de ECHTE carbs, om te kijken hoe goed de ML los v/d carb estimates is
            // TODO/probleem: dit lift mee op bestPatientGeneratedData en dus op BestParticle .... is niet onafhankelijk!
            BestParticleWithRealCarbs = new Particle(BestParticle);
            if (!ObservedPatient.RealData)
            {
                Schedule realScheduleClipped = ObservedPatient.TrueSchedule.CropSchedule2Copy(0, _currentPatientTime, false);
                // we laten de variant met perfecte carbs wel op zelfde vector beginnen, zodat de vergelijking zo weinig mogelijk te maken
                // heeft met cumulerende fouten uit verleden VOOR evaluatie trail. ( Dergelijke fouten zitten uiteraard ook al in de BestParticle en de bestPatientGeneratedData)
                double[] startVector_BestParticleWithRealCarbs = bestPatientGeneratedData.GetValuesFromTime((uint)GetEvaluatieTrailStartTime());// bestPatientWithPerfectCarbsGeneratedData.GetValuesFromTime(GetEvaluatieTrailStartTime());
                BestParticleWithRealCarbs.Evaluate_simple(errorCalculationSettingsForEvaluation, realScheduleClipped, (uint)GetEvaluatieTrailStartTime(), _currentPatientTime, startVector_BestParticleWithRealCarbs);
                // trail updaten:
                //bestPatientWithPerfectCarbsGeneratedData.AddCopyOverwrite(BestParticleWithRealCarbs.ParticlePatient.GeneratedData);

                // maar error NIET op gedeelte waar carb est nog aangepast worden en/of waar op geselecteerd wordt.
                // op deze manier blijft de evaluatie ONAFHANKELIJK van de trail waarop geleerd wordt....
                BestParticleWithRealCarbs.CalculateErrorOnObservations(errorCalculationSettingsForEvaluation, (int)GetEvaluatieTrailStartTime(), GetLongTrainTrailStartTime());
                Console.WriteLine("PF: Bst orig Crb. evaluatietrail t=<" + GetEvaluatieTrailStartTime() + "," + GetLongTrainTrailStartTime() + ">:\n" + BestParticleWithRealCarbs + "\n");
            }



            if (settingsForParticleFilter.LockPredictionsOnMmnts)
            {

                // TODO [prio] -- calc. ahav locken op laatste paar waardes
                // eerst 'lage saaie punten' bepalen (vlak voor insuline? Of iig vlak voor carbs?
                // dan per 'moment'
                //      - de BG locken op smoothed Mmnts,
                //      - dan komende paar uur doorrekenen
                //      - en DAARVAN de CalculateErrorOnObservations doen
                //      - en dan naar volgende saaie moment
                // en alle data in een sovlerresult bijhouden (komt al in bestparticle voor eval trail??)
                // zodat het te plotten valt. En de RMSE van alle korte voorspelstukjes is de evaluatie RMSE (ipv hele 10k minuten eval trail proberen te voorspellen)
                //
                // probleempje: op deze manier heeft de bestparticle maar een klein stukje data aan het einde in solverresult (laatste piek).
                // alles verzamelen en aan einde weer aan particle geven.

                for (int ndx = 0; ndx < bestPatientGeneratedData_met_NaN_gaten.GetCount(); ndx++)
                {
                    uint time = bestPatientGeneratedData_met_NaN_gaten.GetTimeFromIndex(ndx);
                    //if (time >= this.GetEvaluatieTrailStartTime())
                    {
                        bestPatientGeneratedData_met_NaN_gaten.AddCopy(time, null);
                    }
                }


                double SSE_totaal = 0;
                int totale_tijd = 0;
                
                for (int pk = 0; pk < insulinEventsList.Count; pk++)
                {
                    Tuple<PatientEvent, uint> piek = insulinEventsList[pk];
                    PatientEvent insulineEvent = piek.Item1;
                    if(insulineEvent.TrueStartTime < GetEvaluatieTrailStartTime() || insulineEvent.TrueStartTime > (uint)GetCurrentTime())
                    {
                        continue;
                    }
                    int this_run_start = (int)piek.Item1.TrueStartTime - 1;
                    int this_run_end = Math.Min( (int) ObservedPatient.NoisySchedule.GetLastTime() , this_run_start + (int) settingsForParticleFilter.LockPredictionsOnMmnts_End_TimeWindow_in_min);

                    // lock ML patient startvector op smoothed mmnts .. maar dat is natuurlik niet elke minuut... dichtstbijzijnde ervoor opzoeken
                    while (!times2SmoothedNoisy.ContainsKey((uint)this_run_start))
                    {
                        this_run_start--;
                        if (this_run_start == 0)
                        {
                            throw new ArgumentException("oeps");
                        }
                    }
                    double smoothedBg = times2SmoothedNoisy[(uint)this_run_start];
                    // maak startvector... moeten we daar interne X, etc.. meenemen of op 0 zetten?
                    double[] startvector_this_eval = BestParticle.GetCopyOfModifiedModelStartVector((uint)this_run_start);

                    startvector_this_eval[BergmanAndBretonModel.G_Glucose_ODEindex_MG_per_DL] = smoothedBg;
                    Particle BestParticleWithRealCarbs_ditStukje = new Particle(BestParticleWithRealCarbs);
                    BestParticleWithRealCarbs_ditStukje.Evaluate_simple(errorCalculationSettingsForEvaluation, noisyFoodScheduleClippedToTrail_forBest, (uint)this_run_start, (uint)this_run_end, startvector_this_eval);
                    // maar alleen evalueren op laatste stukje, want piek omhoog is niet zo interessant:
                    int this_eval_end = this_run_end;
                    int this_eval_start = this_eval_end - (int)settingsForParticleFilter.LockPredictionsOnMmnts_End_TimeWindow_in_min + (int)settingsForParticleFilter.LockPredictionsOnMmnts_Start_TimeWindow_in_min;
                    BestParticleWithRealCarbs_ditStukje.CalculateErrorOnObservations(errorCalculationSettingsForEvaluation, this_eval_start, this_eval_end);
          
                    // trail updaten, voor grafiek:
                    bestPatientGeneratedData_met_NaN_gaten.AddCopyOverwrite(BestParticleWithRealCarbs_ditStukje.ParticlePatient.GeneratedData);
                    int tijdsduur = BestParticleWithRealCarbs_ditStukje.ParticlePatient.GeneratedData.GetCount();// this_eval_end - this_eval_start;
                    totale_tijd += tijdsduur;
                    SSE_totaal += Math.Pow(BestParticleWithRealCarbs.RMSE_ML2Mmnts, 2) * tijdsduur;
                   
                }
                // particle z'n hele results teruggeven, want op dit moment heeft ie alleen laatste stukkie:
                BestParticleWithRealCarbs.ParticlePatient = new VirtualPatient(BestParticle.ParticlePatient);
                BestParticleWithRealCarbs.ParticlePatient.GeneratedData = bestPatientGeneratedData_met_NaN_gaten.DeepCopy();
                SSE_totaal = Math.Sqrt(SSE_totaal / totale_tijd);
                BestParticleWithRealCarbs.CalculateErrorOnObservations(errorCalculationSettingsForEvaluation, (int)GetEvaluatieTrailStartTime(), GetLongTrainTrailStartTime());
                Console.WriteLine("PF: Bst orig Crb. LOCKED, ev.trl t=<" + GetEvaluatieTrailStartTime() + "," + GetLongTrainTrailStartTime() + ">:\n" + BestParticleWithRealCarbs + "\n");

                //BestParticleWithRealCarbs.
            }

            // wel pf gerund, maar nog geen volledige evaluatie trail gedaan. 
            // op kortere eval. wordt de RMSE bijna altijd kleiner, dus dat geeft scheef beeld bij opstarten.
            pfDataLogger.LogParticle(_currentStopTime_Evaluate_STORED, BestParticle, BestParticleWithRealCarbs, GetSubPopulationRMSEs());


            // carbhyp is avg van alle bijdragen, dus is niet meer deterministisch als dit parallel gaat!
            // TODO: uitzoeken of het WELLICHT SLIM IS om het 'interleaved' deteminisisch parallel te doen? 
            // de beste carbhyp tot een bepaalde tijd 'als een front' parallel doen is wellicht een betere opstap
            // voor de carbhyps erna, dan serieel doen.
            // <time, carbEstimate, offset, learningrate>
            List<Tuple<uint, double, uint, double>>[] alleCarbHypsPerSub = new List<Tuple<uint, double, uint, double>>[subPopuluaties.Count];


            schedules_for_sub = new Dictionary<SubPopulatie, Schedule>();
            foreach (SubPopulatie sub in subPopuluaties)
            {
                schedules_for_sub[sub] = PrepareScheduleInsertCarbHypotheses(noisyFoodScheduleClippedToTrail, sub);
            }


            // if(false)
            if ((UseParallelProcessingParticleEvaluaties || UseParallelProcessingOnSubPopulaties) && !settingsForParticleFilter.UpdateCarbHypDuringSearch)
            {
                // https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/how-to-write-a-simple-parallel-foreach-loop
                // var options = new ParallelOptions { MaxDegreeOfParallelism = -1 }; //2e arg.
                Parallel.ForEach(subPopuluaties, (sub) =>
                {
                    if (sub.ID % settingsForParticleFilter.EvaluateSubCarbhypOncePerN == mmntCounter_Evaluate % settingsForParticleFilter.EvaluateSubCarbhypOncePerN)
                    {
                        alleCarbHypsPerSub[sub.ID] = sub.UpdateCarbHypothesis(errorCalculationSettings, schedules_for_sub[sub], (uint)GetLongTrainTrailStartTime(), _currentPatientTime);
                    }
                });
            }
            else
            {
                foreach (SubPopulatie sub in subPopuluaties)
                {
                    if (sub.ID % settingsForParticleFilter.EvaluateSubCarbhypOncePerN == mmntCounter_Evaluate % settingsForParticleFilter.EvaluateSubCarbhypOncePerN)
                    {
                        alleCarbHypsPerSub[sub.ID] = sub.UpdateCarbHypothesis(errorCalculationSettings, schedules_for_sub[sub], (uint)GetLongTrainTrailStartTime(), _currentPatientTime);
                    }
                }
            }


            if (!settingsForParticleFilter.UpdateCarbHypDuringSearch)
            {
                // alles updaten: SERIEEL!!
                foreach (SubPopulatie sub in subPopuluaties)
                {
                    Particle someParticle = sub.Particles[0]; /* nodig voor sub.id in update*/

                    List<Tuple<uint, double, uint, double>> carbHypsList = alleCarbHypsPerSub[sub.ID];
                    if (carbHypsList == null) { continue; }
                    foreach (Tuple<uint, double, uint, double> carbhyp in carbHypsList)
                    {
                        uint currentCarbInputTime = carbhyp.Item1;
                        double carbHyp = carbhyp.Item2;
                        uint newtime = carbhyp.Item3;
                        double thisLr = 1; // carbhyp.Item4;
                        if (currentCarbInputTime == 22688)
                        {

                        }
                        UpdateCarbEstimation(someParticle, thisLr, currentCarbInputTime, newtime, carbHyp);
                    }
                }
            }

            List<SubPopulatie> actieveSubpop = new List<SubPopulatie>();
            foreach (SubPopulatie sub in subPopuluaties)
            {
                if (!sub.IsInExplorePhase)
                {
                    actieveSubpop.Add(sub);
                }
            }
            gBaseSignal = learningCarbHypothesis.RunEstimationsUpdate(actieveSubpop);
            // Console.WriteLine("RunEstimationsUpdate >>> gBaseSignal = " + gBaseSignal);


        }


        private double gBaseSignal;
        public double GBaseSignal { get { return gBaseSignal; } }



        //run ENTIRE sim. with best particle settings
        // return list met te loggen txt
        public void Finish()
        {

        }





        /////////////////////////////////////////// trace logging etc. //////////////////////////////////////
        /////////////////////////////////////////// trace logging etc. //////////////////////////////////////
        /////////////////////////////////////////// trace logging etc. //////////////////////////////////////
        /////////////////////////////////////////// trace logging etc. //////////////////////////////////////
        /////////////////////////////////////////// trace logging etc. //////////////////////////////////////
        /////////////////////////////////////////// trace logging etc. //////////////////////////////////////
        /////////////////////////////////////////// trace logging etc. //////////////////////////////////////






        public StringBuilder ToCSV()
        {
            StringBuilder txt = new StringBuilder();
            foreach (SubPopulatie sub in subPopuluaties)
            {
                txt.Append(sub.ToCSV());
                txt.Append("\n");
            }
            return txt;
        }


        public string GetDayEtcTxt()
        {
            int dag = (int)(_currentStopTime_Evaluate_STORED / (60 * 24));
            string dagTxt = "day# " + (dag + 1) + " (" + _currentStopTime_Evaluate_STORED + "m) ";
            int uur = (int)((_currentStopTime_Evaluate_STORED - dag * (60 * 24)) / 60);
            int minute = (int)((_currentStopTime_Evaluate_STORED - dag * (60 * 24) - uur * 60));
            string uurTxt = "" + uur;
            if (uurTxt.Length == 1) { uurTxt = "0" + uurTxt; }
            string minuteTxt = "" + minute;
            if (minuteTxt.Length == 1) { minuteTxt = "0" + minuteTxt; }
            string tijdTxt = "" + uurTxt + ":" + minuteTxt;

            return "PF(" + dagTxt + " " + tijdTxt + ", Mm# " + mmntCounter_Evaluate + ") ";
            //    + ",\t ISF [mg/dL/IU] = " + OctaveStuff.MyFormat(BestParticle.ISF) + real_ISF + ",\t ICR [g/IU] = " + OctaveStuff.MyFormat(BestParticle.ICR) + real_ICR
        }




        public static List<string> VARNAMEN_LIJST = new List<string>() { "time", "HR", "RealGlucose", "MeasuredGlucose", "BestPredictedGlucose", "BestPredictedGlucoseWithOrigSchedule", "RealFood", "NoisyFood", "BestPredictedFood", "Insulin", "smoothedNoisyGlucose", "smoothedNoisyGlucoseRico", "smoothedNoisyGlucoseRicoRico", "productRicoXRico", "ActivityZ_Real", "ActivityZ_ML", "ActivityGamma_Real", "ActivityGamma_ML" };
        public static List<string> VAREENHEDEN_LIJST = new List<string>() { "min", "1/min", "mg/dL", "mg/dL", "mg/dL", "mg/dL", "g", "g", "g", "IU", "mg/dL", "mg/dL??", "mg/dL??", "?*?", "?", "?", "?", "?" };


        // logging:
        public Tuple<List<string>, List<string>> GetTraceDataAsCsv()
        {
            // varunits moet in sync zijn met varnamen!
            int G_ndx = BergmanAndBretonModel.G_Glucose_ODEindex_MG_per_DL;

            Tuple<List<uint>, List<double>, List<double>, List<double>, Dictionary<uint, double>> tup = GetSmoothedNoisyMmnts(0, (int)_currentStopTime_Evaluate_STORED, this.settingsForParticleFilter.SMOOTHING_RANGE_FOR_FORGET_DETECTION);
            Dictionary<uint, double> smoothedNoisyMmntsDict = tup.Item5;

            List<string> resultsForCSV = new List<string>();
            int counter = 1;
            string header = "";
            for (int i = 0; i < VARNAMEN_LIJST.Count; i++)
            {
                if (i > 0) { header += ",\t"; }
                header += VARNAMEN_LIJST[i] + " [#" + counter + ": " + VAREENHEDEN_LIJST[i] + "]";
                counter++;
            }

            resultsForCSV.Add(header);
            //uint prev_time_with_gluc_data = 0;


            // per minuut door alle data/events/etc heen om te loggen
            for (uint time = 0; time <= GetCurrentTime(); time++)
            {

                double generated_G_value = Double.NaN;
                bool genDataAanwezig = ObservedPatient.TryGetDataFromTime(time, out double[] generated_values);
                if (genDataAanwezig)
                {
                    generated_G_value = generated_values[BergmanAndBretonModel.G_Glucose_ODEindex_MG_per_DL];
                }
                // HR in trueSchedule (indien aanwezig!) en Noisyschedule zijn hetzelfde. Er is geen HR noise
                double hr = ObservedPatient.GetHeartRate(time);

                // check agenda. We loggen alleen de (relevante) events, en NIET voor elke tijdstap:
                PatientEvent trueMmntEvent = null;
                PatientEvent trueEvent = null;
                PatientEvent bestParticleEvent = null;
                if (ObservedPatient.TrueSchedule != null)
                {
                    ObservedPatient.TrueSchedule.TryGetMmntEvent(time, out trueMmntEvent);
                    ObservedPatient.TrueSchedule.TryGetEvent(time, out trueEvent);
                }
                ObservedPatient.NoisySchedule.TryGetMmntEvent(time, out PatientEvent noisyMmntEvent);
                ObservedPatient.NoisySchedule.TryGetEvent(time, out PatientEvent noisyEvent);
                BestParticle.ParticlePatient.TrueSchedule.TryGetEvent(time, out bestParticleEvent);

                string eventTxt = "" + time + ",\t" + OctaveStuff.MyFormat(hr) + ",\t";
                string trueglucTxt = "NaN";
                string noisyGlucTxt = "NaN";
                string bestParticleGlucTxt = "NaN";
                string bestParticleWithOrigScheduleTxt = "NaN";

                string truecarbTxt = "NaN";
                string noisycarbTxt = "NaN";
                string carbhypTxt = "NaN";
                string insTxt = "NaN";
                string smoothedNoisyGlucTxt = "NaN";
                string smoothedNoisyRicoGlucTxt = "NaN";
                string smoothedNoisyRicoRicoGlucTxt = "NaN";
                string productTxt = "NaN";
                string alpha_times_Z_real_Txt = "NaN";
                string alpha_times_Z_ML_Txt = "NaN";
                string Gamma_real_Txt = "NaN";
                string Gamma_ML_Txt = "NaN";

                
                if (!BestParticle.ParticlePatient.TryGetDataFromTime(time, out double[] bestParticlePredictionWithCarbHyp))
               // if (BestParticle.TryGetValuesFromTime(time, out double[] bestParticlePredictionWithCarbHyp))
                {
                    continue;
                }
                if(bestParticlePredictionWithCarbHyp[BergmanAndBretonModel.G_Glucose_ODEindex_MG_per_DL] >= 5000)
                {

                }            
                BestParticleWithRealCarbs.ParticlePatient.TryGetDataFromTime(time, out double[] bestParticlePredictionWithRealCarbs);
                if(bestParticlePredictionWithRealCarbs == null || bestParticlePredictionWithRealCarbs[0] == -1)
                {

                }


                if (trueMmntEvent != null || (ObservedPatient.RealData && noisyMmntEvent != null))
                {
                    // niet elke keer, omdat grafiek zo vervuilt!
                    Gamma_ML_Txt = OctaveStuff.MyFormat(bestParticlePredictionWithCarbHyp[BergmanAndBretonModel.Gamma_EnergyExpenditure_ODEindex]);

                    if (!ObservedPatient.RealData)
                    {
                        if (trueMmntEvent.Glucose_TrueValue_in_MG_per_DL != generated_G_value)
                        {
                            Console.WriteLine("mogelijk error!: ParticleFilter::GetDataAsCSV::time = " + time + " --> evt.TrueValue != generatedDataValue");
                        }
                        trueglucTxt = OctaveStuff.MyFormat(trueMmntEvent.Glucose_TrueValue_in_MG_per_DL);
                    }

                    // noisy and true GLUC mmnts vallen altijd samen, identiek, zit geen ruis op tijstip en geen 'vergeten'
                    noisyGlucTxt = OctaveStuff.MyFormat(noisyMmntEvent.Glucose_TrueValue_in_MG_per_DL);
                    bestParticleGlucTxt = OctaveStuff.MyFormat(bestParticlePredictionWithCarbHyp[G_ndx]);

                    if (bestParticlePredictionWithRealCarbs != null)
                    {
                        bestParticleWithOrigScheduleTxt = OctaveStuff.MyFormat(bestParticlePredictionWithRealCarbs[G_ndx]);
                    }
                

                    {
                        double smoothedNoisy = Double.NaN;
                        smoothedNoisyMmntsDict.TryGetValue(time, out smoothedNoisy);
                        smoothedNoisyGlucTxt = OctaveStuff.MyFormat(smoothedNoisy);
                    }

                    {
                        slope2_FOR_LOGGING.TryGetValue(time, out double smoothedNoisy);
                        //smoothedNoisy = smoothedNoisy * 100 + 50;
                        smoothedNoisyRicoGlucTxt = OctaveStuff.MyFormat(smoothedNoisy);
                    }

                    {
                        slope3_FOR_LOGGING.TryGetValue(time, out double smoothedNoisy);
                        //smoothedNoisy = smoothedNoisy * 5000 + 50;
                        smoothedNoisyRicoRicoGlucTxt = OctaveStuff.MyFormat(smoothedNoisy);
                    }

                    {
                        product2times3.TryGetValue(time, out double smoothedNoisy);
                        productTxt = OctaveStuff.MyFormat(smoothedNoisy);// * 20000 + 50);
                    }

                }


                // carb / ins:
                if (trueEvent != null)
                {
                    if (bestParticlePredictionWithCarbHyp != null)
                    {
                        bestParticleGlucTxt = OctaveStuff.MyFormat(bestParticlePredictionWithCarbHyp[G_ndx]);
                       // prev_time_with_gluc_data = time;
                    }
                    if (bestParticlePredictionWithRealCarbs != null)
                    {
                        bestParticleWithOrigScheduleTxt = OctaveStuff.MyFormat(bestParticlePredictionWithRealCarbs[G_ndx]);
                       // prev_time_with_gluc_data = time;
                    }
                    if (trueEvent.EventType == Enums.PatientEventType.CARBS)
                    {
                        truecarbTxt = OctaveStuff.MyFormat(trueEvent.Carb_TrueValue_in_gram);
                    }
                    else if (trueEvent.EventType == PatientEventType.INSULIN)
                    {
                        insTxt = OctaveStuff.MyFormat(trueEvent.Insulin_TrueValue_in_IU);
                    }
                   // prev_time_with_gluc_data = time;
                }

                if (noisyEvent != null)
                {
                    if (bestParticlePredictionWithCarbHyp != null)
                    {
                        bestParticleGlucTxt = OctaveStuff.MyFormat(bestParticlePredictionWithCarbHyp[G_ndx]);
                        //prev_time_with_gluc_data = time;
                    }
                    if (bestParticlePredictionWithRealCarbs != null)
                    {
                        bestParticleWithOrigScheduleTxt = OctaveStuff.MyFormat(bestParticlePredictionWithRealCarbs[G_ndx]);
                        //prev_time_with_gluc_data = time;
                    }

                    if (noisyEvent.EventType == Enums.PatientEventType.CARBS)
                    {
                        noisycarbTxt = OctaveStuff.MyFormat(noisyEvent.Carb_TrueValue_in_gram);
                    }
                    else if (ObservedPatient.RealData && noisyEvent.EventType == PatientEventType.INSULIN)
                    {
                        insTxt = OctaveStuff.MyFormat(noisyEvent.Insulin_TrueValue_in_IU);
                    }
                   // prev_time_with_gluc_data = time;
                }


                if (bestParticleEvent != null)
                {
                    if (bestParticlePredictionWithCarbHyp != null)
                    {
                        bestParticleGlucTxt = OctaveStuff.MyFormat(bestParticlePredictionWithCarbHyp[G_ndx]);
                      //  prev_time_with_gluc_data = time;
                    }
                    if (bestParticlePredictionWithRealCarbs != null)
                    {
                        bestParticleWithOrigScheduleTxt = OctaveStuff.MyFormat(bestParticlePredictionWithRealCarbs[G_ndx]);
                       // prev_time_with_gluc_data = time;
                    }
                    if (bestParticleEvent.EventType == Enums.PatientEventType.CARBS)
                    {
                        carbhypTxt = OctaveStuff.MyFormat(bestParticleEvent.Carb_TrueValue_in_gram);
                    }

                   // prev_time_with_gluc_data = time;
                }


                // if(trueEvent == null && bestParticleEvent == null)
                {
                    //   if (time - prev_time_with_gluc_data >= time_between_logs)
                    {
                        if (bestParticlePredictionWithCarbHyp != null)
                        {
                            bestParticleGlucTxt = OctaveStuff.MyFormat(bestParticlePredictionWithCarbHyp[G_ndx]);
                           // prev_time_with_gluc_data = time;
                        }
                        if (bestParticlePredictionWithRealCarbs != null)
                        {
                            bestParticleWithOrigScheduleTxt = OctaveStuff.MyFormat(bestParticlePredictionWithRealCarbs[G_ndx]);
                          // prev_time_with_gluc_data = time;
                        }

                        //eventTxt = geneterated_G_value + ",\tNaN,\tNaN,\tNaN,\tNaN,\tNaN,\tNaN,\tNaN";
                        if (!ObservedPatient.RealData)
                        {
                            //zorgen dat de grafiek niet te haperig wordt.
                            trueglucTxt = OctaveStuff.MyFormat(generated_G_value);
                            //prev_time_with_gluc_data = time;
                        }

                    }
                }

                if (eventTxt != null)
                {
                    alpha_times_Z_ML_Txt = OctaveStuff.MyFormat(bestParticlePredictionWithCarbHyp[BergmanAndBretonModel.Z_EffectOnX_ODEindex] * BestParticle.ParticlePatient.Model.alpha);

                    if (!ObservedPatient.RealData && ObservedPatient.UsesActivityModel)
                    {
                        if (genDataAanwezig)
                        {
                            alpha_times_Z_real_Txt = OctaveStuff.MyFormat(generated_values[BergmanAndBretonModel.Z_EffectOnX_ODEindex] * ObservedPatient.Model.alpha);
                            Gamma_real_Txt = OctaveStuff.MyFormat(generated_values[BergmanAndBretonModel.Gamma_EnergyExpenditure_ODEindex]);
                        }
                    }
                    resultsForCSV.Add(eventTxt + trueglucTxt + ",\t" + noisyGlucTxt + ",\t" + bestParticleGlucTxt + ",\t" + bestParticleWithOrigScheduleTxt
                        + ",\t" + truecarbTxt + ",\t" + noisycarbTxt + ",\t" + carbhypTxt
                        + ",\t" + insTxt + ",\t"
                        + smoothedNoisyGlucTxt + ",\t" + smoothedNoisyRicoGlucTxt + ",\t" + smoothedNoisyRicoRicoGlucTxt + ",\t" + productTxt
                        + ",\t" + alpha_times_Z_real_Txt + ",\t" + alpha_times_Z_ML_Txt + ",\t" + Gamma_real_Txt + ",\t" + Gamma_ML_Txt);
                }
            }
            return new Tuple<List<string>, List<string>>(VARNAMEN_LIJST, resultsForCSV);
        }




    }
}


