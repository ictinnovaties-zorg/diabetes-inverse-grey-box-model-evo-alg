using System;
using System.Collections.Generic;
using Serilog;
using SMLDC.CLI.Commands;

namespace SMLDC.CLI
{

    /*
     * This code originally started as a command line tool built by students
     * (including a simple Euler and Midpoint solver, and the option to run a virtual patient simulation.
     * It was then modified to include Machine Learning.
     * The files in SMLDC.CLI are legacy from the cmd line app.
     */ 
    internal class Program
    {
        private static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .CreateLogger();

            //  Log.Information("Welcome in our 'Smarter Machine Learning for Diabetes Care' Command Bash.");
            //  Log.Information("Type 'help' '/help' to view the command options");
            CommandHandler commandHandler = new CommandHandler();


            List<string> runArgs = new List<string>();
            if(args.Length > 0)
            {
                // for use from outside, e.g. BatchRun (see that project) on command line.
                string config = args[0];
                Console.WriteLine("config: " + config);
                runArgs.Add(config);
                if(args.Length > 1)
                {
                    runArgs.Add(args[1]); // patientnr.
                    runArgs.Add(args[2]); // datestr.
                }
            }
            else
            {
                runArgs.Add("config.ini");
            }

           (new CreateParticleFilterCommand()).RunWithArguments(runArgs.ToArray());
        }
    }
}