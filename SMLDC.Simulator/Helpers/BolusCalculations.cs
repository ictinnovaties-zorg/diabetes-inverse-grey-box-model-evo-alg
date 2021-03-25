using SMLDC.Simulator.DiffEquations.Models;
using SMLDC.Simulator.Schedules;
using SMLDC.Simulator.Schedules.Events;
using SMLDC.Simulator.Utilities;
using System;
using System.Collections.Generic;
using static SMLDC.Simulator.Utilities.Enums;


namespace SMLDC.Simulator.Helpers
{
	public class BolusCalculations
	{


		/// <summary>
		/// This function calculates the bolus needed based on several parameters and returns this as an advice.
		/// </summary>
		/// <param name="carbohydrates">The amount of carbohydrates that are going to be consumed. (grams)</param>
		/// <param name="insulinToCarbohydratesRatio">The factor of how many carbohydrates can be handled by one amount of insuline. (g/e)</param>
		/// <param name="correctionFactor">The factor of how sensitive the patient is to insulin. (mmol/L/e)</param>
		/// <param name="currentBloodGlucose">The current (measured) blood glucose. (mmol/L)</param>
		/// <param name="targetBloodGlucose">The target blood glucose of the patient. (mmol/L)</param>
		/// <param name="insulinOnBoard">The active insulin that is in the patient's body. (IU)</param>
		/// <returns>A <see cref="BolusAdvice"/> based on the given values.</returns>
		private static readonly double carb_bolus_intake_round_at_g = 5;
		private static readonly double ins_bolus_intake_round_at_IU = 0.1;


		// running the simulation (binary search) to find best bolus
		// TODO: perfect staat nu altijd aan!!! HACK
		//   evt.TrueValue == NaN --> binary search (perfect) en dan noise erover.
		//   evt.TrueValue == INF --> binary search (perfect) 	
		public static BolusAdvice CalculateBolus(RandomStuff random, VirtualPatient patient, PatientEvent pEvent)
		{
			if(patient.RealData)
            {
				throw new ArgumentException("Patient met real data! Berekent geen bolus adviezen!");
            }

			// alleen de echte patient moet een bepaling v/d bolus doen. De particles etc... nemen gewoon over.
			if(patient.PatientType != PatientTypeEnum.STANDARD) //== PatientTypeEnum.BINARY_SEARCH)
			{
				return new BolusAdvice(pEvent);
			}

			bool auto_perfect = true; // Double.IsInfinity(pEvent.TrueValue);

			// Retrieve the glucose index from the model.
			int glucoseIndex = BergmanAndBretonModel.G_Glucose_ODEindex_MG_per_DL;
			double[] initialVector = patient.GeneratedData.GetLastValues();
			initialVector[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN] = 0;

			double testTijd_in_uren = 10;
			uint scheduleLength_in_Min = (uint)(60 * testTijd_in_uren);

			double threshold = 6;

			Schedule testSchedule = null;
			try
			{
				testSchedule = GetTestSchedule_relativeTime(patient.TrueSchedule, pEvent, scheduleLength_in_Min, true/*insulinbolus*/); // vullen met alle events na de huidige bolus event
			}
			catch (Exception e)
			{
				Console.WriteLine("BolusCalculations :: caught Exception: " + e);
				return new BolusAdvice(0, BolusAdviceType.NOTHING);
			}
			double totalInsulinNeeded_in_IU = BinarySearch.DoBinarySearch(random, patient, scheduleLength_in_Min, threshold, testSchedule, initialVector, BinarySearch.GlucoseWithinThreshold); // is in IU

			if (Double.IsNaN(totalInsulinNeeded_in_IU))
			{
				// kennelijk gaat Insuline niet helpen, dus dan juist het omgekeerde proberen: carbs eten
				testSchedule = GetTestSchedule_relativeTime(patient.TrueSchedule, pEvent, scheduleLength_in_Min, false/*food*/); // vullen met alle events na de huidige bolus event
				double amountOfCarbsToEat_in_G = BinarySearch.DoBinarySearch(random, patient, scheduleLength_in_Min, threshold, testSchedule, initialVector, BinarySearch.GlucoseWithinThresholdForFood);
					//  / 1000; //mg --> gr

				if (amountOfCarbsToEat_in_G >= carb_bolus_intake_round_at_g /*[gr]*/ )
				{
					//Log.Verbose("Calculated bolus advice. amountOfCarbsToEat: [{amountOfCarbsToEat}]. Used values: weight:[{weight}].",
					//	amountOfCarbsToEat_g);
					if (!auto_perfect)
					{
						double noise = amountOfCarbsToEat_in_G * 0.2 * (random.NextDouble() - 0.5);
						amountOfCarbsToEat_in_G += noise;
						amountOfCarbsToEat_in_G = Math.Max(amountOfCarbsToEat_in_G, 0);
					}
					// afronden naar porties van 5 gram:
					amountOfCarbsToEat_in_G = Math.Round(amountOfCarbsToEat_in_G / carb_bolus_intake_round_at_g) * carb_bolus_intake_round_at_g;
					return new BolusAdvice(amountOfCarbsToEat_in_G, BolusAdviceType.CARBS);
				}

			}
			else
			{
				if (totalInsulinNeeded_in_IU >= ins_bolus_intake_round_at_IU)
				{
					//Log.Verbose("Calculated bolus advice. totalInsulinNeeded: [{totalInsulinNeeded}].", totalInsulinNeeded_IU);
					if (!auto_perfect)
					{
						double noise = totalInsulinNeeded_in_IU * 0.15 * (random.NextDouble() - 0.5);
						totalInsulinNeeded_in_IU += noise;
						totalInsulinNeeded_in_IU = Math.Max(0, totalInsulinNeeded_in_IU);
					}

					totalInsulinNeeded_in_IU = Math.Round(totalInsulinNeeded_in_IU / ins_bolus_intake_round_at_IU) * ins_bolus_intake_round_at_IU;
					return new BolusAdvice(totalInsulinNeeded_in_IU, BolusAdviceType.INSULIN);
				}
			}
			return new BolusAdvice(0, BolusAdviceType.NOTHING);
		}




