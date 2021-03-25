using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using SMLDC.CLI.Helpers;
using SMLDC.MachineLearning;
using SMLDC.Simulator;
using SMLDC.Simulator.Factories;
using SMLDC.Simulator.DiffEquations.Models;
using SMLDC.Simulator.Schedules;
using SMLDC.Simulator.Models.HeartRate.HrFsm;
using SMLDC.Simulator.Utilities;
using System.Threading.Tasks;
using System.Diagnostics;
using SMLDC.Simulator.Models.HeartRate;

namespace SMLDC.CLI.Commands
{
    public class CreateParticleFilterCommand : ICommand
    {
        public string[] Arguments { get; set; } = { "path", "simNr", "datestring" };
        public string Description { get; set; } = "Creates an instance of the particle filter. " +
                                                  "\nYou need to fill in the path of the configuration file.";
        public void Run()
        {
            RunWithArguments(new string[] { "config.ini" } );
        }

        public void RunWithArguments(string[] args)
        {
            try
            {
                if (args.Length < 1)
                {
                    throw new Exception("Missing arguments.");
                }
                if (args.Length > Arguments.Length)
                {   
                    throw new Exception("Too many arguments.");
                }
                
                string configIniPath = MyFileIO.ReturnAbsoluteOrRelativePath(args[0]);
                Globals.ConfigFilePath = configIniPath;

                int simNr = 0;
                bool simNrParameter = false;
                string dateStringForLogging = null;
                if (args.Length > 1)
                {
                    simNr = Int32.Parse(args[1]);
                    dateStringForLogging = args[2];
                    simNrParameter = true;
                }


                Console.WriteLine("CreateParticleFilterCommand: " + Globals.ConfigFilePath);

                if (!File.Exists(configIniPath))
                {
                    Console.WriteLine($"config file does not exists at specified location {configIniPath}");
                    return;
                }

                ConfigurationParser configurationParser = new ConfigurationParser(simNr, configIniPath);

                SimulatorFactory simulatorFactory = SetSimulatorFactory(simNr, configurationParser);

                uint durationInDays = configurationParser.GetSimulatorValueFromConfig<uint>("durationInDays");
                SimulatorConfig simulatorConfig = new SimulatorConfig(configIniPath, durationInDays);

                // init zoekbereik voor particle filters:
                BergmanAndBretonModel.Initialize_boundingRanges(configurationParser.GetParameters("particlefilter-model-ranges"));

                //patienten settings:
                int patientCount = configurationParser.GetSimulatorValueFromConfig<int>("patientAmount");

                ScheduleParameters scheduleparam = configurationParser.GetScheduleParameters();
                HrFsmSettings hrFsmSettings = configurationParser.GetHeartrateFsmSettings();
                
                ParticleFilterSettings particleFilterSettings = configurationParser.GetParticleFilterSettings();

                List<GlucoseInsulinSimulator> simulators = new List<GlucoseInsulinSimulator>();
                Dictionary<GlucoseInsulinSimulator, ParticleFilter> sim2pf = new Dictionary<GlucoseInsulinSimulator, ParticleFilter>();

                for (int createdPatientsCount = (simNrParameter ? simNr: 0); createdPatientsCount < (simNrParameter ? simNr + 1 : patientCount); createdPatientsCount++)
                {
                    PatientSettings patientSettings = configurationParser.GetPatientSettings();
                    Schedule realSchedule = simulatorFactory.CreateSchedule( simulatorConfig.durationInDays, patientSettings, scheduleparam, hrFsmSettings);
                    BergmanAndBretonModel bergmanModel = configurationParser.GetProxyForRealPatientModel();
                    
                    VirtualPatient patient = simulatorFactory.CreatePatient(
                        createdPatientsCount,
                        hrFsmSettings,
                        bergmanModel,
                        realSchedule,
                        patientSettings );


                    GlucoseInsulinSimulator simulator = simulatorFactory.CreateSimulator(simulatorConfig, patient); 
                    simulators.Add(simulator);

                    // TODO: refactor, moet IN patient?!
                    AddHeartRateModel(simulatorConfig, patient, hrFsmSettings);

                    sim2pf[simulator] = new ParticleFilter(simulator, patient, particleFilterSettings, dateStringForLogging);
                }



                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                int runParallel = configurationParser.GetSimulatorValueFromConfig<int>("RunParallelSimulations", "parallel");
                Globals.RunParallelSimulations = runParallel < 0 ||  runParallel >= 1 || simNrParameter; // bepaalt verbose
                int nrcores = runParallel;
                if (nrcores < 0)
                {
                    nrcores = Math.Max(1, Environment.ProcessorCount - 1); // rekening houden met heel zielige systemen :-)
                }

                if (Globals.RunParallelSimulations && !simNrParameter)
                {
                    Parallel.ForEach(
                        simulators,
                         new ParallelOptions { MaxDegreeOfParallelism = nrcores }, 
                         (simulator) =>
                    {
                        // reken de patient door:
                        simulator.Run();
                        simulator.patient.CreateNoisySchedule();

                        ParticleFilter particleFilter = sim2pf[simulator];
                        particleFilter.Run();
                    });
                }
                else
                {
                    foreach (GlucoseInsulinSimulator simulator in simulators)
                    {
                        // reken de patient door:
                        simulator.Run();
                        simulator.patient.CreateNoisySchedule();

                        ParticleFilter particleFilter = sim2pf[simulator];
                        particleFilter.Run();
                    }
                }

                stopwatch.Stop();
                Console.WriteLine("simulations + ML are all finished in " + stopwatch.Elapsed + " (= " + stopwatch.ElapsedMilliseconds + "ms)");

            }
            catch (Exception exception)
            {
                Console.WriteLine($"Particle filter failed. Error: {exception.Message}");
            }
        }



        ///////////////////////////////// helpers ////////////////////////////////////

        public static SimulatorFactory SetSimulatorFactory(int simNr, ConfigurationParser configurationParser) //  string modelString, int seed)
        {
            return new SimulatorFactory(simNr, configurationParser.GetRandomStuffForPatientSettings(), configurationParser.GetRandomStuffForScheduleSettings());
        }





        public static void AddHeartRateModel(SimulatorConfig simulatorConfig, VirtualPatient patient, HrFsmSettings hrFsmSettings)// string heartRateGeneratorString, string heartRatesFilePath)
        {
            // Add heart rate scheme generator to patient for Breton's modifications 

            AbstractHeartRateGenerator heartRateGenerator = null;
            switch (hrFsmSettings.heartRateGeneratorOption.ToLower().Trim())
            {
                case "rnd":
                case "random":
                    heartRateGenerator = new RandomHeartRateGenerator(patient.Random, (int)patient.Model.BaseHeartRate);
                    break;
                case "fsm":  // legacy naam in config.ini
                case "vip":  // very important person or virtually important patient, or just VIrtual Patient. 
                case "virtual":
                    heartRateGenerator = new FSMHeartRateGenerator(patient, hrFsmSettings);
                    break;
                case "data":
                case "real":
                    heartRateGenerator = new HeartRateReader(patient, hrFsmSettings);
                    break;
                default:
                    throw new Exception("Incorrect Heart Rate generator type: " + hrFsmSettings.heartRateGeneratorOption);
            }
            heartRateGenerator.GenerateScheme(simulatorConfig.durationInMinutes);
            patient.SetHeartRateGenerator(heartRateGenerator);
        }


        
    }
}