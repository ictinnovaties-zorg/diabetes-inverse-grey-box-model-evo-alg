using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SMLDC.Simulator.Utilities
{
    // puur en alleen zodat deze waardes gelogd kunnen worden, meer niet!
    public class Globals
    {
        public static int maxSigma = 2;

        public static string ConfigFilePath;

        public static int SeedForPatientSettings;
        public static int SeedForScheduleSettings;
        public static int SeedForParticleFilter;
        public static int SeedForParticleFilterSettings;

        public static bool RunParallelSimulations;
        public static int logEveryNEvaluations;
        private static List<int> patient_indices = new List<int>();
        //public static void AddPatientIndex(int ndx)
        //{
        //    lock(patient_indices)
        //    {
        //        patient_indices.Add(ndx);
        //    }
        //}
        //public static List<int> GetPatientIndices()
        //{
        //    List<int> lijst = new List<int>();
        //    lijst.AddRange(patient_indices);
        //    return lijst;
        //}

        public static string GetConfigShortName()
        {
            return MyFileIO.GetShortFileName(ConfigFilePath);
        }

        public static bool IsLaptop()
        {
            return File.Exists(@"C:\MOERMAN_LAPTOP.TXT");
        }
        public static bool IsVirtual()
        {
            return File.Exists(@"C:\MOERMAN_VIRTUAL.TXT");
        }
        public static bool IsThuis()
        {
            return File.Exists(@"C:\MOERMAN_THUIS.TXT");
        }





    }
}
