using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using LibSharpTave;
using SMLDC.MachineLearning;
using SMLDC.Simulator.Utilities;
using SMLDC.Simulator.DiffEquations.Solvers;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;

namespace SMLDC.MachineLearning
{

    /////////////////////////////// LOGGGING van data en settings, experimenten etc. ////////////////////
    /////////////////////////////// LOGGGING van data en settings, experimenten etc. ////////////////////
    /////////////////////////////// LOGGGING van data en settings, experimenten etc. ////////////////////
    /////////////////////////////// LOGGGING van data en settings, experimenten etc. ////////////////////
    /////////////////////////////// LOGGGING van data en settings, experimenten etc. ////////////////////

        /*
         * The logging has become quite convoluted.
         * The global structure is: This class creates .m (MATLAB/Octave) files for plotting errors and trace (glucose, etc. through time)
         * This is only done on first execution of the logging. (So you could open the .m files in the results folder in
         * e.g. Octave and edit them to change logging, during the run. Useful for tweaking... ten implement the 
         * changes here in the .m files that are generated).
         * TODO??: make some sort of .m template, read it here, use it. (Instead of generating .m code here).
         * 
         * This logger goes to the ML for part of its data.
         */ 

    class ParticleFilterDataLogger
    {
        string pathToOctaveBins = @"C:\Octave\Octave-5.2.0\mingw64\bin\"; // put path to your octave binaries \bin\ folder here


        private static string datestring;
        private static string CommentsForFileName;

        private static readonly object LockObject = new object();

        public void Init()
        {
            lock (LockObject)
            {
                if (datestring == null)
                {
                    datestring = particleFilter.GetDateString();

                    CommentsForFileName = particleFilter.settingsForParticleFilter.CommentsForFileName;
                    if (particleFilter.ObservedPatient.RealData)
                    {
                        CommentsForFileName += "-REAL-" + particleFilter.RealDataPatientNumber;
                    }
                    if (particleFilter.settingsForParticleFilter.debug)
                    {
                        CommentsForFileName = CommentsForFileName + "_DEBUG" + (particleFilter.settingsForParticleFilter.ML_PERFECT_HACK ? "_MLHACK" : "");
                    }
                    else {
                        
                        CommentsForFileName = CommentsForFileName + (particleFilter.settingsForParticleFilter.ML_PERFECT_HACK ? "_MLHACK_" : "") 
                        //+ "_experimentjes_"
                        + (particleFilter.ObservedPatient.RealData ? "_gcReal" : "g" + particleFilter.ObservedPatient.settings.GlucNoiseFactor
                        + "_c" + particleFilter.ObservedPatient.settings.CarbNoiseFactor
                        + "_cOff" + particleFilter.ObservedPatient.settings.CarbTimeNoiseSigma
                        + "_cFgt" + particleFilter.ObservedPatient.settings.FoodForgetFactor

                        )
                        // + "_pars" + particleFilter.settingsForParticleFilter.SearchInSubPopulationType
                        // + (particleFilter.settingsForParticleFilter.SearchInSubPopulationType == 1 ? "s" + particleFilter.settingsForParticleFilter.NumberOfParticlesPerSubSubPopulation + "t" + particleFilter.settingsForParticleFilter.NumberOfTurnsPerSubSubPopulation + "p" + particleFilter.settingsForParticleFilter.NumberOfPeaksPerSubSubPopulation + "x" + particleFilter.settingsForParticleFilter.NumberOfPeaksPerSubSubPopulationStep : "")
                        // + "_Err" + particleFilter.settingsForParticleFilter.ErrorForWeights + "o" + particleFilter.settingsForParticleFilter.USE_OVERSHOOTS_IN_WEIGHT
                        + "_Sub" + particleFilter.settingsForParticleFilter.NumberOfSubPopulations + "x" + particleFilter.settingsForParticleFilter.NumberOfParticlesPerSubPopulation
                        + "_RSelBlz" + particleFilter.settingsForParticleFilter.ExponentialDecayInitValue + "x" + particleFilter.settingsForParticleFilter.ExponentialDecayDecayValue
                        + "_Tr" + particleFilter.settingsForParticleFilter.TrailLengthInMinutes + "e" + particleFilter.settingsForParticleFilter.evaluateEveryNMinutes + "m"
                        // + (SolverResultFactory.ARRAY_BASED ? "_ARRAYs1d" : "_GeenArray")
                       // + "_p" + (this.particleFilter.settingsForParticleFilter.UseParallelProcessingOnSubPopulaties ? "1" : "0") + (this.particleFilter.settingsForParticleFilter.UseParallelProcessingParticleEvaluaties ? "1" : "0")
                       // + (MyMath.USE_ACCURATE_POW ? "_aPow" : "_fPow")
                        // + (this.particleFilter.settingsForParticleFilter.UpdateCarbHypDuringSearch ? "_CuD" : "_CuA")
                        + "_Cs" + (this.particleFilter.settingsForParticleFilter.CarbSearchLinear ? "Lin" : "P" + this.particleFilter.settingsForParticleFilter.MaxNrStepsInParabolicSearch)
                         + "_lr" + this.particleFilter.settingsForParticleFilter.CarbEstimatesPerSubUpdateIsLearningRate
                         + "_CHpFdbk" + (this.particleFilter.settingsForParticleFilter.BaseInitialCarbEstimationOnGlucoseCurve ? "1" : "0")
                         + "_CHrnk" + (this.particleFilter.settingsForParticleFilter.CarbHypRanking ? "1" : "0")
                         + "_IEst" + (this.particleFilter.settingsForParticleFilter.BaseInitialCarbEstimationOnGlucoseCurve ? "1" : "0")
                        ;
                    }

                    //config.ini kopieren, en voeg gebruikte seed toe:
                    string tmp = GetOutputDir(false, datestring, CommentsForFileName);

                    Console.WriteLine("logger: " + Globals.GetConfigShortName());

                    string config_ini_path = tmp + "/" + Globals.GetConfigShortName();
                    if (!File.Exists(config_ini_path)  && particleFilter.PATIENT_INDEX == 0)
                    {
                        File.Copy(particleFilter.ConfigIniPath, config_ini_path, true);
                        string config_txt = File.ReadAllText(config_ini_path);
                       //if (!config_txt.Contains("actual_seed_used_for_settings = "))
                        {
                            config_txt =
                                  "actual_SeedForPatientSettings = " + Globals.SeedForPatientSettings + " ; deze regel weghalen en de waarde gebruiken als seed\n"
                                + "actual_SeedForScheduleSettings = " + Globals.SeedForScheduleSettings + " ; deze regel weghalen en de waarde gebruiken als seed\n"
                                + "actual_SeedForParticleFilter = " + Globals.SeedForParticleFilter + " ; deze regel weghalen en de waarde gebruiken als seed\n"
                                + "actual_SeedForParticleFilterSettings = " + Globals.SeedForParticleFilterSettings + " ; deze regel weghalen en de waarde gebruiken als seed\n"
                                + "\n" +  config_txt;
                        }
                        File.WriteAllText(config_ini_path, config_txt);

                        if (Globals.IsThuis())
                        {
                            // IDEA: when a run is executed with the ML, the logger actually zips the code used for that run
                            // and stores it in the BATCH_RUN folder, and copy it to the results folder as well.
                            //
                            // TODO: small bug here: when the BatchRun executable is run on command line, it copies the code.zip
                            // but meanwhile running a newer version of the ML code from Visual Studio will update the code.zip.
                            //    proposed solution: let the ML executable not only check if it is Globals.IsThuis() but also
                            //    if it is started from VS or from BatchRun.
                            // 
                            // this is very useful during r&d development, iterating, making small changes to algoritms, etc..
                            // It is easy to just diff code from two runs with different settings and/or changes in the algorithms.
                            //
                            //ZipAllCode(@"C:\repo\nieuwe sim\smldc-qsd\", tmp, "code.zip");
                            //string code_in_batch_naam = @"C:\repo\BATCH_RUN\code.zip";
                            //try
                            //{
                            //    if (File.Exists(code_in_batch_naam))
                            //    {
                            //        File.Delete(code_in_batch_naam);
                            //    }
                            //    File.Copy(tmp + "/code.zip", code_in_batch_naam);
                            //}
                            //catch (Exception e)
                            //{
                            //    Console.WriteLine("Logger :: " + e);
                            //}
                        }
                        else
                        {
                            string code_in_batch_naam = @"C:\temp\BATCH_RUN\code.zip";
                            try
                            {
                                File.Copy(code_in_batch_naam, tmp + "/code.zip");
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Logger :: " + e);
                            }

                        }
                        Console.WriteLine("-------> INIT: logging to " + tmp);
                    }
                }
            }
        }


        // zip all cs. (and .ini, .csproj, .sln files) files and save the archive.
        //C:\repo\nieuwe sim\smldc-qsd\
        public static void ZipAllCode(string folder, string resultaat_dir_path, string zipnaam)
        {
            if (!Globals.IsThuis()) { return; }
            try
            {
                string path = folder;
                // maak temp dir voor alle .cs /.ini files?
                string tmp_dir_to_zip = @"c:\temp\TEMP_CS_CODE_FOR_ZIPPING\";
                // recursief alle code files opzoeken en copy-paste naar tmp
                List<string> files = MyFileIO.RecursiveDirSearch(path, new string[] { ".cs", ".ini", ".csproj", ".sln" }, new string[] { "\\.git\\", "\\obj\\", "\\bin\\" });
                for (int i = 0; i < files.Count; i++)
                {
                    string origfile = files[i];
                    string newfile = origfile.Substring(path.Length);
                    newfile = tmp_dir_to_zip + "\\" + newfile;
                    string newdirpath = newfile.Substring(0, newfile.LastIndexOf("\\"));
                    //   Console.WriteLine(origfile + " --> " + newfile);
                    Directory.CreateDirectory(newdirpath);
                    File.Copy(origfile, newfile, true);
                }
                System.IO.Compression.ZipFile.CreateFromDirectory(tmp_dir_to_zip, resultaat_dir_path + "/" + zipnaam.Replace(".zip", "") + ".zip");
                Console.WriteLine("CODE gezipt en opgeslagen in (temp) resultaten dir");
                Directory.Delete(tmp_dir_to_zip, true);
            }
            catch
            {
            }
        }




        // octave output file:
        private static readonly string octaveDataFilePath = "C:/Lectoraat/Matlab/LectoraatOctaveData/"; // C:\Users\cm0093894\Documents\lectoraat - insuline\matlab\LectoraatOctaveData\";  //octave_data_
        private static readonly string tempOctaveDataFilePath = "C:/Lectoraat/Matlab/LectoraatOctaveData/temp/"; //TEMP_octave_data_

        // vars voor output filess
        private HashSet<string> fulllijst_HASHSET = new HashSet<string>(); //is deze nodig voor performance??
        private List<string> fulllijst = new List<string> { "time" };
        private List<string> fulllijstTrace = new List<string>();
        private static readonly List<string> shortlist = new List<string> { "time", "nrOfCalc", 
            "rmseNoisy", "rmseReal", "realLowerThanPredictedAtMinimum", "realHigherThanPredictedAtMaximum", "realLowerThanPredictedAtMaximum", "realHigherThanPredictedAtMinimum", "rmseNoisyMmntsTovReal"  };
        private HashSet<string> diversityKeys_HASHSET = new HashSet<string>(); //is deze nodig voor performance??
        private List<string> diversityKeys = new List<string>();





        private ParticleFilter particleFilter;

        public ParticleFilterDataLogger(ParticleFilter pf)
        {
            particleFilter = pf;
        }

        List<DataLoggerElement> dataLoggerElementsList = new List<DataLoggerElement>();

        private void DataElementAdd(DataLoggerElement dElement, string varname, double value)
        {
            dElement.Add(varname, value);
            if (!fulllijst_HASHSET.Contains(varname))
            {
                fulllijst_HASHSET.Add(varname);
                fulllijst.Add(varname);
            }
        }

