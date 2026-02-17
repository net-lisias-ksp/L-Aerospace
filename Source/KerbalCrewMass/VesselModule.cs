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
	public class Controller : VesselModule
	{
		public bool Active
		{
			get => this.enabled;
			private set
			{
				this.enabled = value;
			}
		}

		private readonly List<KerbalCrewMass> list = new List<KerbalCrewMass>();

		#region KSP Life Cycle

		protected override void OnAwake()
		{
			Log.dbg("{0}:OnAwake", this.ID);
			base.OnAwake();
			this.Active = Globals.Instance.KerbalCrewMass;
		}

		protected override void OnStart()
		{
			Log.dbg("{0}:OnStart", this.ID);
			base.OnStart();
			this.populate();
		}

		public override void OnGoOnRails()
		{
			Log.dbg("{0}:OnGoOnRails {1}", this.ID, this.Active);
			base.OnGoOnRails();
			this.list.Clear();
		}

		public override void OnGoOffRails()
		{
			Log.dbg("{0}:OnGoOffRails {1}", this.ID, this.Active);
			base.OnGoOffRails();
			this.populate();
			this.Active = Globals.Instance.KerbalCrewMass;
		}

		public override void OnLoadVessel()
		{
			Log.dbg("{0}:OnLoadVessel", this.name, this.vessel.GetInstanceID());
			base.OnLoadVessel();
			GameEvents.onVesselChange.Add(this.OnVesselChange);
		}

		public override void OnUnloadVessel()
		{
			Log.dbg("{0}:OnUnloadVessel", this.name, this.vessel.GetInstanceID());
			base.OnUnloadVessel();
			GameEvents.onVesselChange.Remove(this.OnVesselChange);
		}

		private void OnVesselChange(Vessel data)
		{
			this.populate();
		}

		#endregion


		internal void Activate() => this.setActive(true);
		internal void Deactivate() => this.setActive(false);

		private void setActive(bool value)
		{
			for(int i = 0; i < this.list.Count; ++i)
				this.list[i].Active = value;
		}

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

		private String __ID = null;
		public String ID => __ID??(__ID = String.Format("{0}:{1:X}", this.name, this.vessel.GetInstanceID()));
		private static readonly KSPe.Util.Log.Logger Log = KSPe.Util.Log.Logger.CreateForType<Controller>("L_Aerospace.Kerbal.CrewMass", "Controller", 0);
	}
} } }
