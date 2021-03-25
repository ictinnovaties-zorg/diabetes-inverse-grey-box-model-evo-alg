using SMLDC.Simulator.DiffEquations.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMLDC.Simulator.DiffEquations.Solvers
{
    public class SolverResultFactory
    {
        // Since this is only used for debug purposes (to see if the optimized Array1dSolverResult verion is correct)
        // there is no need to make this a setting in the config. This factory just makes it easier to switch to the 
        // old (SolverResultListOfDoubleArrays) version.
        public static readonly bool ARRAY_BASED = true;

        public static SolverResultBase CreateSolverResult(double[] initvector, uint nr_data = 1)
        {
            if (ARRAY_BASED)
            {
                return new Array1DSolverResult(initvector, (uint)nr_data);
            }
            else
            {
                SolverResultBase sr = new SolverResultListOfDoubleArrays();
                uint time = (uint)Math.Round(initvector[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]);
                sr.AddCopy(time, initvector);
                return sr;

            }
        }

    }
}
