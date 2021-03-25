using SMLDC.Simulator.Models.HeartRate.HrFsm;
using SMLDC.Simulator.Utilities;
using SMLDC.Simulator.Utilities.FSM;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMLDC.Simulator.Models.HeartRate.FSM
{
    public class State_ZITTEN : HrActivityBaseState
    {
        private bool verderGaanMetSlapen = false;
        public State_ZITTEN(bool mgs = false)
        {
            verderGaanMetSlapen = mgs;
            Name = "zitten";
        }


        public override void ExecuteWhenEntering()
        {
            hr_factor = myFSM.hrFsmSettings.factorZitten; // hier omdat we in constructor geen toegang tot de fsm hebben
        }

        public override void ExecuteWhenInState()
        {
            base.ExecuteWhenInState();


            //check voor evt toestand
            if (myFSM.KanGaanSlapen())
            {
                if (randomStuff.NextBool(Math.Sqrt(teller / myFSM.hrFsmSettings.maxTijdVanZittenNaarSlapen)))
                {
                   // Console.WriteLine("---> kan gaan slapen, gaat slapen");
                    FSM.CurrentState = new State_SLAPEN();
                }
            }


            if (verderGaanMetSlapen || myFSM.MoetGaanSlapen())
            {
                if (randomStuff.NextBool(teller / (verderGaanMetSlapen ? 15 : myFSM.hrFsmSettings.maxTijdVanZittenNaarSlapenAlsJeMoetGaanSlapen)))
                {
                   // Console.WriteLine("---> MOET gaan slapen, gaat slapen (verderGaanMetSlapen = " + verderGaanMetSlapen + ")");
                    FSM.CurrentState = new State_SLAPEN();
                }
            }
            else
            {
                if (myFSM.KanGaanEten())
                {
                    if (randomStuff.NextBool(1 - myFSM.TijdsduurTotVolgendEten() / 10))
                    {
                        FSM.CurrentState = new State_ETEN();
                    }
                }
                else if (randomStuff.NextBool(0.02))
                {
                    FSM.CurrentState = new State_TRAPLOPEN();
                }
                else if (randomStuff.NextBool(myFSM.hrFsmSettings.kansOpLopen))
                {
                    FSM.CurrentState = new State_LOPEN();
                }

            }

        }

    }
}
