using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using SMLDC.Simulator.DiffEquations.Solvers;
using SMLDC.Simulator.Schedules.Events;
using SMLDC.Simulator.Utilities;


namespace SMLDC.Simulator
{
    public struct SimulatorConfig
    {
        public readonly uint durationInDays;
        public uint durationInMinutes { get { return durationInDays * 60 * 24; } }

        public string configpath;
        public SimulatorConfig(string configpath, uint durationInDays)
        {
            this.configpath = configpath;
            this.durationInDays = durationInDays;
        }
    }

    public class GlucoseInsulinSimulator
    {
        public SimulatorConfig simulatorConfig;
        public string ConfigIniPath {get { return simulatorConfig.configpath; } }

        // A side effect of the exporter is that it also is used to determine whether to delete patients or keep them
        // if the exporter is of any type other than NullExporter the patients will be set to null to free RAM
        // something can be build to export patient data to disk when RAM is getting full


        public VirtualPatient patient;
        private bool _hasRun = false;

        public RandomStuff randomForSchedule;
        public MidpointSolver MyMidpointSolver;
        public GlucoseInsulinSimulator(RandomStuff rnd, SimulatorConfig simulatorConfig, VirtualPatient patient) // List<VirtualPatient> patients) //, IExporter exporter)
        {
            randomForSchedule = rnd;
            MyMidpointSolver = new MidpointSolver();
            this.simulatorConfig = simulatorConfig;
            //this._patients = patients;
            //foreach(VirtualPatient patient in _patients)
            //{
            //    patient.simulator = this;
            //}
            this.patient = patient;
            this.patient.simulator = this;
        }

        public void Run()
        {
            if (_hasRun)
            {
                Log.Information("Simulator has already run");
            }
            //_progressBar ??= new ProgressBar();
            //using (_progressBar)
            //{
                SingleThreadRun();
           //}
            _hasRun = true;
        }



        private void SingleThreadRun() //ProgressBar progressBar)
        {
            //for (int patientCount = _patients.Count; patientCount > 0; patientCount--)
            //{
            //    VirtualPatient patient = _patients[patientCount - 1];
            //    RunOnePatient(randomForSchedule, patient, 0 /*start*/, -1 /*stop*/ , 1 /*fixed interval*/);
            //}
            RunOnePatient(randomForSchedule, patient, 0 /*start*/, -1 /*stop*/ , 1 /*fixed interval*/);
        }




        // voert de hele simulatie uit.
        // als fixedInterval < 0, dan niet fixed in MidpointSolver
        public void RunOnePatient(RandomStuff random, VirtualPatient patient, uint starttime, int stoptime, int fixedInterval)
        {
            if (patient.RealData) // niks te doen hier! Alle (noisy) data staat al in de schedule
            {
                return;
            }

            if(stoptime < 0)
            {
                stoptime = (int) patient.TrueSchedule.GetLastTime();
            }

            bool ignoreMmnts = !patient.LockMLtoNoisyMmnts;
            uint previousTime = starttime;
            bool laatste_keer_zonder_event = true;
            int nrMidpointSolverSteps = 0;
            while (!patient.TrueSchedule.IsEmpty(ignoreMmnts) || laatste_keer_zonder_event)
            {
                // Check if all events are used.
                PatientEvent patientEvent = null;
                uint eventTime = 0;
                if (patient.TrueSchedule.IsEmpty(ignoreMmnts))
                {
                    eventTime = (uint) stoptime;   // zorgen dat we het stuk van laatste event tot echte stoptime ook nog runnen.
                                                    // ondanks dat er geen event meer is.
                    laatste_keer_zonder_event = false;
                }
                else
                {
                    patientEvent = patient.TrueSchedule.PopEvent(ignoreMmnts);
                    eventTime = patientEvent.TrueStartTime;
                }


                if (eventTime < starttime)
                {
                    continue;
                }
                if(eventTime > stoptime) // anders runnen we tot eerstvolgende event, en dat is te ver!
                {
                    eventTime = (uint) stoptime;
                }

                if (!patient.RealData) // run solver
                {
                    // If the last event happened on the same timestamp, the solver does not need to run.
                    if (eventTime != previousTime)
                    {
                        // Run the solver from the current time to the next (i.e. just popped 'start time') event.
                        // and add it to the existing generated data.

                        // solver gebruikt HRgenerator van patient voor HR data
                        SolverResultBase result = MyMidpointSolver.Solve(patient, fixedInterval, previousTime, eventTime);
                        nrMidpointSolverSteps += result.Count;
                        patient.AddSolverResult(result);
                        double[] ld = patient.GetLastData();
                        if(ld[0] != eventTime)
                        {

                        }
                        previousTime = eventTime;
                    }
                    else
                    {
                        //niks spannends. Zelfde tijd, 2 events. 
                    }

                    if (patientEvent != null)
                    {
                        //het laatste stuk vanaf laatste event tot stoptijd is eventloos (tenzij de stoptijd samenvalt met laatste event, uiteraard)

                        ////debug meuk:
//                        if (patientEvent.StartTime > 3500 && patient.PatientType != Enums.PatientTypeEnum.BINARY_SEARCH && patientEvent.EventType != Enums.PatientEventType.GLUCOSE_MASUREMENT)
//                        {
//                            int sdfd = 4;
////                          Console.WriteLine(patient.schedule.ToString(-10, 10));
//                        }
                        //if (patientEvent.StartTime < 3765 || patientEvent.EventType == Enums.PatientEventType.GLUCOSE_MASUREMENT || patientEvent.StartTime ==3766 )
                        //{
                        //    patient.HandleEvent(patientEvent);
                        //}
                        //else
                        //{
                        //    patientEvent.EventType = Enums.PatientEventType.CARBS;
                        //    patientEvent.NoisyValue = 0;
                        //    patientEvent.TrueValue = 0;
                        //}

                        patient.HandleEvent(random, patientEvent);
                    }
                }
                //else
                //{
                //    throw new NotImplementedException("REAL DATA!");
                //}

                if (eventTime >= stoptime)
                {
                    // liever hier in 1x updaten. Dit gebreurt potentieel v/u. meerdere (particle) threads, en er is een lock(...) in updatemethode:
                    MyMidpointSolver.UpdateNrOfSolverSteps(nrMidpointSolverSteps);
                    return; // stop!
                }
            }
        }






    }
}