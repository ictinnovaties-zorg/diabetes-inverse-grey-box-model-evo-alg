using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using SMLDC.Simulator.Models.HeartRate;
using SMLDC.Simulator.Models.HeartRate.HrFsm;
using SMLDC.Simulator.Schedules;
using SMLDC.Simulator.Schedules.Events;
using static SMLDC.Simulator.Utilities.Enums;

namespace SMLDC.Simulator.Utilities
{
    public class RealPatientDataReader
    {
        //https://docs.microsoft.com/en-us/dotnet/api/system.datetime.parseexact?view=netcore-3.1#System_DateTime_ParseExact_System_String_System_String___System_IFormatProvider_System_Globalization_DateTimeStyles_
        private static readonly string[] formats = {
                          "d-M-yyyy HH:mm:ss",    "d-M-yyyy HH:mm",
                          "dd-M-yyyy HH:mm:ss",   "dd-M-yyyy HH:mm",
                          "dd-MM-yyyy HH:mm:ss",  "dd-MM-yyyy HH:mm",
                          "d-MM-yyyy HH:mm:ss",   "d-MM-yyyy HH:mm",
                          "yyyy-M-d HH:mm:ss",    "yyyy-M-d HH:mm",
                          "yyyy-MM-d HH:mm:ss",    "yyyy-MM-d HH:mm",
                          "yyyy-M-dd HH:mm:ss",    "yyyy-M-dd HH:mm",
                          "yyyy-MM-dd HH:mm:ss",    "yyyy-MM-dd HH:mm"
        };


        private static DateTime ParseDateAndTimeString(string datestring, string timestring)
        {
            string datetimestring = datestring + " " + timestring;
            DateTime datetime = DateTime.ParseExact(datetimestring, formats, CultureInfo.InvariantCulture);// new CultureInfo("nl-NL", false), DateTimeStyles.None);
            return datetime;
        }


        private static List<int> abbotLijst = new List<int>(){ 914, 918, 926, 929, 941, 962, 995, 987 };
        public static bool ZitInAbbot(int nr) { return abbotLijst.Contains(nr); }
        public static Schedule ReadGlucoseCarbInsulinData(HrFsmSettings hrFsmSettings)
        {
            List<Reading> records = new List<Reading>();
            using (var reader = new StreamReader(hrFsmSettings.realDataBaseFolder + "/Medtronic.csv"))
            using (var csv = new CsvReader(reader, new CultureInfo("en-US", false)))
            {
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    int subject_code = csv.GetField<int>("Subject code number");
                    if (subject_code != hrFsmSettings.realPatientIndex) { continue; }

                    // string datestring = csv.GetField<string>("Date");
                    // string timestring = csv.GetField<string>("Time");
                    string datetimestring = csv.GetField<string>("Local datetime [ISO8601]");
                    double? bloodGlucoseValue = null;
                    double? bg1 = csv.GetField<double?>("BG Reading [mmol/l]");
                    double? bg2 = csv.GetField<double?>("Sensor Glucose [mmol/l]");
                    if(bg1!= null && bg2 != null)
                    {

                    }
                    else if(bg1 != null)
                    {
                        bloodGlucoseValue = bg1;
                    }
                    else if(bg2 != null)
                    {
                        bloodGlucoseValue = bg2;
                    }
                    var record = new Reading
                    {
                        //Time = ParseDateAndTimeString(datestring, timestring),
                        Time = DateTime.Parse(datetimestring),
                        BloodGlucose = bloodGlucoseValue * 18, // mmol/L --> mg/dL
                        Insuline = csv.GetField<double?>("Bolus Volume Delivered [IU]"),
                        Carbs = csv.GetField<double?>("BWZ Carb Input [g]"),
                    };
                    if (record.BloodGlucose != null || record.Insuline != null || record.Carbs != null)
                    {
                        records.Add(record);
                    }
                }
            }

            if (ZitInAbbot(hrFsmSettings.realPatientIndex))
            {
                using (var reader = new StreamReader(hrFsmSettings.realDataBaseFolder + "/Abbott.csv"))
                using (var csv = new CsvReader(reader, new CultureInfo("en-US", false)))
                {
                    csv.Read();
                    csv.ReadHeader();
                    while (csv.Read())
                    {
                        int subject_code = csv.GetField<int>("Subject code number");
                        if (subject_code != hrFsmSettings.realPatientIndex) { continue; }

                        // string datestring = csv.GetField<string>("Date");
                        // string timestring = csv.GetField<string>("Time");
                        string datetimestring = csv.GetField<string>("Local datetime [ISO8601]");
                        var record = new Reading
                        {
                            //Time = ParseDateAndTimeString(datestring, timestring),
                            Time = DateTime.Parse(datetimestring),
                            BloodGlucose = csv.GetField<double?>("Historic Glucose [mmol/l]") * 18, // mmol/L --> mg/dL
                            Insuline = null,
                            Carbs = null,
                        };
                        if (record.BloodGlucose != null || record.Insuline != null || record.Carbs != null)
                        {
                            records.Add(record);
                        }
                    }
                }
            }
            if(records.Count == 0)
            {
                throw new ArgumentException("GEEN DATA GELEZEN VOOR patient " + hrFsmSettings.realPatientIndex);
            }
            records.Sort();
            DateTime start = records[0].Time;
            start = start.Date; // 'floor' naar begin v/d dag
            Schedule schedule = new Schedule(start);

