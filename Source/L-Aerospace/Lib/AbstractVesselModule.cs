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
	/*
		"Wunderbar"!! /s

		I can't make AbstractVesselModule... abstract... because someone, somewhere, some time ago decided it would be a good
		idea to pre instantiate everything descending from VesselModule without caring if the damned thing is instantiable or not!

		This is what you get from KSP.log if you try this stunt:

		[LOG 06:21:02.440] Can't add script behaviour . The script class can't be abstract!
		[EXC 06:21:02.440] NullReferenceException: Object reference not set to an instance of an object
			VesselModuleManager.AddModulesToVessel (.Vessel vessel, System.Collections.Generic.List`1 modules)
			Vessel.Awake ()
			UnityEngine.GameObject:AddComponent()
			ProtoVessel:Load(FlightState, Vessel)
			ProtoVessel:Load(FlightState)
			FlightState:Load()
			Game:Load()
			<Start>c__Iterator0:MoveNext()
			UnityEngine.SetupCoroutine:InvokeMoveNext(IEnumerator, IntPtr)

		Yeah, right.

		So now this "abstract" class is instantiable to prevent KSP from shooting his own feet. Crap.

		On the less dark side, at least the extending classes will not need to implement dummy methods for what they don't want to use.
	 */
	//public abstract class AbstractVesselModule : VesselModule
	public class AbstractVesselModule : VesselModule
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
			Log.dbg("{0}:OnLoadVessel {1}", this.ID, this.hardActive);
			base.OnLoadVessel();

			GameEvents.onEditorShipModified.Add(this.OnEditorShipModified);
			GameEvents.onVesselWasModified.Add(this.OnVesselWasModified);
			GameEvents.onVesselChange.Add(this.OnVesselChange);
			this.DoLoadVessel();
		}

		protected sealed override void OnStart()
		{
			Log.dbg("{0}:OnStart {1}", this.ID, this.hardActive);
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

			GameEvents.onVesselWasModified.Remove(this.OnVesselWasModified);
			GameEvents.onVesselChange.Remove(this.OnVesselChange);
			GameEvents.onEditorShipModified.Remove(this.OnEditorShipModified);
			this.DoUnloadVessel();
		}

		#endregion

		#region My Internal Call Backs

		protected virtual void DoAwake() { }
		protected virtual void DoLoadVessel() { }
		protected virtual void DoStart() { }
		protected virtual void DoGoOnRails() { }
		protected virtual void DoGoOffRails() { }
		protected virtual void DoUnloadVessel() { }

		protected virtual void DoEditorShipModified(ShipConstruct shipContruct) { }
		protected virtual void DoVesselWasModified(Vessel data) { }
		protected virtual void DoVesselChange(Vessel vessel) { }

		#endregion

		private void OnEditorShipModified(ShipConstruct shipContruct) => this.DoEditorShipModified(shipContruct);
		private void OnVesselWasModified(Vessel vessel) => this.DoVesselWasModified(vessel);
		private void OnVesselChange(Vessel vessel) => this.DoVesselChange(vessel);

		private string __ID = null;
		public string ID => __ID??(__ID = String.Format("{0}:{1:X}", this.name, this.vessel?.vesselName??"NOVESSEL", this.vessel.GetInstanceID()));
		protected readonly KSPe.Util.Log.Logger Log;
		protected virtual KSPe.Util.Log.Logger GetLogger() => new KSPe.Util.Log.DummyLogger();	// You **SHOULD** override this method with a logger of your own. Good look trying to figure it out if you forget it... :/
		protected AbstractVesselModule() : base() => this.Log = this.GetLogger();
	}
}
