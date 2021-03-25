using SMLDC.MachineLearning.subpopulations;
using SMLDC.Simulator.Schedules;
using SMLDC.Simulator.Schedules.Events;
using SMLDC.Simulator.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using static SMLDC.Simulator.Utilities.Enums;

namespace SMLDC.MachineLearning
{
    /*
     * classes for keeping track of the carb hypotheses that the ML does, based on measured glucose curves.
     */
    

    public class CarbHypData<T>
    {
        public CarbHypData(double estimate, int offset)
        {
            this.estimate = estimate;
            this.offset = offset;
        }

        public double estimate;
        public int offset; // ten opzichte van de 'groep'
        public CarbHypDataGroup<T> group;
    }


    public class CarbHypDataGroup<T>
    {
        private CarbHypothesis carbHypothesis;
        private uint referenceTime;
        public uint GetReferenceTime() { return referenceTime; }
        private double aggregateOffset;
        private double aggregateEstimate;
        private double aggregateTotalWeight = 0;
        private bool basedOnPatientEstimate;
        private double orig_estimate;
        public bool BasedOnPatientEstimate { get { return basedOnPatientEstimate; } }

        public int MAX_CARB_OFFSET_BASED_ON_PATIENT_ESTIMATE_in_min { get { return carbHypothesis.MyParticleFilter.settingsForParticleFilter.MAX_CARB_OFFSET_BASED_ON_PATIENT_ESTIMATE_in_min; } }
        public double MAX_CARB_ESTIMATE_in_gr { get { return carbHypothesis.MyParticleFilter.settingsForParticleFilter.MAX_CARB_ESTIMATE_in_gr; } }
        public double MAX_AFWIJKING_VAN_PATIENT_ESTIMATE_in_gr { get { return carbHypothesis.MyParticleFilter.settingsForParticleFilter.MAX_AFWIJKING_VAN_PATIENT_ESTIMATE_in_gr; } }

        public CarbHypDataGroup(CarbHypothesis ch, uint time, double estimate, bool basedOnPatientEstimate)
        {
            this.carbHypothesis = ch;
            this.referenceTime = time;
            this.aggregateEstimate = estimate;
            this.aggregateOffset = 0;
            this.basedOnPatientEstimate = basedOnPatientEstimate;
            if(this.basedOnPatientEstimate)
            {
                orig_estimate = estimate;
            }
            else
            {
                orig_estimate = -1;
            }
            this.carbHypDataDict = new Dictionary<T, CarbHypData<T>>();
        }

        public void OffsetCarbs(T sub, double offsetInCarbs)
        {
            if (carbHypDataDict.ContainsKey(sub))
            {
                //carbHypDataDict[sub].estimate += offsetInCarbs;
                carbHypDataDict[sub].estimate = MyMath.Clip(carbHypDataDict[sub].estimate + offsetInCarbs, 0, MAX_CARB_ESTIMATE_in_gr);
                if (carbHypDataDict[sub].estimate < 0)
                {

                }
            }
        }
        public void Update(T sub, double learningrate, int offset, double estimate)
        {
            if(estimate < 0)
            {

            }
            estimate = MyMath.Clip(estimate, 0, MAX_CARB_ESTIMATE_in_gr);
            if (this.basedOnPatientEstimate)
            {
                // mag niet teveel gaan driften!
                offset = MyMath.Clip(offset, -MAX_CARB_OFFSET_BASED_ON_PATIENT_ESTIMATE_in_min, MAX_CARB_OFFSET_BASED_ON_PATIENT_ESTIMATE_in_min);
                // en niet teveel afwijken
                estimate = MyMath.Clip(estimate, orig_estimate - MAX_AFWIJKING_VAN_PATIENT_ESTIMATE_in_gr, orig_estimate + MAX_AFWIJKING_VAN_PATIENT_ESTIMATE_in_gr);
            }
            if (carbHypDataDict.ContainsKey(sub))
            {
                carbHypDataDict[sub].estimate = learningrate * estimate + (1 - learningrate) * carbHypDataDict[sub].estimate;
                carbHypDataDict[sub].offset = (int)Math.Round(learningrate * offset + (1 - learningrate) * carbHypDataDict[sub].offset);

                if(carbHypDataDict[sub].estimate < 0)
                {

                }
            }
            else
            {
                carbHypDataDict[sub] = new CarbHypData<T>(estimate, offset);
                aggregateEstimate = estimate;
                aggregateOffset = offset;

                if (carbHypDataDict[sub].estimate < 0)
                {

                }
            }
            carbHypDataDict[sub].estimate = MyMath.Clip(carbHypDataDict[sub].estimate, 0, MAX_CARB_ESTIMATE_in_gr);

            if (carbHypDataDict[sub].estimate < 0)
            {

            }

            if (this.basedOnPatientEstimate)
            {
                // mag niet teveel gaan driften!
                carbHypDataDict[sub].offset = MyMath.Clip(carbHypDataDict[sub].offset, -MAX_CARB_OFFSET_BASED_ON_PATIENT_ESTIMATE_in_min, MAX_CARB_OFFSET_BASED_ON_PATIENT_ESTIMATE_in_min);
                // en niet teveel afwijken
                carbHypDataDict[sub].estimate = MyMath.Clip(carbHypDataDict[sub].estimate, orig_estimate - MAX_AFWIJKING_VAN_PATIENT_ESTIMATE_in_gr, orig_estimate + MAX_AFWIJKING_VAN_PATIENT_ESTIMATE_in_gr);
            }
        }


