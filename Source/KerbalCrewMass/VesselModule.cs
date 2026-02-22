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
using System.Collections.Generic;

namespace L_Aerospace { namespace Kerbal { namespace CrewMass
{
	public class Controller : L_Aerospace.Lib.AbstractVesselModule
	{
		private readonly List<KerbalCrewMass> list = new List<KerbalCrewMass>();

		#region KSP Life Cycle

		protected override void DoLoadVessel() => this.hardActive = Globals.Instance.KerbalCrewMass;
		protected override void DoStart() => this.populate();
		protected override void DoGoOnRails() => this.list.Clear();

		protected override void DoGoOffRails()
		{
			this.populate();
			this.Active = this.list.Count > 0;
		}

		protected override void DoVesselChange(Vessel vessel) => this.populate();

		#endregion

		private void populate()
		{
			this.list.Clear(); // Better safer then sorrier.
			for (int i = 0; i < this.vessel.parts.Count; ++i)
			{
				Part p = this.vessel.parts[i];
				KerbalCrewMass m = p.FindModuleImplementing<KerbalCrewMass>();
				if (null == m) continue;
				this.list.Add(m);
			}
		}

		private static new readonly KSPe.Util.Log.Logger Log = KSPe.Util.Log.Logger.CreateForType<Controller>("L_Aerospace.Kerbal.CrewMass", "Controller", 0);
		protected override KSPe.Util.Log.Logger GetLogger() => Log;
	}
} } }
