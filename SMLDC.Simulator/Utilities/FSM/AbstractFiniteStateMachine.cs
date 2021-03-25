using System;
using System.Threading;

namespace SMLDC.Simulator.Utilities.FSM
{
    public abstract class AbstractFiniteStateMachine
    {
        private AbstractFiniteStateMachineBaseState _currentState;
        public virtual AbstractFiniteStateMachineBaseState CurrentState
        {
            get { return _currentState; }
            set
            {
                if (_currentState != null) // check of dit niet de eerste keer is
                {
                    _currentState.ExecuteWhenExiting(); // oude state afhandelen
                }
                _currentState = value;
                _currentState.FSM = this; // laat de FSM zichzelf registreren
                _currentState.ExecuteWhenEntering(); // nieuwe state uitvoeren
            }
        }


        public bool RunStep()
        {
            if (CurrentState != null)
            {
                CurrentState.ExecuteWhenInState();
                return true;
            }
            return false; //klaar
        }
    }


}
