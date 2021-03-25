//using System;
//using System.Collections.Generic;
//using System.IO;
//using Serilog;
//using SMLDC.CLI.Helpers;
//using SMLDC.Simulator;
//using SMLDC.Simulator.DiffEquations.Models;
//using SMLDC.Simulator.Factories;
//using SMLDC.Simulator.Models.HeartRate;
//using SMLDC.Simulator.Models.HeartRate.HrFsm;
//using SMLDC.Simulator.Schedules;

//namespace SMLDC.CLI.Commands
//{
//    public class CreateSimulatorCommand : ICommand
//    {
//        public string[] Arguments { get; set; } = { "path" };

//        public string Description { get; set; } = "Creates an instance of the simulator. " +
//                                                  "\nYou need to fill in the path of the configuration file.";

//        public void Run()
//        {
//            //Log.Information("No arguments have been filled in");
//            RunWithArguments(new string[] { "config.ini" });
//        }

//        public void RunWithArguments(string[] args)
//        {
//            if (args.Length < 1 || args.Length < Arguments.Length)
//            {
//                Log.Information("Too little arguments filled in.");
//            }
//            else if (args.Length > Arguments.Length) {
//                Log.Information("Too many arguments filled in.");
//            }

//            try
//            {
//                string configIniPath = ReturnAbsoluteOrRelativePath(args[0]);
//                ConfigurationParser configurationParser = new ConfigurationParser(0, configIniPath);
//               // RandomStuff random = configurationParser.GetRandomStuffForPatientSettings();
                
//                SimulatorFactory simulatorFactory = SetSimulatorFactory(0, configurationParser);

//                uint durationInDays = configurationParser.GetSimulatorValueFromConfig<uint>("durationInDays");
//                SimulatorConfig simulatorConfig = new SimulatorConfig(configIniPath, durationInDays);

//                //patienten:
//                uint patientCount = configurationParser.GetSimulatorValueFromConfig<uint>("patientAmount");
//                HrFsmSettings hrFsmSettings = configurationParser.GetHeartrateFsmSettings();

//                ScheduleParameters scheduleparam = configurationParser.GetScheduleParameters();

//                //string exportLocation = configurationParser.GetSimulatorValueFromConfig<string>("exportLocation");

//                for (int createdPatientsCount = 1; createdPatientsCount <= patientCount; createdPatientsCount++)
//                {
//                    PatientSettings patientSettings = configurationParser.GetPatientSettings();
//                    throw new NotImplementedException();
//                    Schedule schedule = null; // simulatorFactory.CreateSchedule(random, simulatorConfig.durationInDays, patientSettings, scheduleparam, hrFsmSettings);
//                    BergmanAndBretonModel bergmanModel = configurationParser.GetProxyForRealPatientModel();

//                    VirtualPatient patient = simulatorFactory.CreatePatient(
//                        createdPatientsCount, hrFsmSettings,
//                        bergmanModel,
//                        schedule,
//                        patientSettings);

//                    // Add heart rate scheme generator to patient for Breton's modifications 
//                    CreateSimulatorCommand.AddHeartRateModel(simulatorConfig, patient, hrFsmSettings);

//                    GlucoseInsulinSimulator simulator = simulatorFactory.CreateSimulator(simulatorConfig, patient);

//                    // reken de patient door:
//                    simulator.Run();

//                    Tuple<List<string>, List<string>> logging = null; // MyLogging.GetTraceDataAsCsv(patient, simulatorConfig.durationInDays * 24 * 60, 15);

//                    //using (StreamWriter octaveStream = new StreamWriter(exportLocation + "/data_" + createdPatientsCount + ".csv", false /*don't append, but overwrite old*/ ))
//                    //{
//                    //    foreach (string txt in logging.Item2)
//                    //    {
//                    //        octaveStream.WriteLine(txt);
//                    //    }
//                    //}
//                    //todo: bergman settings etc. loggen? Van alle settings een config.ini maken?


//                    Console.WriteLine("sim finished");
//                }

//             //   Simulator.GlucoseInsulinSimulator simulator = simulatorFactory.CreateSimulator(simulatorConfig, patient, exporter);

//                    //CommandHandler.sim = simulator;

