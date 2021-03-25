namespace SMLDC.Simulator.Utilities
{
    public class Enums
    {
        public enum SubPopulatieType
        {
            NORMAAL = 0,
            SEARCH_REPEATED_OVER_RANGE = 1
        }


        public enum PatientEventType
        {
            CARBS = 1,
            GLUCOSE_MASUREMENT,
            INSULIN,
            DUMMY,
            STOP
        }


        public enum BolusAdviceType
        {
            CARBS,
            INSULIN,
            NOTHING
        }


        public enum PatientTypeEnum
        {
            STANDARD, // nothing, only running patient
            PARTICLE,
            BINARY_SEARCH
        }


    }
}
