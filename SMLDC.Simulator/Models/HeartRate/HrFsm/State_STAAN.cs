using SMLDC.Simulator.Models.HeartRate.FSM;
using SMLDC.Simulator.Utilities;
using SMLDC.Simulator.Utilities.FSM;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMLDC.Simulator.Models.HeartRate.HrFsm
{
    class State_STAAN : HrActivityBaseState
    {
        private bool moetGaanSlapen;
        public State_STAAN(bool mgs = false) 
        {
            moetGaanSlapen = mgs;
            Name = "staan";
        }


        public override void ExecuteWhenEntering()
        {
            hr_factor = myFSM.hrFsmSettings.factorStaan; // hier omdat we in constructor geen toegang tot de fsm hebben
        }

        public override void ExecuteWhenInState()
        {
            base.ExecuteWhenInState();

            //check voor evt toestand
            
            if(moetGaanSlapen)
            {
                if (randomStuff.NextBool(base.teller / myFSM.hrFsmSettings.maxTijdVanStaanNaarSlapenAlsJeMoetGaanSlapen))
                {
                    FSM.CurrentState = new State_SLAPEN();
                }
            }
            else 
            {
                if (myFSM.WensOmTeGaanZitten())
                {
                    if (randomStuff.NextBool(0.25))
                    {
                        FSM.CurrentState = new State_ZITTEN(moetGaanSlapen);
                    }
                }
                else
                {
                    if (randomStuff.NextBool(myFSM.hrFsmSettings.kansOpLopen))
                    {
                        FSM.CurrentState = new State_LOPEN();
                    }
                    else if (randomStuff.NextBool(myFSM.hrFsmSettings.kasnOpZitten))
                    {
                        FSM.CurrentState = new State_ZITTEN();
                    }
                    else if (randomStuff.NextBool(myFSM.hrFsmSettings.kansOpSporten) && myFSM.MagGaanSporten())
                    {
                        if (randomStuff.NextBool(myFSM.hrFsmSettings.kansOpFietsenVsHardlopen))
                        {
                            FSM.CurrentState = new State_FIETSEN();
                        }
                        else
                        {
                            FSM.CurrentState = new State_HARDLOPEN();
                        }
                    }
                }
            }
        }
    }
}
