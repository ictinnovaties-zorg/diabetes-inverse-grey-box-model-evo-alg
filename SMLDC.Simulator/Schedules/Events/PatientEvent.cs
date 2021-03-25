using SMLDC.Simulator.Models;
using SMLDC.Simulator.Utilities;
using System;
using System.Collections.Generic;
using static SMLDC.Simulator.Utilities.Enums;

namespace SMLDC.Simulator.Schedules.Events
{
    public class PatientEvent : IComparable<PatientEvent>
    {

        public PatientEvent(PatientEventType eventType, uint startTime, double trueValue, double duration=1)
        {
            EventType = eventType;
            TrueStartTime = startTime;
            Duration = duration;
            TrueValue = trueValue;
        }

        public PatientEvent(PatientEvent that)
        {
            this.EventType = that.EventType;
            this.TrueStartTime = that.TrueStartTime;
            this.Duration = that.Duration;
            this.TrueValue = that.TrueValue;
        }

     //   public PatientEvent DeepCopy() { return new PatientEvent(this); }
        public virtual PatientEvent CopyEvent() { return new PatientEvent(this); }


        public static List<uint> GetTimes(List<PatientEvent> events)
        {
            List<uint> times = new List<uint>(events.Count);
            for (int i = 0; i < events.Count; i++)
            {
                PatientEvent evt = events[i];
                times.Add(evt.TrueStartTime);
            }
            return times;
        }
        public static List<double> GetTrueValues(List<PatientEvent> events) {
            //  return GetTrueOrNoisyValues(events, true); 
            List<double> values = new List<double>(events.Count);
            for (int i = 0; i < events.Count; i++)
            {
                PatientEvent evt = events[i];
                values.Add(evt.TrueValue);
            }
            return values;
        }
        //public static List<double> GetNoisyValues(List<PatientEvent> events) { return GetTrueOrNoisyValues(events, false); }
        //private static List<double> GetTrueOrNoisyValues(List<PatientEvent> events, bool trueV)
        //{
        //    List<double> values = new List<double>(events.Count);
        //    for (int i = 0; i < events.Count; i++)
        //    {
        //        PatientEvent evt = events[i];
        //        if (trueV)
        //        {
        //            values.Add(evt.TrueValue);
        //        }
        //        else
        //        {
        //            values.Add(evt.NoisyValue);
        //        }
        //    }
        //    return values;
        //}

        //public static Tuple<List<uint>, List<double>, List<double>> GetTimesTrueAndNoisyValues(List<PatientEvent> events)
        //{
        //    return new Tuple<List<uint>, List<double>, List<double>>(GetTimes(events), GetTrueValues(events), GetNoisyValues(events));
        //}
        //public static Tuple<List<uint>, List<double>> GetTimesAndValues(List<PatientEvent> events)
        //{
        //    return new Tuple<List<uint>, List<double>>(GetTimes(events), GetTrueValues(events));
        //}


        public PatientEventType EventType { get; set; }

        public uint TrueStartTime { get; set; }

        public double TrueValue { get; set; }
        public double Carb_TrueValue_in_gram
        {
            get {
                if (this.EventType != PatientEventType.CARBS) { throw new ArgumentException("Dit is geen Carb event!"); }
                return TrueValue; 
            }
            set
            {
                if (this.EventType != PatientEventType.CARBS) { throw new ArgumentException("Dit is geen Carb event!"); }
                TrueValue = value;
            }
        }

        public double Insulin_TrueValue_in_IU
        {
            get
            {
                if (this.EventType != PatientEventType.INSULIN) { throw new ArgumentException("Dit is geen Insulin event!"); }
                return TrueValue;
            }
            set
            {
                if (this.EventType != PatientEventType.INSULIN) { throw new ArgumentException("Dit is geen Insulin event!"); }
                TrueValue = value;
            }
        }


        public double Glucose_TrueValue_in_MG_per_DL
        {
            get
            {
                if (this.EventType != PatientEventType.GLUCOSE_MASUREMENT) { throw new ArgumentException("Dit is geen Glucose Mmnt event!"); }
                return TrueValue;
            }
            set
            {
                if (this.EventType != PatientEventType.GLUCOSE_MASUREMENT) { throw new ArgumentException("Dit is geen Glucose Mmnt event!"); }
                TrueValue = value;
            }
        }

        //public double Glucose_TrueValue_in_MG_per_DL
        //{
        //    get
        //    {
        //        if (this.EventType != PatientEventType.GLUCOSE_MASUREMENT) { throw new ArgumentException("Dit is geen Glucose Mmnt event!"); }
        //        return TrueValue;
        //    }
        //    set
        //    {
        //        if (this.EventType != PatientEventType.GLUCOSE_MASUREMENT) { throw new ArgumentException("Dit is geen Glucose Mmnt event!"); }
        //        TrueValue = value;
        //    }
        //}



        //    public virtual double NoisyValue { get; set; }

        //public double Carb_NoisyValue_in_G
        //{
        //    get
        //    {
        //        if (this.EventType != PatientEventType.CARBS) { throw new ArgumentException("Dit is geen Carb event!"); }
        //        return NoisyValue;
        //    }
        //}

        //public double Insulin_NoisyValue_in_IU
        //{
        //    get
        //    {
        //        if (this.EventType != PatientEventType.INSULIN) { throw new ArgumentException("Dit is geen Insulin event!"); }
        //        return NoisyValue;
        //    }
        //}

        //public double Glucose_NoisyValue_in_MG_per_DL
        //{
        //    get
        //    {
        //        if (this.EventType != PatientEventType.GLUCOSE_MASUREMENT) { throw new ArgumentException("Dit is geen Glucose Mmnt event!"); }
        //        return NoisyValue;
        //    }
        //}



        //ongebruikt. Maar voor de toekomst nuttig: als we carb intake gaan spreiden over de hele maaltijd.
        public double Duration { get; set; }
        public bool EventFinished(uint timestamp)
        {
            return timestamp >= TrueStartTime + Duration;
        }


        public override string ToString()
        {
            string typetxt = PatientEventTypeToString();
//            return "<Evt@" + TrueStartTime + " {" + NoisyStartTime + "} :: " + typetxt + ":" + OctaveStuff.MyFormat(TrueValue) + " {" + OctaveStuff.MyFormat(NoisyValue) + "}>";
            return "<Evt@" + TrueStartTime + " :: " + typetxt + ":" + OctaveStuff.MyFormat(TrueValue) + ">";
        }


        public string PatientEventTypeToString()
        {

            switch (EventType)
            {
                case PatientEventType.CARBS:
                    return "Carb";
                case PatientEventType.INSULIN:
                    return "Ins";
                case PatientEventType.GLUCOSE_MASUREMENT:
                    return "Mmnt";
                case PatientEventType.STOP:
                    return "STOP";
            }
            return "???";
        }

      int IComparable<PatientEvent>.CompareTo(PatientEvent other)
        {
            return (int)this.TrueStartTime - (int)other.TrueStartTime;
        }
    }
}