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

namespace L_Aerospace.Controller.ControlSurface
{
	public class ModuleControlSurfaceController : AbstractController<ModuleControlSurface>
	{
		[UI_Toggle (disabledText = "Fixed", scene = UI_Scene.All, enabledText = "Flaperon", affectSymCounterparts = UI_Scene.All)]
		[KSPField (isPersistant = true, guiActive = false, guiActiveEditor = true)]
		public bool isFlaperon;

		[UI_Toggle (disabledText = "Flap", scene = UI_Scene.All, enabledText = "Slat", affectSymCounterparts = UI_Scene.All)]
		[KSPField (isPersistant = true, guiActive = false, guiActiveEditor = true)]
		public bool isSlat;

		[UI_Toggle (disabledText = "#autoLOC_6001081", scene = UI_Scene.All, enabledText = "#autoLOC_6001080", affectSymCounterparts = UI_Scene.All)]
		[KSPField (isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#autoLOC_6001333")]
		public bool deploy;

		[UI_FloatRange (scene = UI_Scene.All, stepIncrement = 0.1f, maxValue = 150f, minValue = -150f, affectSymCounterparts = UI_Scene.All)]
		[KSPField (guiFormat = "0", isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Deploy Angle", guiUnits = "%")]
		public float deployAngle = 100f;

		public override void OnStart(StartState state)
		{
			if (StartState.None == state) return; // Do nothing on Loading.
			base.OnStart(state);

			//this.firstSetupMyTargets();
			//this.setupMe();
		}
	}
}
