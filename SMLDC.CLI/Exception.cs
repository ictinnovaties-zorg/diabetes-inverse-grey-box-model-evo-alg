using System;
using Serilog;

namespace SMLDC.CLI
{
    public class Exception : System.Exception
    {
        public Exception(string msg) : base(msg)
        {
            Log.Error("ERROR: " + msg);
        }
    }
}