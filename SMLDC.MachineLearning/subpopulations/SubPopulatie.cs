using System;
using System.Text;
using System.Collections.Generic;
using SMLDC.Simulator.DiffEquations.Models;
using SMLDC.Simulator.DiffEquations.Solvers;
using SMLDC.Simulator.Schedules;
using SMLDC.Simulator.Utilities;
using System.Threading.Tasks;
using SMLDC.Simulator;

namespace SMLDC.MachineLearning.subpopulations
{
    public class SubPopulatie
    {
        public int[] bin_indices;
        public void GiveParticleTheRealParameters_Hack()
        {
            for (int i = 0; i < particles.Length; i++)
            {
                particleFilter.GiveParticleTheRealParameters_Hack(particles[i]);
            }
        }

    /*
     * dit is de echte particle filter. 
    */

      public RandomStuff random;

       // public static double exploratieFaseInitieleMutatieStap = 0.01; //om init. vectoren van explorers te maken.
        public static int AANTAL_EXPLORERS = 0; //TODO: refactor? naar PF?
        public static int MAX_AANTAL_EXPLORERS = 3; //TODO: ongebruikt, weghalen


        // elke keer als er expl. fase ingegaan wordt: ophogen.
        // op deze manier kunnen we in logging ook aangeven welke subpop het is (ID & teller samen geven dat aan)
        public int exploratieTeller = 0;

        private int subPopulatieIndex = -1;
        public int ID { get { return subPopulatieIndex; } }
        public void NegateID(SubPopulatie sub) { subPopulatieIndex = -(sub.ID + 10000); }
        public ParticleFilter particleFilter;

        public override int GetHashCode()
        {
            return this.ID;
        }
        public override bool Equals(object obj)
        {
            try
            {
                return this.ID == ((SubPopulatie)obj).ID;
            }
            catch
            {
                return false;
            }
        }
        public SubPopulatie(ParticleFilter pf)
        {
            particleFilter = pf;
            subPopulatieIndex = particleFilter.subPopulatieTeller; //subPopulatieTeller++;
            random = new RandomStuff(particleFilter.settingsForParticleFilter.seedForParticleFilter + Math.Abs(this.ID) + 1);

            particles = new Particle[pf.settingsForParticleFilter.NumberOfParticlesPerSubPopulation];
            // random posities in 'parameter landscape' initializeren:
            for (int p = 0; p < particles.Length; p++)
            {
                //double[] param = new double[particleFilter.NrParamsInModifiedBergman];
                // nieuwe particle wordt gemaakt met rnd waarden uniform verrdeeld wrt lowerHigherBounds:
                particles[p] = new Particle(this, particleFilter.ObservedPatientOriginalStartVector);

                //SUPERHACK: --->  nb hier niet weghalen, maar aan/uit zetten in de functie <----
                particleFilter.GiveParticleTheRealParameters_Hack(particles[p]);
            }
        }

        public override string ToString()
        {
            return "SubPopulatie #" + ID;
        }
        public StringBuilder ToCSV()
        {
            StringBuilder txt = new StringBuilder();
            foreach (Particle particle in Particles)
            {
                txt.Append(particle.ToString());
                txt.Append("\n");
            }
            return txt;
        }


        public bool SameAsPrevious() { return BestParticle.ID == prevBestParticleIndex; }
        public int prevBestParticleIndex = -1;



        public void UpdateBestParticleRef(Particle particle)
        {
            _bestParticle = particle;
            if (this._bestParticle != null)
            {
                this.AddToBestParticleHistory(_bestParticle);
            }
        }


        public void SetBestParticle(Particle particle)
        {
            if(particle == null)
            {
                _bestParticle = null;
                return;
            }
            _bestParticle = new Particle(particle);
            _bestParticle.ParticlePatient = new VirtualPatient(particle.ParticlePatient);
            _bestParticle.ParticlePatient.GeneratedData = particle.ParticlePatient.GeneratedData.DeepCopy();
            if (this._bestParticle != null)
            {
                this.AddToBestParticleHistory(_bestParticle);
            }
        }

        private Particle _bestParticle;
        public Particle BestParticle
        {
            get { return _bestParticle; }
        }
        public virtual SolverResultBase BestPatientGeneratedData
        {
            get
            {
                if (BestParticle == null) { return particleFilter.BestPatientGeneratedData; }
                else { return BestParticle.ParticlePatient.GeneratedData; }
            }
        }

        protected Particle[] particles;
        public Particle[] Particles { get { return particles; } }
        public Particle Get(int i) { return Particles[i]; }
        public int Count { get { return Particles.Length; } }

