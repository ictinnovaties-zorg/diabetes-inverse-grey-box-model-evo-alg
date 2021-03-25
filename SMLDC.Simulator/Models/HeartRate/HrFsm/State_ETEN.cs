using SMLDC.Simulator.Models.HeartRate.FSM;
using SMLDC.Simulator.Utilities;
using SMLDC.Simulator.Utilities.FSM;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMLDC.Simulator.Models.HeartRate.HrFsm
{
    public class State_ETEN : HrActivityBaseState
    {
        public State_ETEN()
        {
            // kan hier nog niet naar myFSM referen, die wordt automatisch toegewezen NA deze constructor ... zucht
            Name = "==== eten =====";
        }

        private bool aanheteten = false; // als we ruim voor insuline/carb moment 'gaan eten' dan zorgt dit ervoor dat we niet al klaar zijn voordat de echte carbs komen

        public override void ExecuteWhenEntering()
        {
           // base.ExecuteWhenEntering();
            hr_factor = myFSM.hrFsmSettings.factorEten; // hier omdat we in constructor geen toegang tot de fsm hebben
            aanheteten = false;
        }

        public override void ExecuteWhenInState()
        {
            base.ExecuteWhenInState();

            if (!myFSM.IsEtenstijd() && aanheteten)
            {                
                if (randomStuff.NextBool(0.2))
                {
                    FSM.CurrentState = new State_ZITTEN();
                }
                if (randomStuff.NextBool(0.2))
                {
                    FSM.CurrentState = new State_STAAN();
                }
            }
            else if(!aanheteten && myFSM.IsEtenstijd())
            {
                aanheteten = true;
            }

        }
    }

}
