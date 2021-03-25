using SMLDC.CLI.Helpers;
using SMLDC.Simulator;
using SMLDC.Simulator.DiffEquations.Models;
using SMLDC.Simulator.Factories;
using SMLDC.Simulator.Models.HeartRate.FSM;
using SMLDC.Simulator.Models.HeartRate.HrFsm;
using SMLDC.Simulator.Schedules;
using SMLDC.Simulator.Utilities;
using SMLDC.Simulator.Utilities.FSM;
using System;

namespace FsmHrSimulatorTests
{
    class Program
    {
        static void Main(string[] args)
        {
            //RandomStuff.Init(1);
            //ConfigurationParser configurationParser = new ConfigurationParser(@"C:\repo\nieuwe sim\smldc-qsd\SMLDC.CLI\config.ini");
            //uint durationInDays = configurationParser.GetSimulatorValueFromConfig<uint>("durationInDays");
           
            //ScheduleParameters scheduleparam = configurationParser.GetScheduleParameters();
            //PatientSettings patientSettings = configurationParser.GetPatientSettings();

            //Schedule schedule = ScheduleGenerator.GenerateSchedule(durationInDays, patientSettings, scheduleparam);
            //BergmanAndBretonModel bergmanModel = configurationParser.GetModel();
            //int baseHr = 60;
            //HrFsmSettings hrFsmSettings = configurationParser.GetHeartrateFsmSettings();
            //HRFiniteStateMachine fsm = new HRFiniteStateMachine(schedule, hrFsmSettings, baseHr);
            //fsm.Run(60*24*10, true);
        }
    }
}