        protected void ClearBestParticleHistory()
        {
            BestParticleHistory.Clear();
        }
        private List<Particle> BestParticleHistory = new List<Particle>();
        private void AddToBestParticleHistory(Particle particle)
        {
            if (!BestParticleHistory.Contains(particle))
            {
                BestParticleHistory.Add(new Particle(particle));
                if (BestParticleHistory.Count > particleFilter.settingsForParticleFilter.MaximumSbHistoryQueueLength)
                {
                    BestParticleHistory.RemoveAt(0); // todo: echte queue van maken?
                }
            }
        }
        public int BestParticleHistoryQueueCount { get { return BestParticleHistory.Count; } }
        public Particle BestParticleHistoryGet(int i) { return BestParticleHistory[i]; }
        private Dictionary<int, BergmanAndBretonModel> exploreVectorsDict; //particle index naar vector. Zo hoeven we Particle niet uit te breiden
        private Dictionary<int, List<double>> exploreErrorsDict;


        public Particle[] GetParticlesToEvaluate()
        {
            return particles;
        }



        protected void GetBounds(double[] parabolicSearchVector_logspace, out double lowerBound, out double upperBound)
        {
            // uitzoeken bij welke t * vector we out of bounds gaan
            upperBound = double.PositiveInfinity;
            lowerBound = double.NegativeInfinity;

            for (int p = 0; p < parabolicSearchVector_logspace.Length; p++)
            {
                if (parabolicSearchVector_logspace[p] != 0)
                {
                    // uitzoeken wanneer voor deze parameter het mis gaat, laag en hoog
                    // delta van huidig tot ondergrens in logspace
                    double laag = Math.Log10(particleFilter.LOWER_HIGHER_BOUNDS[p, 0]);  //laag
                    double hoog = Math.Log10(particleFilter.LOWER_HIGHER_BOUNDS[p, 1]);  //up
                    double huidig = Math.Log10(particles[0].model.GetParameter(p));

                    double min_t, max_t;
                    if (Math.Sign(parabolicSearchVector_logspace[p]) > 0)
                    {
                        min_t = (laag - huidig) / parabolicSearchVector_logspace[p];
                        max_t = (hoog - huidig) / parabolicSearchVector_logspace[p];
                    }
                    else
                    {
                        max_t = (laag - huidig) / parabolicSearchVector_logspace[p];
                        min_t = (hoog - huidig) / parabolicSearchVector_logspace[p];
                    }

                    // het vector-element (parameter nr) met kleinste upper/lowerbound is bepalend
                    if (min_t > lowerBound)
                    {
                        lowerBound = min_t;
                    }
                    if (max_t < upperBound)
                    {
                        upperBound = max_t;
                    }
                }
            }

            if (upperBound < 0 || lowerBound > 0)
            {
                Console.WriteLine("mag niet voorkomen! lowerbound = " + lowerBound + ", upperbound = " + upperBound);
                //    throw new ArgumentException("upperbound < 0 of lowerbound > 0!");
            }
        }


        private int turnCounter = 0;
        public int Turn { get { return turnCounter; } }
        protected StringBuilder NextTurn(ErrorCalculationSettings errorCalculationSettings, Schedule noisyFoodScheduleClippedToTrail, uint starttime, uint currentTime) { 
            this.turnCounter++;

            if (BestParticle == null)
            {
                particles[0].Evaluate_simple(errorCalculationSettings, noisyFoodScheduleClippedToTrail, starttime, currentTime);
                SetBestParticle(particles[0]);
            }

            //gebruik vorige beste om nu de startvectoren te genereren:
            BestParticle.Evaluate_simple(errorCalculationSettings, noisyFoodScheduleClippedToTrail, starttime, currentTime);

            StringBuilder sb = new StringBuilder();
            return sb;
        }


        public virtual StringBuilder EvalueerNaResample(ErrorCalculationSettings errorCalculationSettings, Schedule noisyFoodScheduleClippedToTrail, uint langstarttime, uint starttime, uint currentTime, /*double[] bestStartingVectorForLongTrail,*/ bool useParallel)
        {
            StringBuilder sb = NextTurn(errorCalculationSettings, noisyFoodScheduleClippedToTrail, langstarttime, currentTime);
            StringBuilder temp = Evaluate(errorCalculationSettings, noisyFoodScheduleClippedToTrail, langstarttime, starttime, currentTime,/* bestStartingVectorForLongTrail,*/ useParallel);
            sb.Append(temp);
            return sb;
        }



