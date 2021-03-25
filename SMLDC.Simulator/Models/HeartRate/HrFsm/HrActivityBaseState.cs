using SMLDC.Simulator.Models.HeartRate.FSM;
using SMLDC.Simulator.Utilities;
using SMLDC.Simulator.Utilities.FSM;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMLDC.Simulator.Models.HeartRate.FSM
{
    public abstract class HrActivityBaseState : AbstractFiniteStateMachineBaseState
    {
        public RandomStuff randomStuff { get { return myFSM.random; } }
        public HrActivityBaseState()
        {
            teller = 0;
        }
        public HRFiniteStateMachine myFSM { get { return (HRFiniteStateMachine)FSM; } }

        protected double hr_factor; // vermenigvuldigen met base hr
        protected double teller = 0;

        public int GetHeartRate() {  return (int) Math.Round(hr_factor * myFSM.GetBaseHeartRate()); }


        // ExecuteWhenEntering fungeert als Init().  Override die, ipv in constructor toevoegen,
        // omdat je in constructor nog geen toegang tot myFSM hebt (en dus niet tot myFSM.settings...)
        public override void ExecuteWhenEntering()
        {
            if (myFSM.Verbose)  { Console.WriteLine("ENTERING >> " + Name); }
        }

        public override void ExecuteWhenExiting()
        {
            if (myFSM.Verbose) { Console.WriteLine(Name + " >> EXIT"); }
        }

        public override void ExecuteWhenInState()
        {
            teller++;
            if (myFSM.Verbose) { Console.WriteLine(Name); }
        }
    }

}

