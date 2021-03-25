using SMLDC.Simulator.Utilities;
using System;
using System.Collections.Generic;
using static SMLDC.Simulator.Utilities.Enums;


namespace SMLDC.MachineLearning
{

	/*
	 * a big container for all the settings
	 * Try to use the same setting name in config.ini and as variable name!
	 */

	public class ParticleFilterSettings
	{


		public ParticleFilterSettings DeepCopy()
		{
			// nb this is slow:
			return this.Copy<ParticleFilterSettings>();
		}

		public int seedForParticleFilter;


		public ParticleFilterSettings(RandomStuff random, Dictionary<string, double[]> pfParameters)
		{
			// hier worden de random ranges gebruike die in de ini stonden:
			Dictionary<string, double> patientValues = new Dictionary<string, double>(pfParameters.Count);
			foreach (KeyValuePair<string, double[]> dataField in pfParameters)
			{
				//random range:
				double value = dataField.Value[0];
				if (dataField.Value[1] != 0) 
				{
					// gen randoms verspillen, heeft namelijk invloed op volgende settings, maakt herhaalbaarheid lastiger na toevoegen van para.
					value = random.GetNormalDistributed(dataField.Value[0], dataField.Value[1], Globals.maxSigma);
				}
				patientValues.Add(dataField.Key, value);
			}
			ParseParameters(patientValues);
		}



