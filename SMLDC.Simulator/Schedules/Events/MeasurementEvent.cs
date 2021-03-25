//using System;
//using System.Collections.Generic;
//using System.Text;
//using SMLDC.Simulator.Models;
//using static SMLDC.Simulator.Utilities.Enums;

//namespace SMLDC.Simulator.Schedules.Events
//{
//    public class PatientEvent : PatientEvent
//    {

//        public override string ToString()
//        {
//            return "<MmntEvent[" + TrueStartTime + "]=" + TrueValue + ">";
//        }

//        public override PatientEvent CopyEvent()
//        {
//            PatientEvent m = new PatientEvent(this.TrueStartTime, this.TrueValue);
//            return (PatientEvent) m;
//        }


//        public PatientEvent(uint time, double value) : base(PatientEventType.GLUCOSE_MASUREMENT, time, value)
//        {
//        }
//        public PatientEvent(PatientEvent that) : base(PatientEventType.GLUCOSE_MASUREMENT,that.TrueStartTime, that.TrueValue)
//        {
//        }

//        public double Glucose_TrueValue_in_MG_per_DL
//        {
//            get
//            {
//               // if (this.EventType != PatientEventType.GLUCOSE_MASUREMENT) { throw new ArgumentException("Dit is geen Glucose Mmnt event!"); }
//                return TrueValue;
//            }
//            set
//            {
//               // if (this.EventType != PatientEventType.GLUCOSE_MASUREMENT) { throw new ArgumentException("Dit is geen Glucose Mmnt event!"); }
//                TrueValue = value;
//            }
//        }
//    }
//}
