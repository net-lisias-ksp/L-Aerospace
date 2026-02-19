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

using System.Collections.Generic;

namespace L_Aerospace
{
	public class ModuleManagerSupport : UnityEngine.MonoBehaviour
	{
		public static IEnumerable<string> ModuleManagerAddToModList()
		{
			List<string> tags = new List<string>();

			if (checkForCrewMass())		tags.Add("L-AEROSPACE_CREW-MASS");
			if (checkForCrewHeat())		tags.Add("L-AEROSPACE_CREW-HEAT");
			if (checkForHeatPump())		tags.Add("L-AEROSPACE_HEAT-PUMP");
			return tags.ToArray();
		}

		internal static bool checkForCrewMass()
		{
			// FIXME: Detect and prevent the use with Kerbalism or anything that adds CrewMass themselves!
			if (PhysicsGlobals.KerbalCrewMass > 0.0f)
				Log.info("Kerbal Crew Mass detected with the value: {0} tons", PhysicsGlobals.KerbalCrewMass);

			return
					KSPe.Util.SystemTools.Type.Exists.By("L_Aerospace.Kerbal.CrewMass", "Controller")
				&&
					PhysicsGlobals.KerbalCrewMass > 0.0f
				&&
					(KSPe.Util.KSP.Version.Current < KSPe.Util.KSP.Version.FindByVersion(1,11,0))
			;
		}

		internal static bool checkForCrewHeat()
		{
			return KSPe.Util.SystemTools.Type.Exists.By("L_Aerospace.Kerbal.CrewHeat", "Controller");
		}

		internal static bool checkForHeatPump()
		{
			return KSPe.Util.SystemTools.Type.Exists.By("L_Aerospace.Kerbal.HeatPump", "Controller");
		}
	}
}