		private void ParseParameters(Dictionary<string, double> pfSettingValues)
		{

			// parse de inhoud van de ini file op patient settings.
			this.debug = pfSettingValues["debug"] > 0.5;
			this.ML_PERFECT_HACK = pfSettingValues["ML_PERFECT_HACK"] > 0.5;

			this.ContinuousGlucoseMmntEveryNMinutes = (uint)pfSettingValues["ContinuousGlucoseMmntEveryNMinutes"];
			this.UpdateCarbHypDuringSearch = pfSettingValues["UpdateCarbHypDuringSearch"] > 0.5; // na afloop is sneller (want zoeken kan dan parallel, en geeft zelfde uitkomst)
			this.local_search_learning_rate_base = pfSettingValues["local_search_learning_rate_base"];
			this.local_search_learning_rate_power = pfSettingValues["local_search_learning_rate_power"];
			this.MaxNrStepsInParabolicSearch = (uint)pfSettingValues["MaxNrStepsInParabolicSearch"];
			this.CarbSearchLinear = pfSettingValues["CarbSearchLinear"] > 0.5;
			this.VerwijderCarbHypsDichterBijDanMarge = (uint)pfSettingValues["VerwijderCarbHypsDichterBijDanMarge"];
			this.VoegCarbHypToeAlsVerderwegDanMarg = (uint)pfSettingValues["VoegCarbHypToeAlsVerderwegDanMarg"];
			this.EvaluateSubCarbhypOncePerN = (uint)pfSettingValues["EvaluateSubCarbhypOncePerN"];
			this.MargingProd2x3 = pfSettingValues["MargingProd2x3"];
			this.MarginSlope2 = pfSettingValues["MarginSlope2"];

			this.PowForLobsidedness = pfSettingValues["PowForLobsidedness"];
			this.PowForLobsidednessPerCarbHyp = pfSettingValues["PowForLobsidednessPerCarbHyp"];
			this.SchalingPow = pfSettingValues["SchalingPow"];
			this.CarbEstimatesPerSubUpdateIsLearningRate = pfSettingValues["CarbEstimatesPerSubUpdateIsLearningRate"];
			this.UseLobsidednessSignalFromCarbHypInResample = pfSettingValues["UseLobsidednessSignalFromCarbHypInResample"] > 0.5;
			this.CarbHypRanking = pfSettingValues["CarbHypRanking"] > 0.5;
			this.BaseInitialCarbEstimationOnGlucoseCurve = pfSettingValues["BaseInitialCarbEstimationOnGlucoseCurve"] > 0.5;
			this.SMOOTHING_RANGE_FOR_FORGET_DETECTION = (uint)pfSettingValues["SMOOTHING_RANGE_FOR_FORGET_DETECTION"];

			this.MAX_CARB_OFFSET_BASED_ON_PATIENT_ESTIMATE_in_min = (int)pfSettingValues["MAX_CARB_OFFSET_BASED_ON_PATIENT_ESTIMATE_in_min"];
			this.MAX_CARB_ESTIMATE_in_gr = pfSettingValues["MAX_CARB_ESTIMATE_in_gr"];
			this.MAX_AFWIJKING_VAN_PATIENT_ESTIMATE_in_gr = pfSettingValues["MAX_AFWIJKING_VAN_PATIENT_ESTIMATE_in_gr"];

			this.MaximumHistoryQueueLength = (uint)pfSettingValues["MaximumHistoryQueueLength"];
			this.MaximumSbHistoryQueueLength = (uint)pfSettingValues["MaximumSubHistoryQueueLength"];

			this.NumberOfParticlesFromHistory = (uint)pfSettingValues["NumberOfParticlesFromHistory"];
			this.NumberOfSubParticlesFromHistory = (uint)pfSettingValues["NumberOfSubParticlesFromHistory"];

			this.NumberOfParticlesToKeep = (uint)pfSettingValues["NumberOfParticlesToKeep"];
			this.NumberOfSubPopulations = (uint)pfSettingValues["NumberOfSubPopulations"];

			this.SearchInSubPopulationType = (SubPopulatieType) pfSettingValues["SearchInSubPopulationType"];
			this.UseParabolicSearchInSubPopulationOncePer = (uint)pfSettingValues["UseParabolicSearchInSubPopulationOncePer"];
			this.UseParabolicSearchInSubPopulatioNrParabolicSteps = (uint)pfSettingValues["UseParabolicSearchInSubPopulatioNrParabolicSteps"];

			this.NumberOfParticlesPerSubPopulation = (uint)pfSettingValues["NumberOfParticlesPerSubPopulation"];

			this.NumberOfTurnsPerSubSubPopulation = (uint)pfSettingValues["NumberOfTurnsPerSubSubPopulation"];
			this.RmseForBreak = pfSettingValues["RmseForBreak"];

			this.NumberOfRepeatsPerSubSubPopulation = (uint)pfSettingValues["NumberOfRepeatsPerSubSubPopulation"];
			this.UpdateBestAfterEachSubsubTraining = (pfSettingValues["UpdateBestAfterEachSubsubTraining"] > 0.5);

			this.SubRepeatedSearch_gebruikBesteVanElkeSubSub = pfSettingValues["SubRepeatedSearch_gebruikBesteVanElkeSubSub"] > 0.5;

			this.NumberOfPeaksPerSubSubPopulation = (uint)pfSettingValues["NumberOfPeaksPerSubSubPopulation"];
			this.NumberOfPeaksPerSubSubPopulationStep = (uint)pfSettingValues["NumberOfPeaksPerSubSubPopulationStep"];

			this.MaxNrSubPopulatiesNotExploring = (uint)pfSettingValues["MaxNrSubPopulatiesNotExploring"];
			this.ProbabilityOfSamplingFromBestHistoryToNewSubpopulation = pfSettingValues["ProbabilityOfSamplingFromBestHistoryToNewSubpopulation"];
			this.nearestParticleHashing_nr_bins_per_dimension = (uint)pfSettingValues["nearestParticleHashing_nr_bins_per_dimension"];
			this.nearestParticleHashing_useForStagnationDetection = pfSettingValues["nearestParticleHashing_useForStagnationDetection"] > 0.5;
			this.nearestParticleHashing_FactorUseAfterTime = pfSettingValues["nearestParticleHashing_FactorUseAfterTime"];
			this.nearestParticleHashing_initTerugtelWaarde = (uint)pfSettingValues["nearestParticleHashing_initTerugtelWaarde"];
			this.MaxStagnationCounter = (uint)pfSettingValues["MaxStagnationCounter"];
			this.SlopeForStagnation = pfSettingValues["SlopeForStagnation"];
			this.SlopeForNewSeed = pfSettingValues["SlopeForNewSeed"];
			this.RangeForSlope = (uint)pfSettingValues["RangeForSlope"];

			this.TrailLengthInMinutes = (uint)pfSettingValues["TrailLengthInMinutes"];
			this.TrainTrailFase1Fraction = pfSettingValues["TrainTrailFase1Fraction"];
			this.TrainTrailSkipFirstFraction = pfSettingValues["TrainTrailSkipFirstFraction"];

			this.NrXTrailForCarbSlopeEnd = pfSettingValues["NrXTrailForCarbSlopeEnd"];
			this.NrXTrailForCarbSlopeStart = pfSettingValues["NrXTrailForCarbSlopeStart"];
			this.TrailForEvaluationInMinutes = (uint)pfSettingValues["TrailForEvaluationInMinutes"];

			this.USE_OVERSHOOTS_IN_WEIGHT = (pfSettingValues["USE_OVERSHOOTS_IN_WEIGHT"] > 0.5);
			this.GLUC_THRESHOLD_FOR_UNDERSHOOT = pfSettingValues["GLUC_THRESHOLD_FOR_UNDERSHOOT"];
			this.noisyHigherThanPredictedAtMaximumFactor = pfSettingValues["noisyHigherThanPredictedAtMaximumFactor"];
			this.noisyHigherThanPredictedAtMinimumFactor = pfSettingValues["noisyHigherThanPredictedAtMinimumFactor"];
			this.noisyLowerThanPredictedAtMaximumFactor = pfSettingValues["noisyLowerThanPredictedAtMaximumFactor"];
			this.noisyLowerThanPredictedAtMinimumFactor = pfSettingValues["noisyLowerThanPredictedAtMinimumFactor"];

			this.ISF_lower_bound = pfSettingValues["ISF_lower_bound"];
			this.ISF_upper_bound = pfSettingValues["ISF_upper_bound"];
			this.ICR_lower_bound = pfSettingValues["ICR_lower_bound"];
			this.ICR_upper_bound = pfSettingValues["ICR_upper_bound"];


			this.SolverInterval = 1; // (int)pfSettingValues["SolverInterval"]; // other intervals don't work yet with arraybased solverResults.

			this.evaluateEveryNMinutes = (uint)pfSettingValues["evaluateEveryNMinutes"];


			this.UseParallelProcessingOnSubPopulaties = pfSettingValues["UseParallelProcessingOnSubPopulaties"] > 0.5;
			this.UseParallelProcessingParticleEvaluaties = pfSettingValues["UseParallelProcessingParticleEvaluaties"] > 0.5;


			this.ExponentialDecayInitValue = pfSettingValues["ExponentialDecayInitValue"];
			this.ExponentialDecayDecayValue = pfSettingValues["ExponentialDecayDecayValue"];

			//this.LobSidedErrorFactor = pfSettingValues["LobSidedErrorFactor"];
			this.GammaMomentum = pfSettingValues["GammaMomentum"];
			this.FractionWhenChangeParam = pfSettingValues["FractionWhenChangeParam"];
			this.FractionMomentum = pfSettingValues["FractionMomentum"];

			this.UseSearchPerSub = (pfSettingValues["UseSearchPerSub"] > 0.5);
			this.GetCarbEstimatesPerSub = (pfSettingValues["GetCarbEstimatesPerSub"] > 0.5);

			this.LockPredictionsOnMmnts = pfSettingValues["LockPredictionsOnMmnts"] > 0.5;
			this.LockPredictionsOnMmnts_End_TimeWindow_in_min = (uint)pfSettingValues["LockPredictionsOnMmnts_End_TimeWindow_in_min"];
			this.LockPredictionsOnMmnts_Start_TimeWindow_in_min = (uint)pfSettingValues["LockPredictionsOnMmnts_Start_TimeWindow_in_min"];

			this.ErrorCalcInLogSpace = pfSettingValues["ErrorCalcInLogSpace"] > 0.5;
			this.ErrorForWeights = (uint)pfSettingValues["ErrorForWeights"];
			this.SMOOTHING_RANGE = (uint)pfSettingValues["SMOOTHING_RANGE"];

			this.MLUseBretonActivityModel = pfSettingValues["MLUseBretonActivityModel"] > 0.5;
		}




