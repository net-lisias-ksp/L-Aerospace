using System;

namespace Tests
{
	class MainClass
	{
		private static string FormatToUserInterface(float value, int decimals, string unit = null)
		{
			Console.WriteLine("{0} {1} {2}", value, decimals, unit);
			{
				string repr = ((int)value).ToString();
				int exponent = (int)(repr.Length -1);
				switch (exponent)
				{
					case 0: case 1: case 2:
						break;
					case 3: case 4: case 5:
						unit = "k" + unit??"";
						decimals = 2;
						value /= (float)Math.Pow(10, 3);
						break;
					case 6: case 7: case 8:
						unit = "m" + unit??"";
						decimals = 4;
						value /= (float)Math.Pow(10, 6);
						break;
					default:
						decimals = 6;
						--exponent;
						value /= (float)Math.Pow(10, exponent);
						unit = string.Format("e+{0}", exponent) + unit??"";
						break;
				}
				Console.WriteLine("{0} {1} {2}", repr, exponent, value);
			}
			string mask = "{0:0" + (0 != decimals?"." + new string('0', decimals):"") + "}" + unit??"";
			Console.WriteLine("{0} {1} {2}", mask, value, String.Format(mask, value));
			return string.Format(mask, value);
		}

		private static double ResourceOnus(double x)
		{
			// y=a*log_base_1.5(x-h)+k https://www.desmos.com/calculator
			double thisResourceOnus = Math.Max(1, 0.3 * Math.Log(x - -4.06, 1.5) + -0.2);
			return thisResourceOnus;
		}

		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");
			Console.WriteLine("\n{0}\n---", FormatToUserInterface(99.32f, 2, "oC"));
			Console.WriteLine("\n{0}\n---", FormatToUserInterface(340.23f, 2, "oC"));
			Console.WriteLine("\n{0}\n---", FormatToUserInterface(1340.3f, 2, "oC"));
			Console.WriteLine("\n{0}\n---", FormatToUserInterface(102388.233f, 2, "oC"));
			Console.WriteLine("\n{0}\n---", FormatToUserInterface(10233828.233f, 2, "oC"));

			for (int i = 0 ; i < 100; ++i)
			Console.WriteLine("{0} : {1}", i, ResourceOnus((double)i));
		}
	}
}
