/* first experiment with storing results from midpoint solver more efficiently, in 2d arrays.
 * Not supported any more, superceded by Array1DSolverResult, which stores everything in a chain of 1d arrays.
 * Left here for future reference.

using SMLDC.Simulator.DiffEquations.Models;
using System;
using System.Collections.Generic;

namespace SMLDC.Simulator.DiffEquations.Solvers
{


    class Array2DSolverResult : SolverResultBase
    {

        private readonly List<double[,]> data;
        private int vector_size;


        public Array2DSolverResult(double[] initvector, uint nrData)
        {
            this.vector_size = initvector.Length;
            data = new List<double[,]>(10); // init. capacity, een gokje, maar deze lijsten worden niet extreem lang (tientallen? honderden? arrays)
            uint time = (uint)Math.Round(initvector[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]);
            start_time = (int)time;
            internalCount = 0;
            InitNextSegment(nrData);
            AddCopy(time, initvector);
        }


        private Array2DSolverResult(int initCapacity = 1000)
        {
            data = new List<double[,]>(initCapacity);
        } // voor deepcopy enz.



        public SolverResultBase DeepCopy()
        {
            Array2DSolverResult that;
            lock (data)
            {
                that = new Array2DSolverResult(this.data.Count);
                that.StoreInterpolation = this.StoreInterpolation;
                that.start_time = this.start_time;
                that.vector_size = this.vector_size;
                for (int i = 0; i < this.data.Count; i++)
                {
                    double[,] block = this.data[i];
                    int nrdata = block.GetLength(0);
                    double[,] blockCopy = new double[nrdata, vector_size];
                    for (int j = 0; j < nrdata; j++)
                    {
                        for (int k = 0; k < vector_size; k++)
                        {
                            blockCopy[j, k] = block[j, k];
                        }
                    }
                    that.data.Add(blockCopy);
                }
                that.internalCount = this.internalCount;
            }
            that.UpdateCount();
            return that;
        }


        private void InitNextSegment(uint blocksize)
        {
            lock (data)
            {
                int time_ndx = BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN;
                double[,] newblock = new double[blocksize, vector_size];
                for (int i = 0; i < blocksize; i++)
                {
                    newblock[i, time_ndx] = -1;  // dit is de code dat er nog geen data is (te beschouwen als 'null' voor deze index)
                }
                data.Add(newblock);
                //    indexInBlock = 0;
                UpdateCount();
            }
        }

        public override int GetCount() { return internalCount; }

        private int[] blockEndIndex;
        private int internalCount;

        private void UpdateCount()
        {
            lock (data)
            {
                blockEndIndex = new int[data.Count];
                // tel lengtes van alle stukken bij elkaar op, dat is de totale count!
                internalCount = 0;
                for (int i = 0; i < data.Count; i++)
                {
                    internalCount += data[i].GetLength(0);
                    blockEndIndex[i] = (internalCount - 1);
                }
            }
        }


        private void IntegrityCheck()
        {
            for (int i = 0; i < this.Count; i++)
            {
                uint tijd = GetTimeFromIndex(i);
                double[] values = GetValuesFromIndex(i);
                if ((int)Math.Round(values[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]) != tijd)
                {
                    throw new ArgumentException("tijd klopt niet met index");
                }
            }
        }



        public override double[] GetLastValues()
        {
            lock (data)
            {
                // aanname, altijd op moment dat een array block vol is.
                // TODO : klopt dit?
                int blockNdx = data.Count - 1;
                int laatsteNdx = data[blockNdx].GetLength(0) - 1;
                double[] results = new double[vector_size];
                for (int i = 0; i < results.Length; i++)
                {
                    results[i] = data[blockNdx][laatsteNdx, i];
                }
                return results;
            }
        }


        public override void OverwriteLast(double[] updateData)
        {
            lock (data)
            {
                int blockNdx = data.Count - 1;
                int laatsteNdx = data[blockNdx].GetLength(0) - 1;
                for (int i = 0; i < updateData.Length; i++)
                {
                    data[blockNdx][laatsteNdx, i] = updateData[i];
                }
            }
        }




        public override void AddCopy(uint time, double[] valuesVector)
        {
            lock (data)
            {
                int ndx = getIndexFromTime(time);
                //  de data wordt toch al gecopied in SetDataAtIndex, dus hier niet nodig
                SetDataRaw(ndx, valuesVector);
            }
        }




        public override void AddCopyOverwrite(SolverResultBase updatedData)
        {
            lock (data)
            {
                Add((Array2DSolverResult)updatedData, false); // TODO: bool does nothing
            }
        }

        public override void AddCopy(SolverResultBase updatedData)
        {
            lock (data)
            {
                Add((Array2DSolverResult)updatedData, false); // TODO: bool does nothing
            }
        }



        private void Add(Array2DSolverResult newData, bool orig)
        {
            // blokken achteraan toevoegen, mits het 'aansluit'
            lock (data)
            {

                int expectedNewTime = this.start_time + Count;
                bool ok = true;
                if (newData.start_time != expectedNewTime)
                {
                    ok = false;
                    if (this.data.Count == 1 && this.data[0].GetLength(0) == 1)
                    {
                        if (start_time == newData.start_time)
                        {
                            ok = true;
                            data.Clear();
                        }
                    }
                }
                if (ok)
                {
                    data.AddRange(newData.data);
                }
                else
                {

                    // throw new ArgumentException("tijden matchen niet!");
                    // helaas niet toe te voegen achteraan, DUS: 1-voor-1.
                    for (int n = 0; n < newData.data.Count; n++)
                    {
                        bool dezeOk = false;
                        //check if dit hele block vv kan zijn voor het oude
                        double[,] block = newData.data[n];
                        uint b_start_time = (uint)Math.Round(block[0, BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]);
                        uint b_end_time = (uint)Math.Round(block[block.GetLength(0) - 1, BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]);
                        int start_ndx = getIndexFromTime(b_start_time);
                        int end_ndx = getIndexFromTime(b_end_time);
                        if (start_ndx < 0 && end_ndx >= 0)
                        {
                            throw new ArgumentException("?");
                        }

                        if (start_ndx < 0 && end_ndx < 0)
                        {
                            // kijken of het aansluit op laatste block (zou moeten, gaten accepteren we niet in de sim!)
                            if (this.data[this.data.Count - 1][this.data[this.data.Count - 1].GetLength(0) - 1, BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN] + 1 == block[0, BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN])
                            {
                                this.data.Add(block);
                                dezeOk = true;
                            }
                            else
                            {
                                throw new ArgumentException("?");

                            }
                        }

                        if (!dezeOk)
                        {
                            GetDataBlockIndex(start_ndx, out int blockNdx_start, out int ndxInBlock_start);
                            GetDataBlockIndex(end_ndx, out int blockNdx_end, out int ndxInBlock_end);
                            if (blockNdx_start == blockNdx_end && ndxInBlock_start == 0)
                            {
                                int blocksize = this.data[blockNdx_start].GetLength(0);
                                if (blocksize - 1 == ndxInBlock_end) 
                                {
                                    // ja match!
                                    this.data[blockNdx_start] = block;
                                    dezeOk = true;
                                }

                            }

                            if (!dezeOk)
                            {
                                // doe iets,  overschijven per element?

                                // allemaal IN zelfde block!
                                int block_ndx = blockNdx_start;
                                int ndx_in_data = ndxInBlock_start; 
                                for (int i = 0; i < block.GetLength(0); i++)
                                {
                                    if (this.data[block_ndx][ndx_in_data, 0] != block[i, 0])
                                    {
                                        throw new ArgumentException("?");

                                    }
                                    for (int j = 0; j < block.GetLength(1); j++)
                                    {
                                        this.data[block_ndx][ndx_in_data, j] = block[i, j];
                                    }
                                    ndx_in_data++;
                                    if (ndx_in_data == this.data[block_ndx].GetLength(0))
                                    {
                                        //volgerd block
                                        block_ndx++;
                                        ndx_in_data = 0;
                                        // check of volgend block nog wel bestaat!
                                        if (this.data.Count == block_ndx)
                                        {
                                            // te ver!
                                            // resterend spul in nieuw block toevoegen aan this:
                                            double[,] newBlock = new double[block.GetLength(0) - i - 1, block.GetLength(1)];
                                            for (int k = i + 1; k < block.GetLength(0); k++)
                                            {
                                                for (int l = 0; l < block.GetLength(1); l++)
                                                {
                                                    newBlock[k - i - 1, l] = block[k, l];
                                                }
                                            }
                                            this.data.Add(newBlock);
                                            break; // for i, want klaar met dit nieuwe block
                                        }
                                    }
                                }
                                dezeOk = true;
                            }
                        }

                        if (!dezeOk)
                        {
                            throw new ArgumentException("?");
                        }
                    }
                }
                UpdateCount();
                //     IntegrityCheck();
            }
        }



        private void GetDataBlockIndex(int ndx, out int blockNdx, out int ndxInBlock)
        {
            lock (data)
            {

                if (ndx == 0)
                {
                    blockNdx = 0;
                    ndxInBlock = 0;
                    return;
                }
                if (ndx < 0 || ndx >= this.Count)
                {
                    //TODO: hier rare fout.. ws. door meerdere threads aan zelfde data? lock?
                    //throw new ArgumentException("i = " + ndx + ", count = " + this.Count);
                    blockNdx = -1;
                    ndxInBlock = -1;
                    return; // new Tuple<int, int>(-1, -1);
                }
                if (blockEndIndex == null)
                {
                    UpdateCount();
                }
                // binair zoeken:
                int block_ndx_0 = 0;
                int block_ndx_1 = blockEndIndex.Length - 1;
                int ndx_prev = -1;
                while (true)
                {
                    int ndx_half = (block_ndx_1 + block_ndx_0) / 2;
                    int blockEndIndex_at_ndx_half = blockEndIndex[ndx_half];
                    if (blockEndIndex_at_ndx_half == ndx)
                    {
                        blockNdx = ndx_half;
                        ndxInBlock = this.data[ndx_half].GetLength(0) - 1;
                        //return new Tuple<int, int>(ndx_half, this.data[ndx_half].GetLength(0)-1);
                        return;
                    }
                    if (blockEndIndex_at_ndx_half > ndx)
                    {
                        // naar beneden zoeken
                        block_ndx_1 = ndx_half;
                    }
                    else
                    {
                        // naar boven
                        if (ndx_half == block_ndx_0)
                        {
                            block_ndx_0 = ndx_half + 1;
                        }
                        else
                        {
                            block_ndx_0 = ndx_half;
                        }
                    }
                    if (ndx_half == ndx_prev)
                    {
                        break;
                    }
                    ndx_prev = ndx_half;

                }
                if (block_ndx_0 == 0)
                {
                    blockNdx = 0;
                    ndxInBlock = ndx;
                    return;
                    //return new Tuple<int, int>(0, ndx);
                }
                blockNdx = block_ndx_0;
                ndxInBlock = ndx - blockEndIndex[block_ndx_0 - 1] - 1;
                //                return new Tuple<int, int>(block_ndx_0, ndx - blockEndIndex[block_ndx_0-1] - 1);
            }
        }


        private Tuple<int, int> GetDataBlockIndex_OLD(int ndx)
        {
            lock (data)
            {
                int countTotNuToe = 0;
                for (int i = 0; i < data.Count; i++)
                {
                    int currentCount = data[i].GetLength(0);
                    countTotNuToe += currentCount;
                    if (countTotNuToe > ndx)
                    {
                        // gepasseerd, zit in deze chunk!
                        int gezochte_ndx_in_data_i = ndx - (countTotNuToe - currentCount);
                        return new Tuple<int, int>(i, gezochte_ndx_in_data_i);
                    }
                }
                return new Tuple<int, int>(-1, -1);
            }
        }







        // maakt altijd een copy!
        protected override void SetDataRaw(int ndx, double[] values)
        {
            lock (data)
            {
                //                Tuple<int, int> indexeringInBlock = GetDataBlockIndex(ndx);
                GetDataBlockIndex(ndx, out int blockIndex, out int indexInBlock);
                if (blockIndex >= 0)
                //                if (indexeringInBlock.Item1 >= 0)
                {
                    //  int blockIndex = indexeringInBlock.Item1;
                    //  int inBlockIndex = indexeringInBlock.Item2;
                    if (values == null)
                    {
                        data[blockIndex][indexInBlock, BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN] = -1; // null signaal
                    }
                    else
                    {
                        double[,] temp = data[blockIndex];
                        int values_Length = values.Length;
                        for (int v = 0; v < values_Length; v++)
                        {
                            temp[indexInBlock, v] = values[v];
                        }
                    }
                }
                else
                {
                    throw new ArgumentException("buiten de blokken!");
                }
            }
        }



        protected override double[] GetDataRaw(int ndx, ref double[] reusable_array)
        {
            lock (data)
            {

                if (ndx < 0 || ndx >= this.Count)
                {
                    //TODO: hier rare fout.. ws. door meerdere threads aan zelfde data? lock?
                    throw new ArgumentException("i = " + ndx + ", count = " + this.Count);
                }
                // TODO: ergens een indexje onthouden, zodat we sneller kunnen zoeken! TODO TODO TODO
                //Tuple<int, int> indexeringInBlock = GetDataBlockIndex(ndx);
                GetDataBlockIndex(ndx, out int blockNdx, out int ndxInBlock);
                //if (indexeringInBlock.Item1 >= 0)
                if (blockNdx >= 0)
                {
                    int blockIndex = blockNdx; // indexeringInBlock.Item1;
                    int gezochte_ndx_in_data_i = ndxInBlock; // indexeringInBlock.Item2;
                    double[] result;
                    if (reusable_array != null && reusable_array.Length == vector_size) {
                        result = reusable_array;
                    }
                    else
                    {
                        result = new double[vector_size];
                    }
                    for (int v = 0; v < result.Length; v++)
                    {
                        result[v] = data[blockIndex][gezochte_ndx_in_data_i, v];
                    }
                    return result;
                }
                return null;
            }
        }
    }
}
*/