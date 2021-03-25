using SMLDC.Simulator.DiffEquations.Models;
using SMLDC.Simulator.Schedules;
using SMLDC.Simulator.Schedules.Events;
using SMLDC.Simulator.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using static SMLDC.Simulator.Utilities.Enums;

namespace SMLDC.Simulator.Helpers
{
    public class BinarySearch
    {

		public static int solver_interval = 1;

		private static uint MaxNrOfBinarySearchSteps = 50;




		// binary search for correct insulin value to reach Gbase
		// return IU als het om insuline gaat
		// returns G als het om carbs gaat.
		// dus de eenheden waarin ook de EVENTS zijn.
		// changes value for the InsulinInjection event (the first one) IF it is NAN
		public static double DoBinarySearch(RandomStuff random, VirtualPatient patient, uint testTijd_in_minuten, double threshold, Schedule origSchedule,
									double[] initialVector, BinarySearchCheckStep ThresholdCheckFunction)
		{
			GlucoseInsulinSimulator simulator = patient.simulator;

			int binarySearchStepCounter = 0;
			//uint testTijd_in_minuten = (uint)(testTijd_in_uren * 60);
			// Setup variables for this test.
			double insulinAmountUI = 0.1;
			double insulinStep = insulinAmountUI;

			// begin klein, (verdubbelend) steeds groter zoeken totdat we over gegaan
			//  het threshold punt heen zijn. Daarna binair steeds kleiner zoeken
			bool upscaleSearch = true;
			double firstGlucMmnt = Double.NaN;

			double glucoseBase = patient.Model.Gb_in_MG_per_DL;

			// Retrieve the glucose index from the model.
			int glucoseIndex = BergmanAndBretonModel.G_Glucose_ODEindex_MG_per_DL;

			//binair zoeken naar juiste insuline bolus om binnen bepaalde tijd Glucose op Gb te krijgen.
			VirtualPatient binarySearchPatient = null;
			while (true)
			{
				// insert muInsulinAmount into test schedule:
				Schedule testSchedule = FixScheduleReplaceNanWithValue(origSchedule, insulinAmountUI);

				// Create a new patient with the same parameters as the patient
				binarySearchPatient = new VirtualPatient(patient, testSchedule, initialVector);
				binarySearchPatient.PatientType = PatientTypeEnum.BINARY_SEARCH;

				// Create patient controller and simulate the patient.

				simulator.RunOnePatient(random, binarySearchPatient, 0, (int) testTijd_in_minuten, solver_interval);

				// Retrieve the glucose value at the end of the simulation.
				double glucoseAtEnd = binarySearchPatient.GeneratedData.GetLastValues()[glucoseIndex];
				if (Double.IsNaN(firstGlucMmnt))
				{
					firstGlucMmnt = glucoseAtEnd;
				}
				else
				{
					if (upscaleSearch)
					{
						//check of we geflipt zijn naar andere kant v/d glucoseBase
						if (firstGlucMmnt > glucoseBase && glucoseAtEnd < glucoseBase)
						{
							//flipped:
							upscaleSearch = false;
						}
						else if (firstGlucMmnt < glucoseBase && glucoseAtEnd > glucoseBase)
						{
							//flipped:
							upscaleSearch = false;
						}
					}
				}
				if (upscaleSearch)
				{
					insulinStep = insulinAmountUI; //effectief verdubbelen in ThresholdCheckFunction
				}
				else
				{
					// Half the steps.
					insulinStep *= 0.5;
				}
				// Check if the glucose is within the threshold.
				bool inRange = ThresholdCheckFunction(threshold, glucoseAtEnd, glucoseBase, insulinStep, ref insulinAmountUI);
				if(inRange) { return insulinAmountUI; } // klaar!!!

				binarySearchStepCounter++;
				if (insulinAmountUI < insulinStep) {
					return 0;
				}


					
				if (binarySearchStepCounter > MaxNrOfBinarySearchSteps)
				{
					//throw new ArgumentException("Binary search failed to find needed insulin.");
			//		Console.WriteLine("Binary search failed to find needed insulin.");
					return 0;
				}
			}
			//return insulinAmountUI;
		}





		/////////////////////////////////// helpers ///////////////////////////////////




		public delegate bool BinarySearchCheckStep(
			double threshold,
			double glucoseAtEnd,
			double glucoseBase,
			double muInsulinStep,
			ref double muInsulinAmount);


		public static bool GlucoseWithinThreshold(
			double threshold,
			double glucoseAtEnd,
			double glucoseBase,
			double muInsulinStep,
			ref double muInsulinAmount)
		{
			if (glucoseAtEnd < glucoseBase - threshold)
			{
				muInsulinAmount -= muInsulinStep;
			}
			else if (glucoseAtEnd > glucoseBase + threshold)
			{
				muInsulinAmount += muInsulinStep;
			}
			else
			{
				return true;
			}
			return false;
		}


		public static bool GlucoseWithinThresholdForFood(
			double threshold,
			double glucoseAtEnd,
			double glucoseBase,
			double muInsulinStep,
			ref double amountOfFoodGr)
		{
			if (glucoseAtEnd < glucoseBase - threshold)
			{
				amountOfFoodGr += muInsulinStep;
			}
			else if (glucoseAtEnd > glucoseBase + threshold)
			{
				amountOfFoodGr -= muInsulinStep;
			}
			else
			{
				return true;
			}
			return false;
		}






		// insert muInsulinAmount in the insulin injection event
		private static Schedule FixScheduleReplaceNanWithValue(Schedule s, double insulinAmountUI)
		{
			Schedule schedule = s.DeepCopy();
			int nr_insulin_events = 0; // sanity check
			for (int i = 0; i < schedule.GetEventCount(); i++)
			{
				PatientEvent evt = schedule.GetEventFromIndex(i);
				if (Double.IsNaN(evt.TrueValue))
				{
					//// we willen natuurlijk niet de gluc.mmnts die op NaN (onbekend) staan overschrijven
					////if (evt.EventType != PatientEventType.GLUCOSE_MASUREMENT)
					{
						evt.TrueValue = insulinAmountUI;
						nr_insulin_events++;
					}
				}
			}
			return schedule;
		}







	}
}