        //logt meest recente waarden van rmse, overshoots, etc...
        public void LogParticle(uint time, Particle bestParticleMetCarbHyp, Particle bestParticleMetOrigSchedule, Tuple<SortedDictionary<int, double>, SortedDictionary<int, double>> diversityDict) //, Tuple<double,double> bestParticleRmses_trail=null)
        {
            DataLoggerElement dElement = new DataLoggerElement(time);
            Particle[] particles = { bestParticleMetCarbHyp, bestParticleMetOrigSchedule };


            for (int i = 0; i < particles.Length; i++)
            {
                string tag;
                Particle particle;
                if (i == 0)
                {
                    tag = "";
                    particle = bestParticleMetCarbHyp;
                }
                else
                {
                    tag = "_orig";
                    particle = bestParticleMetOrigSchedule;
                }
                DataElementAdd(dElement, "ISF" + tag, particle.ISF);
                DataElementAdd(dElement, "ICR" + tag, particle.ICR);
                DataElementAdd(dElement, "nrOfCalc" + tag, particleFilter.MyMidpointSolver.GetNrOfSolverSteps());

                DataElementAdd(dElement, "rmseNoisy" + tag, particle.RMSE_ML2Mmnts);
                DataElementAdd(dElement, "rmseReal" + tag, particle.RMSE_ML2Real_ALLEEN_VOOR_REFERENTIE);

                // wat is de ruis in de mmnts tov de echte (vip) data?
                DataElementAdd(dElement, "rmseNoisyMmntsTovReal" + tag, particle.errorDataContainer.ml2Mmnts_TargetReal.RMSE_Mmnts2Target);

                DataElementAdd(dElement, "realLowerThanPredictedAtMinimum" + tag, particle.errorDataContainer.ml2Mmnts_TargetReal.max_LowerThanPredictedAtMinimum); // gevaarlijk, hypo
                DataElementAdd(dElement, "realHigherThanPredictedAtMaximum" + tag, particle.errorDataContainer.ml2Mmnts_TargetReal.max_HigherThanPredictedAtMaximum); // lange termijn problemen
                DataElementAdd(dElement, "realLowerThanPredictedAtMaximum" + tag, particle.errorDataContainer.ml2Mmnts_TargetReal.max_LowerThanPredictedAtMaximum);
                DataElementAdd(dElement, "realHigherThanPredictedAtMinimum" + tag, particle.errorDataContainer.ml2Mmnts_TargetReal.max_HigherThanPredictedAtMinimum);


                SortedDictionary<int, double> ml2noisy_dict = diversityDict.Item1;
                SortedDictionary<int, double> ml2real_dict = diversityDict.Item2;

                foreach (int subNdx in ml2noisy_dict.Keys)
                {
                    string key1 = "ml2rmse_noisy_" + subNdx;
                    string key2 = "ml2rmse_real_" + subNdx;

                    DataElementAdd(dElement, key1, ml2noisy_dict[subNdx]);
                    DataElementAdd(dElement, key2, ml2real_dict[subNdx]);

                    if (!diversityKeys_HASHSET.Contains(key1)) // voorlopig willen we al deze data
                    {
                        diversityKeys_HASHSET.Add(key1);
                        diversityKeys.Add(key1);
                        diversityKeys_HASHSET.Add(key2);
                        diversityKeys.Add(key2);
                    }
                }
            }
            dataLoggerElementsList.Add(dElement);
        }



        //////////////////////////// file IO ////////////////////////////////

        /*
         * juiste dir maken/opvragen
         * csv file met errors schrijven. 
         * csv file met trace schrijven.
         * 'master' octave file voor plotten schrijven
         */






        public void LogToFile()//int particleFilter.PATIENT_INDEX, bool isLast)
        {
            if(dataLoggerElementsList.Count == 0) { return; }
                bool isLast = false;
            Console.WriteLine("-------> logging to file: '" + datestring + ".m'");
            try
            {
                // octave output file:
                uint lasttime = dataLoggerElementsList[dataLoggerElementsList.Count - 1].time;
                string octaveBasePath = GetOutputDir(isLast, datestring, CommentsForFileName);
                string octavePatientFolder = octaveBasePath + "/patient_" + particleFilter.PATIENT_INDEX + "/";
                string particlesfilename = octavePatientFolder + "/traceplots/populations_" + lasttime + ".txt";
                string evoparticlesfilename = octavePatientFolder + "/traceplots/evoparticles_" + lasttime + ".txt";

                Console.WriteLine("-------> logging to folder: '" + octaveBasePath + "'");

                Directory.CreateDirectory(octaveBasePath);
                Directory.CreateDirectory(octavePatientFolder + "/traceplots/");
                Directory.CreateDirectory(octavePatientFolder + "/traceplots/train/");
                Directory.CreateDirectory(octavePatientFolder + "/traceplots/eval/");

                MaakPlotScripts(octaveBasePath);

                // csv met carb data:

                //using (StreamWriter octaveStream = new StreamWriter(octaveDataFileFullNameBase + "carbHypothesis_p" + particleFilter.PATIENT_INDEX + ".csv", false /*don't append, but overwrite old*/ ))
                //{
                //    List<string> errorsToCSV = particleFilter.ExportCarbEstimatesToCsv();
                //    foreach (string txt in errorsToCSV)
                //    {
                //        octaveStream.WriteLine(txt);
                //    }
                //}

                using (StreamWriter octaveStream = new StreamWriter(particlesfilename, false /*don't append, but overwrite old*/ ))
                {
                    octaveStream.WriteLine(particleFilter.ToCSV());
                }

                //using (StreamWriter octaveStream = new StreamWriter(evoparticlesfilename, false /*don't append, but overwrite old*/ ))
                //{
                //    octaveStream.WriteLine(particleFilter.learningCarbHypothesis.ToCSV);
                //}

                // csv file met errors schrijven. 
                using (StreamWriter octaveStream = new StreamWriter(octavePatientFolder + "Errors_p.csv", false /*don't append, but overwrite old*/ ))
                {
                    List<string> errorsToCSV = GetLoggedDataAsCsv();
                    foreach (string txt in errorsToCSV)
                    {
                        octaveStream.WriteLine(txt);
                    }
                }

                //csv file met trace schrijven.
                using (StreamWriter octaveStream = new StreamWriter(octavePatientFolder + "Trace_p.csv", false /*don't append, but overwrite old*/ ))
                {
                    Tuple<List<string>, List<string>> traceToCSV = particleFilter.GetTraceDataAsCsv();
                    foreach (string txt in traceToCSV.Item2)
                    {
                        //todo: hier noisy mmnt en noisy food etc outputten!
                        octaveStream.WriteLine(txt);
                    }
                }


                GenereerPlots(octaveBasePath, octavePatientFolder, lasttime);
            }
            catch (Exception e)
            {
                Console.WriteLine("ParticleFilterDataLogger :: caught Exception " + e + "\n" + e.Message + "\n" + e.StackTrace);
            }
        }


        private static Stopwatch lastErrorTotaalPlotTimeStopwatch = new Stopwatch();
        private static long errorTotaalPlotTimeDelta_in_MS = 1000 * 60;