        protected virtual StringBuilder Evaluate(ErrorCalculationSettings errorCalculationSettings, Schedule noisyFoodScheduleClippedToTrail, uint langstarttime, uint starttime, uint currentTime, /*double[] bestStartingVectorForLongTrail,*/ bool useParallel)
        {
            // klassieke pf zoektocht
            //Dit kon ook met #if ... maar op deze manier is de parallel hier gekoppeld aan die in particle filter, dus maar 1x #define PARALLEL nodig.
            if (useParallel)
            {
                Parallel.ForEach(particles, (particle) =>
                {
                    Schedule noisyFoodScheduleClippedToTrailCopy = noisyFoodScheduleClippedToTrail.DeepCopy();
                    particle.Evaluate_simple(errorCalculationSettings, noisyFoodScheduleClippedToTrailCopy, starttime, currentTime);//TODO: is -1 nog nodig???
                });
            }
            else
            {
                foreach (Particle particle in particles)
                {
                     particle.Evaluate_simple(errorCalculationSettings, noisyFoodScheduleClippedToTrail, starttime, currentTime);//TODO: is -1 nog nodig???
                }
            }
            return EvaluatePost(errorCalculationSettings, noisyFoodScheduleClippedToTrail, langstarttime, currentTime); //, bestStartingVectorForLongTrail);
        }




        // altijd gerund na de valuate aanroep.
        protected virtual StringBuilder EvaluatePost(ErrorCalculationSettings errorCalculationSettings, Schedule noisyFoodScheduleClippedToTrail, uint starttime, uint currentTime) //, double[] bestStartingVectorForLongTrail) //, PatientEvent mostRecentCarbEvent)
        {
            //if (IsInExplorePhase) //todo: dit in resample zetten?
            //{
            //    for (int p = 0; p < particles.Length; p++)
            //    {
            //        //TODO: welke soort error opslaan?
            //        exploreErrorsDict[p].Add(particles[p].ErrorUsedForStagnationAndExploration());
            //    }
            //}

            //  resultaten van vorige evaluatie:
            Array.Sort(particles);
            Array.Reverse(particles); // van hoog naar laag

            // beste opslaan, omdat de gegen.data van bestparticle de beste schatting is voor een startvector
            // in de toekomst. TODO: niet alleen deze, maar alle?? of alleen maar starvectors, en 
            // de VERDELING gebruiken (met random sampling) voor bepalen v/d nieuwe startvector voor de particles??

            SetBestParticle(particles[0]); //deze DeepCopyConstructor geeft alleen een ref naar particlePatient
                                           // icr en isf grenzen:


            BestParticle.Evaluate_simple(errorCalculationSettings, noisyFoodScheduleClippedToTrail, starttime, currentTime);

            errorList.Add(BestParticle.ErrorUsedForStagnationAndExploration());


            icr = BestParticle.ParticlePatient.ForceCalculateICR(random);
            isf = BestParticle.ParticlePatient.ForceCalculateISF(random);


            if (!particleFilter.DISABLED_WARNING) { Console.WriteLine("\t==> particleFilter::Resample - isf en icr calc DISABLED <=="); }

            StringBuilder sb = new StringBuilder();


            //if (IsInExplorePhase)
            //{
            //    sb.Append(BestParticle.ToString() + " X#" + EXPLORE_PHASE_COUNTER + " [" + OctaveStuff.MyFormat(slope, 4) + "]");
            //}
            //else

            string stag_txt = "";
            if (stagnation_counter > 0)
            {
                stag_txt = "|" + stagnation_counter;
            }
            else
            {
                stag_txt = "  ";
            }
            sb.Append(BestParticle.ToString()
                //   + "   (" + OctaveStuff.MyFormat(rico, 4) + stag_txt + ") , ICR=" + OctaveStuff.MyFormat(icr, 3) + ", ISF=" + OctaveStuff.MyFormat(isf, 3)
                );
        

            //if (icr < particleFilter.settingsForParticleFilter.ICR_lower_bound || icr > particleFilter.settingsForParticleFilter.ICR_upper_bound
            //        || isf < particleFilter.settingsForParticleFilter.ISF_lower_bound || isf > particleFilter.settingsForParticleFilter.ISF_upper_bound)
            //{
            //    BestParticle = null;
            //}

            return sb;
        }



        public List<Tuple<uint, double, uint, double>> UpdateCarbHypothesis(ErrorCalculationSettings errorCalculationSettings, Schedule noisyFoodScheduleClippedToTrail, uint starttime, uint stopTime) //, PatientEvent mostRecentCarbEvent)
        {
            if ( !IsInExplorePhase  &&  BestParticle != null)
            {
               // Console.WriteLine("sub " + ID + ":: UpdateCarbHypothesis");
                Particle BestParticleClone = new Particle(BestParticle); // de BestParticle heeft de startvectors, dus die niet hergebruiken
                return BestParticleClone.Evaluate_with_local_search(errorCalculationSettings, noisyFoodScheduleClippedToTrail, starttime, stopTime);
            }
            return null;
        }



