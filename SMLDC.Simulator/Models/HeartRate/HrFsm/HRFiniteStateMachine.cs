using System;
using System.Collections.Generic;
using System.Threading;
using SMLDC.Simulator.Models.HeartRate.HrFsm;
using SMLDC.Simulator.Schedules;
using SMLDC.Simulator.Schedules.Events;
using SMLDC.Simulator.Utilities;
using SMLDC.Simulator.Utilities.FSM;


namespace SMLDC.Simulator.Models.HeartRate.FSM
{
    public class HRFiniteStateMachine : AbstractFiniteStateMachine
    {

        public bool Verbose { get { return verbose; } }
        private bool verbose = false;

        public Schedule schedule;
        public uint tijd = 0;
        public int GetBaseHeartRate() { return baseHeartRate; }
        private int baseHeartRate;

        public HrFsmSettings hrFsmSettings;
        public HrActivityBaseState CurrentHrState
        {
            get { return (HrActivityBaseState)CurrentState; }
        }
        public RandomStuff random;
        public HRFiniteStateMachine(RandomStuff random, Schedule _schedule, HrFsmSettings settings, int baseHr)
        {
            this.random = random;
            this.baseHeartRate = baseHr;
            this.schedule = _schedule;
            this.hrFsmSettings = settings;
            // beginnen in rust, omdat 1e minuut van 1e dag middernacht is.
            VerdeelSportMomentenOverDeDag();
            this.CurrentState = new State_SLAPEN();
        }


        public int TijdsduurTotVolgendEten()
        {
            PatientEvent carbEvt = schedule.GetFirstCarbEventAfterTime(tijd);
            PatientEvent insEvt = schedule.GetFirstInsulinEventAfterTime(tijd);
            if (carbEvt == null && insEvt == null)
            {
                return -1;
            }
            else if (carbEvt == null) //gek: schedule eindigt niet met eten maar met spuiten? correctie vlak voor slapen?
            {
                return (int)insEvt.TrueStartTime - (int)tijd;
            }
            else if (insEvt == null)
            {
                return (int)carbEvt.TrueStartTime - (int)tijd;
            }
            else
            {
                return (int) Math.Min(carbEvt.TrueStartTime, insEvt.TrueStartTime) - (int)tijd;
            }
        }

        public bool KanGaanEten()
        {
            int duur = TijdsduurTotVolgendEten();
            return (duur >= 0 && duur < 30);
        }

        public bool WensOmTeGaanZitten()
        {
            return KanGaanEten() || KanGaanSlapen();
        }

        public bool IsEtenstijd()
        {
            // true als 15 min. voor tot aantal min. na eten (hangt van grootte v/h eten af)
            // ook rekening houden met insuline moment?
            if(  TijdsduurTotVolgendEten() < 1) /*eten begint nu, evt eerst met spuiten*/
            {
                return true;
            }
            PatientEvent carbEvt = schedule.GetLastCarbEventBeforeTime(tijd);
            if (carbEvt != null) {
                // een etensduur ruwweg gerelateerd aan wat er echt gegeten wordt. 1 carb  ~ 1 minuut.
                double rndEtensduur = 0.5 * carbEvt.Carb_TrueValue_in_gram + 0.35 * carbEvt.Carb_TrueValue_in_gram * random.GetNormalDistributed(0, 1);
                if ((int)tijd - (int)carbEvt.TrueStartTime < rndEtensduur)/* zijn nog bezig met eten*/ {
                    return true;
                }
            }

            return false;
        }

        public bool KanGaanSlapen()
        {
            //return true als er een lange periode van niet-eten volgt, bv. de nacht maar ook een siesta
            int tijd_tot_eten = TijdsduurTotVolgendEten();
            uint modTime = GetMinutenInDag();
            return (tijd_tot_eten >= 60 * 3  &&  (modTime >= 21 * 60 || modTime <= 6 * 60));
        }