		// zoek tot volgende eat/ins.inj. event
		private static Schedule GetTestSchedule_relativeTime(Schedule schedule, PatientEvent pEvent, uint scheduleLength_in_Min, bool insulinBolus)
		{
			Schedule testSchedule = new Schedule();
			uint delta_t = 1;
			List<PatientEvent> relevant_events = new List<PatientEvent>();
			int nrEatEventFound = 0;
			for (int i = 0; i < schedule.GetEventCount(); i++)
			{
				//if (nrEatEventFound >= 2) { break; }
				PatientEvent evt = schedule.GetEventFromIndex(i);
				//if (nrEatEventFound >= 2) { break; }

				if (evt.EventType != PatientEventType.INSULIN)
				{
					if (evt.TrueStartTime >= pEvent.TrueStartTime && evt.TrueStartTime <= pEvent.TrueStartTime + scheduleLength_in_Min)
					{
						if (evt.EventType == PatientEventType.CARBS) // || evt.EventType == PatientEventType.INSULIN)
						{
							nrEatEventFound++;
							if (nrEatEventFound == 1)
							{
								relevant_events.Add(evt.CopyEvent());
							}
						}
						//if ((evt.EventType == PatientEventType.ActivityEvent && evt.StartTime < pEvent.StartTime + 60) || evt.EventType != PatientEventType.ActivityEvent)
						else //voor activity etc....
						{
							relevant_events.Add(evt.CopyEvent());
						}
					}
				}
			}
			// alle offsets rechtzetten:
			uint offset = pEvent.TrueStartTime;
			foreach (PatientEvent evt in relevant_events)
			{
				evt.TrueStartTime = evt.TrueStartTime - offset + delta_t;
				testSchedule.AddEvent((PatientEvent)evt);
			}
			// patientEvent toevoegen als insInj/Eat.
			PatientEvent newInsEvent = new PatientEvent((insulinBolus ? PatientEventType.INSULIN : PatientEventType.CARBS), delta_t /*start now*/, double.NaN);
			testSchedule.AddEvent(newInsEvent);

			//hr fixen:
			testSchedule.SetHeartRateGenerator(schedule.GetHeartRateGenerator().Copy(offset));

			return testSchedule;
		}


	}
}
