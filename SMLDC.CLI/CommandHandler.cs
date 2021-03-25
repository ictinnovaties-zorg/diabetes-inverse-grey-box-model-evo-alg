using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Serilog;
using SMLDC.CLI.Commands;

namespace SMLDC.CLI
{
    public class CommandHandler
    {
        public static Simulator.GlucoseInsulinSimulator sim;

        //Static list of all known commands.
        public static Dictionary<string, string> commandList = new Dictionary<string, string>
        {
            {"help", "HelpCommand"},
            {"clear", "ClearCommand"},
    //        {"exit", "ExitCommand"},
      //      {"close", "CloseCommand"},
     //       {"start", "StartCommand"},
     //       {"stop", "StopCommand"},
            {"sim", "CreateSimulatorCommand"},
     //       {"createsim", "CreateSimulatorCommand"},
            {"pf", "CreateParticleFilterCommand"},
     //       {"createpf", "CreateParticleFilterCommand"}
        };

        //public static Dictionary<string, dynamic> commandList = new Dictionary<string, dynamic>
        //{
        //    {"help", new HelpCommand()},
        //    {"clear", new ClearCommand()},
        //    {"exit", new ExitCommand()},
        //    {"close", new CloseCommand()},
        //    {"start", new StartCommand()},
        //    {"stop", new StopCommand()},
        //    {"createsim", new CreateSimulatorCommand()},
        //    {"createpf", new CreateParticleFilterCommand()}
        //};
        //The main loop that keeps the console waiting for input or catching input.
        public void Run()
        {
            string[] inputArray = {""};

            while (inputArray != null)
            {
                Console.Write(">> ");
                inputArray = FormatInput(Console.ReadLine());
                if (inputArray.Length > 0) { Execute(inputArray); }
            }
        }

        public void Run(string[] inputArray)
        {
            if (inputArray.Length > 0) { Execute(inputArray); }
        }

         
        //Removes spaces, tabs, question marks or slashes
        public string[] FormatInput(string input)
        {
            string trimmedInput = input.Trim(' ', '?', '/', '\t').ToLower();

            string[] result =
                trimmedInput.Split(new[] {' ', '\t'},
                    StringSplitOptions.RemoveEmptyEntries); // Does not trim correctly,

            return result;
        }

        //If the command is known it will get executed, this will call the invokefromStringMethod
        public int Execute(string[] input)
        {
            if (commandList.Keys.Contains(input[0]))
            {
                string dynamicObjectType = commandList[input[0]];
                InvokeStringMethod(dynamicObjectType, input);
                return 0;
            }

            //Error message with the given input.
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string val in input)
            {
                stringBuilder.Append(val + " ");
            }

            Log.Error($"{stringBuilder} is not a valid input");
            Log.Information("Try 'help' or '/help' for a list of available commands");
            return 1;
        }

        //This function tries to call the Run method of the command. 
        public void InvokeStringMethod(string typeName, string[] input)
        {
            //Set the full assembly qualified name
            typeName = "SMLDC.CLI.Commands." + char.ToUpper(typeName[0]) + typeName.Substring(1);

            // Get the Type for the class
            Type calledType = Type.GetType(typeName);

            //Create an instance of the object
            ConstructorInfo commandConstructor = null;
            try
            {
                commandConstructor = calledType.GetConstructor(Type.EmptyTypes);
            }
            catch (System.Exception e)
            {
                throw new Exception($"The command could not be found. \nERROR: {e.Message}");
            }

            object commandClassObject = commandConstructor.Invoke(new object[] { });

            //If arguments are given or not the fitting method will be called. 
            MethodInfo command = input.Length == 1 ? calledType.GetMethod("Run") : calledType.GetMethod("RunWithArguments");

            if (command.GetParameters().Length != 0)
            {
                //First value, the command name itself will be skipped.
                input = input.Skip(1).ToArray();

                try
                {
                    Log.Debug($"{commandClassObject} has been called with following arguments:{input}.");
                    command.Invoke(commandClassObject, new object[] { input });
                }
                catch (System.Exception e)
                {
                    throw new Exception($"Command failed to execute. \nERROR: {e.Message}");
                }
            }
            else
            {
              //  try
                {
                    Log.Debug($"{commandClassObject} has been called without arguments.");
                    command.Invoke(commandClassObject, null);
                }
                //catch (System.Exception e)
                //{
                //    throw new Exception($"Command failed to execute. \nERROR: {e.Message}");
                //}
            }
        }
    }
}