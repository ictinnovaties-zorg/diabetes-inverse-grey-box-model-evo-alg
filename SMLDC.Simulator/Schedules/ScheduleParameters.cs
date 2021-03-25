
using SMLDC.Simulator.Schedules.Events;
using SMLDC.Simulator.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using static SMLDC.Simulator.Utilities.Enums;

namespace SMLDC.Simulator.Schedules
{
    public class ScheduleParameters
    {
        public uint ContinuousGlucoseMmntEveryNMinutes;
        public Dictionary<string, EventParameters> eventParameters;

        public ScheduleParameters(Dictionary<string, Dictionary<string, double[]>> eventParams)
        {
            eventParameters = new Dictionary<string, EventParameters>();
            foreach(string key in eventParams.Keys)
            {
                if (key == "glucose_meter")
                {
                    ContinuousGlucoseMmntEveryNMinutes = (uint) eventParams[key]["ContinuousGlucoseMmntEveryNMinutes"][0];
                }
                else
                {
                    eventParameters[key] = new EventParameters(key, eventParams[key]);
                }
            }
        }

        public ScheduleParameters(List<EventParameters> eventparams)
        {
            eventParameters = new Dictionary<string, EventParameters>();
            foreach (EventParameters ep in eventparams)
            {
                eventParameters[ep.name] = ep;
            }

        }

    }


    // info over waardes en hun spreiding (stdddev)
    public class EventParameters
    {
        public string name;
        public uint time;
        public uint time_sigma;
        public uint duration;
        public uint duration_sigma;
        public double value;
        public double value_sigma;
        public double skip_fraction;
        public double insulin_skip_fraction;
        public uint insulin_offset; // voor time.
        public uint insulin_offset_sigma;


        public EventParameters(uint time_in_MIN, uint time_sigma_in_MIN,       uint duration_in_MIN, uint duration_sigma_in_MIN, 
                               double value, double value_sigma,               uint insulin_offset_in_MIN, uint insulin_offset_sigma_in_MIN, 
                               double skip_ffraction,     double insulin_skip_fraction)
        {
            this.time = time_in_MIN;
            this.time_sigma = time_sigma_in_MIN;

            this.duration = duration_in_MIN;
            this.duration_sigma = duration_sigma_in_MIN;

            this.value = value;
            this.value_sigma = value_sigma;

            this.insulin_offset = insulin_offset_in_MIN;
            this.insulin_offset_sigma = insulin_offset_sigma_in_MIN;

            this.skip_fraction = skip_ffraction;
            this.insulin_skip_fraction = insulin_skip_fraction;

        }

        public EventParameters(string name, Dictionary<string, double[]> eparam)
        {
            this.name = name;
            time = (uint)eparam["time"][0] * 60; // van uren naar minuten
            time_sigma = (uint)eparam["time"][1] * 60; // van uren naar minuten

            duration = (uint)eparam["duration"][0];  //is al in minuten
            duration_sigma = (uint)eparam["duration"][1];  //is al in minuten

            value = eparam["value"][0];
            value_sigma = eparam["value"][1];

            skip_fraction = eparam["skip_percentage"][0] / 100d;
            insulin_skip_fraction = eparam["insulin_skip_percentage"][0] / 100d;

            insulin_offset = (uint)eparam["insulin_offset"][0];  //is al in minuten
            insulin_offset_sigma = (uint)eparam["insulin_offset"][1];  //is al in minuten
        }

        public PatientEvent GenerateInsulinEvent(RandomStuff random, PatientEvent mealEvent)
        {
            if(mealEvent.EventType != PatientEventType.CARBS) { return null; }
            if (random.NextDouble() <= this.insulin_skip_fraction) { return null; }
            double rndTime = random.GetNormalDistributed(this.insulin_offset, this.insulin_offset_sigma, Globals.maxSigma);
            uint time = (uint)(mealEvent.TrueStartTime - rndTime);
            return new PatientEvent(PatientEventType.INSULIN, time, Double.PositiveInfinity); //inf --> code voor auto bolus? TODO
        }


        public PatientEvent GenerateCarbIntakeEvent(RandomStuff random, int day)
        {
            if(random.NextDouble() <= this.skip_fraction) { return null; }
            double rndTime = random.GetNormalDistributed(this.time, this.time_sigma, Globals.maxSigma);
            uint time = (uint) (day * 60 * 24 +  rndTime);
            double rndCarbs = random.GetNormalDistributed(this.value, this.value_sigma, Globals.maxSigma);
            return new PatientEvent(PatientEventType.CARBS, time, rndCarbs);
        }
    }
}
