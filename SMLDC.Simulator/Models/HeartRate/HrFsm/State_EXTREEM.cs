using SMLDC.Simulator.Models.HeartRate.FSM;
using SMLDC.Simulator.Utilities;
using SMLDC.Simulator.Utilities.FSM;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMLDC.Simulator.Models.HeartRate.HrFsm
{
    class State_EXTREEM : HrActivityBaseState
    {
        uint extreemTeller;
        public State_EXTREEM(uint ext)
        {
            extreemTeller = ext + 1;
            Name = "extreem";
        }




        public override void ExecuteWhenEntering()
        {
            hr_factor = myFSM.hrFsmSettings.factorExtreem; // hier omdat we in constructor geen toegang tot de fsm hebben
            Name = "extreem (activiteit #" + myFSM.activiteit_teller + ")";
        }


        public override void ExecuteWhenInState()
        {
            base.ExecuteWhenInState();

            //check voor evt toestand
            if (myFSM.WensOmTeGaanZitten())
            {
                if (randomStuff.NextBool(0.3))
                {
                    FSM.CurrentState = new State_HARDLOPEN(extreemTeller + myFSM.hrFsmSettings.maxAantalKeerExtreemPerHardlopen);
                }
            }
            else if(randomStuff.NextBool( Math.Sqrt(teller/ (double)myFSM.hrFsmSettings.maxDuurExtreemSport) ) )
            {
                FSM.CurrentState = new State_HARDLOPEN(extreemTeller);
            }
        }
    }
}
