using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace SMLDC.Simulator.Utilities
{
	public static class CloneUtilities
	{


		public static double[] CloneArray(double[] arr)
		{
			if (arr == null)
			{
				return null;
			}
			double[] output = new double[arr.Length];
			System.Array.Copy(arr, output, arr.Length);
			return output;
		}
	}
}