        public Tuple<uint, double> GetCarbEstimate(T key)
        {
            double offset;
            double estimate;
            if (carbHypothesis.MyParticleFilter.settingsForParticleFilter.GetCarbEstimatesPerSub && key != null && carbHypDataDict.ContainsKey(key))
            {
                offset = carbHypDataDict[key].offset;
                estimate = carbHypDataDict[key].estimate;
            }
            else
            {
                offset = this.aggregateOffset;
                estimate = this.aggregateEstimate;
            }
            int time = (int)(referenceTime + Math.Round(offset));
            time = Math.Max(0, time); // niets VOOR start van sim!
            return new Tuple<uint, double>((uint)time, estimate);
        }



        public Tuple<uint, double> GetCarbEstimateForSub(T key)
        {
            int offset = carbHypDataDict[key].offset;
            double estimate = carbHypDataDict[key].estimate;

            int time = (int)referenceTime + offset;
            time = Math.Max(0, time); // niets VOOR start van sim!
            return new Tuple<uint, double>((uint)time, estimate);
        }


        public bool ContainsKey(T key)
        {
            return carbHypDataDict.ContainsKey(key);
        }
        // container voor alle bij elkaarhorende carb estimates (bv. van alle subpolicies) 
        private Dictionary<T, CarbHypData<T>> carbHypDataDict;

    
        // altijd maar het avg. van alle data.
        // dus subpops die lang actief zijn, hebben meer invloed
        //
        // TODO: varianten als learning rate toepassen?!?!
        // huidige systeem is avg of all time --> flink gewicht op patientschatting.
        // en als er meer eXploratie is, dan is de weging ook lager (want minder subs die carb est. hebben)
        // dit voorkomt 'uit de bocht schieten' van enkele subs???? of is het niet wenselijk?
        public void CalcAggregate(Dictionary<T, double> weging)
        {
            double oude_aggOffset = aggregateOffset;
            double oude_aggEstimate = aggregateEstimate;
            if (carbHypothesis.MyParticleFilter.settingsForParticleFilter.CarbEstimatesPerSubUpdateIsLearningRate != 0)
            {
                aggregateTotalWeight = 0;
            }

            foreach (T key in this.carbHypDataDict.Keys)
            {
                if (weging.ContainsKey(key))
                {
                    double wegingsFactor = weging[key];
                    double thisOffset = carbHypDataDict[key].offset;
                    double thisEstimate = carbHypDataDict[key].estimate;

                    // average over all: voeg nieuwe data gewogen toe
                    aggregateTotalWeight += wegingsFactor;
                    aggregateOffset = (aggregateOffset * (aggregateTotalWeight - wegingsFactor) + thisOffset * wegingsFactor) / (double)aggregateTotalWeight;
                    aggregateEstimate = (aggregateEstimate * (aggregateTotalWeight - wegingsFactor) + thisEstimate * wegingsFactor) / (double)aggregateTotalWeight;
                }
            }


            if (carbHypothesis.MyParticleFilter.settingsForParticleFilter.CarbEstimatesPerSubUpdateIsLearningRate > 0)
            {
                if (oude_aggEstimate >= 0)
                {
                    // gewicht is learningrate. Nog wel even terugschalen, want weging kan 0--1 zijn.
                    double learningRate = carbHypothesis.MyParticleFilter.settingsForParticleFilter.CarbEstimatesPerSubUpdateIsLearningRate;
                    aggregateOffset = learningRate * aggregateOffset + (1 - learningRate) * oude_aggOffset;
                    aggregateEstimate = learningRate * aggregateEstimate + (1 - learningRate) * oude_aggEstimate;
                }
                // else:: dit was eerste keer, dus gewoon waarde overnemen, niet 'leren'
            }
        }


        
        public void MoveToOffsetZero() 
        {
            if (!this.basedOnPatientEstimate)
            {
                this.referenceTime = (uint)(this.referenceTime + Math.Round(aggregateOffset));
                foreach (T key in carbHypDataDict.Keys)
                {
                    carbHypDataDict[key].offset = (int)(carbHypDataDict[key].offset - Math.Round(aggregateOffset));
                }
            }
        }