        // op basis van weights en de gedane actie een nieuwe reeks particles maken
        private double boltzmannCumSumPow = 1;




        // IN resample wordt NIET parallel gewerkt, dus de this.random kan doorgegeven worden aan particles om gebruikt te worden
        public virtual void Resample()
        {
            if (BestParticle == null) { return; } // gebeurt oa de eerste keer dat resample (VOOR evalueer) aangeroepen wordt.

            // bepaal of we moeten gaan exploiteren
            bool skip_resample = false;


            // check of we op een local optimum aanbeland zijn:, maar alleen als er niet al teveel explorerende subpopluaties zijn.
            if (AANTAL_EXPLORERS < particleFilter.subPopuluaties.Count - particleFilter.settingsForParticleFilter.MaxNrSubPopulatiesNotExploring)
            {
                if (DetermineStagnation())
                {
                    //direct samplen, niet eerst wachten op rico enz... geen klassieke exploratie
                    skip_resample = true;
                    //BergmanAndBretonModel newRandomModel = particleFilter.GetRandomSample(random, BestParticle);
                    //particles[0].model = newRandomModel;
                    particles[0] = particleFilter.RegistreerStagnatie(random, BestParticle);
                    for (int p = 1; p < this.particles.Length; p++)
                    {
                        particles[p] = particles[p].CreateMutatedNewParticle(random);
                    }

                    stagnation_counter = 0;

                    slope = double.NaN;
                    errorList = new List<double>(); //deze errors zijn nooit meer nodig, we gaan exploren
                                                    // als we hier de history niet clearen, dan komt de oude beste particle weer als beste uit de bus
                                                    // en zijn we weer terug bij het oude local opt.
                    SetBestParticle(null);
                    BestParticleHistory.Clear(); // TODO: in PF of hier toch iets van beste bijhouden? 
                }
            }

            if (!skip_resample)
            {
                Resample_Basic();
            }
        }



        public bool IsInExplorePhase { get { return false; } }  // see the commented block below

