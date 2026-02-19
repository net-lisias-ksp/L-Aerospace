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
using UnityEngine;
using KSPe.Annotations;
using System.Collections.Generic;

namespace L_Aerospace
{
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	internal class Startup : MonoBehaviour
	{
		[UsedImplicitly]
		private void Start() {
			Log.force("Version {0} with {1}", Version.Text, this.getInstalledModules());
		}

		private object getInstalledModules()
		{
			List<string> installed = new List<string>(5);
			if (ModuleManagerSupport.checkForCrewMass()) installed.Add("CrewMass");
			if (ModuleManagerSupport.checkForCrewHeat()) installed.Add("CrewHeat");
			if (ModuleManagerSupport.checkForHeatPump()) installed.Add("HeatPump");
			return string.Join(", ", installed.ToArray());
		}
	}
}
