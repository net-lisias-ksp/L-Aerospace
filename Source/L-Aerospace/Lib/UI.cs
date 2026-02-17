/*
	This file is part of L Aerospace
		© 2018-2026 LisiasT : http://lisias.net <support@lisias.net>

	L Aerospace is double licensed, as follows:
		* SKL 1.0 : https://ksp.lisias.net/SKL-1_0.txt
		* GPL 2.0 : https://www.gnu.org/licenses/gpl-2.0.txt

	And you are allowed to choose the License that better suit your needs.

	L Aerospace is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

	You should have received a copy of the SKL Standard License 1.0
	along with L Aerospace. If not, see <https://ksp.lisias.net/SKL-1_0.txt>.

	You should have received a copy of the GNU General Public License 2.0
	along with L Aerospace. If not, see <https://www.gnu.org/licenses/>.
*/
using System;
using UnityEngine;

namespace L_Aerospace.Lib
{
	public static class UI
	{
		internal static readonly Color Message_Colour_Confirm = Color.green;
		internal static readonly Color Message_Colour_Confirm_Restrictions = Color.cyan;
		internal static readonly Color Message_Colour_Warning = Color.yellow;
		internal static readonly Color Message_Colour_Error = Color.red;

		public static string Format(float value, int decimals, string unit = null) => Format((double)value, decimals, unit);
		public static string Format(double value, int decimals, string unit = null)
		{
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
						value /= Math.Pow(10, 3);
						break;
					case 6: case 7: case 8:
						unit = "m" + unit??"";
						decimals = 4;
						value /= Math.Pow(10, 6);
						break;
					default:
						decimals = 6;
						--exponent;
						value /= Math.Pow(10, exponent);
						unit = string.Format("e+{0}", exponent) + unit??"";
						break;
				}
			}
			string mask = "{0:0" + (0 != decimals?"." + new string('0', decimals):"") + "}" + unit??"";
			return string.Format(mask, value);
		}

		public static void PostScreenWarning(string msg) => ScreenMessages.PostScreenMessage(msg).color = Message_Colour_Warning;
	}
}
