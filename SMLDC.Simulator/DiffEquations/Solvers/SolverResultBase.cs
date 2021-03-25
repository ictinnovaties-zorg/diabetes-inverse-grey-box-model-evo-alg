using System;
using System.Collections.Generic;
using SMLDC.Simulator.Utilities;
using SMLDC.Simulator.DiffEquations.Models;

namespace SMLDC.Simulator.DiffEquations.Solvers
{
    /// <summary>
    /// Used to store the calculated data from the BaseSolver.Solve() method.
    /// </summary>
    /// 
    // aanname: time step  = 1. Note: there have been some experiments with different time steps in the MidPointSolver,
    // but they are not compatible with the Array1DSolverResult, so this is no longer used.
    // The idea of bigger solver steps was to increase speed (on the 'boring' parts of the curve, and then scale back to step=1 
    // on more interesting parts where the graph changes more). But the Array1DSolverResults store all data in one big 1d array
    // under the assumption that every data point is 1 minute later than the previous one. 
    //
    // stores modelValues for e.g. modified model (or any double[] array). Assumes same length every time.


    public abstract class SolverResultBase
    {
        public bool StoreInterpolation = true;
        protected int vector_size;

        // tijdstap = 1, start @ 0 dus index = tijd
        protected int start_time;

        public int Count { get { return this.GetCount(); } }
        public abstract int GetCount();
        public override string ToString()
        {
            return "SolverResult<@" + start_time + ", count=" + Count + ">";
        }

        public override int GetHashCode()
        {
            // throw new NotImplementedException("geen idee wat slimmer is, maar dit wordt toch niet gebruikt, want we stoppen geen SolverResults ergens in als key");
            return base.GetHashCode();
        }

        public virtual SolverResultBase DeepCopy() { return this; }



        // losse elementen //
        public virtual void AddCopySequential(uint time, double[] valuevector_orig)
        {
            AddCopy(time, valuevector_orig);
        }

        public abstract void AddCopy(uint time, double[] valuevector_orig);

        public void AddCopy(double[] valuesVector)
        {
            uint time = (uint)Math.Round(valuesVector[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]);
            AddCopy(time, valuesVector);
        }

        public void AddNull(uint time)
        {
            AddCopy(time, null);
        }


        // complete SolverResults //

        // TODO: naamgeving is misleidend: het is geen copy!! de array1D variant neemt gewoon ref naar double[] over!!
        // TODO: naamgeving is misleidend: het is geen copy!! de array1D variant neemt gewoon ref naar double[] over!!
        // TODO: naamgeving is misleidend: het is geen copy!! de array1D variant neemt gewoon ref naar double[] over!!
        // TODO: naamgeving is misleidend: het is geen copy!! de array1D variant neemt gewoon ref naar double[] over!!
        // TODO: naamgeving is misleidend: het is geen copy!! de array1D variant neemt gewoon ref naar double[] over!!
        // TODO: naamgeving is misleidend: het is geen copy!! de array1D variant neemt gewoon ref naar double[] over!!
        // TODO: naamgeving is misleidend: het is geen copy!! de array1D variant neemt gewoon ref naar double[] over!!
        // TODO: naamgeving is misleidend: het is geen copy!! de array1D variant neemt gewoon ref naar double[] over!!
        // TODO: naamgeving is misleidend: het is geen copy!! de array1D variant neemt gewoon ref naar double[] over!!
        // TODO: naamgeving is misleidend: het is geen copy!! de array1D variant neemt gewoon ref naar double[] over!!
        public abstract void AddCopyOverwrite(SolverResultBase updatedDataUncast);

        public abstract void AddCopy(SolverResultBase newData);


        protected abstract void SetDataRaw(int ndx, double[] vector);
        protected double[] GetDataRaw(int ndx) {
            double[] array = new double[vector_size];
            return GetDataRaw(ndx, ref array);
          //  return array;
        }
        protected abstract double[] GetDataRaw(int ndx, ref double[] reusable_array);

