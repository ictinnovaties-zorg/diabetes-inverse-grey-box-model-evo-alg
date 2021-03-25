using System;
using System.Collections.Generic;
using System.Text;

using SMLDC.Simulator.Helpers;
using SMLDC.Simulator.Models.HeartRate.FSM;
using SMLDC.Simulator.Models.HeartRate.HrFsm;
using SMLDC.Simulator.Schedules.Events;
using SMLDC.Simulator.Utilities;

namespace SMLDC.Simulator.Models.HeartRate
{
    // Use the event schedule and a FSM to simulate heart rates for the VIP.
    public class FSMHeartRateGenerator: AbstractHeartRateGenerator
    {
        private VirtualPatient patient;
        private HrFsmSettings hrFsmSettings;

        public FSMHeartRateGenerator(VirtualPatient patient, HrFsmSettings hrFsmSettings)
        {
            this.hrFsmSettings = hrFsmSettings;
            this.patient = patient;
        }


        public void UpdateHeartrateScheme(List<int> newHr)
        {
            throw new NotFiniteNumberException();
        }

        public override void GenerateScheme(uint totalCalculationMinutes)
        {
            if(patient.RealData)
            {
                throw new ArgumentException("Real patient data... kan geen FSM voor HR gen. hebben");
            }
            HRFiniteStateMachine fsm = new HRFiniteStateMachine(patient.Random, patient.TrueSchedule, hrFsmSettings, (int) Math.Round(patient.Model.BaseHeartRate));
            List<int> hrscheme = fsm.Run(totalCalculationMinutes + 60 * 24 /*just in case!*/);
            heartRateScheme = hrscheme.ToArray();
        }


        // offset geeft mogelijheid om deze zelfde hr range te gebruiken vanaf bepaald moment
        public override AbstractHeartRateGenerator Copy(uint offset = 0)
        {
            FSMHeartRateGenerator rhr = new FSMHeartRateGenerator(this.patient, this.hrFsmSettings);
            rhr.heartRateScheme = this.heartRateScheme;
            rhr.offset = offset;
            return rhr;
        }

    }
}
