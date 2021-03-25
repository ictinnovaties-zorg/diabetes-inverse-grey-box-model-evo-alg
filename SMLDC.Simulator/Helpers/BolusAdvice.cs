using SMLDC.Simulator.Schedules.Events;
using System;
using System.Collections.Generic;
using System.Text;
using static SMLDC.Simulator.Utilities.Enums;

namespace SMLDC.Simulator.Helpers
{
	/// <summary>
	/// This class defines the format in which the bolus advice is returned.
	/// </summary>
	public class BolusAdvice
	{
		/// <summary>
		/// The quantity of the bolus.
		/// </summary>
		public double Quantity { get; set; }

		/// <summary>
		/// The type of the bolus, based on the enumeration in BolusAdviceType.
		/// </summary>
		public BolusAdviceType Type { get; set; }

		/// <summary>
		/// Returns the correct unit for the bolus advice.
		/// </summary>
		public string Unit
		{
			get
			{
				switch (Type)
				{
					case BolusAdviceType.CARBS:
						return "g";
					case BolusAdviceType.INSULIN:
						return "IU";
					default:
						return "";
				}
			}
		}

		/// <summary>
		/// The constructor for a bolus advice.
		/// </summary>
		/// <param name="quantity">The quantity of the bolus.</param>
		/// <param name="type">The type of the bolus.</param>
		public BolusAdvice(double quantity, BolusAdviceType type)
		{
			Quantity = quantity;
			Type = type;
		}

		public BolusAdvice(PatientEvent evt)
		{
			Quantity = evt.TrueValue;
			Type = BolusAdviceType.NOTHING;
			if (evt.EventType == PatientEventType.INSULIN) 
			{
				Type = BolusAdviceType.INSULIN;
			}
			else if(evt.EventType == PatientEventType.CARBS)
			{
				Type = BolusAdviceType.CARBS;
			}

		}

		public override string ToString()
		{
			switch (Type)
			{
				case BolusAdviceType.CARBS:
					return "<Advice Carbs: "+ Math.Round(Quantity, 1) + "" +  Unit;
				case BolusAdviceType.INSULIN:
					return "<Advice Ins: " + Math.Round(Quantity, 1) + "" + Unit;
			}

			return "No action necessary.";
		}
	}
}