        public bool MoetGaanSlapen()
        {
            uint modTime = GetMinutenInDag();
            return KanGaanEten() && modTime >= 1 * 60 && modTime <= 5 * 60;
        }
        public bool IsTijdOmWakkerTeWorden()
        {
            // tijd voor eten, en nog iets meer (opstaan enz.)
            // van slapend naar zittend/lopend gaan.
            int tijd_tot_eten = TijdsduurTotVolgendEten();
            return (tijd_tot_eten >= 0 && tijd_tot_eten < 60) || GetMinutenInDag() > 60 * 11;
        }




        public int activiteit_teller = 0;
        private uint laatsteSportMomentStart;
        private uint laatsteSportMomentEind;

        private bool[] magGaanSporten_perUur;
        private void VerdeelSportMomentenOverDeDag()
        {
            magGaanSporten_perUur = new bool[24];
            for(int keer = 0; keer < hrFsmSettings.maxAantalKeerSportPerDag; keer++)
            {
                int rnd_ndx = random.NextInt(9, 22);
                magGaanSporten_perUur[rnd_ndx] = true;
                magGaanSporten_perUur[rnd_ndx + 1] = true;
            }
        }
        public bool MagGaanSporten()
        {
            int uur_ndx = (int) GetUurInDag();
            return magGaanSporten_perUur[uur_ndx] && activiteit_teller < hrFsmSettings.maxAantalKeerSportPerDag  &&  (tijd - laatsteSportMomentEind) > hrFsmSettings.minTijdTussenSporten; //param van maken
        }
        public void GaatSporten() // registeren fietsen of hardlopen (nb. extreem is alleen vanuit hardlopen bereikbaar, dus die hoeft niet te checken/registeren)
        {
            activiteit_teller++;
            laatsteSportMomentStart = tijd;
        }

        public void KlaarMetSporten()
        {
            laatsteSportMomentEind = tijd;
            if((int)laatsteSportMomentEind - (int)laatsteSportMomentStart <= 5) // te kort om  sporten genoemd te worden, sprintje naar de bus oid.
            {
                activiteit_teller--;
            }
        }


        private uint GetUurInDag()
        {
            uint min = GetMinutenInDag();
            return min / 60;
        }
        private uint GetDag()
        {
            return (uint) (tijd / (60 * 24));
        }
        private uint GetMinutenInDag()
        {
            return tijd - GetDag() * 24 * 60;
        }

        private uint GetMinutenInUur()
        {
            return tijd - GetDag() * 24 * 60 - GetUurInDag() * 60;
        }

        //return heartrate
        public List<int> Run(uint totalCalculationMinutes, bool verbose=false)
        {
            this.verbose = verbose;
            List<int> heartrates = new List<int>((int)totalCalculationMinutes);
            int dag_tijd = 0; // voor limiet op sport-activiteiten PER DAG.  Deze dag_tijd loopt van minuut 0 tot aan het einde v/d dag en reset dan naar 0

            while (tijd <= totalCalculationMinutes)
            {
                int uur_in_dag = dag_tijd / 60;
                int minuten = dag_tijd - uur_in_dag * 60;
                if (verbose) { Console.Write(GetDag() +  " - " + GetUurInDag() + ":" + GetMinutenInUur() + " --> "); }
                bool res = this.RunStep();
                if (!res)
                {
                    if (verbose) { Console.WriteLine("finished"); }
                    break;
                }
                // klaar met deze tijdstap

                // vraag hr op van state:
                int hr = CurrentHrState.GetHeartRate();
                if(hr == 0)
                {

                }
                heartrates.Add(hr);

                tijd++;
                dag_tijd++;
                if (dag_tijd >= 24 * 60)
                {
                    VerdeelSportMomentenOverDeDag();
                    activiteit_teller = 0;
                    dag_tijd = 0; 
                }

                if(verbose) { 
                    Thread.Sleep(10); // voor testen via console
                }
            }
            return heartrates;
        }


    }
}
