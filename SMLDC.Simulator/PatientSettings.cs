
using SMLDC.Simulator.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using SMLDC.Simulator.Utilities;

namespace SMLDC.Simulator
{
	public class PatientSettings
	{

		//todo: zinvolle waardes geven:
		//public double ICR = 0;
		//public double ISF = 0;

		public string CommentsForFileName;


		/// <summary>
		/// The factor (1==100%) of realistic food noise that is used.
		/// </summary>
		public double CarbNoiseFactor;
		public double CarbTimeNoiseSigma;

		/// <summary>
		/// The factor (1==100%) of realistic gluc. mmnt noise that is used.
		/// </summary>
		public double GlucNoiseFactor;

		/// <summary>
		/// The factor (1==100%) of chance of forgetting to register a food event (e.g. food event value = 0 for  ML instead of real value).
		/// </summary>
		public double FoodForgetFactor;



		public PatientSettings(PatientSettings that)
		{
			this.FoodForgetFactor = that.FoodForgetFactor;
			this.CarbNoiseFactor = that.CarbNoiseFactor;
			this.GlucNoiseFactor = that.GlucNoiseFactor;
			this.CarbTimeNoiseSigma = that.CarbTimeNoiseSigma;
			this.CommentsForFileName = that.CommentsForFileName;
		}


		public PatientSettings(RandomStuff random, Dictionary<string, double[]> patientParameters)
		{
			// hier worden de random ranges gebruike die in de ini stonden:
			Dictionary<string, double> patientValues = new Dictionary<string, double>(patientParameters.Count);
			foreach (KeyValuePair<string, double[]> dataField in patientParameters)
			{
				//random range:
				double value = random.GetNormalDistributed(dataField.Value[0], dataField.Value[1], Globals.maxSigma);
				patientValues.Add(dataField.Key, value);
			}
			ParseParameters(patientValues);
		}


		private void ParseParameters(Dictionary<string, double> patientValues)
		{
			// parse de inhoud van de ini file op patient settings.
			this.FoodForgetFactor = patientValues["FoodForgetFactor"];
			this.CarbNoiseFactor = patientValues["CarbNoiseFactor"];
			this.GlucNoiseFactor = patientValues["GlucoseNoiseFactor"];
			this.CarbTimeNoiseSigma = patientValues["CarbTimeNoiseSigma"];
		}

	}
}
