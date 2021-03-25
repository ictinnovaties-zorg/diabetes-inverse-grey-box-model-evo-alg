using SMLDC.Simulator.DiffEquations.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SMLDC.Simulator.DiffEquations.Solvers
{

    // Note: there have been some experiments with different time steps in the MidPointSolver,
    // but they are not compatible with the Array1DSolverResult, so this is no longer used.
    //
    // The idea of bigger solver steps was to increase speed (on the 'boring' parts of the curve, and then scale back to step=1 
    // on more interesting parts where the graph changes more). But the Array1DSolverResults store all data in one big 1d array
    // under the assumption that every data point is 1 minute later than the previous one. 
    //
    public class MidpointSolver
    {
        private readonly object MidPointSolverLockObject = new object();
        public void UpdateNrOfSolverSteps(int steps)
        {
            lock (MidPointSolverLockObject)
            {
                NrOfSolverStepsSinceLastStopwatch += (ulong)steps;
                NrOfSolverSteps += (ulong)steps;
            }
        }

        private ulong NrOfSolverSteps = 0;
        private ulong NrOfSolverStepsSinceLastStopwatch = 0;
        private Stopwatch NrOfSolverStepsStopwatch = new Stopwatch();
        public void ResetNrOfSolverSteps()
        {
            lock (MidPointSolverLockObject)
            {
                NrOfSolverSteps = 0;
                NrOfSolverStepsSinceLastStopwatch = 0;
                NrOfSolverStepsStopwatch.Restart();
            }
        }
        public ulong GetNrOfSolverSteps() { return NrOfSolverSteps; }
        public int SolverStepsPerSecond()
        {
            int fps = (int)Math.Round(NrOfSolverStepsSinceLastStopwatch / (double)NrOfSolverStepsStopwatch.Elapsed.TotalSeconds);
            lock (MidPointSolverLockObject)
            {
                NrOfSolverStepsSinceLastStopwatch = 0;
                NrOfSolverStepsStopwatch.Restart();
            }
            return fps;
        }




        // sanity check, Gluc is altijd >= 0. Idem X, I, ....:
        private int[] sanityCheckNotBelowZero = null;

        private readonly int[] sanityCheckNotBelowZero_noActivity = {
            BergmanAndBretonModel.G_Glucose_ODEindex_MG_per_DL,
            BergmanAndBretonModel.X_InsulinEffectiveness_ODEindex__1_per_MIN,
            BergmanAndBretonModel.I_Insulin_ODEindex__mIU_per_L, /*Ie_ExogenousInsulinIndex,*/
        };

        private readonly int[] sanityCheckNotBelowZero_incl_Activity = {
            BergmanAndBretonModel.G_Glucose_ODEindex_MG_per_DL,
            BergmanAndBretonModel.X_InsulinEffectiveness_ODEindex__1_per_MIN,
            BergmanAndBretonModel.I_Insulin_ODEindex__mIU_per_L, /*Ie_ExogenousInsulinIndex,*/
            BergmanAndBretonModel.Gamma_EnergyExpenditure_ODEindex
        };


        // todo: zinnige waardes voor elke index
        private readonly double[]  intervalMargin = { 0, 15, 250, 0.01, 5, 0.01, 0.01, 0.01, 0.01, 0.01, 0.01, 0.01 };


        public SolverResultBase Solve(VirtualPatient patient, int interval_, uint startTime, uint endTime)
        {
            if(sanityCheckNotBelowZero == null)
            {
                if(patient.UsesActivityModel) { sanityCheckNotBelowZero = sanityCheckNotBelowZero_incl_Activity; }
                else { sanityCheckNotBelowZero = sanityCheckNotBelowZero_noActivity; }
            }

            bool fixedInterval = (interval_ >= 1);
            int interval = (int) Math.Abs(interval_);
            int minInterval = interval;
            bool useMidpoint = true;
            // midpoint:  0.3 --> err < 0.01.  1 --> error < 0.03.  Bij 3 groeit ie al snel naar 0.25 (na 3000 min, dus na 2 dagen)
            // Euler: 1 --> 0.5 error op Gluc EN soms rare sprongen , dus niet gebruiken met interval > 1
            int intervalFactor = 2;

            // Check for valid arguments.
            if (endTime <= 0)
            {
                var exception = new ArgumentException("EndTime has to be bigger than 0.");
                throw exception;
            }
            if (startTime < 0)
            {
                var exception = new ArgumentException("StartTime has to be bigger than 0 or 0.");
                throw exception;
            }
            if (startTime >= endTime)
            {
                var exception = new ArgumentException("StartTime has to be smaller than EndTime.: " + startTime + " -- " + endTime);
                throw exception;
            }


            // Steps is the amount of steps to take
            uint steps = (uint)Math.Ceiling((endTime - startTime) / (double)interval);

            //Vector<double> y = new DenseVector(patient.StartData);
            
            double[] y = Utilities.CloneUtilities.CloneArray(patient.GetValuesFromTime(startTime));

            double[] currentDelta = new double[y.Length];
            int y_Length = y.Length;

            uint currentTime = startTime;
            y[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN] = startTime;

            //// The first data point is the starting data.
            //SolverResultBase result = new SolverResultBase((int)Math.Ceiling((double)endTime - startTime));
            //result.AddCopy(currentTime, y);
            // The first data point is the starting data.
            SolverResultBase result = null; //SolverResultFactory.CreateSolverResult(y);

            //Vector<double> firstHalfDelta = null;
            double[] firstHalfDelta_and_step = new double[patient.Model.ExpectedVectorLength]; //hier al init, scheelt tijdrovende checks in Derive
            double[] deltaY = new double[patient.Model.ExpectedVectorLength];
            double interval05d = interval * 0.5d;


            for (int i = 0; i < steps; i++)
            {
                if (currentTime + interval > endTime)
                {
                    interval = (int) (endTime - currentTime);
                    if (interval <= 0)
                    {
                        break;
                    }
                }
                //if (y[1] > 10000) //bg
                //{
                //   // Console.WriteLine("BG > 10000");
                //}
                // ter vergelijking: EUler solver  :::  y += patient.Model.Derive(currentTime, y) * interval;


                // Calculate direction in 2 equal parts. 
                // The second half of the equation is used for the change to Y.
                // The set model is used to calculate the new values in Y.

                //// Vector<double> firstHalfDelta = patient.Model.Derive(currentTime, result[currentTime]);
                ///// Vector<double> stepFirstHalf = y + (firstHalfDelta * interval * 0.5d);


                // voeg de HR toe:
                if (patient.Model.UseActivity)
                {
                    y[BergmanAndBretonModel.HR_HeartRate_ODEindex__Hz] = patient.TrueSchedule.GetHeartRate(currentTime);
                }


                if (useMidpoint)
                {
                    firstHalfDelta_and_step = patient.Model.Derive(currentTime, y, firstHalfDelta_and_step);
                    for (int jj = 0; jj < y_Length; jj++)
                    {
                        firstHalfDelta_and_step[jj] = y[jj] + firstHalfDelta_and_step[jj] * interval05d;
                    }

                    // in midpoint solver kan dit ding onder nul gaan 'middenin',  dus voordat de nietOnderNul check (aan einde v/d midpoint) gedaan wordt.
                    foreach (int ndx in sanityCheckNotBelowZero)
                    {
                        if (firstHalfDelta_and_step[ndx] < 0)
                        {
                            firstHalfDelta_and_step[ndx] = 0;
                        }
                    }

                    //double[] stepFirstHalfA = firstHalfDelta_and_step;

                    // DeltaY contains the changes to make to y.
                    /////  Vector<double> deltaY = patient.Model.Derive(currentTime + interval * 0.5d, stepFirstHalf) * interval;
                    //// y += deltaY;
                    deltaY = patient.Model.Derive(currentTime + interval05d, firstHalfDelta_and_step, deltaY);//
                }
                else
                { //Euler
                  // ter vergelijking: EUler solver  :::  y += patient.Model.Derive(currentTime, y) * interval;
                    deltaY = patient.Model.Derive(currentTime, y, deltaY);
                }
                ///                y += deltaY * interval;

                 
                //double[] y_new = new double[y.Length];
                for (int jj = 0; jj < y_Length; jj++)
                {
                    //y[jj] = y[jj] + deltaY[jj] * interval;
                    double temp = deltaY[jj] * interval;
                    currentDelta[jj] = temp;
//                    y_new[jj] = y[jj] + temp;
                    y[jj] += temp;
                }

                foreach (int ndx in sanityCheckNotBelowZero)
                {
//                    if (ndx < deltaY.Length && y_new[ndx] < 0)
                    if (y[ndx] < 0)
                    {
                      //  y_new[ndx] = 0;
                        y[ndx] = 0;
                    }
                }

                //for (int ndx = 0; ndx < y_new.Length; ndx++) {
                //    if(double.IsNaN(y_new[ndx]) || double.IsInfinity(y_new[ndx] ))
                //    {

                //    }
                //}

                //vul tussenliggende tijden met null:
                for (uint t = 1; t < interval; t++)
                {
                    throw new NotImplementedException("//TODO: gaat mis bij ARraySolveRResult... dan moet nr steps anders bepaald worden!");
                    //result.AddOrig(currentTime + t, null);
                    result.AddNull(currentTime + t); //TODO: gaat mis bij ARraySolveRResult... dan moet nr steps anders bepaald worden!
                }

                // Calculate new time.
                currentTime += (uint) interval;
//                y_new[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN] = currentTime;
                y[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN] = currentTime;

                //copy moet opgeslagen worden, omdat y zelf steeds verandert.
                // alternatief is hier in solver y copy doen.
                // y is in solver 2x input. Maar er moet sowieso altijd een 'new' array aangemaakt worden!
                // maar aangezien hierboven de for(jj --> y) loop toch moet, kan dat de copy zijn, en  dat 
                //scheelt 1 copy

                //result.AddCopy(currentTime, y);
                if (result == null)
                {
                    // todo: dit aan begin, zodat de if niet nodig is? Dan moet de result 'leeg' aangemaakt worden
                    result = SolverResultFactory.CreateSolverResult(y, steps);
                }
                else
                {
                   // result.AddCopy(currentTime, y);
                    result.AddCopySequential(currentTime, y);
                }
                if (true && fixedInterval == false)
                {
                    //update interval. Als delta klein is, dan kan het:
                    //todo: Evt gesorteerd op grootste kans van early abort???? TODO
                    bool alles_onder = true;
                    for (int k = 0; k < y.Length; k++)
                    {
                        if (k != BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN &&  Math.Abs(currentDelta[k]) > intervalMargin[k])
                        {
                            alles_onder = false;
                            break;
                        }
                    }
                    if (alles_onder)
                    {
                        if (interval < 30)
                        { //upscale
                            interval = interval + intervalFactor;
                            //if (interval > maxInterval)
                            //{
                            //    maxInterval = interval;
                            //  //  Console.WriteLine("maxInterval = " + maxInterval);
                            //}
                        }
                    }
                    else
                    { //downscale
                        if (interval > intervalFactor)
                        {
                            interval = interval - intervalFactor;
                            if (interval < minInterval) { interval = minInterval; }
                        }
                        else
                        {
                            interval = minInterval;
                        }
                    }
                   // Console.WriteLine("interval = " + interval);
                }
                //y = y_new;
            }
            

            return result;
        }



    }
}
