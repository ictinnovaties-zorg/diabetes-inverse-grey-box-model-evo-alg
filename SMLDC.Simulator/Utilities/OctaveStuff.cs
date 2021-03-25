using System;
using System.Collections.Generic;
using System.Text;

namespace SMLDC.Simulator.Utilities
{
	public class OctaveStuff
	{

		public static void WriteToOctaveStream(System.IO.StreamWriter octaveStream, List<string> lijst)
		{
			foreach (string s in lijst)
			{
				octaveStream.WriteLine(s);
			}
		}

		////////// helper functies /////////////////

		// met end-of-line (...) als te lange regel.
		// varname = [ 1, 2, 3, 4, 5, ...
		//  23, 5,6 4, 4, 4 ];
		// elke regel is een string in de gereturnde list
		public static List<string> ToOctaveArrayDelcaration(List<double> values, string varname, string format = null)
		{
			int ndx_for_newline = 2000;  //erg lange regles in octave
			List<string> resultsList = new List<string>();

			///  ... = octave 'end of line'
			var tempresult = varname + "=[";
			for (int i = 0; i < values.Count; i++)
			{
				if (i % ndx_for_newline == (ndx_for_newline - 1))
				{
					tempresult += "...";
					resultsList.Add(tempresult);
					tempresult = "";
				}
				tempresult += MyFormat(values[i], -1, format) + ", ";
			}
			tempresult += "];\n";
			resultsList.Add(tempresult);
			tempresult = "";

			return resultsList;
		}


		public static String MyFormat(string txt, int nr_chars, bool chopoff = false)
		{
			//while (txt.Length < nr_chars)
			//{
			//	txt = " " + txt;
			//}
			//if (chopoff && txt.Length > nr_chars)
			//{
			//	txt = txt.Substring(0, nr_chars);
			//}
			//return txt;
			StringBuilder sb = new StringBuilder(txt);
			int nr_missing = nr_chars -  sb.Length;
			if(nr_missing > 0)
			{
				sb.Insert(0, " ", nr_missing);
			}
			if(nr_missing < 0)
			{
				//teveel:
				return sb.Remove(nr_chars, -nr_missing).ToString();

			}
			else
			{
				return sb.ToString();
			}
		}


		public static String MyFormat(double v, int nr_chars = 0, string format = null)
		{
			if (format != null)
			{
				try
				{
					return v.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
				}
				catch (FormatException)
				{
					Console.WriteLine("OctaveStuff.MyFormat :: format = " + format + " ---> incorrect");
				}
			}
			if (nr_chars <= 7)
			{
				nr_chars = 7;
			}
			string temp = v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
			double absvalue = Math.Abs(v);

			if (absvalue <= 0.01)
			{
				temp = v.ToString("0.00E0", System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (absvalue <= 0.1)
			{
				temp = v.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (absvalue <= 1)
			{
				temp = v.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
			}

			else if (absvalue >= 1000)
			{
				temp = v.ToString("0.00E0", System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (absvalue >= 100)
			{
				temp = v.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (absvalue >= 10)
			{
				temp = v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
			}

	
			if (v == 0) { temp = "0"; }

			while (temp.Length < nr_chars)
			{
				temp = " " + temp;
			}
			if (double.IsInfinity(v))
			{
				temp = temp.Substring(0, nr_chars);
			}
			// om eoa stomme reden komt er nog steeds een , bij bepaalde locale!!
			temp = temp.Replace(",", ".").Replace("E", "e");
			return temp;
		}




	}
}
