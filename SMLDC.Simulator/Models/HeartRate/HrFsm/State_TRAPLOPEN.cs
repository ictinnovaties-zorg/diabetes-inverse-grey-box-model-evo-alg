using SMLDC.Simulator.Models.HeartRate.FSM;
using SMLDC.Simulator.Utilities;
using SMLDC.Simulator.Utilities.FSM;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMLDC.Simulator.Models.HeartRate.HrFsm
{
    class State_TRAPLOPEN : HrActivityBaseState
    {
        public State_TRAPLOPEN()
        {
            Name = "traplopen";
        }


        public override void ExecuteWhenEntering()
        {
            hr_factor = myFSM.hrFsmSettings.factorTraplopen; // hier omdat we in constructor geen toegang tot de fsm hebben
        }

        public override void ExecuteWhenInState()
        {
            base.ExecuteWhenInState();

            //check voor evt toestand
            if (randomStuff.NextBool(0.9) || teller > 3 /*een hele flat omhoog!*/)
            {
                FSM.CurrentState = new State_STAAN();
            }
        }
    }
}