        public double GetCarbAggregate()
        {
            return this.aggregateEstimate;
        }
        public uint GetTimePlusAggregateOffset()
        {
            return (uint) (this.referenceTime + Math.Round(aggregateOffset));
        }
        public void RemoveKey(T sub)
        {
            lock (this.carbHypDataDict)
            {
                try
                {
                    this.carbHypDataDict.Remove(sub);
                }
                catch { }
            }
        }
    }




    public class CarbHypothesis
    {
        /*
         * het lijkt logischer om de eerste unint (tijd) al meteen incl offset te doen, dan is 2e dict. niet nodig
         * Maar dan ontstaat het volgende probleem: als ene sub de offset update en de andere op een andere manier,
         * hoe weet je dan later nog welke <tijd, carbEstimate> paren bij elkaar horen in diverse subs?
         * Hoe agregeren tot 1 waarde?
         * Wat als we de carbEst+offset tot object maken, en een link naar de 'groep' waar ze bij horen?
         * die groep is op moment van registratie bepaald (en elke sub heeft dan zelfde est.+offset) 
         * maar die kunnen dan uit elkaar gaan lopen --> lijkt handigste manier
         * 
         * 
         //
            zodra out-of-(traintrail)-scope: aggregeer zodanig dat er nog maar 1 key is van type EqualToEverySubPopulatie
            zodat elke sub zelfde antwoord krijgt
            
            TODO: wat te doen bij subloze particles???

         */

        protected SortedDictionary<uint, CarbHypDataGroup<SubPopulatie>> carbHypothesisBeforeTrail;

        private ParticleFilter _particleFilter;
        public ParticleFilter MyParticleFilter { get { return _particleFilter; } }

     //   public virtual string ToCSV { get; set; }

        public CarbHypothesis(ParticleFilter pf)
        {
            _particleFilter = pf;
            carbHypothesisBeforeTrail = new SortedDictionary<uint, CarbHypDataGroup<SubPopulatie>>();
        }



        public Schedule GetCarbHypothesisSchedule(SubPopulatie subPopulatie, uint untilTime)
        { 
            lock (carbHypothesisBeforeTrail)
            {
                Schedule carbEstSchedule = new Schedule();

                foreach (uint time in this.carbHypothesisBeforeTrail.Keys)
                {
                    if (time > untilTime) { break; } // is sorted, dus we zijn nu klaar

                    CarbHypDataGroup<SubPopulatie> group = this.carbHypothesisBeforeTrail[time];
                    Tuple<uint, double> estimate = group.GetCarbEstimate(subPopulatie);
                    carbEstSchedule.AddEventWiggle(PatientEventType.CARBS, estimate.Item1, estimate.Item2);
                }
                return carbEstSchedule;
            }
        }


