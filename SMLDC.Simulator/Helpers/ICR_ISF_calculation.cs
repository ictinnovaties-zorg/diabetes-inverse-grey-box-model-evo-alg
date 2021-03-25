using SMLDC.Simulator.DiffEquations.Models;
using SMLDC.Simulator.Models.HeartRate;
using SMLDC.Simulator.Schedules;
using SMLDC.Simulator.Schedules.Events;
using SMLDC.Simulator.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using static SMLDC.Simulator.Utilities.Enums;

namespace SMLDC.Simulator.Helpers
{
    public class ICR_ISF_calculation
    {
        public static uint TestDuration_in_min = (uint) (60 * 3); //  TODO: 7 is gekozen omdat daar (iig bij 1 patient) ongeveer stabiele waardes voor ICR en ISF waren...
        public static double Gluc_threshold = 1; // voor nauwkeurigheid. Wanneer mag bin.search stoppen?
        public static uint injectionTime_in_min = 15;

        public static double CalculateICR(RandomStuff random, VirtualPatient patientOrig)
        {
            // "the doctors way", simuleer een lab experiment waarbij de virtuele patient (ruwweg) de test ondergaat die past bij ICR
            int mealSizeInGram_for_ICR_Test = 50;
            uint injectionTimeBeforeMeal_in_min = 10;

            Schedule testSchedule = new Schedule();
            testSchedule.AddEvent(new PatientEvent(PatientEventType.INSULIN, injectionTime_in_min, double.NaN, double.NaN));
            testSchedule.AddEvent(new PatientEvent(PatientEventType.CARBS, injectionTime_in_min + injectionTimeBeforeMeal_in_min, mealSizeInGram_for_ICR_Test, double.NaN));
            testSchedule.AddEvent(new PatientEvent(PatientEventType.STOP, TestDuration_in_min, 0, double.NaN));
            AddFlatHeartrate(random, patientOrig, testSchedule);

            VirtualPatient testPatient = new VirtualPatient(patientOrig, testSchedule, null);
            double[] initialVector = testPatient.GetStartData();

            // binary search voor juiste hoeveelheid ins.
            double totalInsulinNeeded_in_IU = BinarySearch.DoBinarySearch(random, testPatient, TestDuration_in_min, Gluc_threshold, testSchedule, initialVector, BinarySearch.GlucoseWithinThreshold);

            // Do the actual calculation for the ICR.
            return mealSizeInGram_for_ICR_Test / totalInsulinNeeded_in_IU;
        }


        private static void AddFlatHeartrate(RandomStuff random, VirtualPatient patientOrig, Schedule testSchedule)
        {
            RandomHeartRateGenerator aroundBaseHeartRate = new RandomHeartRateGenerator(patientOrig.Random, (int)patientOrig.Model.BaseHeartRate);
            aroundBaseHeartRate.UpdateHeartrateScheme((int)patientOrig.Model.BaseHeartRate, TestDuration_in_min + 60);
            testSchedule.SetHeartRateGenerator(aroundBaseHeartRate);
        }



        public static double CalculateISF(RandomStuff random, VirtualPatient patientOrig)
        {
            // "the doctors way", simuleer een lab experiment waarbij de virtuele patient (ruwweg) de test ondergaat die past bij ICR
            int glucoseDeltaWithStart = 100;

            Schedule testSchedule = new Schedule();
            testSchedule.AddEvent(new PatientEvent(PatientEventType.INSULIN, injectionTime_in_min, double.NaN, double.NaN));
            testSchedule.AddEvent(new PatientEvent(PatientEventType.STOP, TestDuration_in_min, 0, double.NaN));
            AddFlatHeartrate(random, patientOrig, testSchedule);

             VirtualPatient testPatient = new VirtualPatient(patientOrig, testSchedule, null);
            double[] initialVector = testPatient.GetStartData();
            initialVector[BergmanAndBretonModel.G_Glucose_ODEindex_MG_per_DL] += glucoseDeltaWithStart;
            testPatient.OverrideLastGeneratedData(initialVector);

            // binary search voor juiste hoeveelheid ins.
            double totalInsulinNeeded_in_IU = BinarySearch.DoBinarySearch(random, testPatient, TestDuration_in_min, Gluc_threshold, testSchedule, initialVector, BinarySearch.GlucoseWithinThreshold);

            // Do the actual calculation for the ISF.
            return glucoseDeltaWithStart / totalInsulinNeeded_in_IU;
        }



        /* idee:
         * Hyperglycemic clamp technique: The plasma glucose concentration is acutely raised to 125 mg/dl above basal levels by a continuous infusion of glucose. 
         * This hyperglycemic plateau is maintained by adjustment of a variable glucose infusion, based on the rate of insulin secretion and glucose metabolism.
         * Because the plasma glucose concentration is held constant, the glucose infusion rate is an index of insulin secretion and glucose metabolism. 
         * The hyperglycemic clamps are often used to assess insulin secretion capacity.
         * (wikipedia)
         */
        /*
        public static double CalculateISF_alt(VirtualPatient patientOrig)
        {
            //todo
        }
        */
    }
}
