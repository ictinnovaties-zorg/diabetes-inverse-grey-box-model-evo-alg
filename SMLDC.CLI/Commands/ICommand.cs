namespace SMLDC.CLI.Commands
{
    internal interface ICommand
    {
        //The parameters needed for the Command
        string[] Arguments { get; }

        //The description that describes the command
        string Description { get; }

        //The main script that runs when command is called
        void Run();

        //The main script that runs when command is called with given arguments
        void RunWithArguments(string[] args);
    }
}