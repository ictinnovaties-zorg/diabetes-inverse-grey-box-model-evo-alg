using SMLDC.Simulator.Models.HeartRate.HrFsm;
using System;


namespace SMLDC.Simulator.Models.HeartRate.FSM
{


    public class State_SLAPEN : HrActivityBaseState
    {
        public State_SLAPEN()
        {
            Name = "zz..";
        }


        public override void ExecuteWhenEntering()
        {
            hr_factor = myFSM.hrFsmSettings.factorSlapen; // hier omdat we in constructor geen toegang tot de fsm hebben
        }

        public override void ExecuteWhenInState()
        {
            base.ExecuteWhenInState();

            //check voor evt toestand
            if (myFSM.IsTijdOmWakkerTeWorden())
            {
                if (randomStuff.NextBool(0.1))
                {
                    // een kans om wakker te worden
                    FSM.CurrentState = new State_ZITTEN();
                }
            }
            else if(myFSM.IsEtenstijd())
            {
                // paniek: direct uit bed naar je eten toe! te lang blijven liggen
                FSM.CurrentState = new State_ETEN();
            }
            else {
                // ff paar minuten wakker worden en dan weer gaan slapen... kans wordt groter naarmate de uren (diepe) slaap verstrijken
                if (teller > 0.3 * myFSM.hrFsmSettings.maxTijdTotWakker)
                {
                    if (randomStuff.NextBool(Math.Pow(teller / myFSM.hrFsmSettings.maxTijdTotWakker,2)))
                    {
                        FSM.CurrentState = new State_ZITTEN(!myFSM.IsTijdOmWakkerTeWorden());
                    }
                    else if (randomStuff.NextBool(Math.Pow(teller / myFSM.hrFsmSettings.maxTijdTotWakker, 2)))
                    {
                        FSM.CurrentState = new State_STAAN(!myFSM.IsTijdOmWakkerTeWorden()); // naar wc.
                    }
                }
            }
        }



    }
}