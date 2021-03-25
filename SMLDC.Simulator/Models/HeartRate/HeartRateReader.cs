using SMLDC.Simulator.Models.HeartRate.HrFsm;
using SMLDC.Simulator.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMLDC.Simulator.Models.HeartRate
{

    public class HeartRateReader : AbstractHeartRateGenerator
    {
        private HrFsmSettings hrFsmSettings; 
        public int RealDataPatientNumber {  get { return hrFsmSettings.realPatientIndex; } }
        private VirtualPatient patient;
        public HeartRateReader(VirtualPatient patient, HrFsmSettings hrFsmSettings)
        {
            this.hrFsmSettings = hrFsmSettings;
            this.patient = patient;
        }


        public void GenerateScheme()
        {
            heartRateScheme = RealPatientDataReader.ReadHeartratesCSV(hrFsmSettings, out DateTime start, out DateTime end);
            // schedule croppen naar HR start--end
            patient.TrueSchedule.CropScheduleToDateTime(start, end);
            int hrbase = (int) Math.Round(GetHrBaseEstimate());
            // blanco (0) data opvullen: voorlopig met heel basale 'interpolatie', vorige doortrekken. Eerste en laatste bestaan sowieso.
            int prev_hr = -1;
            for (int ndx = 0; ndx < heartRateScheme.Length; ndx++)
            {
                if (heartRateScheme[ndx] == 0)
                {
                    heartRateScheme[ndx] = hrbase;
                }
                prev_hr = heartRateScheme[ndx];
            }
        }

        public override void GenerateScheme(uint calculations)
        {
            GenerateScheme();
        }


        // shallow:
        public override AbstractHeartRateGenerator Copy(uint offset=0)
        {
            HeartRateReader rhr = new HeartRateReader(this.patient, this.hrFsmSettings);
            rhr.heartRateScheme = this.heartRateScheme;
            rhr.offset = this.offset;
            return rhr;
        }
}
}