            int prev_time = -1;
            foreach (Reading record in records)
            {
                TimeSpan span = record.Time - start;
                uint time = (uint)Math.Round(span.TotalMinutes);

                if (prev_time == time)
                {
                    // mag dit????
                    time++;
                }

                int toegevoegd = 0;
                if (record.BloodGlucose != null)
                {
                    toegevoegd++;
                    PatientEvent evt = new PatientEvent(PatientEventType.GLUCOSE_MASUREMENT, time, (double)record.BloodGlucose);
                   // evt.NoisyValue = (double)record.BloodGlucose;
                    schedule.AddEvent(evt);
                }
                if (record.Carbs != null)
                {
                    toegevoegd++;
                    PatientEvent evt = new PatientEvent(PatientEventType.CARBS, time, (double)record.Carbs);
                    //evt.NoisyValue = (double)record.Carbs;
                    schedule.AddEvent(evt);
                }
                if (record.Insuline != null)
                {
                    toegevoegd++;
                    schedule.AddEvent(new PatientEvent(PatientEventType.INSULIN, time, (double)record.Insuline));
                }


                if (toegevoegd != 1)
                {
                    // mag dit????

                }
                prev_time = (int)time;
            }

            schedule.SortSchedule();

            return schedule;
        }



        public static int[] ReadHeartratesCSV(HrFsmSettings hrFsmSettings, out DateTime eerste, out DateTime laatste)
        {
            List<HrReading> hr_records = new List<HrReading>();
            using (var reader = new StreamReader(hrFsmSettings.realDataBaseFolder + "/Fitbit-heart-rate.csv"))
            using (var csv = new CsvReader(reader, new CultureInfo("en-US", false)))
            {
                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    int subject_code = csv.GetField<int>("Subject code number");
                    if (subject_code != hrFsmSettings.realPatientIndex) { continue; }

                    string datetimestring = csv.GetField<string>("Local datetime [ISO8601]");

                    HrReading record = new HrReading
                    {
                        Time = DateTime.Parse(datetimestring),
                        HR = csv.GetField<int>("heart rate [#/min]"),
                    };
                    hr_records.Add(record);
                    
                }
            }
            if (hr_records.Count == 0)
            {
                throw new ArgumentException("GEEN DATA GELEZEN VOOR patient " + hrFsmSettings.realPatientIndex);
            }
            hr_records.Sort();

            eerste = hr_records[0].Time;
            laatste = hr_records[hr_records.Count - 1].Time;
            TimeSpan span = laatste - eerste;
            int lengte = (int)Math.Ceiling(span.TotalMinutes) + 2; // omdat we schedule incl.laatste event doen...
            int[] heartRateScheme = new int[lengte];

            foreach (HrReading record in hr_records)
            {
                span = record.Time - eerste;
                int time = (int)Math.Round(span.TotalMinutes);
                heartRateScheme[time] = record.HR;
            }
            heartRateScheme[heartRateScheme.Length - 1] = heartRateScheme[heartRateScheme.Length - 2];
            return heartRateScheme;
        } 
    }
    
    
    
    public class Reading : IComparable<Reading>
    {
        public DateTime Time { get; set; }
        public double? BloodGlucose { get; set; }
        public double? Insuline { get; set; }
        public double? Carbs { get; set; }

        public override string ToString()
        {
            return "<" + Time + " C:" + Carbs + ", G:" + BloodGlucose + ", I:" + Insuline + ">";
        }
        int IComparable<Reading>.CompareTo(Reading other)
        {
            return DateTime.Compare(this.Time, other.Time);
        }
    }


    public class HrReading : IComparable<HrReading>
    {
        public DateTime Time { get; set; }
        public int HR { get; set; }

        public override string ToString()
        {
            return "<" + Time + " hr:" + HR + ">";
        }
        int IComparable<HrReading>.CompareTo(HrReading other)
        {
            return DateTime.Compare(this.Time, other.Time);
        }
    }
}
