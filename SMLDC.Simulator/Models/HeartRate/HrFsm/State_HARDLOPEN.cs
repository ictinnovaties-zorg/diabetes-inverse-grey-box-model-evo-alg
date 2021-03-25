using SMLDC.Simulator.Models.HeartRate.FSM;
using SMLDC.Simulator.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMLDC.Simulator.Models.HeartRate.HrFsm
{
    class State_HARDLOPEN : HrActivityBaseState
    {


        uint extreemTeller = 0;

        public State_HARDLOPEN(uint extteller = 0)
        {
            extreemTeller = extteller;
            Name = "hardlopen";
        }


        public override void ExecuteWhenEntering()
        {
            hr_factor = myFSM.hrFsmSettings.factorHardlopen;
            if (extreemTeller == 0)
            {
                myFSM.GaatSporten();
            }
            else
            {
                // een !wilExtreem is een coolingdown NA extreem, en telt niet als nieuwe activiteit
                base.teller = 10;
            }
            Name = "hardlopen (activiteit #" + myFSM.activiteit_teller + ")";
        }

        public bool doeExtreem = false;

        public override void ExecuteWhenExiting()
        {
            hr_factor = myFSM.hrFsmSettings.factorHardlopen; // hier omdat we in constructor geen toegang tot de fsm hebben
            if (!doeExtreem)
            {
                myFSM.KlaarMetSporten();
            }
        }

        public override void ExecuteWhenInState()
        {
            base.ExecuteWhenInState();

            //check voor evt toestand
            if (myFSM.WensOmTeGaanZitten())
            {
                if (randomStuff.NextBool(0.3))
                {
                    FSM.CurrentState = new State_LOPEN();
                }
            }
            else
            {
                //

                if (teller > 5 && extreemTeller < myFSM.hrFsmSettings.maxAantalKeerExtreemPerHardlopen && randomStuff.NextBool(0.3))
                {
                    doeExtreem = true;
                    FSM.CurrentState = new State_EXTREEM(extreemTeller);
                }
            }


            if (teller > 0.25 * myFSM.hrFsmSettings.maxDuurHardlopen && randomStuff.NextBool(-0.3 + Math.Pow(teller / myFSM.hrFsmSettings.maxDuurHardlopen, 2)))
            {
                FSM.CurrentState = new State_LOPEN();
            }
        }
    }
}