        public double[] GetInterpolatedData_reusable(int ndx, ref double[] reusable_array)
        {
            if (ndx < 0 || ndx >= this.Count) { return null; }
            double[] values = GetDataRaw(ndx, ref reusable_array);
            if (values != null)
            {
                if(reusable_array == null)
                {
                    reusable_array = new double[values.Length];
                    Array.Copy(values, reusable_array, values.Length);
                }
                return values;
            }
            // interpoleer:
            //ervoor
            int prev_ndx = ndx - 1;
            double[] dataPrevNdx = GetDataRaw(prev_ndx);
            while (prev_ndx >= 0 && dataPrevNdx == null)
            {
                prev_ndx--;
                dataPrevNdx = GetDataRaw(prev_ndx);
            }
            if (prev_ndx < 0) { return null; } // geen beginpunt!


            //erna:
            int next_ndx = ndx + 1;
            double[] dataNextNdx = GetDataRaw(next_ndx);
            while (next_ndx < this.Count &&  dataNextNdx == null)
            {
                next_ndx++;
                dataNextNdx = GetDataRaw(next_ndx);
            }
            if (next_ndx >= this.Count) { return null; } // geen eindpunt!
            //interp. berekening: linear interp.

            double length = next_ndx - prev_ndx;
            double prev = 1 - ((ndx - prev_ndx) / length);
            double next = 1 - ((next_ndx - ndx) / length);

            if(reusable_array != null && reusable_array.Length == dataPrevNdx.Length)
            {
                values = reusable_array;
            }
            else
            {
                values = new double[dataPrevNdx.Length];
            }

            for (int v = 0; v < values.Length; v++)
            {
                values[v] = prev * dataPrevNdx[v] + next * dataNextNdx[v];
            }
            values[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN] = start_time + ndx;
            if (StoreInterpolation)
            {
                //geinterpoleerde waarden toevoegen aan data. Scheelt volgende keer weer rekentijd.
                SetDataRaw(ndx, values);
            }
            return values;

        }
        public double[] GetInterpolatedData(int ndx)
        {

            if (ndx < 0 || ndx >= this.Count) { return null; }
            double[] values = GetDataRaw(ndx);
            if (values != null)
            {
                return values;
            }
            return null;
            throw new NotImplementedException("todo: goed checken & compatible maken met Array1Dsolveresult !!");
            // todo: na gecheckt, vervangen door versie hierboven 'reusable'
            // dus: return GetInterpolatedData(ndx, null);

            // interpoleer:
            //ervoor
            int prev_ndx = ndx - 1;
            double[] dataPrevNdx = GetDataRaw(prev_ndx);
            while (prev_ndx >= 0 && dataPrevNdx == null)
            {
                prev_ndx--;
                dataPrevNdx = GetDataRaw(prev_ndx);
            }
            if (prev_ndx < 0) { return null; } // geen beginpunt!
            if (prev_ndx == 0)
            {

            }
            //erna:
            int next_ndx = ndx + 1;
            double[] dataNextNdx = GetDataRaw(next_ndx);
            while (next_ndx < this.Count && dataNextNdx == null)
            {
                next_ndx++;
                dataNextNdx = GetDataRaw(next_ndx);
            }
            if (next_ndx >= this.Count) { return null; } // geen eindpunt!
            //interp. berekening: linear interp.

            double length = next_ndx - prev_ndx;
            double prev = 1 - ((ndx - prev_ndx) / length);
            double next = 1 - ((next_ndx - ndx) / length);

            //double[] dataPrevNdx = GetDataRaw(prev_ndx);
            //double[] dataNextNdx = GetDataRaw(next_ndx);
            values = new double[dataPrevNdx.Length];
            //alues = new DenseVector(DataValues[prev_ndx].Length);

            for (int v = 0; v < values.Length; v++)
            {
                values[v] = prev * dataPrevNdx[v] + next * dataNextNdx[v];
            }
            values[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN] = start_time + ndx;
            if (StoreInterpolation)
            {
                //geinterpoleerde waarden toevoegen aan data. Scheelt volgende keer weer rekentijd.
                SetDataRaw(ndx, values);
            }
            return values;
        }



        public int getIndexFromTime(uint time)
        {
            int i = (int)time - this.start_time;
            if (i < 0) { return -2; }
            if (i >= this.Count) { return -1; }
            return i;
        }


        //public bool TryGetValuesFromTime_reusable(uint time, ref double []reusable_vector)
        //{
        ////// HIER ZIT EEN BUG IN!!! met null vs. tijd_index = -1 enz.
        //    return TryGetValuesFromIndex_reusable(getIndexFromTime(time), ref reusable_vector);
        //}
        public bool TryGetValuesFromTime(uint time, out double[] vector)
        {
            return TryGetValuesFromIndex(getIndexFromTime(time), out vector);
        }


        //public bool TryGetValuesFromIndex_reusable(int index, ref double[] reusable_vector)
        //{
        ////// HIER ZIT EEN BUG IN!!! met null vs. tijd_index = -1 enz.
        //    if (index >= 0 && index < this.Count)
        //    {
        //        GetInterpolatedData_reusable(index, ref reusable_vector);
        //        return true;
        //    }
        //    return false;
        //}

        public bool TryGetValuesFromIndex(int index, out double[] vector)
        {
            if (index >= 0 && index < this.Count)
            {
                vector = GetInterpolatedData(index);
                return vector != null;
            }
            vector = null;
            return false;
        }

        public double[] GetValuesFromIndex(int i)
        {
            if (i < 0 || i >= this.Count)
            {
                throw new ArgumentException("i = " + i + ", count = " + this.Count);
            }
            return (GetInterpolatedData(i));
        }

        public double[] GetValuesFromTime(uint time)
        {
            int i = getIndexFromTime(time);
            return (GetInterpolatedData(i));
        }

        public uint GetTimeFromIndex(int i)
        {
            if (i < 0 || i >= this.Count)
            {
                throw new ArgumentException("i = " + i + ", count = " + this.Count);
            }
            uint time = (uint)(i + this.start_time);
            return time;
        }


        public bool ContainsTime(uint time)
        {
            int ndx = (int)time - this.start_time;
            return (ndx >= 0 && ndx < this.Count);
        }






        public abstract double[] GetLastValues();

        public abstract void OverwriteLast(double[] updateData);

    }

}
