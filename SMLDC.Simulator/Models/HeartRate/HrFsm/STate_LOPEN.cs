using SMLDC.Simulator.Models.HeartRate.FSM;
using SMLDC.Simulator.Utilities;
using SMLDC.Simulator.Utilities.FSM;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMLDC.Simulator.Models.HeartRate.HrFsm
{
    class State_LOPEN : HrActivityBaseState
    {
        public State_LOPEN()
        {
            Name = "lopen";
        }

        public override void ExecuteWhenEntering()
        {
            hr_factor = myFSM.hrFsmSettings.factorLopen; // hier omdat we in constructor geen toegang tot de fsm hebben

        }


        public override void ExecuteWhenInState()
        {
            base.ExecuteWhenInState();

            //check voor evt toestand
            if (myFSM.WensOmTeGaanZitten())
            {
                if (randomStuff.NextBool(0.3))
                {
                    FSM.CurrentState = new State_ZITTEN();
                }
            }
            else if (randomStuff.NextBool(myFSM.hrFsmSettings.kasnOpZitten))
            {
                FSM.CurrentState = new State_STAAN();
            }
            else if (randomStuff.NextBool(myFSM.hrFsmSettings.kasnOpZitten))
            {
                FSM.CurrentState = new State_ZITTEN();
            }
            else if(randomStuff.NextBool(myFSM.hrFsmSettings.kansOpSporten) && myFSM.MagGaanSporten())
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
