using CsvHelper;
using CsvHelper.TypeConversion;
using SMLDC.Simulator.DiffEquations.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace SMLDC.Simulator.DiffEquations.Solvers
{
    class Array1DSolverResult : SolverResultBase
    {

        private readonly List<double[]> data;

        public Array1DSolverResult(double[] initvector, uint nrData)
        {
            this.vector_size = initvector.Length;
            data = new List<double[]>(10); // init. capacity, een gokje, maar deze lijsten worden niet extreem lang (tientallen? honderden? arrays)
            uint time = (uint)Math.Round(initvector[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]);
            start_time = (int)time;
            internalCount = 0;
            InitNextSegment(nrData);
            AddCopy(time, initvector);
        }


        private Array1DSolverResult(int initCapacity = 1000)
        {
            data = new List<double[]>(initCapacity);
        } // voor deepcopy enz.



        public override SolverResultBase DeepCopy()
        {
            Array1DSolverResult that;
            lock (data)
            {
                that = new Array1DSolverResult(this.data.Count);
                that.StoreInterpolation = this.StoreInterpolation;
                that.start_time = this.start_time;
                that.vector_size = this.vector_size;
                for (int i = 0; i < this.data.Count; i++)
                {
                    double[] block = this.data[i];
                    int nrdata = block.GetLength(0);
                    double[] blockCopy = new double[block.Length];
                    Array.Copy(block, blockCopy, block.Length);
                    that.data.Add(blockCopy);
                }
                that.internalCount = this.internalCount;
                that.UpdateCount();
            }
            that.IntegrityCheck();
            return that;
        }


        private void InitNextSegment(uint blocksize)
        {
            lock (data)
            {
                int time_ndx = BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN;
                double[] newblock = new double[blocksize * vector_size];
                for (int i = 0; i < newblock.Length; i += vector_size)
                {
                    newblock[i] = -1;  // dit is de code dat er nog geen data is (te beschouwen als 'null' voor deze index)
                }
                data.Add(newblock);
                prev_ndx_AddCopySequential = -1;
                prev_blockNdx_AddCopySequential = -1;
                UpdateCount();
                //UpdateLUT();
                //this.IntegrityCheck();
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
                    internalCount += data[i].Length / vector_size;
                    blockEndIndex[i] = (internalCount - 1);
                }
            }
        }



        // a method for debugging/testing:
        private void IntegrityCheck()
        {
            return; // put some checks/tests here instead of return:
            
            lock (data)
            {   
                // simpele check: kloppen start en einde?
                double[] values = this.GetValuesFromIndex(this.internalCount - 1);
                int valuetijd = (int)Math.Round(values[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]);
                if (this.start_time + this.internalCount - 1 != valuetijd)
                {

                    int expected_time = start_time;
                    for (int i = 0; i < this.data.Count; i++)
                    {
                        double[] thisblock = data[i];
                        int thisBlockLength = thisblock.Length;
                        int start_time_this_block = (int)Math.Round(thisblock[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]);
                        if (start_time_this_block != expected_time)
                        {
                            // if this code is running in a command window, I want to see that an exception occurs
                            // but there is no logging currently, so just show a lot of messages :-) and sleep.
                            // If we don't do this, the command prompt window will just close and we don't know an error occured.
                            // When in debug mode, this spot would be a great place to put a breakpoint!
                            for (int ii = 0; ii < 100; ii++)
                            {
                                Console.WriteLine("oeps");
                            }
                            Thread.Sleep(1000000);
                            throw new ArgumentException("oeps!");
                        }
                        expected_time += thisBlockLength / vector_size - 1;
                        int end_time_this_block = (int)Math.Round(thisblock[thisBlockLength - vector_size + BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]);
                        if (end_time_this_block != expected_time)
                        {
                            for (int ii = 0; ii < 100; ii++)
                            {
                                Console.WriteLine("oeps");
                            }
                            Thread.Sleep(1000000);
                            throw new ArgumentException("oeps!");
                        }
                        expected_time++;
                    }
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
                int laatsteNdx = data[blockNdx].Length - vector_size;
                double[] results = new double[vector_size];
                Array.Copy(data[blockNdx], laatsteNdx, results, 0, vector_size);
                return results;
            }
        }


        public override void OverwriteLast(double[] updateData)
        {
            lock (data)
            {
                int blockNdx = data.Count - 1;
                int laatsteNdx = data[blockNdx].Length - vector_size;
                Array.Copy(updateData, 0, data[blockNdx], laatsteNdx, vector_size);

                // sanity check:
                int ndx = blockEndIndex[blockNdx];
                uint time = GetTimeFromIndex(ndx);
                uint tm = (uint)Math.Round(updateData[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]);
                if (tm != time)
                {
                    Console.WriteLine("oeps");
                    // if this code is running in a command window, I want to see that an exception occurs
                    // but there is no logging currently, so just show a lot of messages :-) and sleep.
                    // If we don't do this, the command prompt window will just close and we don't know an error occured.
                    // When in debug mode, this spot would be a great place to put a breakpoint!

                    for (int i = 0; i < 100; i++)
                    {
                        Console.WriteLine("oeps");
                    }
                    Thread.Sleep(1000000);
                    throw new ArgumentException("oeps!");
                }
            }
        }


        private int prev_ndx_AddCopySequential = -1;
        private int prev_blockNdx_AddCopySequential = -1;
        public override void AddCopySequential(uint time, double[] valuevector_orig)
        {
            // aanname: dit is 1 time hoger dan vorige aanroep
            lock(data)
            {
                int blockIndex;
                int indexInBlock;
                if (prev_ndx_AddCopySequential == -1)
                {
                    int ndx = this.getIndexFromTime(time);
                    GetDataBlockIndex(ndx, out blockIndex, out indexInBlock);
                }
                else
                {
                    blockIndex = prev_blockNdx_AddCopySequential;
                    prev_ndx_AddCopySequential ++;
                    indexInBlock = prev_ndx_AddCopySequential;
                }
                prev_blockNdx_AddCopySequential = blockIndex;
                prev_ndx_AddCopySequential = indexInBlock;
                SetDataRaw_Sequential(blockIndex, indexInBlock, valuevector_orig);
            }
        }

        private void SetDataRaw_Sequential(int blockIndex, int indexInBlock, double[] values)
        {
            lock (data)
            {
                if (blockIndex >= 0)
                {
                    if (values == null)
                    {
                        data[blockIndex][indexInBlock * vector_size + BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN] = -1; // null signaal
                    }
                    else
                    {
                        double[] temp = data[blockIndex];
                        Array.Copy(values, 0, temp, indexInBlock * vector_size, vector_size);
                    }
                }
                else
                {
                    // if this code is running in a command window, I want to see that an exception occurs
                    // but there is no logging currently, so just show a lot of messages :-) and sleep.
                    // If we don't do this, the command prompt window will just close and we don't know an error occured.
                    // When in debug mode, this spot would be a great place to put a breakpoint!
                    for (int i = 0; i < 100; i++)
                    {
                        Console.WriteLine("buiten de blokken!: blockIndex = " + blockIndex + "; indexInBlock = " + indexInBlock + "; values= " + (values == null ? "null" : "" + values[0]));
                    }
                    Thread.Sleep(1000000);
                    throw new ArgumentException("buiten de blokken!");
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

                // sanity check:
                if (valuesVector != null)
                {
                    uint tm = (uint)Math.Round(valuesVector[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]);
                    if (tm != time)
                    {
                        // if this code is running in a command window, I want to see that an exception occurs
                        // but there is no logging currently, so just show a lot of messages :-) and sleep.
                        // If we don't do this, the command prompt window will just close and we don't know an error occured.
                        // When in debug mode, this spot would be a great place to put a breakpoint!
                        for (int i = 0; i < 100; i++)
                        {
                            Console.WriteLine("oeps");
                        }
                        Thread.Sleep(1000000);
                        throw new ArgumentException("oeps!");
                    }
                }
            }
        }




        public override void AddCopyOverwrite(SolverResultBase updatedData)
        {
            lock (data)
            {
                Add((Array1DSolverResult)updatedData, false/*TODO: doet niks!*/);
            }
        }

        public override void AddCopy(SolverResultBase updatedData)
        {
            lock (data)
            {
                Add((Array1DSolverResult)updatedData, false/*TODO: doet niks!*/);
            }
        }



        private void Add(Array1DSolverResult newData, bool orig)
        {
            // blokken achteraan toevoegen, mits het 'aansluit'
            lock (data)
            {
                // ff 'metadata' opslaan voor debugging
                int orig_end_time = this.start_time + this.Count - 1;
                int[] blocksizes = new int[this.data.Count];
                for(int i = 0; i < this.data.Count; i++)
                {
                    blocksizes[i] = this.data[i].Length;
                }

                for (int n = 0; n < newData.data.Count; n++)
                {
                    //check if dit hele block vv kan zijn voor het oude
                    double[] newDataBlock_n = newData.data[n];
                    uint b_start_time = (uint)Math.Round(newDataBlock_n[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]);
                    uint b_end_time = (uint)Math.Round(newDataBlock_n[newDataBlock_n.Length - vector_size + BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]);
                    int start_ndx = getIndexFromTime(b_start_time);
                    int end_ndx = getIndexFromTime(b_end_time);

                    if (start_ndx < 0 && end_ndx >= 0)
                    {
                        // block begint voor bestaane data (check of start_ndx == -2)?!
                        // aan voorkant toevoegen... gebeurt dit ooit? 
                        // tot die tijd niet implementeren :-)

                        // if this code is running in a command window, I want to see that an exception occurs
                        // but there is no logging currently, so just show a lot of messages :-) and sleep.
                        // If we don't do this, the command prompt window will just close and we don't know an error occured.
                        // When in debug mode, this spot would be a great place to put a breakpoint!

                        for (int i = 0; i < 100; i++)
                        {
                            Console.WriteLine("Add: start/stop < 0");
                        }
                        Thread.Sleep(1000000);

                        throw new ArgumentException("?");
                    }

                    if (start_ndx >= 0 && end_ndx >= 0)
                    {
                        Overwrite_internal_block(newDataBlock_n, start_ndx, end_ndx);
                        IntegrityCheck();
                    }

                    else if (start_ndx == -1 && end_ndx == -1) // achter huidige data.
                    {
                        Add_new_block_at_end(newDataBlock_n);
                        IntegrityCheck();
                    }

                    else if(start_ndx >= 0 && end_ndx == -1)
                    {
                        GetDataBlockIndex(start_ndx, out int blockNdx_start, out int ndxInBlock_start);
                        if (ndxInBlock_start == 0)
                        {
                            // hele block(s) kunnen vv worden, ook al is this block niet even lang als de nieuwe
                            //oude weghalen:
                            //achterstevoren zodat de indices voor de te verwijderen ndx niet veranderen
                            int blockNdx_end = this.data.Count - 1;
                            for (int ndx_to_remove = blockNdx_end; ndx_to_remove >= blockNdx_start; ndx_to_remove--)
                            {
                                data.RemoveAt(ndx_to_remove);
                            }

                            this.Add_new_block_at_end(newDataBlock_n);
                            IntegrityCheck();
                        }
                        else
                        {
                            // OVERLAP, maar ook nieuwe data --> splitsen
                            // eerst overlap copy-pasten, daarna nieuw block toevoegen gevuld met restant
                            int laatste_ndx_in_new_block_matchend_met_bestaand = this.internalCount - start_ndx;
                            // tot hier is het overschrijven, daarna is het nieuwe data
                            this.Overwrite_internal_block(newDataBlock_n, start_ndx, this.internalCount - 1);
                            IntegrityCheck();
                            this.Add_new_block_at_end(newDataBlock_n, laatste_ndx_in_new_block_matchend_met_bestaand);
                            IntegrityCheck();
                        }
                    }
                    else
                    {
                        // if this code is running in a command window, I want to see that an exception occurs
                        // but there is no logging currently, so just show a lot of messages :-) and sleep.
                        // If we don't do this, the command prompt window will just close and we don't know an error occured.
                        // When in debug mode, this spot would be a great place to put a breakpoint!
                        for (int i = 0; i < 100; i++)
                        {
                            Console.WriteLine("oeps");
                        }
                        Thread.Sleep(1000000);
                        throw new ArgumentException("oeps!");

                    }
                }

                UpdateCount();
                IntegrityCheck();

                int newDataEndTime = (int)Math.Round(newData.data[newData.data.Count - 1][newData.data[newData.data.Count - 1].Length - vector_size + BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]);
                if (orig_end_time < newDataEndTime)
                {
                    int thisDataEndTime = (int)Math.Round(this.data[this.data.Count - 1][this.data[this.data.Count - 1].Length - vector_size + BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]);
                    if (thisDataEndTime != newDataEndTime)
                    {

                        for (int i = 0; i < 100; i++)
                        {
                            Console.WriteLine("oeps");
                        }
                        Thread.Sleep(1000000);
                        throw new ArgumentException("oeps!");
                    }
                }

            }
        }



        // kijken of het aansluit op laatste block (zou moeten, gaten accepteren we niet in de sim!)
        private bool Add_new_block_at_end(double[] newDataBlock_n, int start_in_new_block = 0)
        {
            lock (data)
            {
                int ndx_for_time = this.data[this.data.Count - 1].Length - vector_size + BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN;
                int this_data_end_time = (int)Math.Round(this.data[this.data.Count - 1][ndx_for_time]);
                int new_data_begin_time = (int)Math.Round(newDataBlock_n[start_in_new_block * vector_size + BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN]);
                if (this_data_end_time + 1 == new_data_begin_time)
                {
                    if (start_in_new_block == 0)
                    {
                        //hele block overnemen, geen copy! 
                        // nb hebben elders wellicht deepcopy nodig
                        this.data.Add(newDataBlock_n);
                        UpdateCount();

                        return true;
                    }
                    int nr_data_rijen = newDataBlock_n.Length / vector_size - start_in_new_block;
                    double[] toe_te_voegen_block = new double[nr_data_rijen * vector_size];
                    Array.Copy(newDataBlock_n, start_in_new_block * vector_size, toe_te_voegen_block, 0, toe_te_voegen_block.Length);
                    this.data.Add(toe_te_voegen_block);
                    UpdateCount();

                    return true;
                }
                else
                {
                    for (int i = 0; i < 100; i++)
                    {
                        Console.WriteLine("this_data_end_time = " + this_data_end_time + "; new_data_begin_time = " + new_data_begin_time + "; start_in_new_block = " + start_in_new_block);
                    }
                    Thread.Sleep(1000000);
                    throw new ArgumentException("?");
                }
            }
        }

        private void Overwrite_internal_block(double[] newDataBlock_n, int start_ndx, int end_ndx)
        {
            lock (this.data)
            {
                // overlap met huidig, maar niet MEER dan huidig.
                // juiste blocks opzoeken en juiste stuk uit newdata copy-pasten
                GetDataBlockIndex(start_ndx, out int blockNdx_start, out int ndxInBlock_start);
                GetDataBlockIndex(end_ndx, out int blockNdx_end, out int ndxInBlock_end);

                // uitzonderlijke situatie: matcht precies met een heel block (of meerdere)
                if (ndxInBlock_start == 0)
                {
                    int this_block_end_length = (this.data[blockNdx_end].Length - 1) / vector_size;
                    int new_block_end_length = (newDataBlock_n.Length - 1) / vector_size;
                    if (new_block_end_length == ndxInBlock_end && this_block_end_length == ndxInBlock_end)
                    {
                        // huidige blocks ditchen en vervangen door nieuwe!
                        // nb. dan hebben we soms deepcopy nodig!

                        //achterstevoren zodat de indices voor de te verwijderen ndx niet veranderen
                        for (int ndx_to_remove = blockNdx_end; ndx_to_remove >= blockNdx_start; ndx_to_remove--)
                        {
                            data.RemoveAt(ndx_to_remove);
                        }
                        data.Insert(blockNdx_start, newDataBlock_n);
                        return;
                    }
                }

                // nieuw block is meerdere  this.data blocks.
                int ndx_in_new_block = 0;
                for (int bl_ndx = blockNdx_start; bl_ndx <= blockNdx_end; bl_ndx++)
                {
                    int start_ndx_in_dit_block = 0;
                    if (bl_ndx == blockNdx_start)
                    {
                        start_ndx_in_dit_block = ndxInBlock_start;
                    }
                    int end_ndx_in_dit_block = (this.data[bl_ndx].Length - 1)/vector_size;
                    if (bl_ndx == blockNdx_end)
                    {
                        end_ndx_in_dit_block = ndxInBlock_end;
                    }
                    int length = (end_ndx_in_dit_block - start_ndx_in_dit_block + 1) * vector_size;
                    Array.Copy(newDataBlock_n, ndx_in_new_block, this.data[bl_ndx], start_ndx_in_dit_block * vector_size, length);
                    ndx_in_new_block += length;
                }
            }
        }


        // some code to check performance of different ways of getting the indices:
        //private void GetDataBlockIndex(int ndx, out int blockNdx, out int indexInBlock)
        //{
        //    lock(data)
        //    {
        //        if (data.Count <- 5)
        //        {
        //            GetDataBlockIndex_LIN(ndx, out blockNdx, out indexInBlock);
        //        }
        //        else
        //        {
        //            GetDataBlockIndex_BIN(ndx, out blockNdx, out indexInBlock);
        //        }

        //        //GetDataBlockIndex_BIN(ndx, out int blockNdx_BIN, out int indexInBlock_BIN);
        //        //GetDataBlockIndex_LUT(ndx, out int blockNdx_LIN, out int indexInBlock_LIN);
        //        //if (blockNdx_BIN != blockNdx_LIN || indexInBlock_BIN != indexInBlock_LIN)
        //        //{

        //        //    GetDataBlockIndex_LUT(ndx, out blockNdx_LIN, out indexInBlock_LIN);
        //        //    GetDataBlockIndex_LUT(ndx, out blockNdx_LIN, out indexInBlock_LIN);
        //        //}
        //        //blockNdx = blockNdx_BIN;
        //        //indexInBlock = indexInBlock_BIN;
        //    }
        //}


        // BIN is iets sneller (5-10%?) dan LIN zoeken.
        private void GetDataBlockIndex/*_BIN*/(int ndx, out int blockNdx, out int ndxInBlock)
        {
            lock (data)
            {
                if (ndx < 0 || ndx >= this.internalCount)
                {
                    blockNdx = -1;
                    ndxInBlock = -1;
                    return; 
                }
                if (ndx == 0)
                {
                    blockNdx = 0;
                    ndxInBlock = 0;
                    return;
                }

                if (blockEndIndex == null)
                {
                    UpdateCount();
                }
                // binair zoeken:
                int block_ndx_0 = 0;
                int block_ndx_1 = (blockEndIndex.Length - 1);
                int ndx_prev = -1;
                while (true)
                {
                    int ndx_half = (block_ndx_1 + block_ndx_0) / 2;
                    int blockEndIndex_at_ndx_half = blockEndIndex[ndx_half];
                    if (blockEndIndex_at_ndx_half == ndx)
                    {
                        blockNdx = ndx_half;
                        // dit is een fictieve index, omdat we hier een 2d array simuleren. Dit is de [ndxInBlock, ...] eerste index.
                        ndxInBlock = (this.data[ndx_half].Length - 1)/ vector_size;
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
                }
                blockNdx = block_ndx_0;
                ndxInBlock = ndx - blockEndIndex[block_ndx_0 - 1] - 1;
            }
        }



        // linear search, slower than binary seach (but easier to debug and check for correctness, so can be used 
        // to run parallel to the binary search version to test if all outputs are the same, after the binary search version
        // is altered.
        private void GetDataBlockIndex_LIN(int ndx, out int blockNdx, out int ndxInBlock)
        {
            lock (data)
            {
                if (ndx < 0 || ndx >= this.internalCount)
                {
                    blockNdx = -1;
                    ndxInBlock = -1;
                    return; 
                }
                if (ndx == 0)
                {
                    blockNdx = 0;
                    ndxInBlock = 0;
                    return;
                }

                if (blockEndIndex == null)
                {
                    UpdateCount();
                }
                if(ndx < blockEndIndex[0])
                {
                    blockNdx = 0;
                    ndxInBlock = ndx;
                    return;
                }

                for (blockNdx = 0; blockNdx < blockEndIndex.Length; blockNdx++)
                {
                    if (ndx <= blockEndIndex[blockNdx])
                    {
                        // gepasseerd, zit in deze chunk!
                        if (blockNdx == 0)
                        {
                            ndxInBlock = ndx;
                        }
                        else
                        {
                            ndxInBlock = ndx - 1 -blockEndIndex[blockNdx - 1];
                        }
                        return;
                    }
                }
                blockNdx = -1;
                ndxInBlock = -1;
            }
        }





        /*
         // a lookup table for the indices of the blocks. Turns out to be slower than binary and linear search.
          
        private void UpdateLUT()
        {
            LUT_BlockNdx = new int[this.internalCount];
            LUT_NdxInBlock = new int[this.internalCount];
            int ndx = 0;
            for (int i = 0; i < data.Count; i++)
            {
                int length = data[i].Length / vector_size;
                for (int j = 0; j < length; j++)
                {
                    LUT_BlockNdx[ndx] = i;
                    LUT_NdxInBlock[ndx] = j;
                    ndx++;
                }
            }

        }

        private int[] LUT_BlockNdx;
        private int[] LUT_NdxInBlock;

        // LUT is behoorlijk trager (20-30%?) dan BIN en LIN zoeken.
        private void GetDataBlockIndex_LUT(int ndx, out int blockNdx, out int ndxInBlock)
        {
            lock (this.data)
            {
                if (ndx < 0 || ndx >= this.internalCount)
                {
                    blockNdx = -1;
                    ndxInBlock = -1;
                    return;
                }
                if (ndx == 0)
                {
                    blockNdx = 0;
                    ndxInBlock = 0;
                    return;
                }
                if(LUT_BlockNdx.Length != this.internalCount)
                {
                    UpdateLUT();
                }

                blockNdx = LUT_BlockNdx[ndx];
                ndxInBlock = LUT_NdxInBlock[ndx];
            }
        }
        */




        // maakt altijd een copy!
        protected override void SetDataRaw(int ndx, double[] values)
        {
            lock (data)
            {

                GetDataBlockIndex(ndx, out int blockIndex, out int indexInBlock);
                if (blockIndex >= 0)
                {
                    if (values == null)
                    {
                        data[blockIndex][indexInBlock * vector_size + BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN] = -1; // null signaal
                    }
                    else
                    {
                        double[] temp = data[blockIndex];
                        Array.Copy(values, 0, temp, indexInBlock * vector_size, vector_size);
                    }
                }
                else
                {
                    for (int i = 0; i < 100; i++)
                    {
                        Console.WriteLine("buiten de blokken!: ndx = " + ndx + " ; blockIndex = " + blockIndex + "; indexInBlock = " + indexInBlock + "; values= " + (values == null ? "null" : "" + values[0]));
                    }
                    Thread.Sleep(1000000);
                    throw new ArgumentException("buiten de blokken!");
                }
               // this.IntegrityCheck();
            }
        }



        protected override double[] GetDataRaw(int ndx, ref double[] reusable_array)
        {
            lock (data)
            {

                if (ndx < 0 || ndx >= this.internalCount)
                {
                    //TODO: hier rare fout.. ws. door meerdere threads aan zelfde data? lock?
                    for (int i = 0; i < 100; i++)
                    {
                        Console.WriteLine("GetDataRaw - i = " + ndx + ", count = " + this.Count);
                    }
                    Thread.Sleep(1000000);
                    throw new ArgumentException("GetDataRaw - i = " + ndx + ", count = " + this.Count);
                }
                // TODO: ergens een indexje onthouden, zodat we sneller kunnen zoeken! TODO TODO TODO
                GetDataBlockIndex(ndx, out int blockIndex, out int ndxInBlock);
                if (blockIndex >= 0)
                {
                    double[] result;
                    if (reusable_array != null && reusable_array.Length == vector_size)
                    {
                        result = reusable_array;
                    }
                    else
                    {
                        result = new double[vector_size];
                    }
                    Array.Copy(data[blockIndex], ndxInBlock * vector_size, result, 0, vector_size);
                    if(result[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN] == -1)
                    {
                        return null;
                    }
                    return result;
                }
                return null;
            }
        }
    }
}
