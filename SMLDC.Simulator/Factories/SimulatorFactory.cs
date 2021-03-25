using System;
using System.Collections.Generic;

using SMLDC.Simulator.DiffEquations.Models;
using SMLDC.Simulator.Models.HeartRate.HrFsm;
using SMLDC.Simulator.Schedules;
using SMLDC.Simulator.Utilities;


namespace SMLDC.Simulator.Factories
{
    public class SimulatorFactory
    {

        public RandomStuff randomForPatientSettings;
        public RandomStuff randomForScheduleSettings;

        public Schedule ReadGlucoseCarbInsulinData { get; private set; }

        public SimulatorFactory(int simNr, RandomStuff randForPatientSettings, RandomStuff randForScheduleSettings)
        {
            this.randomForPatientSettings = randForPatientSettings;
            this.randomForScheduleSettings = randForScheduleSettings;
        }



        public Schedule CreateSchedule(uint simulationDays, PatientSettings patientSettings, ScheduleParameters scheduleParam, HrFsmSettings hrFsmSettings)
        {
            if (hrFsmSettings.heartRateGeneratorOption == "real")
            {
                //throw new NotImplementedException();
                return RealPatientDataReader.ReadGlucoseCarbInsulinData(hrFsmSettings);
            }
            else
            {
                return ScheduleGenerator.GenerateSchedule(this.randomForScheduleSettings, simulationDays, patientSettings, scheduleParam);
            }
        }

        public VirtualPatient CreatePatient(int id, HrFsmSettings hrFsmSettings, BergmanAndBretonModel model, Schedule schedule, PatientSettings settings)
        {
            if (hrFsmSettings.heartRateGeneratorOption == "real")
            {
                return new VirtualPatient(id, schedule);
            }
            else
            {
                return new VirtualPatient( id, model, settings, schedule);
            }
        }


        public GlucoseInsulinSimulator CreateSimulator(SimulatorConfig simulatorConfig, VirtualPatient patient)
        {
            return new GlucoseInsulinSimulator(randomForScheduleSettings, simulatorConfig, patient);
        }
    }
}