        public void GenereerPlots(string octaveBasePath, string octaveDataPatientFolder, uint lasttime)
        {
            string imgfilename = octaveDataPatientFolder + "/traceplots/tracePlot_"; //in octave TracePlot gebruikt
            // en dan nog automatisch een plot genereren:
            try
            {
                Octave octaveProcess_ErrorPlot = new Octave(pathToOctaveBins, true);// true: omdat er een plot (img) gemaakt moet worden, dus octave moet bv. gnuplot kunnen starten.
                try
                {
                    //StartMinimizeThread(octaveProcess_ErrorPlot);
                    octaveProcess_ErrorPlot.ExecuteCommand("cd '" + octaveDataPatientFolder + "'", 30000);

                    //gewoon plotten indien het kan
                    //Console.WriteLine("cmd naar octave: ErrorPlot(true, '');");
                    octaveProcess_ErrorPlot.ExecuteCommandDontWait("ErrorPlot(true, 'IGNORE');");
                    //Console.WriteLine("ErrorPlot cmd verzonden");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                lock (lastErrorTotaalPlotTimeStopwatch)
                {
                    if (lastErrorTotaalPlotTimeStopwatch.ElapsedMilliseconds > errorTotaalPlotTimeDelta_in_MS || !lastErrorTotaalPlotTimeStopwatch.IsRunning)
                    {
                        lastErrorTotaalPlotTimeStopwatch.Restart();
                        Octave octaveProcess_ErrorTotaalPlot = new Octave(pathToOctaveBins, true);// true: omdat er een plot (img) gemaakt moet worden, dus octave moet bv. gnuplot kunnen starten.
                        try
                        {
                            //StartMinimizeThread(octaveProcess_ErrorPlot);
                            octaveProcess_ErrorTotaalPlot.ExecuteCommand("cd '" + octaveBasePath + "'", 30000);

                            //gewoon plotten indien het kan
                            //Console.WriteLine("cmd naar octave: ErrorPlot(true, '');");
                            octaveProcess_ErrorTotaalPlot.ExecuteCommandDontWait("ErrorTotaalPlot(true, 'IGNORE');");
                            //Console.WriteLine("ErrorPlot cmd verzonden");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }


                try
                {

                    string tracePlotFilenameBase = imgfilename + lasttime;

                    if (!particleFilter.ObservedPatient.RealData)
                    {
                        using (StreamWriter octaveStream = new StreamWriter(tracePlotFilenameBase + ".csv", false /*don't append, but overwrite old*/ ))
                        {
                            // alle patienten zijn toch hetzelfde dus (voorlopig) in 1 output voor octave
                            // let particle filter log stuff (statistics, results, etc) to the list
                            octaveStream.WriteLine("VIP|ML,    \t" + particleFilter.ObservedPatient.Model.ToCSVHeader() + ",\tRMSE_real,\tRMSE_smoothed,\tRMSE_noisy");
                            octaveStream.WriteLine("VIP,       \t" + particleFilter.ObservedPatient.Model.ToCSV() + ",\t0,\t0,\t0");
                            for (int i = 0; i < particleFilter.subPopuluaties.Count; i++)
                            {
                                Particle part = particleFilter.subPopuluaties[i].BestParticle;
                                if (part == null)
                                {
                                    octaveStream.WriteLine("ML_sub(" + i + ") ===> exploreren");
                                }
                                else
                                {
                                    octaveStream.WriteLine("ML_sub(" + i + "),\t" + part.ModelParametersToCSV() + ",\t" + OctaveStuff.MyFormat(part.RMSE_ML2Real_ALLEEN_VOOR_REFERENTIE) + ",\t" + OctaveStuff.MyFormat(part.RMSE_ML2Smooothed) + ",\t" + OctaveStuff.MyFormat(part.RMSE_ML2Mmnts));
                                }
                            }

                            Particle bestpart = particleFilter.BestParticle;
                            octaveStream.WriteLine("ML_best[" + bestpart.SubPopulatieInfo + "],\t" + bestpart.ModelParametersToCSV() + ",\t" + OctaveStuff.MyFormat(bestpart.RMSE_ML2Real_ALLEEN_VOOR_REFERENTIE) + ",\t" + OctaveStuff.MyFormat(bestpart.RMSE_ML2Smooothed) + ",\t" + OctaveStuff.MyFormat(bestpart.RMSE_ML2Mmnts));
                        }
                    }

                    //                    string cmd = "TracePlot(true, '" + tracePlotFilenameBase + ".png" + "');";
                    {
                        string cmd = "TracePlot(true, true);";
                        //Console.WriteLine("cmd naar octave: " + cmd);
                        Octave octaveProcess_TracePlot = new Octave(pathToOctaveBins, true);
                        octaveProcess_TracePlot.ExecuteCommand("cd '" + octaveDataPatientFolder + "'", 30000);
                        octaveProcess_TracePlot.ExecuteCommandDontWait(cmd);
                    }
                    {
                        string cmd = "TracePlot(true, false);";
                        //Console.WriteLine("cmd naar octave: " + cmd);
                        Octave octaveProcess_TracePlot = new Octave(pathToOctaveBins, true);
                        octaveProcess_TracePlot.ExecuteCommand("cd '" + octaveDataPatientFolder + "'", 30000);
                        octaveProcess_TracePlot.ExecuteCommandDontWait(cmd);
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                //                    Console.WriteLine("klaar met octave plot commands");
            }
            catch
            {
                Console.WriteLine("iets met octave plot ging fout");
                // //herstarten dan maar
                //  octaveProcess_ErrorPlot = new Octave(@"C:\Octave\Octave-4.4.1\bin", true);// true: omdat er een plot (img) gemaakt moet worden, dus octave moet bv. gnuplot kunnen starten.
                //   octaveProcess_TracePlot = new Octave(@"C:\Octave\Octave-4.4.1\bin", true);
            }
        }




        private bool eersteKeer = true;
        private void MaakPlotScripts(string octaveDataFolder)
        {
            if (eersteKeer)
            {
                // dit moet echt maar 1x gebeuren per logger, omdat je anders als je de .m in octave (of notepad++ oid) 
                // open hebt, je elke zoveel seconden een refresh krijgt. De file hoort ook niet te veranderen.
                eersteKeer = false;
                MaakTracePlotScript(octaveDataFolder);
                MaakErrorPlotScript(octaveDataFolder);
            }
            MaakErrorTotaalPlotScript(octaveDataFolder);
        }

        private void MaakTracePlotScript(string octaveDataFolder)
        {

            string octaveDataFileFullNameBase = octaveDataFolder + "/patient_" + particleFilter.PATIENT_INDEX + "/";
            string imgfilename = octaveDataFileFullNameBase + "traceplots/TRAINOFEVAL/tracePlot_"; //in octave TracePlot gebruikt   // train --> eval.

            // trace plot code:
            try
            {
                // geen reden om de master files te herschrijven. Daar verandert toch niks, tenzij er een nieuwe patient trace + error zijn.

                using (StreamWriter octaveStream = new StreamWriter(octaveDataFileFullNameBase + "ParticleFilterSettings.m", false))
                {
                    // let particle filter log stuff (statistics, results, etc) to the list
                    octaveStream.WriteLine("## particle filtering for patient " + particleFilter.PATIENT_INDEX + " ##");
                    octaveStream.WriteLine(particleFilter.settingsLogger.ToString());
                }

                if (!particleFilter.ObservedPatient.RealData)
                {
                    using (StreamWriter octaveStream = new StreamWriter(octaveDataFileFullNameBase + "VipSettings.csv", false))
                    {
                        octaveStream.WriteLine("## particle filtering for patient " + particleFilter.PATIENT_INDEX + " ##");
                        octaveStream.WriteLine(particleFilter.ObservedPatient.Model.ToCSVHeader());
                        octaveStream.WriteLine(particleFilter.ObservedPatient.Model.ToCSV());
                    }
                }


                using (StreamWriter octaveStream = new StreamWriter(octaveDataFileFullNameBase + "TracePlot.m", false))
                {
                    octaveStream.WriteLine("function imgfilename = TracePlot(varargin)");
                    // some general stuff:
                    MatlabCodeGeneralStuff(octaveStream, CommentsForFileName, datestring);//, isLast);
                    octaveStream.WriteLine("");
                    //octaveStream.WriteLine("dirnaam = \"" + octaveDataFileFullNameBase + "\"; # deze aanpassen als de folder verplaatst is");
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("# LOAD DATA from CSV");
                    octaveStream.WriteLine("csv_data = {};");
                    octaveStream.WriteLine("trace_filename = [pwd, \"/Trace_p.csv\"];");
                    octaveStream.WriteLine("csv_data = dlmread(trace_filename, \",\", 1, 0);");

                    // van data cell naar array en avg.:
                    List<string> traceToCSV_varnamen = ParticleFilter.VARNAMEN_LIJST;
                    foreach (string varnaam in traceToCSV_varnamen)
                    {
                        int matlab_ndx = GetMatlabIndexInFullList(traceToCSV_varnamen, varnaam);
                        octaveStream.WriteLine("DATA_" + varnaam + " = csv_data(:," + matlab_ndx + ");");
                    }
                    octaveStream.WriteLine("clear(\"csv_data\"); # niet meer nodig, maar het geheugen wel :-)");

                    // niet nan tijd:
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("# events, carb, ins:");
                    octaveStream.WriteLine("# nan eruit");
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("if (~exist('maxTijdVoorFilename', 'var'))");
                    octaveStream.WriteLine("    maxTijdVoorFilename = max(DATA_time);");
                    octaveStream.WriteLine("end");
                    //if (particleFilter.ObservedPatient.RealData)
                    //{
                    //    // de realGluc bestaat hier niet...
                    //    octaveStream.WriteLine("ndxNotNan = ~isnan(DATA_BestPredictedGlucose);");
                    //}
                    //else
                    //{
                    //    octaveStream.WriteLine("ndxNotNan = ~isnan(DATA_RealGlucose);");
                    //}
                    octaveStream.WriteLine("tijd_ndx = (DATA_time >= DATA_time(end) - START_TRACE_LENGTH) & (DATA_time <= DATA_time(end) - END_TRACE_LENGTH) & (DATA_time <= maxTijdVoorFilename);");
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("dezePatientTime = DATA_time(tijd_ndx);");
                    octaveStream.WriteLine("");
                   // octaveStream.WriteLine("# wat als eerste tijd behoort bij NAN in gluc. graph...? Eerste geldige tijd opzoeken en tijd_ndx inperken");
                   // octaveStream.WriteLine("eerste_tijd_in_gluc = dezePatientTime_NotNan(1);");
                   // octaveStream.WriteLine("tijd_ndx = tijd_ndx & (DATA_time >= eerste_tijd_in_gluc);");
                    octaveStream.WriteLine("dezePatientTime = dezePatientTime * TIJD_CONVERSIE; #naar dagen omrekenen");
                    octaveStream.WriteLine("");
                   // octaveStream.WriteLine("# measured, noisy, predicted etc.. kunnen nan zijn als RealGluc dat niet is,");
                   // octaveStream.WriteLine("# omdat er minder vaak gemeten wordt dan dat er tijden + VIP berekende waardes worden gelogd.");
                  //  octaveStream.WriteLine("ndxNotNanMmnts = ~isnan(DATA_BestPredictedGlucose);");
                   // octaveStream.WriteLine("ndxNotNanMmnts = ndxNotNanMmnts & tijd_ndx;");
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("DATA_RealGlucose = DATA_RealGlucose(tijd_ndx);");
                    octaveStream.WriteLine("DATA_MeasuredGlucose = DATA_MeasuredGlucose(tijd_ndx);");
                    octaveStream.WriteLine("DATA_ActivityZ_Real = DATA_ActivityZ_Real(tijd_ndx);");
                    octaveStream.WriteLine("DATA_ActivityZ_ML = DATA_ActivityZ_ML(tijd_ndx);");
                    octaveStream.WriteLine("DATA_ActivityGamma_Real = DATA_ActivityGamma_Real(tijd_ndx);");
                    octaveStream.WriteLine("DATA_ActivityGamma_ML = DATA_ActivityGamma_ML(tijd_ndx);"); octaveStream.WriteLine("DATA_BestPredictedGlucose = DATA_BestPredictedGlucose(tijd_ndx);");
                    octaveStream.WriteLine("DATA_BestPredictedGlucoseWithOrigSchedule = DATA_BestPredictedGlucoseWithOrigSchedule(tijd_ndx);");
                    octaveStream.WriteLine("DATA_smoothedNoisyGlucose = DATA_smoothedNoisyGlucose(tijd_ndx);");
                    octaveStream.WriteLine("DATA_smoothedNoisyGlucoseRico = DATA_smoothedNoisyGlucoseRico(tijd_ndx);");
                    octaveStream.WriteLine("DATA_smoothedNoisyGlucoseRicoRico = DATA_smoothedNoisyGlucoseRicoRico(tijd_ndx);");
                    octaveStream.WriteLine("DATA_productRicoXRico = DATA_productRicoXRico(tijd_ndx);");

                    
                    octaveStream.WriteLine("# data voor inputs (food,ins.) plot:");
                    octaveStream.WriteLine("dezePatientTime = DATA_time(tijd_ndx) * TIJD_CONVERSIE;");
                    octaveStream.WriteLine("Insulin = DATA_Insulin(tijd_ndx);");
                    octaveStream.WriteLine("NoisyFood = DATA_NoisyFood(tijd_ndx);");
                    octaveStream.WriteLine("RealFood = DATA_RealFood(tijd_ndx);");
                    octaveStream.WriteLine("BestPredictedFood = DATA_BestPredictedFood(tijd_ndx);");
                    octaveStream.WriteLine("HR = DATA_HR(tijd_ndx);");






                    //helaas lukt echt 'invisible' niet (kan alleen met qt maar dat werkt weer niet vanaf octave-cli.exe :-(
                    octaveStream.WriteLine("\n\n\nif invisibleAndSave");
                    octaveStream.WriteLine("#   # fltk werkt NIET invisible;   gnuplot en qt wel. gnuplot geeft fijnere/dunnere lijnen. qt is grof");
                    octaveStream.WriteLine("    graphics_toolkit gnuplot;"); //geen enkele plot werkt met invisible vanuit cli  :-(
                                                                             //octaveStream.WriteLine("    figure_handle = figure('visible', false);"); //op deze manier krijgt TracePlot altijd titel 'Figure 2' van gnuplot
                    octaveStream.WriteLine("    figure_handle = figure( 'position', get(0, 'screensize') +[0, 75, 0, -175], 'visible', false);");
                    octaveStream.WriteLine("    set(0, 'defaulttextfontsize', 24) % title");
                    octaveStream.WriteLine("    set(0, 'defaultaxesfontsize', 16) % axes labels");
                    octaveStream.WriteLine("    set(0, 'defaulttextfontname', 'Arial')");
                    octaveStream.WriteLine("    set(0, 'defaultaxesfontname', 'Arial')");
                    octaveStream.WriteLine("    set(0, 'defaultlinelinewidth', 2)");
                    octaveStream.WriteLine("else");
                    octaveStream.WriteLine("    graphics_toolkit fltk; # qt is mooier (en werkt wel 'invisible') maar trager");
                    octaveStream.WriteLine("    figure_handle = figure( 'position', get(0, 'screensize') +[0, 75, 0, -175], 'visible', true);");
                    octaveStream.WriteLine("end\n\n");

                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("mysubplot(3, 1,1:2, 1);");
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("# gemeten G (ruizig), rood, .\'.\'.");
                    octaveStream.WriteLine("plotNotNan(dezePatientTime, DATA_MeasuredGlucose / conversieNaarMmolL, 'marker', 'o', 'color', rood, 'markerfacecolor', rood, 'linestyle', 'none', 'markersize', 4);");
                    octaveStream.WriteLine("hold on;");
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("# beste ML (blauw) met carb hypotheses:  -");
                    octaveStream.WriteLine("plotNotNan(dezePatientTime, DATA_BestPredictedGlucose / conversieNaarMmolL, 'color', helderBlauw, 'marker', 'none', 'linewidth', 4);");
                    
                   
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("# echte G (volgens VIP berekening), oranje: -");
                    octaveStream.WriteLine("plotNotNan(dezePatientTime, DATA_RealGlucose / conversieNaarMmolL, 'marker', 'none', 'color', oranje, 'markerfacecolor', oranje, 'linewidth', 5);");
                    octaveStream.WriteLine("");

                    if(particleFilter.ObservedPatient.RealData)
                    {
                        octaveStream.WriteLine("## smoothed versie van noisy mmnts: ORANJE (omdat we toch geen ground truth VIP model hebben)");
                        octaveStream.WriteLine(" plotNotNan(dezePatientTime, DATA_smoothedNoisyGlucose / conversieNaarMmolL, 'color', oranje, 'marker', 'none',  'linewidth', 4, 'linestyle', '-');");
                    }
                    else
                    {
                        octaveStream.WriteLine("## smoothed versie van noisy mmnts: groen,  rico");
                        octaveStream.WriteLine(" plotNotNan(dezePatientTime, DATA_smoothedNoisyGlucose / conversieNaarMmolL, 'color', groen, 'marker', 'none',  'linewidth', 4, 'linestyle', '-');");
                    }
                    octaveStream.WriteLine("# plotNotNan(dezePatientTime, 50 + 100 * maxNan(0, DATA_smoothedNoisyGlucoseRico) / conversieNaarMmolL, 'color', blauw, 'marker', 'none', 'markersize', 5, 'markerfacecolor', blauw, 'linewidth', 2, 'linestyle', '-');");
                    octaveStream.WriteLine("# plotNotNan(dezePatientTime, 50 + 10000 * minNan(0, DATA_smoothedNoisyGlucoseRicoRico) / conversieNaarMmolL, 'color', paars, 'marker', 'none', 'markersize', 5, 'markerfacecolor', paars, 'linewidth', 2, 'linestyle', '-');");
                    octaveStream.WriteLine("# plotNotNan(dezePatientTime, 50 + 10000 * DATA_productRicoXRico / conversieNaarMmolL, 'color', rood, 'marker', 'none', 'markersize', 5, 'markerfacecolor', rood, 'linewidth', 5, 'linestyle', '-');");
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("# Z");
                    octaveStream.WriteLine("# activityZ_schaal = GLUCOSE_MAX_SCALE/conversieNaarMmolL;");
                    octaveStream.WriteLine("#plotNotNan(dezePatientTime, activityZ_schaal * DATA_ActivityZ_Real, 'color', rood , 'marker', 'none', 'linewidth', 3, 'linestyle', '-');");
                    octaveStream.WriteLine("#plotNotNan(dezePatientTime, activityZ_schaal * DATA_ActivityZ_ML, 'color', zwart, 'marker', 'none', 'linewidth', 4, 'linestyle', '-');");
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("# beste ML met real carb schedule (zwart):  -");
                    octaveStream.WriteLine("plot(dezePatientTime, DATA_BestPredictedGlucoseWithOrigSchedule / conversieNaarMmolL, 'color', zwart, 'marker', '*', 'markersize', 4, 'linewidth', 2);");
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("");

                    octaveStream.WriteLine("# base lines voor target/low etc... ");
                    octaveStream.WriteLine("line([dezePatientTime(1), dezePatientTime(end)], [70, 70], 'color', 'black', 'linewidth', 1);");
                    octaveStream.WriteLine("line([dezePatientTime(1), dezePatientTime(end)], [50, 50], 'color', 'black', 'linewidth', 3);");
                    octaveStream.WriteLine("");

                    octaveStream.WriteLine("# ins als vertikale groene lijn:");
                    octaveStream.WriteLine("ndx_ins_not_nan = find(~isnan(Insulin));");
                    octaveStream.WriteLine("for n = 1:numel(ndx_ins_not_nan)");
                    octaveStream.WriteLine("  ndx = ndx_ins_not_nan(n);");
                    octaveStream.WriteLine("  line([dezePatientTime(ndx), dezePatientTime(ndx)], [0, GLUCOSE_MAX_SCALE / conversieNaarMmolL], 'color', groen, 'linewidth', 1, 'linestyle', ':') ;");
                    octaveStream.WriteLine("end#for");
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("");


                    octaveStream.WriteLine("set(gca, 'xlim', [dezePatientTime(1), dezePatientTime(end)]);");
                    octaveStream.WriteLine("set(gca, 'ylim', [0, GLUCOSE_MAX_SCALE/conversieNaarMmolL] );");
                    octaveStream.WriteLine("xlabel(['time [day], <', sprintf('%.1f', START_TRACE_LENGTH/(24*60)), ' to ', sprintf('%.1f', END_TRACE_LENGTH/(60*24)),'> days ago']);");
                    octaveStream.WriteLine("ylabel(['Blood glucose ', conversieNaarMmoLeenheid]);");
                    octaveStream.WriteLine("CleanupGraph(gca, gcf, 'grid', 'xy');");
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("# alternatieve eenheid ");
                    octaveStream.WriteLine(" yticks = get(gca, 'ytick');");
                    octaveStream.WriteLine("yticklabels = get(gca, 'yticklabel');");
                    octaveStream.WriteLine("for n = 1:numel(yticks)");
                    octaveStream.WriteLine("  if (MG_PER_DL)");
                    octaveStream.WriteLine("    alttxt = sprintf('%.1f', yticks(n) / 18);");
                    octaveStream.WriteLine("  else");
                    octaveStream.WriteLine("    alttxt = sprintf('%.0f', yticks(n) * 18);");
                    octaveStream.WriteLine("  end #if ");
                    octaveStream.WriteLine("   yticklabels{n} = [yticklabels{n}, '  (', alttxt, ')'];");
                    octaveStream.WriteLine(" end # for");
                    octaveStream.WriteLine(" set(gca, 'yticklabel', yticklabels);");
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("");



                    // carb & ins:
                    octaveStream.WriteLine("mysubplot(3, 1, 3, 1);");
                    octaveStream.WriteLine("# schaling goed krijgen met fake datapunt op y-as. TODO: onzichtbaar maken?");
                    octaveStream.WriteLine("maxfood = max( [160; RealFood; NoisyFood; BestPredictedFood] );");
                    octaveStream.WriteLine("RealFood(1) = maxfood; BestPredictedFood(1) = maxfood; NoisyFood(1) = maxfood; HR(1) = maxfood;");


                    octaveStream.WriteLine("# groen: insuline, zwart: ML schatting van carbs");
                    octaveStream.WriteLine("# oranje vierkantje: ECHTE carb waarde");
                    octaveStream.WriteLine("# rood rondje: ruisige carbs opgegeven door patient");

                    octaveStream.WriteLine("[scale_factor1, scale_factor2] = myyyplot(dezePatientTime, 'time [day]',  BestPredictedFood, 'carbs [g]', zwart, 'o',    Insulin, 'insulin [IU]', groen, '>',     1, 'lines', '', 'spikes', '12', 'linewidth', 0, 'markersize', 13, 'logscale', false );");
                    octaveStream.WriteLine("hold on;");
                    octaveStream.WriteLine("set(gca,'xtick',[]);");
                    octaveStream.WriteLine("set(gca, 'xlabel', '');");

                    octaveStream.WriteLine("");

                    octaveStream.WriteLine("plot(dezePatientTime, scale_factor1 * RealFood, 'color', zwart, 'marker', 's', 'markersize', 15, 'markerfacecolor', oranje);");

                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("plot(dezePatientTime, scale_factor1 * NoisyFood, 'color', rood, 'marker', 'o', 'markersize', 21, 'markerfacecolor', rood);");


                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("# Z");
                    octaveStream.WriteLine("activityZ_schaal = maxfood;");
                    octaveStream.WriteLine("plot(dezePatientTime, activityZ_schaal * DATA_ActivityZ_Real, 'color', rood, 'marker', 'none', 'linewidth', 3, 'linestyle', '-');");
                    octaveStream.WriteLine("plot(dezePatientTime, activityZ_schaal * DATA_ActivityZ_ML, 'color', zwart, 'marker', 'none', 'linewidth', 5, 'linestyle', '-');");

                    octaveStream.WriteLine("");

                    octaveStream.WriteLine("# HR");
                    octaveStream.WriteLine("hrlinestyle = '-';");
                    octaveStream.WriteLine("plot(dezePatientTime, scale_factor1 * HR, 'color', geel, 'linestyle', hrlinestyle,   'marker', '.', 'markersize', 2, 'markerfacecolor', geel);");
                    octaveStream.WriteLine("plot(dezePatientTime, scale_factor1 * DATA_ActivityGamma_ML, 'color', donkerGrijs, 'linestyle', hrlinestyle,   'marker', 'o', 'markersize', 3, 'markerfacecolor', donkerGrijs);");
                    octaveStream.WriteLine("plot(dezePatientTime, scale_factor1 * DATA_ActivityGamma_Real, 'color', blauw, 'linestyle', hrlinestyle,   'marker', 'none', 'markersize', 1, 'markerfacecolor', blauw);");

                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("# volgorde aanpassen, zodat de grafieken elkaar minder overlappen");
                    octaveStream.WriteLine("# https://stackoverflow.com/questions/7674700/how-to-change-the-order-of-lines-in-a-matlab-figure");
                    octaveStream.WriteLine("chH = get(gca, 'Children');");
                    octaveStream.WriteLine("set(gca, 'Children', flipud(chH));");



                    //octaveStream.WriteLine("# https://stackoverflow.com/questions/7674700/how-to-change-the-order-of-lines-in-a-matlab-figure");
                    //octaveStream.WriteLine("# gca.Children=gca.Children([end:-1:1]); ");
                    //octaveStream.WriteLine("chH = get(gca, 'Children');");
                    //octaveStream.WriteLine("set(gca, 'Children', flipud(chH));");

                 
                    octaveStream.WriteLine("if invisibleAndSave");
                    octaveStream.WriteLine("    # saveas(figure_handle, imgfilename);  ");
                    octaveStream.WriteLine("    if show_eval_or_train");
                    octaveStream.WriteLine("      eval_or_train_trail = 'EVAL';");
                    octaveStream.WriteLine("    else");
                    octaveStream.WriteLine("      eval_or_train_trail = 'train';");
                    octaveStream.WriteLine("    end");
                    octaveStream.WriteLine("    imgfilename_base = strrep('" + imgfilename + "', 'TRAINOFEVAL', eval_or_train_trail);");
                    octaveStream.WriteLine("    imgfilename = [imgfilename_base, int2str(maxTijdVoorFilename),'_', eval_or_train_trail , '.png'];");
                    octaveStream.WriteLine("    imgfilename_current = [imgfilename_base, eval_or_train_trail, '_current.png'];");
                    


                    octaveStream.WriteLine("    disp(['saving to image ', imgfilename, ' ...']);");
                    // octaveStream.WriteLine("    set(figure_handle, \"visible\", false);");
                    octaveStream.WriteLine("    print(imgfilename, '-r100',  '-S1900,1000');");
                    octaveStream.WriteLine("    print(imgfilename_current, '-r100',  '-S1900,1000');");
                    octaveStream.WriteLine("    print(['C:/Lectoraat/Matlab/LectoraatOctaveData/temp/traceplot_',eval_or_train_trail,'_current.png'], '-r100',  '-S1900,1000');");
                    octaveStream.WriteLine("    close all;");
                    // octaveStream.WriteLine("    copyfile(imgfilename, 'C:/Lectoraat/Matlab/LectoraatOctaveData/temp/traceplot_current.png', 'f');");
                    // octaveStream.WriteLine("    copyfile(imgfilename, imgfilename_current, 'f');");
                    // octaveStream.WriteLine("    #restore");
                    //  octaveStream.WriteLine("    graphics_toolkit fltk; # terug naar de 'default' snel");
                    octaveStream.WriteLine("    exit # dit termineert de octave sessie!");
                    octaveStream.WriteLine("end");

                    octaveStream.WriteLine("end #function");
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("");


                    octaveStream.WriteLine("function plotNotNan(x, y, varargin)");
                    octaveStream.WriteLine("  idx_not_nan = ~isnan(y);");
                    octaveStream.WriteLine("  plot(x(idx_not_nan), y(idx_not_nan), varargin{:} );");
                    octaveStream.WriteLine("endfunction");

                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("function res = maxNan(x, y)");
                    octaveStream.WriteLine("  res = max(x, y);");
                    octaveStream.WriteLine(" idx_nan = isnan(x) | isnan(y);");
                    octaveStream.WriteLine("  res(idx_nan) = NaN;");
                    octaveStream.WriteLine("endfunction");
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("function res = minNan(x, y)");
                    octaveStream.WriteLine("  res = min(x, y);");
                    octaveStream.WriteLine("  idx_nan = isnan(x) | isnan(y);");
                    octaveStream.WriteLine("  res(idx_nan) = NaN;");
                    octaveStream.WriteLine("endfunction");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("ParticleFilterDataLogger::MaakErrorPlotScript() :: caught Exception " + e + "\n" + e.Message + "\n" + e.StackTrace);
            }
        }



        private void MaakErrorPlotScript(string octaveDataFolder)
        {
            string octaveDataFileFullNameBase = octaveDataFolder + "/patient_" + particleFilter.PATIENT_INDEX + "/";
            string errimgfilename = octaveDataFileFullNameBase + "traceplots/errorPlot_";

            try
            {
                // 'master' octave file voor plotten schrijven	
                using (StreamWriter octaveStream = new StreamWriter(octaveDataFileFullNameBase + "ErrorPlot.m", false /*don't append, but overwrite old*/ ))
                {
                    octaveStream.WriteLine("function ErrorPlot(varargin)");
                    MatlabCodeGeneralStuff(octaveStream, CommentsForFileName, datestring);//, isLast);

                    octaveStream.WriteLine("");
                    //octaveStream.WriteLine("dirnaam = \"" + octaveDataFileFullNameBase + "\"; # deze aanpassen als de folder verplaatst is");
                    octaveStream.WriteLine("");
                    octaveStream.WriteLine("# LOAD DATA from CSV");
                    octaveStream.WriteLine("trace_filename = [pwd, \"/Errors_p.csv\"];");
                    octaveStream.WriteLine("csv_data = dlmread(trace_filename, \",\", 1, 0);");

                    // van data cell naar array en avg.:
                    foreach (string varnaam in shortlist)
                    {
                        int matlab_ndx = GetMatlabIndexInFullList(fulllijst, varnaam);
                        octaveStream.WriteLine("DATA_" + varnaam + " = csv_data(:," + matlab_ndx + ");");
                        try
                        {
                            matlab_ndx = GetMatlabIndexInFullList(fulllijst, varnaam + "_orig");
                            octaveStream.WriteLine("DATA_" + varnaam + "_orig = csv_data(:," + matlab_ndx + ");");
                        }
                        catch (ArgumentException) { }
                    }
                    octaveStream.WriteLine("");

                    // errors per subpopulatie inlezen.
                    octaveStream.WriteLine("# errors per sub");
                    octaveStream.WriteLine("ml2noisy_rmse_per_sub = {};");
                    octaveStream.WriteLine("ml2real_rmse_per_sub = {};");
                    //eerste var ndx bepalen, rest is opvolgend. Splitsen naar noisy en real data.
                    int first_matlab_ndx = GetMatlabIndexInFullList(fulllijst, diversityKeys[0]);
                    int last_matlab_ndx = GetMatlabIndexInFullList(fulllijst, diversityKeys[diversityKeys.Count - 2]);
                    octaveStream.WriteLine("teller = 1;");
                    octaveStream.WriteLine("for n=" + first_matlab_ndx + ":2:" + last_matlab_ndx);
                    octaveStream.WriteLine("\tml2noisy_rmse_per_sub{teller,1} = csv_data(:,n);");
                    octaveStream.WriteLine("\tml2real_rmse_per_sub{teller,1} = csv_data(:,n + 1);");
                    octaveStream.WriteLine("teller = teller + 1;");
                    octaveStream.WriteLine("end#for");
                    octaveStream.WriteLine();

                    octaveStream.WriteLine("clear(\"csv_data\"); # niet meer nodig, maar het geheugen wel :-)");
                    octaveStream.WriteLine("");

                    // en de tijd :
                    octaveStream.WriteLine("# time:");
                    octaveStream.WriteLine("nietnan = ~isnan(DATA_time);");
                    octaveStream.WriteLine("dezePatientTime_NotNan = DATA_time(nietnan);");
                    octaveStream.WriteLine("dezePatientTime_NotNan = dezePatientTime_NotNan / (60 * 24); # naar dagen");

                    octaveStream.WriteLine("clear(\"deze_csv\"); # niet meer nodig, maar het geheugen wel :-)");
                    octaveStream.WriteLine("clear(\"csv_data\");");

                    // alle patienten zijn toch hetzelfde dus (voorlopig) in 1 output voor octave
                    // let particle filter log stuff (statistics, results, etc) to the list
                    octaveStream.WriteLine("## particle filtering for patient " + particleFilter.PATIENT_INDEX + " ##");
                    octaveStream.WriteLine(particleFilter.settingsLogger.ToString());

                    octaveStream.WriteLine("");

                    // algemene plotcode:
                    //  List<string> plotCodeResults = this.OctaveMsePlotCode();
                    //   foreach (string txt in plotCodeResults) { octaveStream.WriteLine(txt); }

                    // plot code voor errors:
                    //bool usedRealData = particleFilter.ObservedPatient.RealData;
                   // if (!usedRealData)
                    {
                        // continue stream van generated data
                        octaveStream.WriteLine("if true   # 'false' to disable this plot. Only for simulated data ");
                        // todo: dit gebeurt maar eenmalig, ergens anders (??) aantal calc. in titel krijgen
                        octaveStream.WriteLine("  TITEL = [ engn(DATA_nrOfCalc(end)), ' calc'];");

                        //helaas lukt echt 'invisible' niet (kan alleen met qt maar dat werkt weer niet vanaf octave-cli.exe :-(
                        octaveStream.WriteLine("\n\n\nif invisibleAndSave");
                        octaveStream.WriteLine("    # fltk werkt NIET invisible;   gnuplot en qt wel. gnuplot geeft fijnere/dunnere lijnen. qt is grof");
                        octaveStream.WriteLine("    graphics_toolkit gnuplot;"); //geen enkele plot werkt met invisible vanuit cli  :-(
                        octaveStream.WriteLine("    figure_handle = figure( 'position', get(0, 'screensize') +[0, 75, 0, -175], 'visible', false);");
                        octaveStream.WriteLine("    set(0, 'defaulttextfontsize', 24) % title");
                        octaveStream.WriteLine("    set(0, 'defaultaxesfontsize', 16) % axes labels");
                        octaveStream.WriteLine("    set(0, 'defaulttextfontname', 'Arial')");
                        octaveStream.WriteLine("    set(0, 'defaultaxesfontname', 'Arial')");
                        octaveStream.WriteLine("    set(0, 'defaultlinelinewidth', 2)");
                        octaveStream.WriteLine("else");
                        octaveStream.WriteLine("    graphics_toolkit fltk; # qt is mooier (en werkt wel 'invisible') maar trager");
                        octaveStream.WriteLine("    figure_handle = figure( 'position', get(0, 'screensize') +[0, 75, 0, -175], 'visible', true);");
                        octaveStream.WriteLine("end\n\n");

                        octaveStream.WriteLine("  xbegin = dezePatientTime_NotNan(1);");
                        octaveStream.WriteLine("  xlimit = dezePatientTime_NotNan(end);");

                        octaveStream.WriteLine("  ## subplot(5,2,[1,3,5,7,9]);  # volledige plothoogte");
                        octaveStream.WriteLine("  mysubplot(7, 1, 1:4, 1);");

                        octaveStream.WriteLine("  # linestyle moet solid of 'none' zijn, omdat we gnuplot gebruiken voor rendering, en die geeft veeeel te veel ruimte tussen de dots/dashes ");
                        octaveStream.WriteLine("  # van niet solid lijn! Maar wel veel hogere resolutie mooiere plots");
                        octaveStream.WriteLine("");

                        //diversiteit:
                        octaveStream.WriteLine("  # sub errors:");
                        octaveStream.WriteLine("  for n=1:" + particleFilter.settingsForParticleFilter.NumberOfSubPopulations);
                        octaveStream.WriteLine("     # indicatie dat hier nieuwe subpop na expl. begint.");
                        octaveStream.WriteLine("     #  De NaN voor de nieuwe data op 10000 zetten");
                        octaveStream.WriteLine("     tmpdiv = ml2noisy_rmse_per_sub{ n};");
                        octaveStream.WriteLine("     tmpdiv_indices = 1:numel(tmpdiv);");
                        octaveStream.WriteLine("     ndx_tmpdiv_nan = find(~isnan(tmpdiv));");
                        octaveStream.WriteLine("     ndx_tmpdiv_nan_diff = diff(ndx_tmpdiv_nan);");
                        octaveStream.WriteLine("     ndx_dif_groter_dan_1 = find(ndx_tmpdiv_nan_diff > 1);");
                        octaveStream.WriteLine("     indices = tmpdiv_indices(ndx_dif_groter_dan_1);");
                        octaveStream.WriteLine("     indices = indices + 1;");
                        octaveStream.WriteLine("     entry_points = ndx_tmpdiv_nan(indices);");
                        octaveStream.WriteLine("     tmpdiv(entry_points - 1) = 10000;");
                        octaveStream.WriteLine("     plot(dezePatientTime_NotNan, tmpdiv / conversieNaarMmolL, 'linestyle', '-', 'linewidth', 3); hold on;");
                        octaveStream.WriteLine("  end #for");
                        octaveStream.WriteLine("");
                        octaveStream.WriteLine("");

                        octaveStream.WriteLine("  plot(dezePatientTime_NotNan, DATA_rmseNoisy/conversieNaarMmolL, 'linestyle', 'none', 'color', rood, 'linewidth', 6, 'marker', 'o', 'markersize', 5, 'markerfacecolor', rood); hold on;");
                        octaveStream.WriteLine("  plot(dezePatientTime_NotNan, DATA_rmseReal/conversieNaarMmolL, 'linestyle', 'none','color',  groen, 'linewidth', 6, 'marker', 'o', 'markersize',  5, 'markerfacecolor', groen); hold on;");
                        if (particleFilter.ObservedPatient.RealData)
                        {
                            // de real_orig is toch altijd nan, want er is geen real bekend!
                            octaveStream.WriteLine("  plot(dezePatientTime_NotNan, DATA_rmseNoisy_orig/conversieNaarMmolL, 'linestyle', 'none','color',  zwart, 'linewidth', 6, 'marker', '*', 'markersize',  5, 'markerfacecolor', zwart); hold on;");
                        }
                        else
                        {
                            // zowel noisy als real in grafiek wordt te druk.
                            octaveStream.WriteLine("  plot(dezePatientTime_NotNan, DATA_rmseReal_orig/conversieNaarMmolL, 'linestyle', 'none','color',  zwart, 'linewidth', 6, 'marker', '*', 'markersize',  5, 'markerfacecolor', zwart); hold on;");
                        }
                        octaveStream.WriteLine("");
                        octaveStream.WriteLine("  plot(dezePatientTime_NotNan, DATA_rmseNoisyMmntsTovReal/conversieNaarMmolL, 'linestyle', '-','color',  donkerGroen, 'linewidth', 4, 'marker', 'o', 'markersize', 3); hold on;");
                        octaveStream.WriteLine("");
                        octaveStream.WriteLine("");


                        octaveStream.WriteLine("  ylabel(['RMSE, square root of MSE ', conversieNaarMmoLeenheid]);");
                        octaveStream.WriteLine("  title([TITEL, ':: RMSE of ML wrt noisy (|) and real (:) vip values, and (-) RMSE of noisy Mmnts wrt real vip values']);");
                        octaveStream.WriteLine("  set(gca, 'ylim', [0/conversieNaarMmolL, MAX_RMSE_OP_Y_AS/conversieNaarMmolL]);");
                        octaveStream.WriteLine("  set(gca, 'xlim', [xbegin, xlimit]);");
                        octaveStream.WriteLine("set(gca,'xtick',[]);");
                        octaveStream.WriteLine("set(gca, 'xlabel', '');");
                        octaveStream.WriteLine("  CleanupGraph(gca, gcf, 'grid', 'x');");
                        // new fig:
                        // low~high:
                        octaveStream.WriteLine("  disp('');");
                        octaveStream.WriteLine("  disp('');");
                        octaveStream.WriteLine("  ## subplot(5,2,[2,4]); # small top right plot");
                        octaveStream.WriteLine("  % mysubplot(3, 1, 3, 1);");
                        octaveStream.WriteLine("  mysubplot(7, 1, 5:7, 1);");

                        octaveStream.WriteLine("  ### subplot(5, 2, [2,4,6,8,10]); # full right half plot (remove settings!)");

                        string orig_postfix = "_orig";
                        //at max.
                        octaveStream.WriteLine("  plot(dezePatientTime_NotNan, DATA_realHigherThanPredictedAtMaximum"+ orig_postfix + "/conversieNaarMmolL, 'color', oranje, 'linewidth', 3, 'linestyle', '-'); hold on;");
                        octaveStream.WriteLine("  plot(dezePatientTime_NotNan, DATA_realLowerThanPredictedAtMaximum" + orig_postfix + "/conversieNaarMmolL, 'color', oranje, 'linewidth', 3, 'linestyle', '-'); hold on;");
                        //at minimum
                        octaveStream.WriteLine("  plot(dezePatientTime_NotNan, DATA_realLowerThanPredictedAtMinimum" + orig_postfix + "/conversieNaarMmolL, 'color', rood, 'linewidth', 4); hold on;");
                        octaveStream.WriteLine("  plot(dezePatientTime_NotNan, DATA_realHigherThanPredictedAtMinimum" + orig_postfix + "/conversieNaarMmolL, 'color', rood, 'linewidth', 4); hold on;");

                        //rmse tov real ter rereferentie
                        octaveStream.WriteLine("  plot(dezePatientTime_NotNan, DATA_rmseReal" + orig_postfix + "/conversieNaarMmolL, 'linestyle', '-','color',  groen, 'linewidth', 4); hold on;");


                        octaveStream.WriteLine("  line( [0, xlimit], [0, 0], 'color', 'black');"); // plot x-as


                        // octaveStream.WriteLine("  title([TITEL, ':: under/overshoots at high (orange) and low (red)']);");
                        octaveStream.WriteLine("  xlabel('time [day]');");
                        octaveStream.WriteLine("  set(gca, 'ylim', [-40/conversieNaarMmolL, 40/conversieNaarMmolL]);");
                        octaveStream.WriteLine("  set(gca, 'xlim', [xbegin, xlimit]);");
                        //      octaveStream.WriteLine("  xlabel('time [day]');");

                        octaveStream.WriteLine("  ylabel(['difference ', conversieNaarMmoLeenheid]);");
                        octaveStream.WriteLine("  CleanupGraph(gca, gcf, 'grid', 'x');");
                        octaveStream.WriteLine("end;#if");

                        octaveStream.WriteLine("");
                        octaveStream.WriteLine("");
                        //octaveStream.WriteLine("disp(TITEL);");
                        octaveStream.WriteLine("## place textbox with settings in plot");
                        octaveStream.WriteLine("my_text = ['#data = ', int2str(size(DATA_rmseNoisy, 2))];");//my_text = ['#data = ', int2str(size(DATA_noisyErrors, 2))];

                        octaveStream.WriteLine("#for n = 1:numel(settings)");
                        octaveStream.WriteLine("#\tmy_text = [my_text, \"\\n\", settings{ n}];");
                        octaveStream.WriteLine("#\tdisp(settings{n});");
                        octaveStream.WriteLine("#end %for");
                        // dump een annotatie box met de gelogde settings en opmerkingen rechtsonderin
                        octaveStream.WriteLine("annotation('textbox',[0.55 0.01 0.1 0.1], 'string', my_text, 'fontsize', 20, 'edgecolor', 'b', 'linewidth', 1, 'fitboxtotext', 'on');");

                        octaveStream.WriteLine("");
                        octaveStream.WriteLine("");

                        octaveStream.WriteLine("");
                        octaveStream.WriteLine("");


                        octaveStream.WriteLine("if invisibleAndSave");
                        octaveStream.WriteLine("    # saveas(figure_handle, imgfilename);  ");
                        octaveStream.WriteLine("    imgfilename = '" + errimgfilename + ".png';");
                        octaveStream.WriteLine("    disp(['saving to image ', imgfilename, ' ...']);");
                        //    octaveStream.WriteLine("    set(figure_handle, \"visible\", false);");
                        octaveStream.WriteLine("    print(imgfilename, '-r100',  '-S1900,1000');");
                        octaveStream.WriteLine("    print( 'C:/Lectoraat/Matlab/LectoraatOctaveData/temp/errorPlot.png', '-r100',  '-S1900,1000');");
                        // octaveStream.WriteLine("    close(figure_handle);");
                        octaveStream.WriteLine("    close all;");

                        // octaveStream.WriteLine("    copyfile(imgfilename, 'C:/Lectoraat/Matlab/LectoraatOctaveData/temp/errorPlot.png', 'f');");
                        //     octaveStream.WriteLine("    #restore");
                        //     octaveStream.WriteLine("    graphics_toolkit fltk; # terug naar de 'default' snel");
                        octaveStream.WriteLine("    exit # dit termineert de octave sessie!");
                        octaveStream.WriteLine("end");
                    }

                    //   Console.WriteLine("-------> TITEL = '" + datestring + ".m'");
                    octaveStream.WriteLine("end #function");
                }//end using octave file
            }

            catch (Exception e)
            {
                Console.WriteLine("ParticleFilterDataLogger::MaakTracePlotScript() :: caught Exception " + e + "\n" + e.Message + "\n" + e.StackTrace);
            }
        }



        private static readonly object lockObjectVoorTotaalPlot = new object();

        private List<int> prev_patient_indices = new List<int>();
        private void MaakErrorTotaalPlotScript(string octaveDataFolder)
        {
            lock (lockObjectVoorTotaalPlot)
            {
                string totaalErrorPlotScript = octaveDataFolder + "ErrorTotaalPlot.m";

                List<int> gevonden_indices = new List<int>();
                string[] folders_in_folder = Directory.GetDirectories(octaveDataFolder);
                foreach (string folder in folders_in_folder)
                {
                    if (folder.Contains("patient_"))
                    {
                        try
                        {
                            string shortfile = MyFileIO.GetShortFileName(folder).Replace("patient_", "");
                            int nr = Int32.Parse(shortfile);
                            gevonden_indices.Add(nr);
                        }
                        catch
                        {

                        }
                    }
                }

                bool alles_aanwezig = true;
                foreach(int ndx in gevonden_indices)
                {
                    if (!prev_patient_indices.Contains(ndx))
                    {
                        alles_aanwezig = false;
                        break;
                    }
                }
                if(alles_aanwezig)
                {
                    return;
                }
                prev_patient_indices = gevonden_indices;


                string errimgfilename = octaveDataFolder + "/errorTotaalPlot_";
                try
                {
                    // 'master' octave file voor plotten schrijven	
                    using (StreamWriter octaveStream = new StreamWriter(totaalErrorPlotScript, false /*don't append, but overwrite old*/ ))
                    {

                        octaveStream.WriteLine("function ErrorTotaalPlot(varargin)");
                        MatlabCodeGeneralStuff(octaveStream, CommentsForFileName, datestring);//, isLast);

                        
                        string patient_indices = "#  PATIENT_NDX = [" + String.Join(", ", gevonden_indices) + "];";
                        octaveStream.WriteLine(patient_indices);
                        
                        octaveStream.WriteLine("");
                        //octaveStream.WriteLine("dirnaam = \"" + octaveDataFolder + "\"; # deze aanpassen als de folder verplaatst is");
                        octaveStream.WriteLine("");
                        octaveStream.WriteLine("# LOAD DATA from CSVs");

                        // errors per patient inlezen.
                        // todo: ook gem, bounds etc. bepalen?
                        octaveStream.WriteLine("DATA_rmseNoisy_per_patient = { };");
                        octaveStream.WriteLine("DATA_rmseReal_per_patient = { };");
                        octaveStream.WriteLine("dezePatientTime_NotNan_per_patient = { };");

                        octaveStream.WriteLine("DATA_realLowerThanPredictedAtMinimum_per_patient = { };");
                        octaveStream.WriteLine("DATA_realHigherThanPredictedAtMaximum_per_patient = { };");
                        octaveStream.WriteLine("DATA_realLowerThanPredictedAtMaximum_per_patient = { };");
                        octaveStream.WriteLine("DATA_realHigherThanPredictedAtMinimum_per_patient = { };");


                        octaveStream.WriteLine("xbegin = 100000000; ");
                        octaveStream.WriteLine("xlimit = 0; ");


                        octaveStream.WriteLine("teller = 1;");
                        //octaveStream.WriteLine("for patient_ndx_nr = 1:numel(PATIENT_NDX)");
                        //octaveStream.WriteLine("  patient_ndx = PATIENT_NDX(patient_ndx_nr);");



                        string orig_postfix = "_orig";

                        octaveStream.WriteLine("# LOAD DATA from CSV");
                        octaveStream.WriteLine("dirs = readdir(pwd);");
                        octaveStream.WriteLine("for n = 1:size(dirs, 1)");
                        octaveStream.WriteLine("  naam = dirs{ n,1}; ");
                        octaveStream.WriteLine("  if isdir(naam) ");
                        octaveStream.WriteLine("    ndx_find = strfind(naam, \"patient_\"); ");
                        octaveStream.WriteLine("    if (numel(ndx_find) > 0) ");
                        octaveStream.WriteLine("      if (ndx_find(1) == 1) ");
                        octaveStream.WriteLine("         disp(naam); ");

                        octaveStream.WriteLine("         trace_filename = [pwd, \"/\", naam, \"/\",  \"Errors_p.csv\"];");
                        octaveStream.WriteLine("         if (exist(trace_filename, 'file') == 2)");
                        octaveStream.WriteLine("           try");
                        octaveStream.WriteLine("              csv_data = dlmread(trace_filename, \",\", 1, 0);");
                        octaveStream.WriteLine("              DATA_time = csv_data(:, 1);");
                        octaveStream.WriteLine("              DATA_nrOfCalc = csv_data(:, 4);");
                        // nb deze (:,nr_in_csv) gewoon opzoeken uit errors_p.csv
                        // nb deze (:,nr_in_csv) gewoon opzoeken uit errors_p.csv
                        // nb deze (:,nr_in_csv) gewoon opzoeken uit errors_p.csv
                        // nb deze (:,nr_in_csv) gewoon opzoeken uit errors_p.csv

                        octaveStream.WriteLine("              DATA_rmseNoisy_per_patient{ teller, 1} = csv_data(:, " + GetMatlabIndexInFullList(fulllijst, "rmseNoisy" + orig_postfix) + ");");
                        octaveStream.WriteLine("              DATA_rmseReal_per_patient{ teller,1} = csv_data(:, " + GetMatlabIndexInFullList(fulllijst, "rmseReal" + orig_postfix) + ");");
//                        octaveStream.WriteLine("      DATA_rmseReal_orig_per_patient{ teller,1} = csv_data(:, " + GetMatlabIndexInFullList(fulllijst, "rmseReal_orig") + ");");
                        octaveStream.WriteLine("              DATA_realLowerThanPredictedAtMinimum_per_patient{ teller, 1} = csv_data(:, " + GetMatlabIndexInFullList(fulllijst, "realLowerThanPredictedAtMinimum" + orig_postfix) + ");");
                        octaveStream.WriteLine("              DATA_realHigherThanPredictedAtMaximum_per_patient{ teller, 1} = csv_data(:, " + GetMatlabIndexInFullList(fulllijst, "realHigherThanPredictedAtMaximum" + orig_postfix) + ");");
                        octaveStream.WriteLine("              DATA_realLowerThanPredictedAtMaximum_per_patient{ teller, 1} = csv_data(:, " + GetMatlabIndexInFullList(fulllijst, "realLowerThanPredictedAtMaximum" + orig_postfix) + ");");
                        octaveStream.WriteLine("              DATA_realHigherThanPredictedAtMinimum_per_patient{ teller, 1} = csv_data(:, " + GetMatlabIndexInFullList(fulllijst, "realHigherThanPredictedAtMinimum" + orig_postfix) + ");");

                        octaveStream.WriteLine("              DATA_rmseNoisyMmntsTovReal = csv_data(:, 7);");


                        octaveStream.WriteLine("             clear(\"csv_data\"); # niet meer nodig, maar het geheugen wel :-)");
                        octaveStream.WriteLine("             clear(\"deze_csv\"); # niet meer nodig");

                        octaveStream.WriteLine("             # time:");
                        octaveStream.WriteLine("             nietnan = ~isnan(DATA_time); ");
                        octaveStream.WriteLine("             dezePatientTime_NotNan = DATA_time(nietnan); ");
                        octaveStream.WriteLine("             dezePatientTime_NotNan = dezePatientTime_NotNan / (60 * 24); # naar dagen");

                        octaveStream.WriteLine("             dezePatientTime_NotNan_per_patient{ teller, 1} = dezePatientTime_NotNan; ");
                        octaveStream.WriteLine("             xlimit = max(xlimit, dezePatientTime_NotNan(end)); ");
                        octaveStream.WriteLine("             xbegin = min(xbegin, dezePatientTime_NotNan(1)); ");
                        octaveStream.WriteLine("             teller = teller + 1;");
                        octaveStream.WriteLine("           catch");
                        octaveStream.WriteLine("           end_try_catch");
                        octaveStream.WriteLine("        end#if");
                        octaveStream.WriteLine("      end#if");
                        octaveStream.WriteLine("    end#if");
                        octaveStream.WriteLine("  end#if");
                        octaveStream.WriteLine("end #for patient_ndx_nr");

                        // alle patienten zijn toch hetzelfde dus (voorlopig) in 1 output voor octave
                        // let particle filter log stuff (statistics, results, etc) to the list
                        octaveStream.WriteLine("## particle filtering for patient " + particleFilter.PATIENT_INDEX + " ##");
                        octaveStream.WriteLine(particleFilter.settingsLogger.ToString());

                        octaveStream.WriteLine("");

                       
                        // plot code voor errors:
                        bool usedRealData = particleFilter.ObservedPatient.RealData;
                        if (!usedRealData)
                        {
                            octaveStream.WriteLine("  TITEL = [ engn(DATA_nrOfCalc(end)), ' calc'];");
                            octaveStream.WriteLine("");
                            octaveStream.WriteLine("\n\n\nif invisibleAndSave");
                            octaveStream.WriteLine("    # fltk werkt NIET invisible;   gnuplot en qt wel. gnuplot geeft fijnere/dunnere lijnen. qt is grof");
                            octaveStream.WriteLine("    graphics_toolkit gnuplot;"); 
                            octaveStream.WriteLine("    figure_handle = figure( 'position', get(0, 'screensize') +[0, 75, 0, -175], 'visible', false);");
                            octaveStream.WriteLine("    set(0, 'defaulttextfontsize', 24) % title");
                            octaveStream.WriteLine("    set(0, 'defaultaxesfontsize', 16) % axes labels");
                            octaveStream.WriteLine("    set(0, 'defaulttextfontname', 'Arial')");
                            octaveStream.WriteLine("    set(0, 'defaultaxesfontname', 'Arial')");
                            octaveStream.WriteLine("    set(0, 'defaultlinelinewidth', 2)");
                            octaveStream.WriteLine("else");
                            octaveStream.WriteLine("    graphics_toolkit fltk; # qt is mooier (en werkt wel 'invisible') maar trager");
                            octaveStream.WriteLine("    figure_handle = figure( 'position', get(0, 'screensize') +[0, 75, 0, -175], 'visible', true);");
                            octaveStream.WriteLine("end\n\n");
                            octaveStream.WriteLine("");


                            octaveStream.WriteLine("  ## subplot(5,2,[1,3,5,7,9]);  # volledige plothoogte");
                            octaveStream.WriteLine("  mysubplot(7, 1, 1:4, 1);");
                            octaveStream.WriteLine("");
                            octaveStream.WriteLine("## TODO: gemiddelden, upper/lower bounds et");
                            octaveStream.WriteLine("#  # linestyle moet solid zijn, omdat we gnuplot gebruiken voor rendering, en die geeft veeeel te veel ruimte tussen de dots/dashes ");
                            octaveStream.WriteLine("#  # van niet solid lijn! Maar wel veel hogere resolutie mooiere plots");
                            octaveStream.WriteLine("#  plot(dezePatientTime_NotNan, DATA_rmseNoisy/conversieNaarMmolL, 'linestyle', 'none', 'color', rood, 'linewidth', 6, 'marker', 'o', 'markersize', 3); hold on;");
                            octaveStream.WriteLine("#  plot(dezePatientTime_NotNan, DATA_rmseReal/conversieNaarMmolL, 'linestyle', 'none','color',  groen, 'linewidth', 6, 'marker', 'o', 'markersize', 3); hold on;");
                            octaveStream.WriteLine("#  plot(dezePatientTime_NotNan, DATA_rmseNoisyMmntsTovReal/conversieNaarMmolL, 'linestyle', '-','color',  donkerGroen, 'linewidth', 4, 'marker', 'o', 'markersize', 3); hold on;");


                            //alle patienten:
                            octaveStream.WriteLine("  # alle errors:");
                            octaveStream.WriteLine("  for n=1: numel(DATA_rmseReal_per_patient)");

                            octaveStream.WriteLine("     kleur_rood = rood;");
                            octaveStream.WriteLine("     kleur_groen = groen;");
                            octaveStream.WriteLine("     endtime = dezePatientTime_NotNan_per_patient{ n};");
                            octaveStream.WriteLine("     endtime = endtime(end);");
                            octaveStream.WriteLine("     if (endtime < xlimit)");
                            octaveStream.WriteLine("         kleur_rood = oranje;");
                            octaveStream.WriteLine("     kleur_groen = blauw;");
                            octaveStream.WriteLine("     end#if");
                            //octaveStream.WriteLine("     plot(dezePatientTime_NotNan_per_patient{ n}, DATA_rmseNoisy_per_patient{ n} / conversieNaarMmolL, 'linestyle', '-', 'linewidth', 1, 'color', kleur_rood, 'marker', 'none'); hold on;");
                            octaveStream.WriteLine("     plot(dezePatientTime_NotNan_per_patient{ n}, DATA_rmseReal_per_patient{ n} / conversieNaarMmolL, 'linestyle', '-', 'linewidth', 1, 'color', kleur_groen, 'marker', 'none'); hold on;");
                            octaveStream.WriteLine("  end #for");


                            octaveStream.WriteLine("  ylabel(['RMSE, square root of MSE ', conversieNaarMmoLeenheid]);");
                            octaveStream.WriteLine("  title([TITEL, ':: RMSE of ML wrt noisy (|) and real (:) vip values, and (-) RMSE of noisy Mmnts wrt real vip values']);");
                            octaveStream.WriteLine("  set(gca, 'ylim', [0/conversieNaarMmolL, 50/conversieNaarMmolL]);");
                            octaveStream.WriteLine("  set(gca, 'xlim', [xbegin, xlimit]);");
                            octaveStream.WriteLine("  set(gca,'xtick',[]);");
                            octaveStream.WriteLine("  set(gca, 'xlabel', '');");
                            octaveStream.WriteLine("  CleanupGraph(gca, gcf, 'grid', 'x');");
                            // new fig:
                            // low~high:
                            octaveStream.WriteLine("  disp('');");
                            octaveStream.WriteLine("  disp('');");
                            octaveStream.WriteLine("  ## subplot(5,2,[2,4]); # small top right plot");
                            octaveStream.WriteLine("  % mysubplot(3, 1, 3, 1);");
                            octaveStream.WriteLine("  mysubplot(7, 1, 5:7, 1);");

                            octaveStream.WriteLine("  ### subplot(5, 2, [2,4,6,8,10]); # full right half plot (remove settings!)");

                            //at max.
                            octaveStream.WriteLine("  for n=1: numel(DATA_rmseReal_per_patient);");
                            octaveStream.WriteLine("     plot(dezePatientTime_NotNan_per_patient{n}, DATA_realHigherThanPredictedAtMaximum_per_patient{n}/conversieNaarMmolL, 'color', oranje, 'linewidth', 1, 'linestyle', '-'); hold on;");
                            octaveStream.WriteLine("     plot(dezePatientTime_NotNan_per_patient{n}, DATA_realLowerThanPredictedAtMaximum_per_patient{n}/conversieNaarMmolL, 'color', oranje, 'linewidth', 1, 'linestyle', '-'); hold on;");
                            //at minimum
                            octaveStream.WriteLine("     plot(dezePatientTime_NotNan_per_patient{n}, DATA_realLowerThanPredictedAtMinimum_per_patient{n}/conversieNaarMmolL, 'color', rood, 'linewidth', 1); hold on;");
                            octaveStream.WriteLine("     plot(dezePatientTime_NotNan_per_patient{n}, DATA_realHigherThanPredictedAtMinimum_per_patient{n}/conversieNaarMmolL, 'color', rood, 'linewidth', 1); hold on;");

                            //rmse tov real ter rereferentie
                            octaveStream.WriteLine("     plot(dezePatientTime_NotNan_per_patient{n}, DATA_rmseReal_per_patient{n}/conversieNaarMmolL, 'linestyle', '-','color',  groen, 'linewidth', 1); hold on;");
                            octaveStream.WriteLine("  end; #for");


                            octaveStream.WriteLine("  line( [0, xlimit], [0, 0], 'color', 'black');"); // plot x-as


                            // octaveStream.WriteLine("  title([TITEL, ':: under/overshoots at high (orange) and low (red)']);");
                            octaveStream.WriteLine("  xlabel('time [day]');");
                            octaveStream.WriteLine("  set(gca, 'ylim', [-40/conversieNaarMmolL, 40/conversieNaarMmolL]);");
                            octaveStream.WriteLine("  set(gca, 'xlim', [xbegin, xlimit]);");
                            //      octaveStream.WriteLine("  xlabel('time [day]');");

                            octaveStream.WriteLine("  ylabel(['difference ', conversieNaarMmoLeenheid]);");
                            octaveStream.WriteLine("  CleanupGraph(gca, gcf, 'grid', 'x');");
                       
                            //octaveStream.WriteLine("end;#if");

                            octaveStream.WriteLine("");
                            octaveStream.WriteLine("");
                            octaveStream.WriteLine("## place textbox with settings in plot");
                            octaveStream.WriteLine("my_text = ['#data = ', int2str(size(dezePatientTime_NotNan_per_patient, 1))];");//my_text = ['#data = ', int2str(size(DATA_noisyErrors, 2))];

                            octaveStream.WriteLine("#for n = 1:numel(settings)");
                            octaveStream.WriteLine("#\tmy_text = [my_text, \"\\n\", settings{ n}];");
                            octaveStream.WriteLine("#\tdisp(settings{n});");
                            octaveStream.WriteLine("#end %for");
                            // dump een annotatie box met de gelogde settings en opmerkingen rechtsonderin
                            octaveStream.WriteLine("annotation('textbox',[0.55 0.01 0.1 0.1], 'string', my_text, 'fontsize', 20, 'edgecolor', 'b', 'linewidth', 1, 'fitboxtotext', 'on');");

                            //octaveStream.WriteLine("");
                            //octaveStream.WriteLine("");
                            //octaveStream.WriteLine("if false");
                            //octaveStream.WriteLine("  % individuele plot");
                            //octaveStream.WriteLine("  for run_index = 1:5");
                            //octaveStream.WriteLine("  \tsubplot(5, 2, 2*run_index-1);");
                            //octaveStream.WriteLine("  \tplot( DATA_rmseNoisy(:, run_index),'color',  color0, 'linewidth', 1); hold on;");
                            //octaveStream.WriteLine("  \tplot( DATA_rmseReal(:, run_index), 'color', color0, 'linewidth', 1); hold on;");
                            //octaveStream.WriteLine("  \tplot( DATA_maxUndershootAtMinimum(:, run_index),'color',  color0, 'linewidth', 1); hold on;");
                            //octaveStream.WriteLine("  \tplot( DATA_maxOvershootAtMaximum(:, run_index), 'color', color0, 'linewidth', 1); hold on;");
                            //octaveStream.WriteLine("  \ttitle(['run #', int2str(n)]);");
                            //octaveStream.WriteLine("  \tline( [0, 200], [0, 0]);");
                            //octaveStream.WriteLine("  \tset(gca, 'ylim', [-50, 100]);");
                            //octaveStream.WriteLine("  \tset(gca, 'xlim', [xbegin, xlimit]);");
                            //octaveStream.WriteLine("  \tset(gca, 'fontsize', 16);"); // size of tick values
                            //octaveStream.WriteLine("  \thx=xlabel('measurements');");
                            //octaveStream.WriteLine("  \tset(hx, 'fontsize', 17);"); // xlabel font
                            //octaveStream.WriteLine("  \thy=ylabel('square root of MSE [mg/dL] and difference [mg/dL]');");
                            //octaveStream.WriteLine("  \tset(hy, 'fontsize', 17);"); // ylabel font
                            //octaveStream.WriteLine("  end;#for");
                            //octaveStream.WriteLine("end;#if");
                            //octaveStream.WriteLine("");
                            //octaveStream.WriteLine("");


                            octaveStream.WriteLine("if invisibleAndSave");
                            octaveStream.WriteLine("    # saveas(figure_handle, imgfilename);  ");
                            octaveStream.WriteLine("    imgfilename = '" + errimgfilename + ".png';");
                            octaveStream.WriteLine("    disp(['saving to image ', imgfilename, ' ...']);");
                            //    octaveStream.WriteLine("    set(figure_handle, \"visible\", false);");
                            octaveStream.WriteLine("    print(imgfilename, '-r100',  '-S1900,1000');");
                            octaveStream.WriteLine("    print( 'C:/Lectoraat/Matlab/LectoraatOctaveData/temp/errorTotaalPlot.png', '-r100',  '-S1900,1000');");
                            // octaveStream.WriteLine("    close(figure_handle);");
                            octaveStream.WriteLine("    close all;");

                            // octaveStream.WriteLine("    copyfile(imgfilename, 'C:/Lectoraat/Matlab/LectoraatOctaveData/temp/errorPlot.png', 'f');");
                            //     octaveStream.WriteLine("    #restore");
                            //     octaveStream.WriteLine("    graphics_toolkit fltk; # terug naar de 'default' snel");
                            octaveStream.WriteLine("    exit # dit termineert de octave sessie!");
                            octaveStream.WriteLine("end");
                        }

                        //   Console.WriteLine("-------> TITEL = '" + datestring + ".m'");
                        octaveStream.WriteLine("end #function");
                    }//end using octave file

                }
                catch (Exception e)
                {
                    Console.WriteLine("ParticleFilterDataLogger :: caught Exception " + e + "\n" + e.Message + "\n" + e.StackTrace);
                }
            }
        }



        private void MatlabCodeGeneralStuff(StreamWriter octaveStream, string CommentsForFileName, string datestring)//, bool isLast)
        {
            // some general stuff:
            //if (!isLast) { octaveStream.WriteLine("## !!! --- TEMP FILE --- !!! ##"); }
            octaveStream.WriteLine("## octave ##");
            octaveStream.WriteLine("## " + CommentsForFileName + "##");
            octaveStream.WriteLine("more off     # no pagination in output");
            octaveStream.WriteLine("#clear all;   # clear all data in octave memory");
            octaveStream.WriteLine("# close all; # closes all open plots ");
            octaveStream.WriteLine("addpath('C:/Lectoraat/Matlab/'); # plek waar myyyplot staat");
            octaveStream.WriteLine("show_eval_or_train = true;");
            octaveStream.WriteLine("TITEL = '" + datestring + "__" + CommentsForFileName + "....m';");
            Console.WriteLine("-------> TITEL = '" + datestring + "__" + CommentsForFileName + "....m'");
            octaveStream.WriteLine("");
            octaveStream.WriteLine("# ================= patient en eenheden ==================");
            octaveStream.WriteLine("invisibleAndSave = false; # true--> plot naar file (en toon geen figure)");
            octaveStream.WriteLine("PATIENT_NDX = 1;");
            octaveStream.WriteLine("MG_PER_DL = true;  # true: [mg/dL];  false: [mmol/L]");
            octaveStream.WriteLine("GLUCOSE_MAX_SCALE = 250;");
            octaveStream.WriteLine("MAX_RMSE_OP_Y_AS = 100;");
            
            octaveStream.WriteLine("TIJD_CONVERSIE = 1 / (60 * 24);");
            octaveStream.WriteLine("");
            octaveStream.WriteLine("# ========================================================");
            octaveStream.WriteLine("");
            octaveStream.WriteLine("extraParameters = false;");
            octaveStream.WriteLine("if (exist('varargin', 'var'))");
            octaveStream.WriteLine("  if (numel(varargin) > 0)");
            octaveStream.WriteLine("     extraParameters = true;");
            octaveStream.WriteLine("     invisibleAndSave = varargin{1};");
            octaveStream.WriteLine("     if (numel(varargin) > 1)");
            octaveStream.WriteLine("        show_eval_or_train = varargin{2};");
            octaveStream.WriteLine("        if (numel(varargin) > 2)");
            octaveStream.WriteLine("            PATIENT_NDX = varargin{3};");
            octaveStream.WriteLine("            if (numel(varargin) > 3)");
            octaveStream.WriteLine("                MG_PER_DL = varargin{4};");
            octaveStream.WriteLine("                if (numel(varargin) > 4)");
            octaveStream.WriteLine("                    START_TRACE_LENGTH = varargin{5};");
            octaveStream.WriteLine("                    if (numel(varargin) > 5)");
            octaveStream.WriteLine("                        GLUCOSE_MAX_SCALE = varargin{6};");
            octaveStream.WriteLine("                    end");
            octaveStream.WriteLine("                end");
            octaveStream.WriteLine("            end");
            octaveStream.WriteLine("        end");
            octaveStream.WriteLine("     end");
            octaveStream.WriteLine("  end");
            octaveStream.WriteLine("end");
            octaveStream.WriteLine(""); 
            octaveStream.WriteLine("");
            octaveStream.WriteLine("if(show_eval_or_train)");
            octaveStream.WriteLine("  START_TRACE_LENGTH = " + (int)(particleFilter.settingsForParticleFilter.TrailForEvaluationInMinutes + particleFilter.settingsForParticleFilter.TrailLengthInMinutes) + "; # begin van te tonen trace (in minuten, gerekend terug vanaf het heden)");
            octaveStream.WriteLine("  END_TRACE_LENGTH =   " + (int)(particleFilter.settingsForParticleFilter.TrailForEvaluationInMinutes + particleFilter.settingsForParticleFilter.TrailLengthInMinutes - 7000) + "; # einde van te tonen trace (minuten)");
            octaveStream.WriteLine("else");
            octaveStream.WriteLine("  START_TRACE_LENGTH = 10000; #" + (int)(particleFilter.settingsForParticleFilter.TrailLengthInMinutes) + "; # begin van te tonen trace (in minuten, gerekend terug vanaf het heden)");
            octaveStream.WriteLine("  END_TRACE_LENGTH =   3000; # " + (int)(particleFilter.settingsForParticleFilter.TrailLengthInMinutes - 7000) + "; # einde van te tonen trace (minuten)");
            octaveStream.WriteLine("end #if");
            octaveStream.WriteLine("");
            octaveStream.WriteLine("");


            octaveStream.WriteLine("");
            octaveStream.WriteLine("if ( MG_PER_DL )");
            octaveStream.WriteLine("  conversieNaarMmolL = 1;");
            octaveStream.WriteLine("  conversieNaarMmoLeenheid = '[mg/dL]  [mmol/L]';");
            octaveStream.WriteLine("else");
            octaveStream.WriteLine("  conversieNaarMmolL = 18;");
            octaveStream.WriteLine("  conversieNaarMmoLeenheid = '[mmol/L]  [mg/dL]';");
            octaveStream.WriteLine("end#if");
            octaveStream.WriteLine("rood =        [1,    0,    0];");
            octaveStream.WriteLine("oranje =      [1,    0.5,  0];");
            octaveStream.WriteLine("groen =       [0,    0.75, 0];");
            octaveStream.WriteLine("blauwGroen =  [0,    0.75, 0.65];");
            octaveStream.WriteLine("donkerGroen = [0,    0.5,  0];");
            octaveStream.WriteLine("geel =        [0.9,  0.9,  0];");
            octaveStream.WriteLine("cyaan =       [0.55, 0.9,  1];");
            octaveStream.WriteLine("blauw =       [0,    0,    1];");
            octaveStream.WriteLine("donkerBlauw = [0,    0,    0.3];");
            octaveStream.WriteLine("helderBlauw = [0.35, 0.35, 1];");
            octaveStream.WriteLine("paars =       [0.8,  0,    0.8];");
            octaveStream.WriteLine("donkerGrijs = [0.4,  0.4,  0.4];");
            octaveStream.WriteLine("zwart =       [0,    0,    0];");
        }





        // eerste regel is header (namen van kolommen). separator = ","
        public List<string> GetLoggedDataAsCsv()
        {
            List<string> resultsForCSV = new List<string>();
            string header = ""; // "time [#1],\t";
            int counter = 1;
            foreach (string varnaam in fulllijst)
            {
                header += varnaam + " [#" + counter + "],\t";
                counter++;
            }
            header = header.Substring(0, header.Length - 2);
            resultsForCSV.Add(header);

            for (int index = 0; index < dataLoggerElementsList.Count; index++)
            {
                //string txt = "";// + dataLoggerElementsList[index].time + ",\t";
                StringBuilder txt = new StringBuilder();
                foreach (string varnaam in fulllijst)
                {
                    // txt += dataLoggerElementsList[index].Get(varnaam) + ",\t";
                    if (varnaam == "time")
                    {
                        txt.Append(dataLoggerElementsList[index].Get(varnaam));
                    }
                    else
                    {
                        txt.Append(OctaveStuff.MyFormat(dataLoggerElementsList[index].Get(varnaam)));
                    }
                    txt.Append(",\t");
                }
                txt.Remove(txt.Length - 2, 2);
                //txt = txt.Substring(0, txt.Length - 2);
                resultsForCSV.Add(txt.ToString());
            }
            return resultsForCSV;
        }






        // als niet laatste, dan in temp dir (in tempOctaveDataFilePath). Als laatste, dan verplaatsen naar definitieve plek (in octaveDataFilePath)
        public string GetOutputDir(bool isLast, string datestring, string CommentsForFileName)
        {
            string octaveDataDirName;
            if (isLast)
            {
                octaveDataDirName = octaveDataFilePath + datestring + "__" + CommentsForFileName + "/";
                // oude temp locatie --> copy naar results
                if (particleFilter.PATIENT_INDEX == 0)  // first = last, maar 1 run:
                {
                    Directory.CreateDirectory(octaveDataDirName);
                }
                else
                {
                    try
                    {
                        //octaveDataFileName = octaveDataDirName + "/" + datestring + "__" + CommentsForFileName + "/";
                        string tempDirName = tempOctaveDataFilePath + "/" + datestring + "__" + CommentsForFileName + "/";
                        Directory.Move(tempDirName, octaveDataDirName); // verplaatsen (met alle traces etc) en dan pas laatste erin zetten
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("fout bij verplaatsen ... dir \"" + octaveDataDirName + "\" : " + e);
                        // poging 2, nu zonder problematische comments
                        CommentsForFileName = "";
                        octaveDataDirName = octaveDataFilePath + datestring + "__" + CommentsForFileName + "/";

                        string tempDirName = tempOctaveDataFilePath + "/" + datestring + "__" + CommentsForFileName + "/";
                        Directory.Move(tempDirName, octaveDataDirName); // verplaatsen (met alle traces etc) en dan pas laatste erin zetten
                    }
                }
            }
            else
            {
                octaveDataDirName = tempOctaveDataFilePath + "/" + datestring + "__" + CommentsForFileName + "/";
                try
                {
                    Directory.CreateDirectory(octaveDataDirName);
                }
                catch (Exception e)
                {
                    Console.WriteLine("fout bij aanmaken dir \"" + octaveDataDirName + "\" : " + e);
                    // poging 2, nu zonder problematische comments
                    CommentsForFileName = "";
                    octaveDataDirName = tempOctaveDataFilePath + "/" + datestring + "__" + CommentsForFileName + "/";
                    //octaveDataFileName = tempOctaveDataFilePath + "/" + datestring + "__" + CommentsForFileName + "/";
                    Directory.CreateDirectory(octaveDataDirName);
                }
            }
            return octaveDataDirName;
        }



        private int GetMatlabIndexInFullList(List<string> paramlijst, string varnaam)
        {
            int ndx_in_fulllist = 0;
            for (int i = 0; i < paramlijst.Count; i++)
            {
                if (paramlijst[i].Equals(varnaam))
                {
                    ndx_in_fulllist = i + 1 /*matlab offset = 1*/;
                    break;
                }
            }
            if (ndx_in_fulllist <= 0)
            { 
                throw new ArgumentException("niet gevonden?! varnaam = " + varnaam);
            }
            return ndx_in_fulllist;
        }


        //public static void PrintOctave(string msg, string octaveoutput)
        //{
        //    msg = "octave (" + msg + ") >> ";
        //    Console.WriteLine(msg + octaveoutput.Replace("\n", "\n" + msg));
        //}

    }




    class DataLoggerElement
    {
        public DataLoggerElement(uint t)
        {
            time = t;
            values = new Dictionary<string, double>();
            values["time"] = t;
        }

        public uint time;
        private Dictionary<string, double> values;

        public double Get(string key)
        {
            return values[key];
        }
        public void Add(string key, double value)
        {
            values[key] = value;
        }
    }



    // log elk tekst 1x. Handig voor experimenten die nog niet via functies gaan of via officiele settings.
    public class SettingsLogger
    {
        private List<string> lijst = new List<string>();
        private ParticleFilter particleFilter;
        public SettingsLogger(ParticleFilter pf) { particleFilter = pf; }

        // alleen nieuwe gegevens loggen in SettingsLogger
        public void Add(string s)
        {
            if (!lijst.Contains(s))
            {
                lijst.Add(s);
            }
        }


        public override string ToString()
        {
            string s = "";
            List<string> lijst = SettingsToOctave();
            foreach (string t in lijst)
            {
                s += t + "\n";
            }
            return s;
        }


        // particleFilter settings in octave vriendelijk format:
        public List<string> SettingsToOctave()
        {
            List<string> resultsForOctave = new List<string>();
            int counter = 1;

            resultsForOctave.Add("settings{" + (counter++) + "} = '****************** SETTINGS for patient #" + particleFilter.ObservedPatient.ID + " ******************';");
            resultsForOctave.Add("settings{" + (counter++) + "} = '* " + particleFilter.settingsForParticleFilter.CommentsForFileName + "' ;");
            resultsForOctave.Add("settings{" + (counter++) + "} = '* randomSeedForParticleFilter = " + particleFilter.random.RandomSeedToString() + "';");
            if (particleFilter.ObservedPatient.RealData)
            {
                resultsForOctave.Add("settings{" + (counter++) + "} = '* observed: REAL DATA (patient#: " + particleFilter.RealDataPatientNumber + ")';");
            }
            else
            {
                resultsForOctave.Add("settings{" + (counter++) + "} = '* real (sim.):   " + particleFilter.ObservedPatient.Model + "';");
            }
            //if (particleFilter.RealPatient.BestParticlePatient != null)
            //{
            //	resultsForOctave.Add("settings{" + (counter++) + "} = '* learned (pf.): " + particleFilter.RealPatient.BestParticlePatient.Model.ModelData + "';");
            //}
            resultsForOctave.Add("settings{" + (counter++) + "} = '* NumberOfSubPopulations = " + particleFilter.NumberOfSubPopulations + "';");
            resultsForOctave.Add("settings{" + (counter++) + "} = '* NumberOfParticlesPerSub = " + particleFilter.settingsForParticleFilter.NumberOfParticlesPerSubPopulation + "';");
            resultsForOctave.Add("settings{" + (counter++) + "} = '* TrailLength  = " + particleFilter.TrailLengthInMinutes + "';");
            resultsForOctave.Add("settings{" + (counter++) + "} = '* NrXTrailForCarb  = " + particleFilter.settingsForParticleFilter.NrXTrailForCarbSlopeEnd + "';");
            //resultsForOctave.Add("settings{" + (counter++) + "} = '* selectie: " + particleFilter.ExponentialDecayInitValue + " * Exp(" + particleFilter.ExponentialDecayScalingValue +"*  sse)';");
            resultsForOctave.Add("settings{" + (counter++) + "} = '* BestParticleHistoryQueueLength = " + particleFilter.settingsForParticleFilter.NumberOfParticlesFromHistory + "';");
            resultsForOctave.Add("settings{" + (counter++) + "} = '* NumberOfParticlesToKeep = " + particleFilter.NumberOfParticlesToKeep + "';");
            resultsForOctave.Add("settings{" + (counter++) + "} = '* FractionWhenChangeParam = " + particleFilter.settingsForParticleFilter.FractionWhenChangeParam + "';");
            if (particleFilter.settingsForParticleFilter.GammaMomentum * particleFilter.settingsForParticleFilter.FractionMomentum == 0)
            {
                resultsForOctave.Add("settings{" + (counter++) + "} = '* Momentum: NONE';");
            }
            else
            {
                resultsForOctave.Add("settings{" + (counter++) + "} = '* momentum: { <=" + particleFilter.settingsForParticleFilter.FractionMomentum + ", += *" + particleFilter.settingsForParticleFilter.GammaMomentum + "}';");
            }
//            resultsForOctave.Add("settings{" + (counter++) + "} = '* LobSidedSseFactor = " + particleFilter.settingsForParticleFilter.LobSidedErrorFactor + "';");
            if (!particleFilter.ObservedPatient.RealData)
            {
                resultsForOctave.Add("settings{" + (counter++) + "} = '* SimulateNoisyGlucoseMmnts = " + particleFilter.ObservedPatient.SimulateNoisyGlucoseMmnts + " (" + particleFilter.ObservedPatient.GlucNoiseFactor + ")';");
               // resultsForOctave.Add("settings{" + (counter++) + "} = '* SimulateNoisyFoodMmnts = " + particleFilter.ObservedPatient.HasNoisyFoodMmnts + " (" + particleFilter.ObservedPatient.FoodNoiseFactor + ")';");
                resultsForOctave.Add("settings{" + (counter++) + "} = '* SometimesForgetFoodEvent = " + particleFilter.ObservedPatient.FoodForgetFactor + "';");
            }

            resultsForOctave.Add("settings{" + (counter++) + "} = '* UseActivity = " + particleFilter.UseActivityModel + "';");
            //if (particleFilter.UseActivityModel)
            //{
            //    resultsForOctave.Add("settings{" + (counter++) + "} = '*  ActivityAlpha = " + particleFilter.ActivityAlpha + "';");
            //    resultsForOctave.Add("settings{" + (counter++) + "} = '*  ActivityBeta = " + particleFilter.ActivityBeta + "';");
            //    resultsForOctave.Add("settings{" + (counter++) + "} = '*  activityAfactorGamma = " + particleFilter.activityAfactorGamma + "';");
            //    resultsForOctave.Add("settings{" + (counter++) + "} = '*  activityPowGamma = " + particleFilter.activityPowGamma + "';");
            //    resultsForOctave.Add("settings{" + (counter++) + "} = '*  ActivityTauHeartRate = " + particleFilter.ActivityTauHeartRate + "';");
            //    resultsForOctave.Add("settings{" + (counter++) + "} = '*  activityTauZ = " + particleFilter.activityTauZ + "';");
            //}

            // hacks in code:
            foreach (string settingTxt in lijst)
            {
                string settingTxt_filtered = settingTxt;
                settingTxt_filtered.Replace("_", "\\_");
                resultsForOctave.Add("settings{" + (counter++) + "} = '* " + settingTxt_filtered + "';");
            }
            resultsForOctave.Add("settings{" + counter + "} = '***********************************';");
            return resultsForOctave;
        }


    }



}