		public bool debug;
		public bool ML_PERFECT_HACK;


		public bool UpdateCarbHypDuringSearch;

		public uint ContinuousGlucoseMmntEveryNMinutes;


		public bool MLUseBretonActivityModel;

		public bool LockPredictionsOnMmnts;
		public uint LockPredictionsOnMmnts_Start_TimeWindow_in_min;
		public uint LockPredictionsOnMmnts_End_TimeWindow_in_min;
		public uint ErrorForWeights;
		public uint SMOOTHING_RANGE;
		public bool ErrorCalcInLogSpace;
		public bool UseSearchPerSub;
		public bool GetCarbEstimatesPerSub;
		/// <summary>
		/// The length of the queue of past best particles (inserted in the current list in Resample phase).
		/// </summary>
		public uint NumberOfParticlesFromHistory = 0;
		public uint NumberOfSubParticlesFromHistory;

		public uint MaximumSbHistoryQueueLength;
		public uint MaximumHistoryQueueLength;


		public double local_search_learning_rate_base = 0.5;
		public double local_search_learning_rate_power = 1;
		public uint SMOOTHING_RANGE_FOR_FORGET_DETECTION;

		public int MAX_CARB_OFFSET_BASED_ON_PATIENT_ESTIMATE_in_min = 15;
		public double MAX_CARB_ESTIMATE_in_gr = 300;
		public double MAX_AFWIJKING_VAN_PATIENT_ESTIMATE_in_gr = 20; // setting, koppelen aan stddev oid uit literatuur?


