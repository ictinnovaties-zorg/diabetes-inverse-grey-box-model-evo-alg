using SMLDC.Simulator.DiffEquations.Models;
using SMLDC.Simulator.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMLDC.Simulator.DiffEquations.Solvers
{

    /*
     * storing the results of the simulition (each time a double[]) as a long list.
     * Is not highly efficient, but very simple. See Array1DSolveRResult for a highly optimized version.
     */
    public class SolverResultListOfDoubleArrays : SolverResultBase
    {
        private readonly List<double[]> DataValues; // todo: omzetten naar double[][] ??? of (vector<>)[] ?????


        public SolverResultListOfDoubleArrays(int initialCapacity = 10000)
        {
            DataValues = new List<double[]>(initialCapacity);
            start_time = -1;
        }


        public override int GetCount() { return DataValues.Count; }




        public override int GetHashCode()
        {
            // throw new NotImplementedException("geen idee wat slimmer is, maar dit wordt toch niet gebruikt, want we stoppen geen SolverResults ergens in als key");
            return base.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            try
            {
                SolverResultListOfDoubleArrays that = (SolverResultListOfDoubleArrays)obj;
                if (this.start_time != that.start_time)
                {
                    return false;
                }
                if (this.DataValues.Count != that.DataValues.Count)
                {
                    return false;
                }
                for (int i = 0; i < this.DataValues.Count; i++)
                {
                    for (int k = 0; k < this.DataValues[i].Length; k++)
                    {
                        if (this.DataValues[i][k] != that.DataValues[i][k])
                        {
                            return false;
                        }
                    }
                }
                //geen afwijkingen gevonden
                return true;
            }
            catch
            {
                return false;
            }
        }




        public override void AddCopy(uint time, double[] valuevector_orig)
        {
            _Add(time, CloneUtilities.CloneArray(valuevector_orig));
        }


        private void _Add(uint time, double[] valuesVector)
        {
            if (start_time < 0) //leeg
            {
                if (DataValues.Count > 0)
                {
                    //trouble
                    throw new ArgumentException("fout! DataValues.count > 0 maar start_time < 0");
                }
                start_time = (int)time;
                DataValues.Add(valuesVector);
                return;
            }

            // bepaal de tijd volgens SolverResult:
            if (this.DataValues.Count > 0)
            {
                int mynewtime = start_time + DataValues.Count;
                if (valuesVector == null)
                {
                    DataValues.Add(valuesVector);
                    return;
                }

                //                uint newTime = (uint)Math.Round(valuesVector[BergmanAndBretonModel.T_DebugTimeSignalIndex_MIN]);
                uint newTime = (uint)(valuesVector[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]);

                if (mynewtime != newTime)
                {
                    if (mynewtime == newTime + 1) //aansluiting met nieuwe solver run
                    {
                        //terugzoeken
                        int newindex = getIndexFromTime(newTime);
                        // en overschrijven:
                        this.DataValues[newindex] = valuesVector;
                        return;
                    }
                    else
                    {
                        //trouble!
                        throw new ArgumentException("tijd mismatch!");
                        // terug zoeken indien mogelijk:
                    }
                }
                else
                {
                    DataValues.Add(valuesVector);
                    return;
                }
            }
            else
            {
                //eerste data toegevoegd aan lege SolverResult (met starttime = 0, wat nu nergens op slaat)
                this.start_time = (int)Math.Round(valuesVector[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]);
                DataValues.Add(valuesVector);
            }
        }



        protected void IntegrityCheck()
        {
            //for (int i = 0; i < this.DataValues.Count; i++)
            //{
            //    if ((int)Math.Round(this.DataValues[i][ModifiedModel.T_DebugTimeSignalIndex]) != (i + this.start_time))
            //    {
            //        Console.WriteLine("tijd klopt niet met index");
            //    }
            //}
        }




        public override void AddCopyOverwrite(SolverResultBase updatedDataUncast)
        {
            SolverResultListOfDoubleArrays updatedData = (SolverResultListOfDoubleArrays)updatedDataUncast;
            // plak newData er achteraan
            for (int i = 0; i < updatedData.DataValues.Count; i++)
            {
                uint newtime = (uint)(updatedData.start_time + i);
                int ndx_to_replace = getIndexFromTime(newtime);
                if (ndx_to_replace >= 0)
                {
                    //bestaaat
                    this.DataValues[ndx_to_replace] = Utilities.CloneUtilities.CloneArray(updatedData.DataValues[i]);
                }
                else
                {
                    this.AddCopy(newtime, updatedData.DataValues[i]);
                }
            }
        }





        public override void AddCopy(SolverResultBase updatedDataUncast)
        {
            SolverResultListOfDoubleArrays newData = (SolverResultListOfDoubleArrays)updatedDataUncast;

            //sanity check:
            // is eerste lijst leeg? 
            if (this.start_time < 0)
            {
                if (newData.start_time < 0)
                {
                    //Console.Write("BEIDE LIJSTEN LEEG: < 0  -->   RETURN");
                    return;
                }
                else
                {
                    start_time = 0;
                }
            }

            // plak newData er achteraan
            for (int i = 0; i < newData.DataValues.Count; i++)
            {
                uint newtime = (uint)(newData.start_time + i);
                this.AddCopy(newtime, newData.DataValues[i]);
            }
            IntegrityCheck();
            // Console.WriteLine("   ===> this = " + this.ToStringShort());
        }


        protected override double[] GetDataRaw(int ndx, ref double[] reusable_array)
        {
            if (ndx < 0 || ndx >= this.Count) { return null; }
            return this.DataValues[ndx];
        }


        protected override void SetDataRaw(int ndx, double[] vector)
        {
            if (ndx < 0 || ndx >= this.Count) { throw new ArgumentException("index buiten array! ndx = " + ndx + ", this.DataValues.Count = " + this.DataValues.Count); }
            this.DataValues[ndx] = vector;
        }



        public override double[] GetLastValues()
        { //zou nooit te interpoleren moeten zijn, is altijd echt punt
            return CloneUtilities.CloneArray(DataValues[this.DataValues.Count - 1]);
        }


        public override void OverwriteLast(double[] updateData)
        {
            //DataValues[this.DataValues.Count - 1] = updateData;
            for (int i = 0; i < updateData.Length; i++)
            {
                DataValues[this.DataValues.Count - 1][i] = updateData[i];
            }
        }
    }
}
