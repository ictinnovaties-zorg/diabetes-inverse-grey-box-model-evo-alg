using SMLDC.Simulator.Schedules.Events;
using SMLDC.Simulator.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMLDC.Simulator.DiffEquations.Models
{
	// bevat de DifEq om de data vector te berekenen
	// de indices voor de data vector (voor X, G, etc.)
	// de vrije parameters (Gb, Vb, p1, p2, etc)
	// methodes om vrije parameters te manipuleren en uit config te parsen etc.
	public class BergmanAndBretonModel
	{


		///////////////////////////////////////// DATA VECTOR //////////////////////////////////////
		///////////////////////////////////////// DATA VECTOR //////////////////////////////////////
		///////////////////////////////////////// DATA VECTOR //////////////////////////////////////
		///////////////////////////////////////// DATA VECTOR //////////////////////////////////////
		///////////////////////////////////////// DATA VECTOR //////////////////////////////////////
		///////////////////////////////////////// DATA VECTOR //////////////////////////////////////
		///////////////////////////////////////// DATA VECTOR //////////////////////////////////////
		///////////////////////////////////////// DATA VECTOR //////////////////////////////////////


		//////////////////////// indices v/d values in de data vector /////////////////////////////
		//////////////////////// indices v/d values in de data vector /////////////////////////////

		/// <summary>
		///DEBUG time signal index
		/// </summary>
		public static readonly int T_DebugTimeSignal_ODEindex_MIN = 0; // [min]


		/// <summary>
		/// Blood glucose concentration indexer.
		/// </summary>
		public static readonly int G_Glucose_ODEindex_MG_per_DL = 1;


		/// <summary>
		/// Blood insulin concentration indexer.
		/// </summary>
		public static readonly int I_Insulin_ODEindex__mIU_per_L = 2;

		/// <summary>
		/// The effect of active insulin indexer.
		/// </summary>
		public static readonly int X_InsulinEffectiveness_ODEindex__1_per_MIN = 3;


		/// <summary>
		/// Blood insulin concentration indexer.
		/// </summary>
		public static readonly int D_MealDisturbance_ODEindex__mG_per_DL_per_MIN = 4;


		/// <summary>
		/// HeartRate indexer. (proxy for activity)
		/// </summary>
		public static readonly int HR_HeartRate_ODEindex__Hz = 5;

		/// <summary>
		/// Breton's Z value indexer.
		/// </summary>
		public static readonly int Z_EffectOnX_ODEindex = 6;

		/// <summary>
		/// Energy expenditure indexer.
		/// </summary>
		public static readonly int Gamma_EnergyExpenditure_ODEindex = 7;



		/// <summary>
		/// This is the length that the vector for this model is expected to have.
		/// </summary>
		public static readonly int BretonExpectedVectorLength = 8; // is incl .time signal

		public static readonly int BergmanExpectedVectorLength = 5; // incl. time signal

		public int ExpectedVectorLength
		{
			get
			{
				if (this.UseActivity)
				{
					return BretonExpectedVectorLength;
				}
				else
				{
					return BergmanExpectedVectorLength;
				}
			}
		}


		public static double[] GenerateInitialValues(bool useActivity)
        {
			BergmanAndBretonModel model = new BergmanAndBretonModel(useActivity);
			return model.GenerateInitialValues();
        }

		public double[] GenerateInitialValues()
		{
			double[] startData = new double[this.UseActivity ? BergmanAndBretonModel.BretonExpectedVectorLength : BergmanAndBretonModel.BergmanExpectedVectorLength];
			startData[BergmanAndBretonModel.T_DebugTimeSignal_ODEindex_MIN] = 0;
			startData[BergmanAndBretonModel.G_Glucose_ODEindex_MG_per_DL] = this.Gb_in_MG_per_DL;
			startData[BergmanAndBretonModel.X_InsulinEffectiveness_ODEindex__1_per_MIN] = 0;
			startData[BergmanAndBretonModel.I_Insulin_ODEindex__mIU_per_L] = 0;
			startData[BergmanAndBretonModel.D_MealDisturbance_ODEindex__mG_per_DL_per_MIN] = 0;

			if (this.UseActivity)
			{
				startData[BergmanAndBretonModel.Z_EffectOnX_ODEindex] = 0;
				startData[BergmanAndBretonModel.Gamma_EnergyExpenditure_ODEindex] = this.BaseHeartRate; // gamma is gesmoothe hr.
				startData[BergmanAndBretonModel.HR_HeartRate_ODEindex__Hz] = this.BaseHeartRate;
			}
			return startData;
		}



		/////////////////////////// CONSTRUCTORS, parsen van config /////////////////////////////
		/////////////////////////// CONSTRUCTORS, parsen van config /////////////////////////////
		/////////////////////////// CONSTRUCTORS, parsen van config /////////////////////////////
		/////////////////////////// CONSTRUCTORS, parsen van config /////////////////////////////
		/////////////////////////// CONSTRUCTORS, parsen van config /////////////////////////////
		/////////////////////////// CONSTRUCTORS, parsen van config /////////////////////////////




		public BergmanAndBretonModel DeepCopy() { return new BergmanAndBretonModel(this); }
		public BergmanAndBretonModel( bool useActivity) { this.UseActivity = useActivity; }
		public BergmanAndBretonModel(BergmanAndBretonModel that)
		{
			this.Gb_in_MG_per_DL = that.Gb_in_MG_per_DL;
			this.P1 = that.P1;
			this.P2 = that.P2;
			this.P3 = that.P3;
			this.P4 = that.P4;
			this.Vb_in_L = that.Vb_in_L;
			this.Vi_in_L = that.Vi_in_L;
			this.DRate = that.DRate;
			this.Carbs2Gluc = that.Carbs2Gluc;

			this.UseActivity = that.UseActivity;

			this.BaseHeartRate = that.BaseHeartRate;
			this.alpha = that.alpha;
			this.beta = that.beta;
			this.GammaFaFactor = that.GammaFaFactor;
			this.GammaFnPower = that.GammaFnPower;
			this.GammaFaFactor_X_BaseHeartRate = this.GammaFaFactor * this.BaseHeartRate;
			this.TauGamma = that.TauGamma;
			this.tauZ = that.tauZ;
		}

		public BergmanAndBretonModel(RandomStuff random, Dictionary<string, double[]> patientParameters) //, Dictionary<string, double[]> pfRangeParameters)
		{
			this.UseActivity = (patientParameters["UseBretonActivityModel"][0] > 0.5);
			for (int i = 0; i < freeParamNames.GetLength(0); i++)
			{
				double[] values = patientParameters[freeParamNames[i, 1]]; // officiele naam gebruiken
																		   // hier worden de random stddev ranges gebruikt die in de ini stonden:
				double value = random.GetNormalDistributed(values[0], values[1], Globals.maxSigma);
				this.SetParameter(i, value);
			}
		}




		public static void Initialize_boundingRanges(Dictionary<string, double[]> pfRangeParameters)
		{
			for (int i = 0; i < freeParamNames.GetLength(0); i++)
			{
				double[] values = pfRangeParameters[freeParamNames[i, 1]]; // officiele naam gebruiken
				lowerHigherBounds[i, 0] = values[0];
				lowerHigherBounds[i, 1] = values[1];
			}
		}

		public static void Initialize_boundingRanges(int i, double minvalue, double maxvalue)
        {
			lowerHigherBounds[i, 0] = minvalue;
			lowerHigherBounds[i, 1] = maxvalue;
		}


		// todo refactor in termen van getParam en paramNames etc...
		public override string ToString()
		{
			string txt = $"gb:{OctaveStuff.MyFormat(Gb_in_MG_per_DL)} " +
				   $"p1:{OctaveStuff.MyFormat(P1)} " +
				   $"p2:{OctaveStuff.MyFormat(P2)} " +
				   $"p3:{OctaveStuff.MyFormat(P3)} " +
				   $"p4:{OctaveStuff.MyFormat(P4)} " +
				   $"Vi:{OctaveStuff.MyFormat(Vi_in_L)} " +
				   $"Vb:{OctaveStuff.MyFormat(Vb_in_L)} " +
				   $"HRb:{OctaveStuff.MyFormat(BaseHeartRate)} " +
				   $"dRate:{OctaveStuff.MyFormat(DRate)} " +
				   $"Carb2gluc:{OctaveStuff.MyFormat(Carbs2Gluc)} " +
			$"UseActivity:" + (UseActivity ? "true " : "false ");

			if (UseActivity)
			{
				txt += $"tauZ:{OctaveStuff.MyFormat(tauZ)} " +
				   $"TauHeartRate:{OctaveStuff.MyFormat(TauGamma)} " +
					$"powGamma:{OctaveStuff.MyFormat(GammaFnPower)} " +
					$"a_factorGamma:{OctaveStuff.MyFormat(GammaFaFactor)} " +
					$"alpha:{OctaveStuff.MyFormat(alpha)} " +
					$"beta:{OctaveStuff.MyFormat(beta)} " +
					"";
			}
			return txt;
		}


		// todo: refactor in termen van Paramnames ...
		public string ToCSVHeader()
		{
			//de \t zijn afgestemd (ongeveer) op ToCSV() functie
			string txt = "\tgb,\t\t\tp1,\t\t\tp2,\t\t\tp3,\t\t\tp4,\t\t\tVi,\t\t\tVb,\t\tDRate,\t\tCarb2gluc";
			if (UseActivity)
			{
				txt += ", \tHRb, \ttauZ,\tTauHR,\tpowGamma,\ta_factorGamma,\talpha,\tbeta";
			}
			return txt;
		}


		// todo refactor .. zie hierboven
		public string ToCSV()
		{
			// afgestemd (wbt \t) ongeveer op ToCSVHeader()
			string txt = "" + OctaveStuff.MyFormat(Gb_in_MG_per_DL) + ",\t" + OctaveStuff.MyFormat(P1) + ",\t" + OctaveStuff.MyFormat(P2) + ",\t" +
				   OctaveStuff.MyFormat(P3) + ",\t" + OctaveStuff.MyFormat(P4) + ",\t" + OctaveStuff.MyFormat(Vi_in_L) + ",\t" +
				   OctaveStuff.MyFormat(Vb_in_L) + ",\t" +
				   OctaveStuff.MyFormat(DRate) + ",\t" +
				   OctaveStuff.MyFormat(Carbs2Gluc);

			if (UseActivity)
			{
				txt += ",\t" + OctaveStuff.MyFormat(BaseHeartRate) + ",\t" +
					OctaveStuff.MyFormat(tauZ) + ",\t" +
				   OctaveStuff.MyFormat(TauGamma) + ",\t" +
				   OctaveStuff.MyFormat(GammaFnPower) + ",\t" +
				   OctaveStuff.MyFormat(GammaFaFactor) + ",\t" +
				   OctaveStuff.MyFormat(alpha) + ",\t" +
				   OctaveStuff.MyFormat(beta);
			}
			return txt;
		}










		///////////////////////////////////////// DIFF EQ BEREKENINGEN //////////////////////////////////////
		///////////////////////////////////////// DIFF EQ BEREKENINGEN //////////////////////////////////////
		///////////////////////////////////////// DIFF EQ BEREKENINGEN //////////////////////////////////////
		///////////////////////////////////////// DIFF EQ BEREKENINGEN //////////////////////////////////////
		///////////////////////////////////////// DIFF EQ BEREKENINGEN //////////////////////////////////////
		///////////////////////////////////////// DIFF EQ BEREKENINGEN //////////////////////////////////////
		///////////////////////////////////////// DIFF EQ BEREKENINGEN //////////////////////////////////////





		public double[] Derive(double time, double[] y, double[] deltaY /*optimalisation: reuse array*/)
		{
			//update time: done in caller (midpointSolver)

			double G = y[G_Glucose_ODEindex_MG_per_DL];
			double I = y[I_Insulin_ODEindex__mIU_per_L];
			double D = y[D_MealDisturbance_ODEindex__mG_per_DL_per_MIN];
			double X = y[X_InsulinEffectiveness_ODEindex__1_per_MIN];

			// Calculates the effect of active insulin
			deltaY[X_InsulinEffectiveness_ODEindex__1_per_MIN] = -P2 * X + P3 * I; // (I - this.Ib_in_MIU_per_L);

			// Calculates the blood insulin concentration
			double u = 0; //wilco: hier stond y[ExogenousInsulinIndex] maar dat wordt nu al in HandleAddInsulinInMUI afgehandeld
			deltaY[I_Insulin_ODEindex__mIU_per_L] = -(this.P4 * I) + (u / this.Vi_in_L);

			// Calculates the meal disturbance.
			deltaY[D_MealDisturbance_ODEindex__mG_per_DL_per_MIN] = -this.DRate * D;


			if (this.UseActivity)
			{
				deltaY[HR_HeartRate_ODEindex__Hz] = 0; //constant (alleen activity/hr verandert HR).

				double gamma_EnergyExpenditure = y[Gamma_EnergyExpenditure_ODEindex];
				double Z = y[Z_EffectOnX_ODEindex];

				// Breton: artikel: Physical Activity—The Major Unaccounted Impediment to Closed Loop Control
				double eeFactor = 1; // 0.01;  // 1: helemaal de HR volgen (snel). 0.01: heel vertraagd
				deltaY[Gamma_EnergyExpenditure_ODEindex] = (-1 / this.TauGamma) * gamma_EnergyExpenditure + (1 / this.TauGamma) * (y[HR_HeartRate_ODEindex__Hz] - this.BaseHeartRate);
				deltaY[Gamma_EnergyExpenditure_ODEindex] = eeFactor * deltaY[Gamma_EnergyExpenditure_ODEindex];

				double fGamma = GammaFunction(gamma_EnergyExpenditure);
				// toename is vooral van FY_breton afhankelijk, is snel.
				// fGamma tussen 0 en 1.
				deltaY[Z_EffectOnX_ODEindex] = -(fGamma + 1 / this.tauZ) * Z + fGamma;

				// breton paper: Physical Activity—The Major Unaccounted Impediment to Closed Loop Control
				// orig bergman::: -(_data.P1 + x) * g + _data.P1 * gb;  == -p1 * (G - Gb) - X * G

				deltaY[G_Glucose_ODEindex_MG_per_DL] = -this.P1 * (G - this.Gb_in_MG_per_DL)
								- (1 + this.alpha * Z) * X * G
								- this.beta * gamma_EnergyExpenditure * G
								+ D;

			}
			else
			{
				// Calculates the blood glucose concentration. 
				deltaY[G_Glucose_ODEindex_MG_per_DL] = -this.P1 * (G - this.Gb_in_MG_per_DL) - X * G + D;
			}

			return deltaY;
		}



		// als in Breton's paper over activity
		private double GammaFunction(double d)
		{
			double d_over_HRb_pow_n = Math.Abs(MyMath.FastPow(d / (this.GammaFaFactor_X_BaseHeartRate), this.GammaFnPower));
			return Math.Max(0, d_over_HRb_pow_n / (1 + d_over_HRb_pow_n)); // aftoppen voor geval de base rate hoger is
																		   // en dus de  input d < 0 en dan zou je bij hr < hrbase meer gaan verbruiken!
		}





		public void HandleAddInsulinEvent(PatientEvent evt, double[] currentValues)
		{
			double insuline_in_mIU = 1000 * evt.Insulin_TrueValue_in_IU;
			currentValues[I_Insulin_ODEindex__mIU_per_L] += insuline_in_mIU / this.Vi_in_L;

		}


		public void HandleEatingEvent(PatientEvent evt, double[] currentValues)
		{
			double glucose_in_mG = evt.Carb_TrueValue_in_gram * 1000; // van gr --> mg.
																   //gluc aan mealdist. toevoegen:
																   //omrekenen van mgGluc naar D [mg/dl] van Fisher functie:
																   //integraal berekend (op papier) en dan formules doorrekenen, levert dit resultaat
			double dFisher = (glucose_in_mG / (this.Vb_in_L * 10)) * this.DRate;

			// dFisher [mg/dl] Fischer D-functie startwaarde, en wordt eenmalig toegevoegd
			currentValues[D_MealDisturbance_ODEindex__mG_per_DL_per_MIN] += this.Carbs2Gluc * dFisher;
		}






		////////////////////////////////////// VRIJE PARAMETERS in DIFF EQ //////////////////////////////////////
		////////////////////////////////////// VRIJE PARAMETERS in DIFF EQ //////////////////////////////////////
		////////////////////////////////////// VRIJE PARAMETERS in DIFF EQ //////////////////////////////////////
		////////////////////////////////////// VRIJE PARAMETERS in DIFF EQ //////////////////////////////////////
		////////////////////////////////////// VRIJE PARAMETERS in DIFF EQ //////////////////////////////////////
		////////////////////////////////////// VRIJE PARAMETERS in DIFF EQ //////////////////////////////////////
		////////////////////////////////////// VRIJE PARAMETERS in DIFF EQ //////////////////////////////////////
		////////////////////////////////////// VRIJE PARAMETERS in DIFF EQ //////////////////////////////////////
		////////////////////////////////////// VRIJE PARAMETERS in DIFF EQ //////////////////////////////////////


		/// Basal blood glucose concentration.
		public double Gb_in_MG_per_DL;

		/// Glucose clearance rate independent of insulin // ("Glucose afname, onafhankelijk van insuline")] //[MeasurementUnit("1/min")]
		public double P1;

		/// Rate of clearance of active insulin (decrease of uptake). //[FullName("Snelheid van actieve insuline ( p2 omhoog, ICR & ISF omlaag)")]
		//[MeasurementUnit("1/min")]
		public double P2;

		/// Increase in uptake ability caused by insulin. //[FullName("Verhoog het opnamevermogen veroorzaakt door insuline (p3 omhoog, ICR & ISF omhoog)")]
		//[MeasurementUnit("L/(min*min)mE")]
		public double P3;

		/// Decay rate of blood insulin.  /[FullName("Afbraaksnelheid van insuline in het bloed (p4 omhoog, ICR & ISF omlaag)")]
		//[MeasurementUnit("1/min")]
		public double P4;

		/// Rate of decrease of the meal disturbance.
		//[FullName("Snelheid van afname van de glucose toename na een maaltijd (nb als te laag, dan problemen met binair zoeken naar ICR, ISF!)")]
		//[MeasurementUnit("1/min")]
		public double DRate;

		/// factor (niet in Bergman) "conversie" van carbs naar glucose.//[FullName("factor van carbs naar glucose (niet in Bergman)")]
		//[MeasurementUnit("[dimensieloos]")]
		public double Carbs2Gluc;

		/// Volume of insulin pool in person [L]. //[FullName("Insuline volume  (omhoog, ICR & ISF omlaag)")] 		//[MeasurementUnit("L")]
		public double Vi_in_L;

		/// Rate of utilization.	//[FullName("Gebruikssnelheid")]
	//	public double Rutln;

		/// Volume of BLOOD in person [L]. 	//[FullName("blood volume (omhoog, alleen ICR omhoog)")]	//[MeasurementUnit("L")]
		public double Vb_in_L;


		// activity model:

		//[FullName("UseActivity")]		//[MeasurementUnit("BOOLEAN")]
		public bool UseActivity;

		/// Base Heart Rate (experimental) [BPM]. 		//[FullName("base heart rate")]	//[MeasurementUnit("1/min")]
		public double BaseHeartRate;

		//[FullName("tauZ")]		//[MeasurementUnit("min.")]
		public double tauZ;

		//[FullName("TauHeartRate")]	//[MeasurementUnit("min.")]
		public double TauGamma; //TODO: leerbare param van maken??? 

		//[FullName("powGamma")]	
		public double GammaFnPower; //scherpte

		//[FullName("a_factorGamma")]	
		public double GammaFaFactor; //niks over in Breton paper! maakt de V curve breder (hoog) of smaller (laag).

		//[FullName("alpha")]
		public double alpha;

		//[FullName("beta")]
		public double beta;


		public double GammaFaFactor_X_BaseHeartRate;




		//////////////////////////// indices for free parameters. ///////////////////////////////////
		//////////////////////////// indices for free parameters. ///////////////////////////////////
		//////////////////////////// indices for free parameters. ///////////////////////////////////
		//////////////////////////// indices for free parameters. ///////////////////////////////////
		//////////////////////////// indices for free parameters. ///////////////////////////////////


		// Hiermee kunnen van buiten makkelijk mbv een loop alle param van een model gemanipuleerd worden
		public const int parameter_index_Gb = 0;
		public const int parameter_index_P1 = 1;
		public const int parameter_index_P2 = 2;
		public const int parameter_index_P3 = 3;
		public const int parameter_index_P4 = 4;
		public const int parameter_index_Vi = 5;
		public const int parameter_index_Vb = 6;
		public const int parameter_index_DRate = 7;

		public const int parameter_index_Carbs2Gluc = 8;
		//activity
		public const int parameter_index_HRb = 9;
		public const int parameter_index_activityTauZ = 10;
		public const int parameter_index_ActivityTauHeartRate = 11;
		public const int parameter_index_activityPowGamma = 12;
		public const int parameter_index_activityAfactorGamma = 13;
		public const int parameter_index_ActivityAlpha = 14;
		public const int parameter_index_ActivityBeta = 15;

		public double GetParameter(int ndx)
		{
			switch (ndx) {
				// kan helaas niet met de hierboven gedef. static int's (want niet const!)
				case parameter_index_Gb: return this.Gb_in_MG_per_DL;
				case parameter_index_P1: return this.P1;
				case parameter_index_P2: return this.P2;
				case parameter_index_P3: return this.P3;
				case parameter_index_P4: return this.P4;
				case parameter_index_Vi: return this.Vi_in_L;
				case parameter_index_Vb: return this.Vb_in_L;
				case parameter_index_DRate: return this.DRate;
			//	case index_Rutln: return this.Rutln;
				case parameter_index_Carbs2Gluc: return this.Carbs2Gluc;
				case parameter_index_HRb: return this.BaseHeartRate;
				case parameter_index_activityTauZ: return this.tauZ;
				case parameter_index_ActivityTauHeartRate: return this.TauGamma;
				case parameter_index_activityPowGamma: return this.GammaFnPower;
				case parameter_index_activityAfactorGamma: return this.GammaFaFactor;
				case parameter_index_ActivityAlpha: return this.alpha;
				case parameter_index_ActivityBeta: return this.beta;
				//case index_Ib: return this.Ib_in_MIU_per_L;
				default: { throw new ArgumentException("GetRealModelValue(" + ndx + ") ==> parameter bestaat niet!"); }
			}
		}

		public void SetParameterIgnoreBounds(int ndx, double value)
        {
			SetParameter(ndx, value, true);
        }
		public void SetParameter(int ndx, double value, bool ignoreClipping=false)
		{ 
			if(double.IsNaN(value) || double.IsInfinity(value))
			{
				throw new ArgumentException("VALUE IS NAN or INF");
			}
			if(!ignoreClipping)
			{
				value = MyMath.Clip(value, lowerHigherBounds[ndx, 0], lowerHigherBounds[ndx, 1]);
			}
			switch (ndx) {
				case parameter_index_Gb: { this.Gb_in_MG_per_DL = value; break; }
				case parameter_index_P1: { this.P1 = value; break; }
				case parameter_index_P2: { this.P2 = value; break; }
				case parameter_index_P3: { this.P3 = value; break; }
				case parameter_index_P4: { this.P4 = value; break; }
				case parameter_index_Vi: { this.Vi_in_L = value; break; }
				case parameter_index_Vb: { this.Vb_in_L = value; break; }
				case parameter_index_DRate: { this.DRate = value; break; }
			//	case index_Rutln: { this.Rutln = value; break; }
				case parameter_index_Carbs2Gluc: { this.Carbs2Gluc = value; break; }
				case parameter_index_HRb: {
						this.BaseHeartRate = value;
						this.GammaFaFactor_X_BaseHeartRate = this.GammaFaFactor * this.BaseHeartRate;
						break; 
					}
				case parameter_index_activityTauZ: { this.tauZ = value; break; }
				case parameter_index_ActivityTauHeartRate: { this.TauGamma = value; break; }
				case parameter_index_activityPowGamma: { this.GammaFnPower = value; break; }
				case parameter_index_activityAfactorGamma: {
						this.GammaFaFactor = value;
						this.GammaFaFactor_X_BaseHeartRate = this.GammaFaFactor * this.BaseHeartRate;
						break; }
				case parameter_index_ActivityAlpha: { this.alpha = value; break; }
				case parameter_index_ActivityBeta: { this.beta = value; break; }
				default: { throw new ArgumentException("SetRealModelValue(" + ndx + ") ==> parameter bestaat niet!"); }
			}
		}



		public void SetParameters_RandomInLogRange(RandomStuff random, double[,] lower_higher_bounds_in_orig_space = null)
		{
			if(lower_higher_bounds_in_orig_space == null)
            {
				lower_higher_bounds_in_orig_space = lowerHigherBounds;
			}
			for (int i = 0; i < GetNrOfParameters(); i++)
			{
				SetParameter_RandomInLogRange(random, i, lower_higher_bounds_in_orig_space[i, 0], lower_higher_bounds_in_orig_space[i, 1]);
			}
		}

		private void SetParameter_RandomInLogRange(RandomStuff random, int ndx, double minvalue, double maxvalue)
		{
			// random  log10_space uniformverdeelde waarde kiezen
			double rndvalue = random.GetRandomValue_UniformInLogRange(minvalue, maxvalue);
			SetParameter(ndx, rndvalue);
		}






		// per rij: korte naam en lange (config ini naam)
		public static readonly string[,] freeParamNames =   {
			{ "Gb",                 "Gb_in_MG_per_DL"},
			{ "P1",                 "P1" },
			{ "P2",                 "P2" },
			{ "P3",                 "P3" },
			{ "P4",                 "P4" },
			{ "Vi",                 "Vi_in_L" },
			{ "Vb",                 "Vb_in_L"},
			{ "DRate",              "DRate" },
			{ "Carb2Gl",            "Carb2Gluc" },
			// activity model:
			{ "HRb",                "RestingHeartRate"},
			{ "acTauZ",             "TauZ" },
			{ "acTauHR",            "TauGamma" },
			{ "acPowG",				"GammaFnPower" },
			{ "acAfacG",			"GammaFaFactor" },
			{ "acAlpha",            "Alpha" },
			{ "acBeta",             "Beta" }
		};

		public string GetParameterName(int i) { return freeParamNames[i, 0]; }
		public static  string GetParameterName(bool activity, int i) { return GetParameterNames(activity)[i]; }
		public string[] GetParameterNames() { return GetParameterNames(this.UseActivity); }
		public static string[] GetParameterNames(bool UseActivityModel)
		{
			string[] names = new string[GetNrOfParameters(UseActivityModel)];
			for (int i = 0; i < names.Length; i++)
			{
				names[i] = freeParamNames[i, 0];
			}
			return names;
		}

		public uint GetNrOfParameters() { return GetNrOfParameters(this.UseActivity); }
		public static uint GetNrOfParameters(bool UseActivityModel)
		{
			if (UseActivityModel)
			{
				return 16; // Ib wordt hier meegerekend
			}
			else
			{
				return 9;
			}
		}




		private static double[,] lowerHigherBounds = new double[,] {
														{ 70,       150 },        // Gb
														{0.000001,  0.01 },       // P1
														{0.00001,   0.3 },        // P2
														{1e-7,      1e-3 },       // P3
														{0.001,     0.9 },        // P4
														{8,         16 },         // Vi
														{2.5,       7 },          // Vb
                                                        {0.01,      0.1 },        // Drate

                                                        {0.1,       10 } ,       // carb2gluc

                                                        {30,        130 },       // HRb
														{100,       3000  } ,    // TauZ
														{1,         100 },       // TauHR
														{1,         10 },        // PowGamma
														{1,         10 },        // AFactor gamma
														{0.1,       1 },         // alpha
														{1e-5,      1e-3},       // beta
														{ 0,        0 }          // Ib
											};

		public static double[,] Get_LOWER_HIGHER_BOUNDS()
		{
			return lowerHigherBounds;
		}
		public static double GetLowerBound(int param)
		{
			return lowerHigherBounds[param, 0];
		}
		public static  double GetHigherBound(int param)
		{
			return lowerHigherBounds[param, 1];
		}
		public static bool InBounds(int parameter, double value)
        {
			return (value >= lowerHigherBounds[parameter, 0] && value <= lowerHigherBounds[parameter, 1]);
        }
		public void Add(BergmanAndBretonModel that)
		{
			this.Gb_in_MG_per_DL += that.Gb_in_MG_per_DL;
			this.P1 += that.P1;
			this.P2 += that.P2;
			this.P3 += that.P3;
			this.P4 += that.P4;
			this.Vb_in_L += that.Vb_in_L;
			this.Vi_in_L += that.Vi_in_L;
			this.DRate += that.DRate;
			this.Carbs2Gluc += that.Carbs2Gluc;

			if (this.UseActivity)
			{
				this.BaseHeartRate += that.BaseHeartRate;
				this.alpha += that.alpha;
				this.beta += that.beta;
				this.GammaFaFactor += that.GammaFaFactor;
				this.GammaFnPower += that.GammaFnPower;
				this.TauGamma += that.TauGamma;
				this.tauZ += that.tauZ;
			}
		}

		public BergmanAndBretonModel Add_InLogSpace(BergmanAndBretonModel sigmas)
		{
			BergmanAndBretonModel newSigmas = new BergmanAndBretonModel(sigmas.UseActivity);
			for (int i = 0; i < sigmas.GetNrOfParameters(); i++)
			{
				double logParamValue = Math.Log10(this.GetParameter(i));
				double value = Math.Pow(10, logParamValue + sigmas.GetParameter(i));
				// aftoppen , binnen range blijven door te "stuiteren" tegen de bbox:
				double factor = 1; // --> 1 is zelfde richting aanhouden, -1 is omkeren.
				if (value < lowerHigherBounds[i, 0])
				{
					value = lowerHigherBounds[i, 0];
					factor = -1; // omkeren
				}
				else if (value > lowerHigherBounds[i, 1])
				{
					value = lowerHigherBounds[i, 1];
					factor = -1; // omkeren
				}
				this.SetParameter(i, value);
				// vector van richting veranderen op dimensie die buiten range zou komen, stuiteren tegen 'wanden' (bounding hypercube)
				newSigmas.SetParameterIgnoreBounds(i, factor * sigmas.GetParameter(i));
			}
			return newSigmas;
		}



	}



}
