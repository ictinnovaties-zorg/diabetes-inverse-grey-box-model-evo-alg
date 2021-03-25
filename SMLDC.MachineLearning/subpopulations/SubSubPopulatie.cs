using SMLDC.Simulator.DiffEquations.Models;
using SMLDC.Simulator.DiffEquations.Solvers;
using SMLDC.Simulator.Schedules;
using SMLDC.Simulator.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMLDC.MachineLearning.subpopulations
{

    // niet alles vanuit de particle filter betrekken, maar juist van de subpopulatie waar deze een subsub van is.
    class SubSubPopulatie : SubPopulatie
    {
        public SubPopulatie subpopulatie;
       

        public SubSubPopulatie(SubPopulatie subpopulatie, Particle[] initParticles) : base(subpopulatie.particleFilter)
        {
            this.subpopulatie = subpopulatie;
            this.NegateID(this.subpopulatie);
            //rnd krijgt in base al een waarde maar dat is op basis van oude id dus elke subsub krijgt zelfde rnd van particlefilter!!!
            random = new RandomStuff(particleFilter.settingsForParticleFilter.seedForParticleFilter + Math.Abs(this.ID) + 1);

            particles = initParticles;
            for (int p = 0; p < particles.Length; p++)
            {
                particles[p].subPopulatie = this;
            }
        }



        private uint startTime, endTime;
        public void SetStartAndEndTime(uint start, uint end)
        {
            startTime = start;
            endTime = end;
        }
        public override SolverResultBase BestPatientGeneratedData
        {
            get
            {
                if (BestParticle == null) { return this.subpopulatie.BestPatientGeneratedData; }
                else { return BestParticle.ParticlePatient.GeneratedData; }
            }
        }

        public override StringBuilder EvalueerNaResample(ErrorCalculationSettings errorCalculationSettings, Schedule noisyFoodScheduleClippedToTrail, uint langstarttime, uint starttime, uint currentTime, /*double[] bestStartingVectorForLongTrail,*/ bool useParallel)
        {
            StringBuilder temp = Evaluate(errorCalculationSettings, noisyFoodScheduleClippedToTrail, langstarttime, starttime, currentTime,/* bestStartingVectorForLongTrail,*/ useParallel);
            return temp;
        }


        // altijd gerund na de valuate aanroep.
        protected override StringBuilder EvaluatePost(ErrorCalculationSettings errorCalculationSettings, Schedule noisyFoodScheduleClippedToTrail, uint starttime, uint stopTime) //, PatientEvent mostRecentCarbEvent)
        {
            //  resultaten van vorige evaluatie:
            Array.Sort(particles);
            Array.Reverse(particles); // van hoog naar laag
            StringBuilder sb = new StringBuilder();

            // todo: !!
            // beste opslaan, omdat de gegen.data van bestparticle de beste schatting is voor een startvector
            // in de toekomst. TODO: niet alleen deze, maar alle?? of alleen maar starvectors, en 
            // de VERDELING gebruiken (met random sampling) voor bepalen v/d nieuwe startvector voor de particles??

            return sb;
        }


        public override uint NumberOfParticlesFromHistory { get { return particleFilter.settingsForParticleFilter.NumberOfSubParticlesFromHistory; } }

        public override void Resample()
        {
           // if (BestParticle == null) { return; } // gebeurt oa de eerste keer dat resample (VOOR evalueer) aangeroepen wordt.

            //if (!particleFilter.MagEvalueren)
            //{
            //    if (!particleFilter.DISABLED_WARNING) { Console.WriteLine("!queue full --> skipping resample !!!!!!!!!!"); }
            //    return;
            //}
            // alle exploratie gedoe hebben we niet in deze subsub, dus direct door naar de kern v/d resample
            Resample_Basic();

            //SUPERHACK: --->  nb hier niet weghalen, maar aan/uit zetten in de functie <----
            GiveParticleTheRealParameters_Hack();
        }

    }

}