       /*
        * Previous version of resample, still based on the idea of sending out particle to 'explore':
        * 
        * It is older version, not using the nearestParticleHashing
        * 
        * but instead uses the following idea: if a subpop stagnates, just let it 'explode': send all 
        * particles in a different direction into the parameter space, AND IGNORE their fitness (error, weight).
        * So the subpop goes into exploration mode, and particles are no longer eliminated/propagated etc.
        * based on their fitness. 
        * after some time, some of the particles may enter a new local optimum (high fitness, low error)
        * The first particle that gets decreasing errors, is selected as the new seedparticle (founding father)
        * of the subpopulation. The rest of the particles are removed, and new ones are genereated around the seedparticle.
        * 
        * the code should still be compatible with the rest.


        private int EXPLORE_PHASE_COUNTER = 0; // update the EvaluatePost (txt output) as well
        private bool EXPLORE_PHASE = false;
        public bool IsInExplorePhase { get { return EXPLORE_PHASE; } }
         
        // IN resample wordt NIET parallel gewerkt, dus de this.random kan doorgegeven worden aan particles om gebruikt te worden
        public virtual void Resample()
        {
            if (BestParticle == null) { return; } // gebeurt oa de eerste keer dat resample (VOOR evalueer) aangeroepen wordt.

            //if (!particleFilter.MagEvalueren)
            //{
            //    if (!particleFilter.DISABLED_WARNING) { Console.WriteLine("!queue full --> skipping resample !!!!!!!!!!"); }
            //    return;
            //}


            // bepaal of we moeten gaan exploiteren
            bool skip_resample = false;
            if (IsInExplorePhase)
            {

                //Console.WriteLine("sub#" + ID + " is Exploring(#" + this.exploratieTeller + "), counter = " + EXPLORE_PHASE_COUNTER);
                //bool use_smoothed = false;
                bool conditie = EXPLORE_PHASE_COUNTER > particleFilter.settingsForParticleFilter.RangeForSlope;


                // van elk particle checken of z'n exploratiepad 'uit het dal' aan het klimmen is en de error weer beter wordt
                // nadat ie een tijdje omhoog is gegaan. Dat is een teken dat ie 'over de rand' van het dal heen is 
                // en in een ander dal is aanbeland.
                slope = double.NaN;
                if (conditie)
                {
                    Particle seedParticle = null;
                    double seed_rmse = Double.PositiveInfinity;
                    double laagste_rico = Double.PositiveInfinity;
                    for (int p = 0; p < particles.Length; p++)
                    {
                        Particle particle = particles[p];
                        List<double> errs = exploreErrorsDict[p];
                        double dezeRico = MyMath.LeastSquaresLineFitSlope(errs, errs.Count - 1 - (int)particleFilter.settingsForParticleFilter.RangeForSlope, errs.Count);
                        if (!double.IsInfinity(dezeRico))
                        {
                            if (dezeRico < laagste_rico)
                            {
                                laagste_rico = dezeRico;
                            }
                            if (dezeRico < particleFilter.settingsForParticleFilter.SlopeForNewSeed)  //weer aan 't dalen... TODO: beetje geheugen toevoegen? smoothing? minimaal 2x achter elkaar rico omlaag? etc
                                                                                                      //we gaan omhoog... deze particle wordt de 'seed' voor de nieuwe populatie
                                                                                                      // als ie tenminste de beste is die deze ronde omhoog gaat
                            {
                                if (particle.ErrorUsedForStagnationAndExploration() < seed_rmse)
                                {
                                    if (!particleFilter.IsBezet(particle))
                                    {
                                        seedParticle = particle;
                                        seed_rmse = particle.ErrorUsedForStagnationAndExploration();
                                    }
                                    else
                                    {
                                        // helaas, verder zoeken!
                                        // Console.WriteLine("===>  Subpopulatie exploratie - rico omlaag, maar BEZET! verder zoeken <===");
                                    }
                                }

                            }
                        }
                    }
                    slope = laagste_rico;
                    bool seedParticleVanuitRegistratie = false;
                    if (double.IsInfinity(slope) || double.IsNaN(slope))
                    {
                        // weg hier! dit gaat mis, dan maar iets opvragen bij pf.
                        //     Console.WriteLine("rico is inf of nan");
                        while (seedParticle == null)
                        {
                            seedParticle = particleFilter.RegistreerStagnatie(random, null);
                        }
                        seedParticleVanuitRegistratie = true;
                        seedParticle.subPopulatie = this;
                    }
                    if (seedParticle != null)
                    {
                        particleFilter.SubStartExploiteren(this);
                        //Console.WriteLine("sub#" + ID + " -----> seedparticle gevonden (rico=" + rico + "):");
                        SetBestParticle(seedParticle);
                        if (seedParticleVanuitRegistratie)
                        {
                            SetBestParticle(null);
                        }
                        else
                        {
                            SetBestParticle(seedParticle);
                            //                            BestParticle.ParticlePatient = seedParticle.ParticlePatient;
                            //                        BestParticle = new Particle(BestParticle);
                            //                          BestParticle.ParticlePatient.GeneratedData = BestParticle.ParticlePatient.GeneratedData.DeepCopy();
                        }

                        AANTAL_EXPLORERS--;
                        EXPLORE_PHASE_COUNTER = 0;
                        EXPLORE_PHASE = false;
                        exploreErrorsDict = null;
                        slope = double.NaN;
                        errorList = new List<double>(); //deze errors zijn nooit meer nodig, we gaan nieuwe kolonie stichten :-)

                        // Console.WriteLine("seed = " + seedParticle);
                        //kennelijk zijn we gestopt. Nieuwe populatie maken rondom 'seed'
                        // TODO: hoe doen we het met errros/weights?? allemaal weight =1 doen ofzo? of nu resample gewoon skippen?
                        for (int p = 0; p < particles.Length; p++)
                        {
                            particles[p] = seedParticle.CreateMutatedNewParticle(random);
                            //                           particles[p].ParticlePatient = seedParticle.ParticlePatient; //zodat we een starvector hebben!
                        }
                        skip_resample = true; // het creeren van de nieuwe populatie IS al een soort resample stap. Verder niet nodig deze beurt/timestamp.
                    }
                }
            }
            else
            {
                // check of we op een local optimum aanbeland zijn:, maar alleen als er niet al teveel explorerende subpopluaties zijn.
                if (AANTAL_EXPLORERS < particleFilter.subPopuluaties.Count - particleFilter.settingsForParticleFilter.MaxNrSubPopulatiesNotExploring)
                {
                    if (DetermineStagnation())
                    {
                       
                        if(true) {

                            Particle newSeedParticle;
                            newSeedParticle = particleFilter.RegistreerStagnatie(random, BestParticle);
                            if (DetermineDeadEnd())
                            {
                                // Bij dead end sowieso nieuw random seed pakken ipv een (evt.) oude particle van de pf. 
                                // heeft geen nut om in buurt v/e dead end verder te zoeken
                                // kans op goede particle is ws. beter 'elders' in parameter landschap.
                                newSeedParticle = new Particle(this, null);
                                newSeedParticle.errorDataContainer = BestParticle.errorDataContainer; //omdat error gebruikt wordt in aanmaken v/d nieuwe particles rondom de seed :-(
                            }

                            if (newSeedParticle != null) //meteen klaar met exploratie! Een nieuwe seed gekregen om te gaan exploiteren
                            {
                                //Console.WriteLine("sub#" + ID + ": stagnatie --> seedparticle van pf gekregen!\n--> NS:" + newSeedParticle);
                                //todo: refactor omdat dit bijna copy-paste is van elders
                                newSeedParticle.subPopulatie = this;
                                //BestParticle = newSeedParticle;
                                SetBestParticle(newSeedParticle);
                                EXPLORE_PHASE_COUNTER = 0;
                                EXPLORE_PHASE = false;

                                // resample gewoon eenmalig skippen
                                skip_resample = true;
                                for (int p = 0; p < particles.Length; p++)
                                {
                                    particles[p] = newSeedParticle.CreateMutatedNewParticle(random);
                                }
                            }
                            else
                            {
                                BergmanAndBretonModel newRandomModel = particleFilter.GetRandomSample(random, BestParticle);
                                AANTAL_EXPLORERS++;
                                exploratieTeller++;
                                // Console.WriteLine("sub#" + ID + ": stagnatie --> exploratiefase(" + exploratieTeller + ") !!!");
                                EXPLORE_PHASE_COUNTER = 0;
                                EXPLORE_PHASE = true;

                                //break up population, give each particle a fixed explore direction (vector)
                                exploreVectorsDict = new Dictionary<int, BergmanAndBretonModel>();
                                exploreErrorsDict = new Dictionary<int, List<double>>();
                                //exploratie vector bepalen: random uit elkaar in willekeurige richting en die dan volhouden.
                                for (int p = 0; p < particles.Length; p++)
                                {
                                    if (newRandomModel != null)
                                    {
                                        particles[p].model = newRandomModel;
                                    }
                                    // random step in logspace:
                                    BergmanAndBretonModel exploreVector = new BergmanAndBretonModel(particleFilter.UseActivityModel);
                                    for (int paramNdx = 0; paramNdx < exploreVector.GetNrOfParameters(); paramNdx++)
                                    {
                                        // std op basis van de log-stap van min naar max.
                                        double value = random.NormalDistributionSample() * particleFilter.SIGMA_STEP_SIZES_logspace[paramNdx] * 0.01; // TODO: make it a setting in config: particleFilter.settingsForParticleFilter.ExploratieFaseInitieleMutatieStapFactor;
                                        exploreVector.SetParameterIgnoreBounds(paramNdx, value);
                                    }
                                    exploreVectorsDict[p] = exploreVector;
                                    exploreErrorsDict[p] = new List<double>();
                                }
                            }
                        }
                        
                        stagnation_counter = 0;

                        slope = double.NaN;
                        errorList = new List<double>(); //deze errors zijn nooit meer nodig, we gaan exploren
                                                        // als we hier de history niet clearen, dan komt de oude beste particle weer als beste uit de bus
                                                        // en zijn we weer terug bij het oude local opt.
                        SetBestParticle(null);
                        BestParticleHistory.Clear(); // TODO: in PF of hier toch iets van beste bijhouden? 
                    }
                }
            }

            if (EXPLORE_PHASE)
            {
                //resample a.h.v. explorevector
                for (int p = 0; p < particles.Length; p++)
                {
                    Particle particle = particles[p];
                    BergmanAndBretonModel exploreVector = exploreVectorsDict[p];
                    // toevoegen, en geeft geupdate expl vector terug (met evt. omkering van richtingen, bounce, stuiter)
                    exploreVector = particle.model.Add_InLogSpace(exploreVector);
                    exploreVectorsDict[p] = exploreVector;
                }
                EXPLORE_PHASE_COUNTER++;

            }
            else
            {
                if (!skip_resample)
                {
                    Resample_Basic();
                }
            }
        }
        */