        // aanroepen als main pf een sub verwijdert
        public void RemoveSubPopulatie(SubPopulatie sub)
        {
            lock (carbHypothesisBeforeTrail)
            {
                foreach (uint time in this.carbHypothesisBeforeTrail.Keys)
                {
                    this.carbHypothesisBeforeTrail[time].RemoveKey(sub);
                }
            }
        }


        public void AddNewCarbHypothesisGroupsFromSchedule(Schedule schedule, int starttime=0)
        {
            for(int i = 0; i < schedule.GetEventCount(); i++)
            {
                PatientEvent evt = schedule.GetEventFromIndex(i);
                if(evt.EventType == PatientEventType.CARBS && evt.TrueStartTime >= starttime)
                {
                    bool basedOnTruth = (evt.Carb_TrueValue_in_gram > 0); // als het een carb=0 is, dan kan er gegeten zijn maar wilde de patient alleen maar een correctie doen en heeft niks ingevuld
                    AddNewCarbHypothesisGroup(evt.TrueStartTime, basedOnTruth, evt.Carb_TrueValue_in_gram, 1);
                }
            }
        }




        // returnt de juiste key als er een hypothese (en dus hashkey) dicht genoeg in de buurt is
        // en anders -1
        public int GetTimeKey(uint hypothesisTime, uint zoek_marge = 180)
        {
            lock (carbHypothesisBeforeTrail)
            {
                // niet alleen op tijd zelf checken, maar ook op nabijheid van andere hyp!
                uint start_time = (uint)Math.Max(0, (int)hypothesisTime - zoek_marge);
                uint end_time = hypothesisTime + zoek_marge;
                for (uint time_step = 0; time_step <= zoek_marge; time_step++)
                {
                    uint time = (uint)(hypothesisTime + time_step);
                    if (this.carbHypothesisBeforeTrail.ContainsKey(time))
                    {
                        return (int)time;
                    }
                    time = (uint)( (int)hypothesisTime - (int)time_step);
                    if (this.carbHypothesisBeforeTrail.ContainsKey(time))
                    {
                        return (int)time;
                    }

                }
                return -1;
            }
        }

        //true : nieuw toegevoegd
        public bool AddNewCarbHypothesisGroup(uint hypothesisTime,bool basedOnPatientEstimate=false, double estimate=0, int minAfstand=-1)
        {
            lock (carbHypothesisBeforeTrail)
            {
                if(minAfstand < 0)
                {
                    minAfstand = (int) MyParticleFilter.settingsForParticleFilter.VerwijderCarbHypsDichterBijDanMarge;
                }

                if(basedOnPatientEstimate)
                {

                }
                if (verboden_tijden.Contains(hypothesisTime)) { 
                    return false; 
                }
                // niet alleen op tijd zelf checken, maar ook op nabijheid van andere hyp!
                int potentialTime = GetTimeKey(hypothesisTime, (uint) minAfstand);
                if(potentialTime < 0)
                {
                    this.carbHypothesisBeforeTrail[hypothesisTime] = new CarbHypDataGroup<SubPopulatie>(this, hypothesisTime, estimate, basedOnPatientEstimate);
                    return true;
                }
                else
                {

                }
                return false;
            }
        }


        public void RemoveGroup(uint time)
        {
            lock(carbHypothesisBeforeTrail)
            {
                carbHypothesisBeforeTrail.Remove(time);
            }
        }
        public void UpdateCarbEstimation(Particle particle, double learningrate, uint time, uint nieuwetijd, double estimate)
        {
            lock (carbHypothesisBeforeTrail)
            {
                //if (!carbHypothesisBeforeTrail.ContainsKey(time))
                int keyTime = GetTimeKey(time);
                if(keyTime < 0)
                {
                    AddNewCarbHypothesisGroup(time);
                    keyTime = (int)time;
                }
                else
                {
                    time = (uint)keyTime;
                }
                int offset = (int)nieuwetijd - keyTime;
                this.carbHypothesisBeforeTrail[time].Update(particle.subPopulatie, learningrate, offset, estimate);
            }
        }


