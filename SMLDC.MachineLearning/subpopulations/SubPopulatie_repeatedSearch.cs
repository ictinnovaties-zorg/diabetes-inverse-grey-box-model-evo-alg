using SMLDC.Simulator.DiffEquations.Models;
using SMLDC.Simulator.Schedules;
using SMLDC.Simulator.Schedules.Events;
using SMLDC.Simulator.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMLDC.MachineLearning.subpopulations
{
    class SubPopulatie_repeatedSearch : SubPopulatie
    {
        /**
         * idee: gebruik een lange (maanden?) trail, en doe daarin de pf steeds op kleine stukjes (e.g. een paar dagen)
         * en loop over hele lange trail heen, en aaan het einde weer bij begin beginnnen.
         * Op die manier kan wellicht beter gebruik gemaakt worden van de oude data die 'toen' aan het begin slechts diende
         * voor de ruwste eerste schifting op paricles. Maar na 1x lange trail zijn particles ingezoemd, en kan eerste data
         * opnieuw van meer nut zijn.
         * 
         *
         * beste altijd runnen op hele trail, zodat we altijd startwaardes hebben!!!
         */
        public SubPopulatie_repeatedSearch(ParticleFilter pf) : base(pf)
        {
        }


        public override StringBuilder EvalueerNaResample(ErrorCalculationSettings errorCalculationSettings, Schedule noisyFoodScheduleClippedToTrail, uint langstarttime, uint starttime, uint currentTime, bool useParallel)
        {
            StringBuilder sb = NextTurn(errorCalculationSettings, noisyFoodScheduleClippedToTrail, langstarttime, starttime);
            if (IsInExplorePhase) //todo: dit in resample zetten?
            {
                return base.Evaluate(errorCalculationSettings, noisyFoodScheduleClippedToTrail, langstarttime, starttime, currentTime, useParallel);
            }

            // negeer starttime : TODO: hub refactoren, en hier alleen de uiterste grens meegeven (of schedule door hub heel ruim laten clippen en sub zelf rest laten kiezen)
            // particles de hele nu beschikbare trail af laten gaan, in stukjes van een aantal dagen
            // en na elk stukje resamplen

            this.particleFilter.BestPatientGeneratedData.TryGetValuesFromTime(starttime, out double[] startvector);
            if (startvector == null) //kennelijk is deze data er nog nooit geweest
            {
                BestParticle.Evaluate_simple(errorCalculationSettings, noisyFoodScheduleClippedToTrail, langstarttime, currentTime);
            }
            SubSubPopulatie subsubpop = new SubSubPopulatie(this, this.particles);

            //Console.WriteLine("sub#" + this.ID + "===========================================================================");

            List<Particle> bestSubSubParticles = new List<Particle>();

            // eerst per kort stukje (piek?) een pf search runnen ~~  idee van local search in particle (naar carbhyp) gebruiken? 
            // opslitsen in pieken, zodat je 'laag' begint
            // probleempje: als een tijdstuk (van piek tot piek) toevallig te klein is, valt er geen gluc mmnt in, en kan de error
            // niet berekend worden. Daarom de afstand tussen de gluc metingen gebruiken als minimale afstand. Als de pieken dichter op
            // elkaar liggen, dan overlappen de bereiken
            uint margeVoorInsuline = 30;
            bool firsttime = true;
            uint minimaleTijd = particleFilter.settingsForParticleFilter.ContinuousGlucoseMmntEveryNMinutes + 1;
            List<Tuple<PatientEvent, uint>> insulinEvents = noisyFoodScheduleClippedToTrail.GetInsulineEvents(starttime, currentTime, minimaleTijd);
            if (insulinEvents.Count > 0)
            {

                if (insulinEvents[0].Item1.TrueStartTime > starttime)
                {
                    PatientEvent dummyEvent = new PatientEvent(Simulator.Utilities.Enums.PatientEventType.DUMMY, starttime, 0);
                    uint endtime = Math.Max(starttime + minimaleTijd, insulinEvents[0].Item1.TrueStartTime - 1);
                    insulinEvents.Insert(0, new Tuple<PatientEvent, uint>(dummyEvent, endtime));
                }

                int stap = (int)particleFilter.settingsForParticleFilter.NumberOfPeaksPerSubSubPopulationStep;


              
                for (int repeats = 0; repeats < particleFilter.settingsForParticleFilter.NumberOfRepeatsPerSubSubPopulation; repeats++)
                {
                    for (int pk = 0; pk < insulinEvents.Count; pk += stap)
                    {
                        Tuple<PatientEvent, uint> piek = insulinEvents[pk];
                        int this_trail_start = (int)Math.Max((int)starttime, (int)piek.Item1.TrueStartTime - 1 - margeVoorInsuline);

                        int endpk = Math.Min(pk + (int)particleFilter.settingsForParticleFilter.NumberOfPeaksPerSubSubPopulation, insulinEvents.Count - 1);
                        uint this_trail_end = insulinEvents[endpk].Item2;

                        if (this_trail_end - this_trail_start < minimaleTijd)
                        {
                            continue;
                        }

                        // evalueer-resample cyclus
                        // Console.WriteLine("\t sub#" + this.ID + " ---- subsub op " + this_trail_start + " tot " + this_trail_end + " -----");
                        for (int t = 0; t < particleFilter.settingsForParticleFilter.NumberOfTurnsPerSubSubPopulation; t++)
                        {
                            if (!firsttime)
                            {
                                subsubpop.Resample();
                            }
                            firsttime = false;
                            StringBuilder temp = subsubpop.EvalueerNaResample(errorCalculationSettings, noisyFoodScheduleClippedToTrail, langstarttime, (uint)this_trail_start, this_trail_end, useParallel);

               //             Console.WriteLine("\tSUB#" + this.ID +" subsub particle[0]..weight [itr." +t+ "] = " + subsubpop.Particles[0].Weight);
                            // op dit punt is er geen bestparticle omdat de subsub dat niet bijhoudt. Want als we dat wel doen,
                            // dan gaat ie bootstrappen omdat elke subsub iteratie dan  de startvector van de best v/d vorige iteratie pakt
                            if(subsubpop.Particles[0].Weight <= particleFilter.settingsForParticleFilter.RmseForBreak)
                            {
                                break;
                            }
                        }
                        subsubpop.UpdateBestParticleRef(subsubpop.Particles[0]);
                        bestSubSubParticles.Add(new Particle(subsubpop.BestParticle));
                        bestSubSubParticles.Add(new Particle(subsubpop.Particles[1])); //2nd best

                        //  Console.WriteLine(temp);
                        if (particleFilter.settingsForParticleFilter.UpdateBestAfterEachSubsubTraining)
                        {
                            Particle heleTrailTotNuToeParticle = new Particle(subsubpop.BestParticle);
                            heleTrailTotNuToeParticle.subPopulatie = this;
                            // de huidige gefinetunede hypothese gebruiken om hele  train trail tot hier te updaten (via Best)
                            heleTrailTotNuToeParticle.Evaluate_simple(errorCalculationSettings, noisyFoodScheduleClippedToTrail, starttime, this_trail_end);
                            // alles in de lange trail moeten we wel blijven behouden.
                            this.BestParticle.ParticlePatient.GeneratedData.AddCopyOverwrite(heleTrailTotNuToeParticle.ParticlePatient.GeneratedData);
                        }
                        // volgende stap
                    }
                }

                if (particleFilter.settingsForParticleFilter.SubRepeatedSearch_gebruikBesteVanElkeSubSub)
                {
                    //de bestSubSubParticles zijn in feite exploratie  richtingen?!? want 
                    // ze bootstrappen op vorige piek-interval beste, dus ze zijn vast niet goed voor hele traject.

                    // particles weer terug claimen van de subsub:
                    Particle[] newParticles = new Particle[subsubpop.Particles.Length + bestSubSubParticles.Count];
                    int ndx = 0;
                    foreach(Particle particle in subsubpop.Particles)
                    {
                        newParticles[ndx] = particle;
                        ndx++;
                    }
                    //                    this.particles = bestSubSubParticles.ToArray(); // subsubpop.Particles;
                    foreach (Particle particle in bestSubSubParticles)
                    {
                        newParticles[ndx] = particle;
                        ndx++;
                    }
                    this.particles = newParticles;
                }
                else
                {
                    this.particles = subsubpop.Particles;
                }
                // claimen:
                foreach (Particle particle in particles) { particle.subPopulatie = this; }

                // hele populatie evalueren, zodat er daarna geresampled kan worden.
             //   StringBuilder temptotaal = Evaluate(sse_pow_for_error, noisyFoodScheduleClippedToTrail, langstarttime, starttime, currentTime, useParallel);
             //   sb.Append(temptotaal);
             
            }
            else
            {
                Console.WriteLine("geen nieuwe ronde!");
            }
            //zorgen voor startvector voor tijdens evaluate
            BestParticle.Evaluate_simple(errorCalculationSettings, noisyFoodScheduleClippedToTrail, langstarttime, starttime + 1);

            // nodig? beste op hele (lange) trail doen
            StringBuilder tempsb = Evaluate(errorCalculationSettings, noisyFoodScheduleClippedToTrail, langstarttime, starttime, currentTime, useParallel);

            //sort/reverse al in evaluate gedaan
         //   Array.Sort(this.particles);
        //    Array.Reverse(this.particles);
         //   BestParticle = new Particle(particles[0]);

            // teveel aan particles terugsnoeien/opvullen (zijn er nu evenveel als er pieken waren)
            Particle[] newparticles = new Particle[particleFilter.settingsForParticleFilter.NumberOfParticlesPerSubPopulation];
            for (int i = 0; i < newparticles.Length; i++)
            {
                newparticles[i] = particles[i % particles.Length];
            }
            particles = newparticles;



            BestParticle.ParticlePatient.GeneratedData = BestParticle.ParticlePatient.GeneratedData.DeepCopy();

            return sb.Append(tempsb);
        }


        protected override void Resample_Basic()
        {
           // Resample_Basic_ORIG();
            base.Resample_Basic();
        }

        protected void Resample_Basic_ORIG()
        {
            if (NumberOfParticlesFromHistory > 0)
            {
                Array.Sort(this.particles); // slechtste bovenaan

                // slechtste vervangen door 'iets uit history'
                int nstart2 = (int)Math.Min(NumberOfParticlesFromHistory, BestParticleHistoryQueueCount);
                if (nstart2 >= particles.Length)
                {
                    throw new ArgumentException("incorrecte settings!");
                }
                int step = Math.Max(BestParticleHistoryQueueCount / (int)NumberOfParticlesFromHistory, 1);
                for (int n = 0; n < nstart2; n++)
                {
                    //int rnd_ndx = random.NextInt(BestParticleHistoryQueueCount);
                    int rnd_ndx = Math.Max(0, BestParticleHistoryQueueCount - 1 - n * step);
                    particles[n] = new Particle(BestParticleHistoryGet(rnd_ndx));
                    // delta is 0 bij copy, "stilstand"
                    particles[n].deltaModifiedModelParameters = new BergmanAndBretonModel(particleFilter.UseActivityModel);
                }
            }

         


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
            // Alle resampling is al in evaluate gedaan.
        }

    }
}