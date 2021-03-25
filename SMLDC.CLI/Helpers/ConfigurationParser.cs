using System;
using System.Collections.Generic;
using System.Globalization;
using IniParser;
using IniParser.Model;
using SMLDC.MachineLearning;
using SMLDC.Simulator;
using SMLDC.Simulator.DiffEquations.Models;
using SMLDC.Simulator.Models.HeartRate.HrFsm;
using SMLDC.Simulator.Schedules;
using SMLDC.Simulator.Utilities;

namespace SMLDC.CLI.Helpers
{
    public class ConfigurationParser
    {
        private readonly IniData _iniFile;

        private RandomStuff randomForPatientSettings;
        public RandomStuff GetRandomStuffForPatientSettings() { return randomForPatientSettings; }
        private RandomStuff InitRandomStuffForPatientSettings(int simNr)
        {
            if (randomForPatientSettings == null)
            {
                if (Globals.SeedForPatientSettings < 0)
                {
                    randomForPatientSettings = new RandomStuff();
                    Globals.SeedForPatientSettings = randomForPatientSettings.GetRandomSeed();
                }
                else
                {
                    randomForPatientSettings = new RandomStuff(Globals.SeedForPatientSettings + simNr);
                    Globals.SeedForPatientSettings = randomForPatientSettings.GetRandomSeed() - simNr;
                }
            }
            return randomForPatientSettings;
        }


        private RandomStuff randomForScheduleSettings;
        public RandomStuff GetRandomStuffForScheduleSettings() { return randomForScheduleSettings; }
        private RandomStuff InitRandomStuffForScheduleSettings(int simNr)
        {
            if (randomForScheduleSettings == null)
            {
                if (Globals.SeedForScheduleSettings < 0)
                {
                    randomForScheduleSettings = new RandomStuff();
                    Globals.SeedForScheduleSettings = randomForScheduleSettings.GetRandomSeed();
                }
                else
                {
                    randomForScheduleSettings = new RandomStuff(Globals.SeedForScheduleSettings + simNr);
                    Globals.SeedForScheduleSettings = randomForScheduleSettings.GetRandomSeed() - simNr;
                }
            }
            return randomForScheduleSettings;
        }




        private RandomStuff randomForParticleFilterSettings;
        private RandomStuff InitRandomStuffForParticleFilterSettings(int simNr)
        {
            if (randomForParticleFilterSettings == null)
            {
                if (Globals.SeedForParticleFilterSettings < 0)
                {
                    randomForParticleFilterSettings = new RandomStuff();
                    Globals.SeedForParticleFilterSettings = randomForParticleFilterSettings.GetRandomSeed();
                }
                else
                {
                    randomForParticleFilterSettings = new RandomStuff(Globals.SeedForParticleFilterSettings + simNr);
                    Globals.SeedForParticleFilterSettings = randomForParticleFilterSettings.GetRandomSeed() - simNr;
                }
            }
            return randomForParticleFilterSettings;
        }

        public ConfigurationParser(int simNr, string path)
        {
            try
            {
                //https://github.com/rickyah/ini-parser/wiki/First-Steps 
                // [sectie]  
                // ; single line comment
                FileIniDataParser parser = new FileIniDataParser();
                _iniFile = parser.ReadFile(path);

                GetGlobals();

                InitRandomStuffForPatientSettings(simNr);
                InitRandomStuffForParticleFilterSettings(simNr);
                InitRandomStuffForScheduleSettings(simNr);
            }
            catch (Exception e)
            {
                throw new Exception($"Configuration not found. Exception: {e.Message}");
            }
        }