        // return een indicatie van hoe lobsided het 'collectief' is, en in welke richting Gb aangepast kan worden.
        public double RunEstimationsUpdate(List<SubPopulatie> subPopuluaties)
        {
            lock (carbHypothesisBeforeTrail)
            {
                uint starttime = (uint) MyParticleFilter.GetLongTrainTrailStartTime();
                uint stoptime = MyParticleFilter.GetCurrentTime();
                StringBuilder output = new StringBuilder();
                double lobsidednessSignaal = 0;
                Schedule noisySchedule = MyParticleFilter.ObservedPatient.NoisySchedule;
                // bepaal weging
                Dictionary<SubPopulatie, double> carbEstimates = new Dictionary<SubPopulatie, double>();
                Dictionary<SubPopulatie, double> carbsInSchedule = new Dictionary<SubPopulatie, double>();
                Dictionary<SubPopulatie, double> lobsidedness_rmse = new Dictionary<SubPopulatie, double>();
                Dictionary<SubPopulatie, double> lobsidedness_linear = new Dictionary<SubPopulatie, double>();
                Dictionary<SubPopulatie, int> lobsidedness_aantal = new Dictionary<SubPopulatie, int>();

                // weging wordt kleiner als er meer 'lobsided' schattingen zijn
                /// schema afgaanen voor elke event checken voor de gegeven subs of er een estimate was
                /// en bepaal op basis daarvan per sub de lobsidedness
                /// de 'lobsidedness' is hier bepaald door gewoon totaal van beide (noisy patient opgave en carb est.) te vergelijken.
                /// Idee erachter: op de totalen zal het ongeveer gelijk moeten uitkomen voor scahtting en correctie (i.e. hypothese)
                /// // ALTERNATIEF: een soort rmse per estimate, dus per estimate de patient schatting en de carb hyp. vergelijken
                for (int i = 0; i < noisySchedule.GetEventCount(); i++)
                {
                    PatientEvent evt = noisySchedule.GetEventFromIndex(i);
                    // carb > 0 omdat we anders de automatische "ik laat 0 staan omdat ik te lui ben om iets te loggen"-waardes
                    // de boel anders omlaag trekken.
                    if (evt.EventType == PatientEventType.CARBS && evt.Carb_TrueValue_in_gram > 0  && evt.TrueStartTime >= starttime && evt.TrueStartTime <= stoptime)
                    {
                        int timekey = GetTimeKey(evt.TrueStartTime);
                        if (timekey >= 0)
                        {
                            // er is iig een estimate voor. 
                            CarbHypDataGroup<SubPopulatie> group = this.carbHypothesisBeforeTrail[(uint)timekey];
                            foreach (SubPopulatie sub in subPopuluaties)
                            {
                                if (group.ContainsKey(sub))
                                {
                                    Tuple<uint, double> estimate = group.GetCarbEstimateForSub(sub);
                                    if (!carbsInSchedule.ContainsKey(sub))
                                    {
                                        carbsInSchedule[sub] = 0;
                                        carbEstimates[sub] = 0;
                                        lobsidedness_rmse[sub] = 0;
                                        lobsidedness_linear[sub] = 0;
                                        lobsidedness_aantal[sub] = 0;
                                    }
                                    carbsInSchedule[sub] += evt.Carb_TrueValue_in_gram;
                                    carbEstimates[sub] += estimate.Item2;
                                    double dif = evt.Carb_TrueValue_in_gram - estimate.Item2;

                                    lobsidedness_rmse[sub] += Math.Sign(dif) * Math.Pow(Math.Abs(dif), MyParticleFilter.settingsForParticleFilter.PowForLobsidednessPerCarbHyp);
                                    lobsidedness_linear[sub] += dif;
                                    lobsidedness_aantal[sub]++;
                                }
                            }
                        }
                    }
                }

                double maxweight = 0;
                Dictionary<SubPopulatie, double> wegingsFactoren = new Dictionary<SubPopulatie, double>();

                // idee: alle carbhyps opgeteld zitten te ver van de vip_estimates af.
                // update alle Carbhyps zodat ze gemiddeld weer dezelfde waarde opleveren als de som van alle vip_estimates
                // Daarvoor is som v/d diffs /aantal nodig als update voor alle carbhyps (alleen 'in view'? of allemaal????)
                Dictionary<SubPopulatie, double> offsetInCarbHypOmNaar0TeGaan = new Dictionary<SubPopulatie, double>();
                int aantal = 0;

                foreach (SubPopulatie sub in subPopuluaties)
                {
                    if (lobsidedness_rmse.ContainsKey(sub))
                    {
                        aantal++;
                        double lobsidedness = lobsidedness_rmse[sub] / lobsidedness_aantal[sub];
                        double lobsidednessFactor = Math.Pow(1 / (1 + Math.Abs(lobsidedness)), MyParticleFilter.settingsForParticleFilter.PowForLobsidedness);
                        wegingsFactoren[sub] = lobsidednessFactor;
                        lobsidednessSignaal += lobsidedness;

                        if (lobsidednessFactor > maxweight)
                        {
                            maxweight = lobsidednessFactor;
                        }
                        if (double.IsNaN(lobsidednessFactor))
                        {
                            throw new ArgumentException("factor is nan!");
                        }
                        double alleCarbHypUpdate = lobsidedness_linear[sub] / lobsidedness_aantal[sub];
                        foreach (uint time in carbHypothesisBeforeTrail.Keys) 
                        {
                            if (time >= starttime && time <= stoptime)
                            {
                                CarbHypDataGroup<SubPopulatie> group = this.carbHypothesisBeforeTrail[time];
                                group.OffsetCarbs(sub, alleCarbHypUpdate);
                            }
                        }
                    }
                    else
                    {
                        wegingsFactoren[sub] = 0;
                        output.Append("wegingsFactoren[" + sub.ID + "] = 0;\n");
                    }
                }
                lobsidednessSignaal /= aantal;


                Dictionary<SubPopulatie, double> ranking = wegingsFactoren;
                if (MyParticleFilter.settingsForParticleFilter.CarbHypRanking)
                {
                    ranking = MyMath.GetRanking(wegingsFactoren, true);
                }
                maxweight = maxweight / Math.Pow(MyParticleFilter.BestParticle.Weight + 1, 0.3);
                foreach (SubPopulatie sub in subPopuluaties)
                {
                    ranking[sub] = Math.Pow((ranking[sub] + 1) / (double)ranking.Count, MyParticleFilter.settingsForParticleFilter.SchalingPow) * maxweight; // +1 : anders krijgen we weging 0 als er maar eentje is!
                    output.Append("sub #" + sub.ID + ": " + OctaveStuff.MyFormat(wegingsFactoren[sub]) + " --> weging op rang: " + OctaveStuff.MyFormat(ranking[sub]) + "\n");
                }

                //if (!Globals.RunParallelSimulations)
                //{
                //    Console.WriteLine(output);
                //}
                 
                foreach (uint time in this.carbHypothesisBeforeTrail.Keys)
                {
                    this.carbHypothesisBeforeTrail[time].CalcAggregate(ranking);
                }

                Opschonen();

                // reference tijd van elke groep zodanig dat aggregate offset = 0
                SortedDictionary<uint, CarbHypDataGroup<SubPopulatie>>  new_carbHypothesisBeforeTrail = new SortedDictionary<uint, CarbHypDataGroup<SubPopulatie>>();
                foreach (uint timekey in this.carbHypothesisBeforeTrail.Keys)
                {
                    CarbHypDataGroup<SubPopulatie> group = this.carbHypothesisBeforeTrail[(uint)timekey];
                    group.MoveToOffsetZero();
                    new_carbHypothesisBeforeTrail[group.GetReferenceTime()] = group;
                }
                carbHypothesisBeforeTrail = new_carbHypothesisBeforeTrail;

                return lobsidednessSignaal;
            }
        }




