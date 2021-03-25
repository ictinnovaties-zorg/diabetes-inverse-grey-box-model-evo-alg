/*
 * This file is a slightly modified version of the one that Boroş Tiberiu created.
 * I found OctaveSharp on https://www.codeproject.com/Articles/342007/OctaveSharp-Running-GNU-Octave-with-Csharp
 *
 * my changes:
 *  - some variables renamed
 *  - added ExecuteCommandDontWait method, which does what then name says :-)  This method does not wait for the Octave output
 *    so it doesn't stop the flow of the program calling it.
 *  - included ThreadEx found on https://github.com/dotnet/coreclr/blob/master/tests/src/baseservices/threading/regressions/threadex.cs
 *    so I could use ThreadEx.Abort(thread) instead of thread.Abort() because of some difficulties I ran into with thread.Abort 
 *    (Could be that this was system dependent. This was the quickest fix for me).
 * 
 * 
------------------------------------------------------------------------------------------
Author:
Boroş Tiberiu                                                                                                  
Administrator Sistem                                                    Tel.: +40745310081
Institutul de Cercetări pentru Inteligenţă Artificială,
Academia Română
Calea 13 Septembrie, Nr. 13, CASA ACADEMIEI, Bucuresti 050711, ROMANIA
Tel.: +40-(0)213188103 
Fax: +40-(0)213188142 
E-mail: office@racai.ro
Web: http://www.racai.ro
------------------------------------------------------------------------------------------
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Reflection; // used by ThreadEx

namespace LibSharpTave {

    public class Octave {

        private string pathToOctaveBins;
        private bool createOctaveWindow;
        Process OctaveProcess { get; set; }
        private string OctaveEchoString { get; set; }

        public Octave(string _pathToOctaveBinaries) {
            StartOctave(_pathToOctaveBinaries, false);
        }
        public Octave(string _pathToOctaveBinaries, bool CreateWindow)
        {
            StartOctave(_pathToOctaveBinaries, CreateWindow);
        }
        public Octave(bool CreateWindow)
        {
            StartOctave(null, CreateWindow);
        }



        private void StartOctave(string _pathToOctaveBinaries, bool CreateWindow) {
            if (_pathToOctaveBinaries != null)
            {
                pathToOctaveBins = _pathToOctaveBinaries;
            }
            createOctaveWindow = CreateWindow;
            this.OctaveEchoString = Guid.NewGuid().ToString();
            OctaveProcess = new Process();
            ProcessStartInfo pi = new ProcessStartInfo();
            if (pathToOctaveBins[pathToOctaveBins.Length - 1] != '\\')
            {
                pathToOctaveBins = pathToOctaveBins + "\\";
            }
            pi.FileName = pathToOctaveBins + "octave-cli.exe";
            pi.RedirectStandardInput = true;
            pi.RedirectStandardOutput = true;
            pi.RedirectStandardError = true;
            pi.UseShellExecute = false;
            pi.CreateNoWindow = !CreateWindow;

            pi.Verb = "open";
            //
            pi.WorkingDirectory = ".";
            OctaveProcess.StartInfo = pi;
            //OctaveProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; //werkt niet
            OctaveProcess.Start();
            OctaveProcess.OutputDataReceived += new DataReceivedEventHandler(OctaveProcess_OutputDataReceived);
            OctaveProcess.BeginOutputReadLine();
            OctaveEntryText = ExecuteCommand(null);
        }

        public double GetScalar(string scalar) {
            string rasp = ExecuteCommand(scalar, 30000);
            string val = rasp.Substring(rasp.LastIndexOf("\\") + 1).Trim();
            return double.Parse(val);
        }

        public double[] GetVector(string vector) {
            string rasp = ExecuteCommand(vector, 30000);
            string[] lines = rasp.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int i = 0;
            //catam urmatorul entry
            List<double> data = new List<double>();
            while (i != lines.Length) {
                string line = lines[i];
                if (line.Contains("through") || line.Contains("and")) {
                    i++;
                    line = lines[i];
                    string[] dataS = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int k = 0; k < dataS.Length; k++) {
                        data.Add(double.Parse(dataS[k]));
                    }
                }
                i++;
            }
            //caz special in care a pus toate rezultatele pe o singura linie
            if (data.Count == 0) {
                string[] dataS = lines[lines.Length - 1].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (dataS.Length != 0)
                    for (int k = 0; k < dataS.Length; k++) {
                        data.Add(double.Parse(dataS[k]));
                    }
            }
            return data.ToArray();
        }

        public double[][] GetMatrix(string matrix) {
            //string rasp = ExecuteCommand(matrix);
            //aflam numarul de randuri
            string rasp = ExecuteCommand(matrix + "(:,1)", 30000);
            string[] lines = rasp.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            double[][] mat = new double[lines.Length - 1][];
            for (int i = 0; i < mat.Length; i++) {
                mat[i] = GetVector(matrix + "(" + (i + 1) + ",:)");
            }
            return mat;
        }

        StringBuilder SharedBuilder = new StringBuilder();
        ManualResetEvent OctaveDoneEvent = new ManualResetEvent(false);
        public string OctaveEntryText { get; internal set; }

        public void WorkThread(object o) {
            string command = (string)o;
            SharedBuilder.Clear();
            OctaveDoneEvent.Reset();
            if (command != null) {
                OctaveProcess.StandardInput.WriteLine(command);
            }
            //ca sa avem referinta pentru output
            OctaveProcess.StandardInput.WriteLine("\"" + OctaveEchoString + "\"");
            OctaveDoneEvent.WaitOne();
        }
        public string ExecuteCommand(string command, int timeout) {
            if (OctaveProcess.HasExited) {
                StartOctave(pathToOctaveBins, createOctaveWindow);
                if (OctaveRestarted != null) { OctaveRestarted(this, EventArgs.Empty); }
            }
            exitError = false;

            Thread tmp = new Thread(new ParameterizedThreadStart(WorkThread));
            tmp.Start(command);

            if (!tmp.Join(timeout)) {
                //tmp.Abort();
                ThreadEx.Abort(tmp); // zie https://github.com/dotnet/coreclr/blob/master/tests/src/baseservices/threading/regressions/threadex.cs
                throw new Exception("Octave timeout");
            }
            if (exitError) {
                throw new Exception(errorMessage);
            }
            return SharedBuilder.ToString();
        }


        public string ExecuteCommand(string command)
        {
            Thread tmp = new Thread(new ParameterizedThreadStart(WorkThread));
            tmp.Start(command);

            tmp.Join();

            return SharedBuilder.ToString();
        }

        public void ExecuteCommandDontWait(string command)
        {
            if (OctaveProcess.HasExited)
            {
                StartOctave(pathToOctaveBins, createOctaveWindow);
                if (OctaveRestarted != null) { OctaveRestarted(this, EventArgs.Empty); }
            }
            //exitError = false;

            Thread tmp = new Thread(new ParameterizedThreadStart(WorkThread));
            tmp.Start(command);
        }

        public bool CheckIfFinished()
        {
            Thread tmp = new Thread(new ParameterizedThreadStart(WorkThread));
            string msg = "disp('C#-->CheckIfFinished')";
            tmp.Start(msg);
            Thread.Sleep(10);
            if (tmp.ThreadState == System.Threading.ThreadState.Stopped)
            {
                return true;
            }
            if (!tmp.Join(1))
            {
                return false;
            }

            return true;
        }

        public bool CheckIfBusy()
        {
            return !CheckIfFinished();
        }



        bool exitError = false;
        string errorMessage = null;
        void OctaveProcess_OutputDataReceived(object sender, DataReceivedEventArgs e) {
            if (e.Data == null) {
                SharedBuilder.Clear();
                errorMessage = OctaveProcess.StandardError.ReadToEnd();
                SharedBuilder.Append("Octave has exited with the following error message: \r\n" + errorMessage);
                exitError = true;
                OctaveDoneEvent.Set();
                return;
            }
            if (e.Data.Trim() == "ans = " + OctaveEchoString)
                OctaveDoneEvent.Set();
            else
                SharedBuilder.Append(e.Data + "\r\n");
        }
        public event OctaveRestartedEventHandler OctaveRestarted;
        public delegate void OctaveRestartedEventHandler(object sender, EventArgs e);
    }



    // https://github.com/dotnet/coreclr/blob/master/tests/src/baseservices/threading/regressions/threadex.cs

    public class ThreadEx
    {
        public static void Abort(Thread thread)
        {
            MethodInfo abort = null;
            foreach (MethodInfo m in thread.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (m.Name.Equals("AbortInternal") && m.GetParameters().Length == 0) abort = m;
            }
            if (abort == null)
            {
                throw new Exception("Failed to get Thread.Abort method");
            }
            abort.Invoke(thread, new object[0]);
        }
    }

}
