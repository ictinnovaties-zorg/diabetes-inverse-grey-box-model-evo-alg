using SMLDC.Simulator.Models.HeartRate.FSM;
using SMLDC.Simulator.Utilities;
using SMLDC.Simulator.Utilities.FSM;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMLDC.Simulator.Models.HeartRate.HrFsm
{
    class State_FIETSEN : HrActivityBaseState
    {
        public State_FIETSEN()
        { 
            Name = "fietsen";
        }

        public override void ExecuteWhenEntering()
        {
            myFSM.GaatSporten();
            hr_factor = myFSM.hrFsmSettings.factorFietsen; // hier omdat we in constructor geen toegang tot de fsm hebben
            Name = "fietsen (activiteit #" + myFSM.activiteit_teller + ")";
        }

        public override void ExecuteWhenExiting()
        {
            myFSM.KlaarMetSporten();
        }


        public override void ExecuteWhenInState()
        {
            base.ExecuteWhenInState();

            //check voor evt toestand
            if (myFSM.WensOmTeGaanZitten())
            {
                if (randomStuff.NextBool(0.5))
                {
                    FSM.CurrentState = new State_LOPEN();
                }
            }
            else if(teller > 0.3 * myFSM.hrFsmSettings.maxDuurFietsen && randomStuff.NextBool( -0.3 + Math.Pow(teller/(double)myFSM.hrFsmSettings.maxDuurFietsen, 2)))
            {
                FSM.CurrentState = new State_LOPEN();
            }
        }
    }
}

