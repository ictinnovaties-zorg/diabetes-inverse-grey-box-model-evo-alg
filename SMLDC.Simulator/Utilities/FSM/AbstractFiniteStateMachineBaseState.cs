using System;

namespace SMLDC.Simulator.Utilities.FSM
{
    public abstract class AbstractFiniteStateMachineBaseState
    {
        public virtual AbstractFiniteStateMachine FSM { get; set; }
        public string Name { get; set; }
        public Random RandomGenerator { get; set; }

        public abstract void ExecuteWhenInState();
        public abstract void ExecuteWhenEntering();
        public abstract void ExecuteWhenExiting();

        public AbstractFiniteStateMachineBaseState()
        {
            RandomGenerator = new Random();
        }
    }
}