        public T GetSimulatorValueFromConfig<T>(string valueName, string categorie="simulator")
        {
            try
            {
                switch (Type.GetTypeCode(typeof(T)))
                {
                    case TypeCode.Double:
                        return (T)(object)TryParseDoubleFromInput(_iniFile[categorie][valueName].Trim());
                    case TypeCode.Int32:
                        return (T)(object)TryParseIntFromInput(_iniFile[categorie][valueName].Trim());
                    case TypeCode.UInt32:
                        return (T)(object)TryParseUintFromInput(_iniFile[categorie][valueName].Trim());
                    case TypeCode.Boolean:
                        UInt32 value = TryParseUintFromInput(_iniFile[categorie][valueName].Trim());
                        return (T)(object)(value > 0.5);
                    case TypeCode.String:
                        {
                            return (T)(object) TryParseStringFromInput(_iniFile[categorie][valueName].Trim());
                        }
                    default:
                        throw new Exception($"Value of type {Type.GetTypeCode(typeof(T))} is not supported. Can't return value of {valueName}.");
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Parsing {valueName} from .ini failed: {e.Message}");
            }

        }


        public void GetGlobals()
        {
            Globals.logEveryNEvaluations = GetSimulatorValueFromConfig<int>("logEveryNEvaluations");
            // seeds
            Globals.SeedForPatientSettings = GetSimulatorValueFromConfig<int>("SeedForPatientSettings");
            Globals.SeedForParticleFilterSettings = GetSimulatorValueFromConfig<int>("SeedForParticleFilterSettings");
            Globals.SeedForScheduleSettings = GetSimulatorValueFromConfig<int>("SeedForScheduleSettings");

            Globals.SeedForParticleFilter = GetSimulatorValueFromConfig<int>("SeedForParticleFilter");
        }


        public BergmanAndBretonModel GetProxyForRealPatientModel()
        {
            Dictionary<string, double[]> modelParam = GetParameters("proxy-for-real-patient-model");
            //Dictionary<string, double[]> pfRangeParam = GetParameters("particlefilter-model-ranges");
            return new BergmanAndBretonModel(this.randomForPatientSettings, modelParam); //, pfRangeParam);
        }

        public PatientSettings GetPatientSettings()
        {
            Dictionary<string, double[]> patientParameters = GetParameters("patient");
            PatientSettings psettings = new PatientSettings(randomForPatientSettings, patientParameters);
            HrFsmSettings hrFsmSettings = GetHeartrateFsmSettings();
            return psettings;
        }

        public ParticleFilterSettings GetParticleFilterSettings()
        {
            Dictionary<string, double[]> pfParameters = GetParameters("particlefilter");
            Dictionary<string, double[]> pfParameters2 = GetParameters("glucose_meter");
            foreach (string key in pfParameters2.Keys)
            {
                pfParameters[key] = pfParameters2[key];
            }
            Dictionary<string, double[]> pfParameters3 = GetParameters("parallel");
            foreach (string key in pfParameters3.Keys)
            {
                pfParameters[key] = pfParameters3[key];
            }
            return new ParticleFilterSettings(this.randomForParticleFilterSettings, pfParameters);
        }

        public HrFsmSettings GetHeartrateFsmSettings()
        {
            Dictionary<string, double[]> hrParameters = GetParameters("heartrate_fsm");
            string heartRateGeneratorOption = GetSimulatorValueFromConfig<string>("heartRateGeneratorOption");
            string realDataBaseFolder = GetSimulatorValueFromConfig<string>("RealDataBaseFolder");
            //string heartRateFileName = GetSimulatorValueFromConfig<string>("heartRateFileName");
            int realPatientIndex = GetSimulatorValueFromConfig<int>("realPatientIndex");
            return new HrFsmSettings(this.randomForScheduleSettings, heartRateGeneratorOption, realDataBaseFolder, realPatientIndex,  hrParameters);
        }

      //  public Dictionary<string, double[]> GetPatientParameters() { return GetParameters("patient"); }

        public Dictionary<string, double[]> GetParameters(string sname)
        { 
            KeyDataCollection patientSection = _iniFile[sname];
            Dictionary<string,double[]> param = new Dictionary<string, double[]>();

            foreach (KeyData dataField in patientSection)
            {
                ExtractMeanAndStandardDeviationFromKeyData(dataField, out double mean, out double standardDeviation);
                param.Add(dataField.KeyName, new double[] {mean, standardDeviation});
            }
            return param;
        }


        public ScheduleParameters GetScheduleParameters()
        {
            Dictionary<string, Dictionary<string, double[]>> eventParameters = new Dictionary<string, Dictionary<string, double[]>>();

            foreach (SectionData section in _iniFile.Sections)
            {
                Dictionary<string, double[]> dezeParam = new Dictionary<string, double[]>();

                if (!section.SectionName.Contains("eatevent") && section.SectionName != "glucose_meter" ) { continue; }

                foreach (KeyData dataField in section.Keys)
                {
                    ExtractMeanAndStandardDeviationFromKeyData(dataField, out double mean, out double stddev);
                    dezeParam[dataField.KeyName] = new double[] { mean, stddev };
                }
                string sectionName = section.SectionName.Substring(section.SectionName.IndexOf("-") + 1);
                eventParameters[sectionName] = dezeParam;
            }
            return new ScheduleParameters(eventParameters);
        }


        private static string RemoveCommentsFromInput(string input)
        {
            int ndx_puntkomma = input.IndexOf(";");
            if (ndx_puntkomma >= 0)
            {
                input = input.Substring(0, ndx_puntkomma);
            }
            return input;
        }

        private static string TryParseStringFromInput(string input)
        {
            try
            {
                input = RemoveCommentsFromInput(input);
                input = input.Trim();
                string result = Convert.ToString(input);
                if (result == "")
                {
                    throw new Exception($"No Input found at: {input}");
                }
                return result;
            }
            catch (Exception e)
            {
                throw new Exception($"Incorrect input: {input}. Exception: {e.Message}");
            }
        }

        private static uint TryParseUintFromInput(string input)
        {
            try
            {
                input = RemoveCommentsFromInput(input);
                return Convert.ToUInt32(input);
            }
            catch (Exception e)
            {
                throw new Exception($"Incorrect input: {input}. Exception: {e.Message}");
            }
        }
        

        private static int TryParseIntFromInput(string input)
        {
            try
            {
                input = RemoveCommentsFromInput(input);
                return Convert.ToInt32(input);
            }
            catch (Exception e)
            {
                throw new Exception($"Incorrect input: {input}. Exception: {e.Message}");
            }
        }

        private static double TryParseDoubleFromInput(string input)
        {
            try
            {
                input = RemoveCommentsFromInput(input);
                //We give it cultureinfo because some computers will interpret decimal points and comma's the wrong way. This helps preventing that
                return Convert.ToDouble(input, CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                throw new Exception($"Incorrect input: {input}. Exception: {e.Message}");
            }
        }


        private static readonly string[] string_splitters = new string[] { "::", " ", "~" };
        // waarde::spreiding
        // waarde   (in dit geval is spreiding automatisch 0)
        private static void ExtractMeanAndStandardDeviationFromKeyData(KeyData dataField, out double mean, out double standardDeviation)
        {
            try
            {
                //We expect the string to extract at most two values. Therefore count is 2.
                // maar: de inifile haalt geen comments aan einde vd line weg!!! SUF, maar niet aan te passen. Dus eerst hier doen:
                dataField.Value = RemoveCommentsFromInput(dataField.Value);
                string[] splitString = dataField.Value.Split(string_splitters, StringSplitOptions.RemoveEmptyEntries);
                if (splitString.Length == 0)
                {
                    throw new Exception($"No input found at {dataField.KeyName}");
                }

                //Converting to double can run into problems because of points and comma's depending on the system culture.
                //Using System.Globalization.CultureInfo.InvariantCulture solves this problem
                mean = Convert.ToDouble(splitString[0], CultureInfo.InvariantCulture);
                standardDeviation = 0;
                if (splitString.Length == 2)
                {
                    standardDeviation = Convert.ToDouble(splitString[1], CultureInfo.InvariantCulture);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Extracting mean and standard deviation failed: {e.Message}");
            }
        }
    }
}