        public virtual uint NumberOfParticlesFromHistory { get { return particleFilter.settingsForParticleFilter.NumberOfParticlesFromHistory; } }


        protected virtual void Resample_Basic()
        {
            double SSE_best = Double.PositiveInfinity; 
            // nb dit is lelijk, maar ach :-)  ... en de bestparticle heeft na eerste eval. een tweede eval met langere trail gehad, dus die waarde kunnen we niet automatisch gebruiken. 
            // TODO: opslaan ergens? Is ws niet moeite waard, fractie van miliseconde extra werk hier.
            foreach(Particle p in particles)
            {
                double err = p.ErrorUsedForStagnationAndExploration();
                if (err < SSE_best)
                {
                    SSE_best = err;
                }
            }
            if(double.IsNaN(SSE_best) || double.IsInfinity(SSE_best))
            {
               // Console.WriteLine("... skip resample (isnan/isinf)");
                return;
            }
            boltzmannCumSumPow = 0.5 + particleFilter.settingsForParticleFilter.ExponentialDecayInitValue * Math.Exp(-particleFilter.settingsForParticleFilter.ExponentialDecayDecayValue * SSE_best) ; // particleFilter.ExponentialDecayInitValue + 0.5 - boltzmannCumSumPow; // TODO: hack weghalen

            // relatieve ranking
            // selectie op basis van *relatieve* ordering vd weights (=SSE)?  ... gewogen mbv Math.pow(value, boltzmann)
            double[] relativeWeights = Particle.RankBasedRelativeWeights(particles, boltzmannCumSumPow);
            
            Particle[] newParticles = new Particle[particles.Length];
            // beste bewaren:
            int nstart = (int)particleFilter.NumberOfParticlesToKeep;
            for (int n = 0; n < nstart; n++)
            {
                newParticles[n] = new Particle(particles[n]);
            }

            int nstart2 = 0;
            if (NumberOfParticlesFromHistory > 0)
            {

                // random sample van best history erbij voegen.
                nstart2 = (int)Math.Min(NumberOfParticlesFromHistory, BestParticleHistoryQueueCount);
                if (nstart + nstart2 >= newParticles.Length)
                {
                    throw new ArgumentException("incorrecte settings!");
                }
                int step = Math.Max(BestParticleHistoryQueueCount / (int)NumberOfParticlesFromHistory, 1);
                for (int n = nstart; n < nstart + nstart2; n++)
                {
                    // int rnd_ndx = random.NextInt(BestParticleHistoryQueueCount);
                    int rnd_ndx = Math.Max(0, BestParticleHistoryQueueCount - 1 - (n - nstart) * step);
                    newParticles[n] = new Particle(BestParticleHistoryGet(rnd_ndx));
                    // delta is 0 bij copy, "stilstand"
                    newParticles[n].deltaModifiedModelParameters = new BergmanAndBretonModel(particleFilter.UseActivityModel);
                }
            }

            // resampling a.h.v. cumulativeWeights 
            int[] randomIndices = MyMath.GetIndicesInCumulativeArray(random, relativeWeights, newParticles.Length - nstart - nstart2);
            for (int n = nstart + nstart2; n < newParticles.Length; n++)
            {
                int p = randomIndices[n - nstart - nstart2];
                newParticles[n] = particles[p].CreateMutatedNewParticle(random);
            }

            // vv oude door nieuwe
            particles = newParticles;


            // sanity check
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] == null)
                {
                    // fix. Geen idee waarom dit soms nog voor kan komen!!
                    // misschien als er iets mis gaat met de historyQueue?
                    particles[i] = BestParticle;
                }
            }

            //SUPERHACK: --->  nb hier niet weghalen, maar aan/uit zetten in de functie <----
            GiveParticleTheRealParameters_Hack();

        }//end Resample


        public void CarbHypGBaseFeedback(double signal)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                // gebruik het GBase signal (dat uit de carb hyp komt, en een indicatie is voor of de carbhyps in algemeen groter (of kleiner)
                // zijn dan de door patient opgegeven waardes. Als dat het geval is, dan wordt de carbhyp over/onderschat en de ML geeft
                // dan een corresponderende onder/overschatting van de Gbase.

                double offset = Math.Sign(signal);
                double rndValue = random.NormalDistributionSampleClipped(2, offset, Math.Abs(2 * offset));
                rndValue = rndValue * Math.Min(3,  0.1 * Math.Sqrt(Math.Abs(signal)));
                double currentGb = particles[i].model.Gb_in_MG_per_DL;
                particles[i].model.SetParameter(BergmanAndBretonModel.parameter_index_Gb, currentGb + rndValue);
            }
        }

        // mechanisme om te detecteren of de populatie vast zit op lokaal optimum:
        // errors (noisy of smoothed noisy?) bijhouden, en kijken of er geen progressie meer is.
        // maar omdat het noisy is en er andere factoren zijn die voor schommelingen zorgen,
        // is een smoothed versie van de beste errors uit verleden nodig.
        private List<double> errorList = new List<double>();

        private double slope = Double.NaN;
        private double icr = double.NaN;
        private double isf = double.NaN;

        public double ICR { get { return icr; } }
        public double ISF { get { return isf; } }


        // scenario: toevallig is deze sub aan het stagneren, en is er bijna.
        // volgende iteratie is ie toevallig 2e en gaat in exploratie...
        // en de dan 1e was een toevalstreffer. Dan zijn we de beste kwijt.
        //
        // kies maar een waarde :-)... lager 'beschermt' langer.
        public void ResetStagnatie() { stagnation_counter = - ((int) particleFilter.settingsForParticleFilter.MaxStagnationCounter); } 
        private int stagnation_counter = 0;

        //voor inf en nan rico of icr, isf
        private bool DetermineDeadEnd()
        {
            // icr en isf grenzen:
            // icr = BestParticle.ParticlePatient.ForceCalculateICR();
            // if (!double.IsInfinity(icr))
            {
                if (icr < particleFilter.settingsForParticleFilter.ICR_lower_bound || icr > particleFilter.settingsForParticleFilter.ICR_upper_bound)
                {
                    slope = Double.NaN;
                    //stagnation_counter += 3;
                    return true;
                }
            }
           // isf = BestParticle.ParticlePatient.ForceCalculateISF();
            //if (!double.IsInfinity(isf))
            {
                if (isf < particleFilter.settingsForParticleFilter.ISF_lower_bound || isf > particleFilter.settingsForParticleFilter.ISF_upper_bound)
                {
                    slope = Double.NaN;
                    //stagnation_counter += 3;
                    return true;
                }
            }
            if(double.IsInfinity(slope))
            {
                return true;
            }
            return false;
        }
        private bool DetermineStagnation()
        {
            if(particleFilter.settingsForParticleFilter.nearestParticleHashing_useForStagnationDetection &&  particleFilter.IsBezet(this.BestParticle))
            {
                return true;
            }
            if (errorList.Count > particleFilter.settingsForParticleFilter.RangeForSlope)
            {
               this.slope = MyMath.LeastSquaresLineFitSlope(errorList, errorList.Count - 1 - (int)particleFilter.settingsForParticleFilter.RangeForSlope, errorList.Count);
            }

            
            if (stagnation_counter >= this.particleFilter.settingsForParticleFilter.MaxStagnationCounter)
            {
                //Console.WriteLine("sub#" + ID + ": stagnatie!");
                return true;
            }

            // if (!double.IsInfinity(icr))
            {
                if (icr < particleFilter.settingsForParticleFilter.ICR_lower_bound || icr > particleFilter.settingsForParticleFilter.ICR_upper_bound)
                {
                  //  rico = Double.NaN;
                    stagnation_counter += (int) (this.particleFilter.settingsForParticleFilter.MaxStagnationCounter / 2);
                    return false;
                }
            }
            // isf = BestParticle.ParticlePatient.ForceCalculateISF();
            //if (!double.IsInfinity(isf))
            {
                if (isf < particleFilter.settingsForParticleFilter.ISF_lower_bound || isf > particleFilter.settingsForParticleFilter.ISF_upper_bound)
                {
                   // rico = Double.NaN;
                    stagnation_counter += (int)(this.particleFilter.settingsForParticleFilter.MaxStagnationCounter / 2);
                    return false;
                }
            }

            if (errorList.Count > particleFilter.settingsForParticleFilter.RangeForSlope)
            {
               // rico = MyMath.Rico(errorList, errorList.Count- 1 - (int)particleFilter.settingsForParticleFilter.RangeForSlope, errorList.Count);
                if(double.IsNaN(slope))
                {
                    return true;
                }
                if (slope > this.particleFilter.settingsForParticleFilter.SlopeForStagnation)
                {
                    stagnation_counter++;
                }
                else
                {
                    // niet alle opgebouwde stangatie 'verliezen'
                    stagnation_counter -= 2;
                    if(stagnation_counter < 0) { stagnation_counter = 0; }
                }

                if (this.BestParticle == this.particleFilter.BestParticle)
                {
                    stagnation_counter = 0;
                    return false;
                }

                //if (stagnation_counter >= this.particleFilter.settingsForParticleFilter.MaxStagnationCounter)
                //{ 
                //    //Console.WriteLine("sub#" + ID + ": stagnatie!");
                //    return true;
                //}
                return false;
            }
            else
            {
                slope = double.NaN;
            }
            return false;
        }

        public static int solver_interval = 1;
       

    }

}
