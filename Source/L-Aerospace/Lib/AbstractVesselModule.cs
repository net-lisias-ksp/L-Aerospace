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

namespace L_Aerospace.Lib
{
	public abstract class AbstractVesselModule : VesselModule
	{
		protected bool hardActive = true;	// In Kraken, we trust! :)
		public bool Active
		{
			get => this.hardActive && this.enabled;
			protected set
			{
				this.enabled = value;
			}
		}

		#region KSP Life Cycle

		protected sealed override void OnAwake()
		{
			Log.dbg("{0}:OnAwake", this.name);	// prevents a NRE due this.vessel.GetInstaceId not working yet.
			base.OnAwake();

			this.DoAwake();
		}

		public sealed override void OnLoadVessel()
		{
			Log.dbg("{0}:OnLoadVessel", this.name, this.vessel.GetInstanceID());
			base.OnLoadVessel();

			GameEvents.onEditorShipModified.Add(this.OnEditorShipModified);
			GameEvents.onVesselChange.Add(this.OnVesselChange);
			this.DoLoadVessel();
		}

		protected sealed override void OnStart()
		{
			Log.dbg("{0}:OnStart", this.ID);
			base.OnStart();

			this.DoStart();
		}

		public sealed override void OnGoOnRails()
		{
			Log.dbg("{0}:OnGoOnRails {1}", this.ID, this.Active);
			base.OnGoOnRails();

			this.DoGoOnRails();
		}

		public sealed override void OnGoOffRails()
		{
			Log.dbg("{0}:OnGoOffRails {1}", this.ID, this.Active);
			base.OnGoOffRails();

			this.DoGoOffRails();
		}

		public sealed override void OnUnloadVessel()
		{
			Log.dbg("{0}:OnUnloadVessel", this.name, this.vessel.GetInstanceID());
			base.OnUnloadVessel();

			GameEvents.onVesselChange.Remove(this.OnVesselChange);
			GameEvents.onEditorShipModified.Remove(this.OnEditorShipModified);
			this.DoUnloadVessel();
		}

		#endregion

		#region My Internal Call Backs

		protected abstract void DoAwake();
		protected abstract void DoLoadVessel();
		protected abstract void DoStart();
		protected abstract void DoGoOnRails();
		protected abstract void DoGoOffRails();
		protected abstract void DoUnloadVessel();

		protected abstract void DoVesselChange(Vessel vessel);
		protected abstract void DoEditorShipModified(ShipConstruct shipContruct);

		#endregion

		private void OnVesselChange(Vessel vessel) => this.DoVesselChange(vessel);
		private void OnEditorShipModified(ShipConstruct shipContruct) => this.DoEditorShipModified(shipContruct);


		private String __ID = null;
		public String ID => __ID??(__ID = String.Format("{0}:{1:X}", this.name, this.vessel?.vesselName ?? "NOVESSEL", this.vessel.GetInstanceID()));
		protected readonly KSPe.Util.Log.Logger Log;
		protected abstract KSPe.Util.Log.Logger GetLogger();
		protected AbstractVesselModule() : base() => this.Log = this.GetLogger();
	}
}
