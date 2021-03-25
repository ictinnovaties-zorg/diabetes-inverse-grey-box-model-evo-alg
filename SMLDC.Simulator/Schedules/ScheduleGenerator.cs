using System;
using System.Collections.Generic;
using SMLDC.Simulator.Schedules.Events;
using SMLDC.Simulator.Utilities;

namespace SMLDC.Simulator.Schedules
{
    public class ScheduleGenerator
    {
        public static Schedule GenerateSchedule(RandomStuff random, uint days, PatientSettings patientSettings, ScheduleParameters eventParameters)
        {
            uint nr_cont_per_day = (60 * 24) / eventParameters.ContinuousGlucoseMmntEveryNMinutes;
            uint nr_events_schatting = days * (nr_cont_per_day + 12);

            Schedule trueSchedule = new Schedule(); 
            // cont. gluc. mmnts toevoegen:
            uint endTime = (days + 1) * 60 * 24;
            for (uint time_in_min = 0; time_in_min < endTime; time_in_min += eventParameters.ContinuousGlucoseMmntEveryNMinutes)
            {
                trueSchedule.AddUniqueEventWithoutSorting(new PatientEvent(Enums.PatientEventType.GLUCOSE_MASUREMENT, time_in_min, Double.NaN));
            }
            trueSchedule.SortSchedule();

            for (int currentDay = 0; currentDay < days; currentDay++)
            {
                List<PatientEvent> generatedDailyEvents = CreateDailyEvents(random, eventParameters, currentDay);
               
                for (int e = 0; e < generatedDailyEvents.Count; e++)
                {
                    PatientEvent evt = generatedDailyEvents[e];
                    trueSchedule.AddEventWiggle(evt);
                }
            }
            trueSchedule.SortSchedule();
            trueSchedule.AddStopEvent();

            return trueSchedule;
        }


        private static List<PatientEvent> CreateDailyEvents(RandomStuff random, ScheduleParameters scheduleParam, int day)
        {
            List<PatientEvent> dailyEvents = new List<PatientEvent>(12);
            bool vorige_vergeten = false;
            foreach (string key in scheduleParam.eventParameters.Keys)
            {
                // TODO: betere random scheduling (zie oude ScheduleGenerator??)

                PatientEvent mealEvent = scheduleParam.eventParameters[key].GenerateCarbIntakeEvent(random, day);
                if (mealEvent != null)
                {
                    dailyEvents.Add(mealEvent);
                    {
                        PatientEvent bolusEvent = scheduleParam.eventParameters[key].GenerateInsulinEvent(random, mealEvent);
                        while(vorige_vergeten && bolusEvent == null)
                        {
                            bolusEvent = scheduleParam.eventParameters[key].GenerateInsulinEvent(random, mealEvent);
                        }
                        if (bolusEvent != null)
                        {
                            vorige_vergeten = false;
                            if(bolusEvent.TrueStartTime == mealEvent.TrueStartTime)
                            {
                                bolusEvent.TrueStartTime -= 5;
                            }
                            dailyEvents.Add(bolusEvent);
                        }
                        else
                        {
                            vorige_vergeten = true;
                        }
                    }
                }
            }
            // event op zelfde tijd eruit halen:
            for(int i = dailyEvents.Count-1; i >= 1; i--)
            {
                if(dailyEvents[i].TrueStartTime == dailyEvents[i-1].TrueStartTime)
                {
                    if (dailyEvents[i].EventType == Enums.PatientEventType.INSULIN)
                    {
                        dailyEvents[i - 1].EventType = Enums.PatientEventType.INSULIN;
                        dailyEvents[i - 1].TrueValue = dailyEvents[i].TrueValue;
                    }
                    dailyEvents.RemoveAt(i);
                }
            }
            return dailyEvents;
        }




    }
}