		public uint MaxNrStepsInParabolicSearch;
		public bool CarbSearchLinear;
		public double PowForLobsidedness;
		public double PowForLobsidednessPerCarbHyp;
		public double SchalingPow;
		public double CarbEstimatesPerSubUpdateIsLearningRate;
		public bool UseLobsidednessSignalFromCarbHypInResample;
		public bool CarbHypRanking;
		public bool BaseInitialCarbEstimationOnGlucoseCurve;
		public uint VerwijderCarbHypsDichterBijDanMarge;
		public uint VoegCarbHypToeAlsVerderwegDanMarg;
		public uint EvaluateSubCarbhypOncePerN;
		public double MargingProd2x3;
		public double MarginSlope2;



		/// <summary>
		/// The discount (gamma) for momentum for particle filtering
		/// </summary>
		public double GammaMomentum = 0;

		/// <summary>
		/// The chance that a param is changed when 'mutated'
		/// </summary>
		public double FractionWhenChangeParam = 0;

		/// <summary>
		/// The chance that momentum for particle filtering is used
		/// </summary>
		public double FractionMomentum = 0;

		///// <summary>
		///// The amount of particles for the particle filtering
		///// </summary>
		//public bool DoParticleFiltering = false;


		/// <summary>
		/// The amount of (best) particles to keep in each resampling for the particle filtering
		/// </summary>
		public uint NumberOfParticlesToKeep;


		/// <summary>
		/// The amount of (sub)populations for the particle filtering
		/// </summary>
		public uint NumberOfSubPopulations;

		//todo alle namen refactoren. Parabolic eruit!
		public SubPopulatieType SearchInSubPopulationType;

		public uint UseParabolicSearchInSubPopulationOncePer;
		public uint UseParabolicSearchInSubPopulatioNrParabolicSteps;




		// <summary>
		/// The amount of particles PER (sub)population for the particle filtering
		/// </summary>
		public uint NumberOfParticlesPerSubPopulation;

		public uint NumberOfTurnsPerSubSubPopulation;
		public double RmseForBreak;
		public uint NumberOfRepeatsPerSubSubPopulation;

		public bool UpdateBestAfterEachSubsubTraining;

		public bool SubRepeatedSearch_gebruikBesteVanElkeSubSub;
		public uint NumberOfPeaksPerSubSubPopulation;
		public uint NumberOfPeaksPerSubSubPopulationStep;


		public uint MaxNrSubPopulatiesNotExploring;
		public uint nearestParticleHashing_nr_bins_per_dimension;
		public bool nearestParticleHashing_useForStagnationDetection;
		public double nearestParticleHashing_FactorUseAfterTime;
		public uint nearestParticleHashing_initTerugtelWaarde;
		public double ProbabilityOfSamplingFromBestHistoryToNewSubpopulation;

		public uint MaxStagnationCounter;
		public double SlopeForNewSeed;
		public double SlopeForStagnation;
		public uint RangeForSlope;

		/// <summary>
		/// The amount of Mmnts in trail for the particle filtering
		/// </summary>
		public uint TrailLengthInMinutes;
		public double TrainTrailFase1Fraction;
		public double TrainTrailSkipFirstFraction;

		public double NrXTrailForCarbSlopeEnd;
		public double NrXTrailForCarbSlopeStart;
		public uint TrailForEvaluationInMinutes;

		public bool USE_OVERSHOOTS_IN_WEIGHT;
		public double GLUC_THRESHOLD_FOR_UNDERSHOOT;
		public double noisyLowerThanPredictedAtMaximumFactor;
		public double noisyHigherThanPredictedAtMaximumFactor;
		public double noisyLowerThanPredictedAtMinimumFactor;
		public double noisyHigherThanPredictedAtMinimumFactor;


		public double ICR_lower_bound;
		public double ICR_upper_bound;

		public double ISF_lower_bound;
		public double ISF_upper_bound;

		//kan ook < 0 zijn: is dan niet fixed interval (en uiteraard abs(solverInterval) voor het interval
		public int SolverInterval = 1;

		/// <summary>
		///log elke N evaluaties de hele boel naar files
		/// </summary>
		//public uint logEveryNEvaluations;

		/// <summary>
		///log elke N evaluaties de hele boel naar files
		/// </summary>
		public uint evaluateEveryNMinutes;


		//public bool UseParallelProcessing;

		public bool UseParallelProcessingOnSubPopulaties;
		public bool UseParallelProcessingParticleEvaluaties;

		/// <summary>
		/// Boltzmann selection initial value, the particle filtering
		/// </summary>
		public double ExponentialDecayInitValue;
		/// <summary>
		/// Boltzmann selection decay value, the particle filtering
		/// </summary>
		public double ExponentialDecayDecayValue;



		public string CommentsForFileName;
		public string CommentsForPatientIndex;


	}

}