        public void Opschonen()
        {
            // boel opschonen: als 2 hyps te dicht bij elkaar zitten, samenvoegen. 
            // ook als sommige subs ze NIET dichtbij hebben, maar de meeste wel.(?)
            // Hyps dicht bij elkaar leveren problemen op, omdat ze dan tijdens local search interfereren
            HashSet<Tuple<uint, uint>> pairs = new HashSet<Tuple<uint, uint>>();
            HashSet<uint> removeList = new HashSet<uint>();
            foreach (uint time1 in this.carbHypothesisBeforeTrail.Keys)
            {
                if(removeList.Contains(time1)) { continue; }
                CarbHypDataGroup<SubPopulatie> group1 = this.carbHypothesisBeforeTrail[time1];

                uint time_offset_1 = group1.GetTimePlusAggregateOffset();
                bool safe1 = group1.BasedOnPatientEstimate; // schatting v/d patient nooit kandidaat voor opheffing!

                foreach (uint time2 in this.carbHypothesisBeforeTrail.Keys)
                {
                    if (removeList.Contains(time2)) { continue; }

                    if (time1 == time2) { continue; } //zelfde hyp.
                    if(pairs.Contains(new Tuple<uint, uint>(time1, time2)) || pairs.Contains(new Tuple<uint, uint>(time2,time1)))
                    {
                        continue;
                    }
                    CarbHypDataGroup<SubPopulatie> group2 = this.carbHypothesisBeforeTrail[time2];

                    uint time_offset_2 = group2.GetTimePlusAggregateOffset();
                    if (Math.Abs(time_offset_2 - time_offset_1) < MyParticleFilter.settingsForParticleFilter.VerwijderCarbHypsDichterBijDanMarge)
                    {
                        //opheffen!
                        bool safe2 = group2.BasedOnPatientEstimate;
                        // als de geschatte tijden hetzelfde zijn, dan MOET er eentje weg, ook  al zijn ze beide 
                        // een patient estimate/safe. Want anders komen de carbhyps op dezelfde tijd en dat mag niet!
                        if (safe1 && safe2) {
                            if (Math.Abs(time_offset_1 - time_offset_2) > 1)
                            {
                                continue;
                            }
                            else
                            {
                                //botsing
                               // continue;
                            }
                        }

                        if(Math.Abs(time_offset_1 - time_offset_2) <= 1)
                        {

                        }
                        double estimate_1 = group1.GetCarbAggregate();
                        double estimate_2 = group2.GetCarbAggregate();
                        bool keepNr1 = true;
                        if (safe1 && !safe2)
                        {
                            keepNr1 = true;
                        }
                        else if (safe2 && !safe1)
                        {
                            keepNr1 = false;
                        }
                        else // dit zou ook safe1&&safe2 kunnen zijn: dan kiezen // if(!safe1 && !safe2)
                        {
                            if (estimate_1 > estimate_2)
                            {
                                keepNr1 = true;
                            }
                            else
                            {
                                keepNr1 = false;
                            }
                        }

                        if (!safe1 || !safe2)
                        {
                            if (keepNr1)
                            {
                                //2e elem. wordt verwijderd: time2
                                pairs.Add(new Tuple<uint, uint>(time1, time2));
                                removeList.Add(time2);
                            }
                            else
                            {
                                pairs.Add(new Tuple<uint, uint>(time2, time1));
                                removeList.Add(time1);
                            }
                        }
                    }
                }
            }
            //verwijderen!:
            foreach (Tuple<uint, uint> pair in pairs)
            {
                uint tijd2 = pair.Item2;
                RemoveGroup(tijd2);
                verboden_tijden.Add(tijd2);
            }
        }


        private HashSet<uint> verboden_tijden = new HashSet<uint>();
    }
}