//                Console.WriteLine("simulations finished");
//                Log.Information("createSim command executed successfully.");
//            }
//            catch (System.Exception e)
//            {
//                throw new Exception($"Path was not correct. Configuration file has not been found. \nERROR: {e.Message}");
//            }
//        }



//        //public List<VirtualPatient> GeneratePatients(SimulatorFactory simulatorFactory, SimulatorConfig simulatorConfig, 
//        //                                            uint amountOfPatientsToCreate, PatientSettings patientParameters, BergmanAndBretonModel bmodel,
//        //                                            ScheduleParameters eventParameters, string heartRateGeneratorString, string heartRatesFilePath)
//        //{
//        //    List<VirtualPatient> patients = new List<VirtualPatient>();

//        //    for (uint patientCreatedCount = 0; patientCreatedCount < amountOfPatientsToCreate; patientCreatedCount++)
//        //    {

//        //        PatientSettings patientSettings = new PatientSettings(patientParameters);
//        //        Schedule schedule = simulatorFactory.CreateSchedule( simulatorConfig.durationInDays, patientSettings, eventParameters);
//        //        BergmanAndBretonModel bergmanModel = new BergmanAndBretonModel(bmodel);

//        //        VirtualPatient patient = simulatorFactory.CreatePatient(
//        //            (int)patientCreatedCount,  (patientCreatedCount == amountOfPatientsToCreate-1 ),
//        //            bergmanModel,
//        //            schedule,
//        //            patientSettings
//        //        );

//        //        AddHeartRateModel(simulatorFactory, simulatorConfig, patient, heartRateGeneratorString, heartRatesFilePath);

//        //        patients.Add(patient);
//        //    }

//        //    return patients;
//        //}




//        ///////////////////////////////// helpers ////////////////////////////////////

//        public static SimulatorFactory SetSimulatorFactory(int simNr, ConfigurationParser configurationParser) //  string modelString, int seed)
//        {
//            return new SimulatorFactory(simNr, configurationParser.GetRandomStuffForPatientSettings(), configurationParser.GetRandomStuffForScheduleSettings());
//        }





//        public static void AddHeartRateModel(SimulatorConfig simulatorConfig, VirtualPatient patient, HrFsmSettings hrFsmSettings)// string heartRateGeneratorString, string heartRatesFilePath)
//        {
//            // Add heart rate scheme generator to patient for Breton's modifications 

//            AbstractHeartRateGenerator heartRateGenerator = null;
//            switch (hrFsmSettings.heartRateGeneratorOption.ToLower().Trim())
//            {
//                case "rnd":
//                case "random":
//                    heartRateGenerator = new RandomHeartRateGenerator(patient.Random, (int)patient.Model.BaseHeartRate);
//                    break;
//                case "fsm":  // legacy naam in config.ini
//                case "vip":  // very important person or virtually important patient, or just VIrtual Patient. 
//                case "virtual":
//                    heartRateGenerator = new FSMHeartRateGenerator(patient, hrFsmSettings);
//                    break;
//                case "data":
//                case "real":
//                    heartRateGenerator = new HeartRateReader(patient, hrFsmSettings);
//                    break;
//                default:
//                    throw new Exception("Incorrect Heart Rate generator type: " + hrFsmSettings.heartRateGeneratorOption );
//            }
//            heartRateGenerator.GenerateScheme(simulatorConfig.durationInMinutes);
//            patient.SetHeartRateGenerator(heartRateGenerator);
//        }

//        public static string ReturnAbsoluteOrRelativePath(string inputPath)
//        {
//            string filePath = "";
//            if (Path.IsPathRooted(inputPath))
//                //Absolute. Path is inputPath
//                filePath = inputPath;
//            else
//                //Relative. Path is ../../../InputPath
//                //Because the directory it starts at needs to go back to 4 levels.
//                //from project/bin/debug/.netcoreapp3
//                filePath = Path.GetFullPath($"../../../{inputPath}");

//            DoesFileExist(filePath);

//            return filePath;
//        }

//        public static void DoesFileExist(string filePath)
//        {
//            try
//            {
//                File.Exists(filePath);
//            }
//            catch (System.Exception e)
//            {
//                throw new Exception($"File does not exist. \nERROR: {e.Message}");
//            }
//        }
//    }
//}