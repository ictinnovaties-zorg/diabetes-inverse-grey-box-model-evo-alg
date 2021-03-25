//
using IniParser;
using IniParser.Model;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace BatchRun
{

    /*
     * This project creates an executable that can run multiple config files.
     * Useful for running a lot of experiments when away from computer.
     * 
     * put the Release folder in the BATCH_RUN folder. 
     * put the version of the ML (SMLDC.CLI) executable in the SMLDC folder (nb not the release folder, but the contents of the release folder)
     * and put all config files that need to be run, in configs folder.
     * The file "1.PARALLEL" (or any number.PARALLEL) is used to determine how many PARALLEL experiments are run.
     * If BatchRun sees 3.PARALLEL, it will try to start 3 parallel patients (see GetNrParallel) for the same config
     * (unless the config says that less than 3 runs are needed: e.g. patientAmount = 2, then it will only run 2 patients). 
     * If the config says patientAmount = 5, then first it will run 3 patients at the same time, and then the remaining 2.
     * Only then will it go to the next config. 
     * X.PARALLEL is useful, because even with e.g. many cores, the ML will not use them all. This is because not everything can be
     * done parallel within 1 experiment (and parallel has its own overhead). So with eg. 10 cores, it may only take up 40% of the CPU. 
     * But staring two experiments/patients at the same time, will result in using about 80%.
     * Obviously, starting 5 at the same time will only yield near 100%, so the individual runs will be slower.
     * But as long as there is (a lot of) CPU power left on a system, X.PARALLEL can be increased (this can be done while BatchRun
     * is active. The updated value will be used for the next config).
     * Once configs are picked up, they are moved. And they are moved again when done. (note: the config file is copied to the results folder as well)
     * 
     * Zip the entire batch folder and move it to another system, and it can be ran there without any problems.
     * (if octave is present, etc...)
     * 
     * (NOTE: the code.zip will be added when the CLI is run, so it will always be an up to date version of the code, used for that batch run.
     * Note, there is still a bug here: if you run the CLI from visual studio again, it will update the code.zip, so then it is not 
     * actually the code used to build the executable). No time to fix this bug now. 
     * 
     * 
    */

    class RunConfigParallel
    {

        private static void RunOneSim(string config_file_in_doing_folder, int nr, string dateString)
        {
            Process process = new Process();
            process.StartInfo.FileName = Program.exe_locatie;
            process.StartInfo.Arguments = "\"" + config_file_in_doing_folder + "\" " + nr + "  \"" + dateString + "\"";
            process.StartInfo.ErrorDialog = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            process.Start();
            process.WaitForExit();
        }




        public static void Run(string config_file)
        {
            Console.WriteLine("======================================== config_file: " + config_file + " ========================================");
            int patientAmount = Program.GetNrPatients(config_file);

            // verplaats de config naar DOING:
            string short_path = Program.GetShortFileName(config_file);
            string config_file_in_doing_folder = Program.doing_folder + short_path;

            try
            {
                Directory.CreateDirectory(Program.doing_folder);
                File.Move(config_file, config_file_in_doing_folder);
                int maxNrParallel = Program.GetNrParallel();
                Console.WriteLine("BatchJob: maxNrParallel = " + maxNrParallel);
                string dateString = DateTime.Now.ToString(@"yyyy\-MM\-dd~~HH\h\-mm\m\-ss\s-") + DateTime.Now.Millisecond + "ms"; // exporter.GetCurrentDateString();


                Parallel.For(0, patientAmount, new ParallelOptions { MaxDegreeOfParallelism = maxNrParallel },
                      index =>
                      {
                          RunOneSim(config_file_in_doing_folder, index, dateString);
                      });

                string done_path = Program.done_folder + short_path;
                Directory.CreateDirectory(Program.done_folder);
                File.Move(config_file_in_doing_folder, done_path);

            }

            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


    }





    class Program
    {
        public static string config_folder_name;
        public static string base_folder_name;
        public static string exe_locatie;
        public static string doing_folder;
        public static string done_folder;
       
   

        public static int GetNrPatients(string path)
        {
            try
            {
                //https://github.com/rickyah/ini-parser/wiki/First-Steps 
                // [sectie]  
                // ; single line comment
                FileIniDataParser parser = new FileIniDataParser();
                IniData _iniFile = parser.ReadFile(path);
                string input = _iniFile["simulator"]["patientAmount"].Trim();
                int ndx_puntkomma = input.IndexOf(";");
                if (ndx_puntkomma >= 0)
                {
                    input = input.Substring(0, ndx_puntkomma);
                }
                return Convert.ToInt32(input);

            }
            catch (Exception e)
            {
                throw new Exception($"Configuration not found. Exception: {e.Message}");
            }
        }



        static void Main(string[] args)
        {
            Console.WriteLine("BatchRun");
            base_folder_name = System.Reflection.Assembly.GetEntryAssembly().Location;
            Console.WriteLine(base_folder_name);

            int ndx_bin = base_folder_name.IndexOf("\\BATCH_RUN\\");
            base_folder_name = base_folder_name.Substring(0, ndx_bin) + "\\BATCH_RUN\\";

            config_folder_name = base_folder_name + "\\configs\\";
            exe_locatie = base_folder_name + "\\SMLDC\\SMLDC.CLI.exe";

            Console.WriteLine("exe: " + exe_locatie);

            doing_folder = config_folder_name + "\\DOING\\";
            done_folder = config_folder_name + "\\DONE\\";
            Directory.CreateDirectory(doing_folder);
            Directory.CreateDirectory(done_folder);

            if (!File.Exists(@"C:\MOERMAN_THUIS.TXT"))
            {
                string code_in_batch = base_folder_name + "\\code.zip";
                string code_in_batch_naam_temp = @"C:\temp\BATCH_RUN\code.zip";

                try
                {
                    Directory.CreateDirectory(GetShortFileName(code_in_batch_naam_temp));
                    if (File.Exists(code_in_batch_naam_temp))
                    {
                        File.Delete(code_in_batch_naam_temp);
                    }
                    File.Copy(code_in_batch, code_in_batch_naam_temp);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            while (true)
            {
                string[] config_files = Directory.GetFiles(config_folder_name);
                if (config_files.Length == 0) { break; }

                foreach (string config_file in config_files)
                {
                    if (config_file.EndsWith(".ini") && config_file.Contains("config"))
                    {
                        RunConfigParallel.Run(config_file);
                        break;
                    }
                }
            }

       //     Console.WriteLine("Batch run klaar ... <ENTER>");
       //     Console.ReadLine();
        }



        public static int GetNrParallel()
        {
            // check op bestaan van file met uitgang ".PARALLEL"
            // die geef maxNrParallel aan
            string[] files_in_base = Directory.GetFiles(base_folder_name);
            foreach (string file in files_in_base)
            {
                if (file.EndsWith(".PARALLEL"))
                {
                    string short_filename = GetShortFileName(file).Replace(".PARALLEL", "");
                    try
                    {
                        return Int32.Parse(short_filename);
                    }
                    catch { }
                }
            }
            return 1;
        }



        public static void Pause(string msg)
        {
            Console.WriteLine(msg + " ... <ENTER>");
            Console.ReadLine();
        }



        public static string GetShortFileName(string filePath)
        {
            int ndx_slash = filePath.LastIndexOf("/");
            int ndx_backslash = filePath.LastIndexOf("\\");
            int ndx_last = Math.Max(ndx_slash, ndx_backslash);
            if (ndx_last >= 0)
            {
                return filePath.Substring(ndx_last + 1);
            }
            else { return filePath; }
        }



    }
}
