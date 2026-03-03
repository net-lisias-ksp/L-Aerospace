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

namespace L_Aerospace { namespace Kerbal { namespace HeatPump
{
	public class KerbalHeatDissipatorSuperCharger : L_Aerospace.Lib.AbstractPartModuleAutoActivable
	{
		public bool Ready => null != this.targetModule && this.superChargerEnabled && this.Active;

		[KSPField (isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Super Charger")]
		[UI_Toggle (disabledText = "#autoLOC_900890", scene = UI_Scene.All, enabledText = "#autoLOC_900889", affectSymCounterparts = UI_Scene.All)]
		public bool superChargerEnabled = false;

		[KSPField (isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Super Charger")]
		public string status = "status";

		private Controller vesselModule;
		private KerbalHeatDissipator targetModule;
		private ResourceDef[] resources = new ResourceDef[0];
		private Dictionary<ResourceDef,ModuleResourceIntake[]> intakes = new Dictionary<ResourceDef, ModuleResourceIntake[]>();

		// This crap only works because PartModules are called sequentially, without concurrency. Otherwise we would need a mutex
		// to protected this dude!
		private double availableEnergy = 0;

		#region KSP Life Cycle

		protected override void DoAwake()
		{
			this.hardActive = Globals.Instance.KerbalHeatPump;
		}

		protected override void DoEditorPartEvent(ConstructionEventType eventType, Part part)
		{
			if (ConstructionEventType.PartAttached == eventType) this.setupMe();
		}
		protected override void DoWillBeCopied (bool asSymCounterpart) { }
		protected override void DoCopy(PartModule fromModule)
		{
			this.superChargerEnabled = (fromModule as KerbalHeatDissipatorSuperCharger).superChargerEnabled;
			this.resources = (fromModule as KerbalHeatDissipatorSuperCharger).resources;
		}
		protected override void DoWasCopied(PartModule fromModule, bool asSymCounterpart)
		{
			// These guys are tied to their original Part, I can't just copy them to this one!
			this.targetModule = null;
			this.vesselModule?.Announce(this);
		}

		protected override void DoSave(KSPe.ConfigNodeWithSteroids node) { }
		protected override void DoPrefabLoad(KSPe.ConfigNodeWithSteroids node) { }
		protected override void DoLoad(KSPe.ConfigNodeWithSteroids node){ }

		protected override void DoStart(StartState state) { }
		protected override void DoStartFinished(StartState state)
		{
			this.vesselModule = Controller.GetVesselModule(this); // this don't work on DoStart??
			this.vesselModule?.Announce(this); // VesselModule is not available on Editor!
			this.setupMe();
			this.Active = false;
			Log.dbg("{0}:DoStartFinished.middle {1} {2} {3}", this.ID, state, null != this.vesselModule, null != this.targetModule);
		}

		protected override string DoGetInfo()
		{
			string r = "";
			if (this.resources.Length > 0)
			{
				r += "<b>Consumables</b>";
				for (int i = 0; i < this.resources.Length; ++i)
				{
					if (this.resources[i].ratio > 0)
						r += string.Format(
								"\n\t{0} : {1} {2}"
							, this.resources[i].name
							, this.resources[i].ratio*this.resources[i].def.density*1000
							, "kG/J" 
						);
					if (this.resources[i].hsp > 0)
						r += string.Format(
								"\n\t{0} : {1} {2}"
							, this.resources[i].name
							, this.resources[i].hsp
							, "J/(kG°K)" 
						);
				}
			}
			return r;
		}

		protected override void DoUpdate() { }

		protected override void DoFixedUpdate()
		{
			this.availableEnergy = 0;
			if (!this.Ready) return;

			Log.dbg("{0}:DoFixedUpate {1} {2}", this.ID, null != this.part, null != this.part.Resources);

			for (int i = 0; i < this.resources.Length; ++i)
			{
				ResourceDef r = this.resources[i];

				if (r.hspu < Lib.Physics.CUTOFF)
				{
					double demand = r.ratio * this.availableEnergy;
					if (demand > Lib.Physics.CUTOFF)
					{
						double consumed = this.part.RequestResource(r.id, demand, r.def.resourceFlowMode);
						this.availableEnergy *= consumed/demand;
					}
					continue;
				}

				// Only the coolant in the part is accountable for thermal transfer! Heat Dissipators don't work remotely! :)
				double energy = this.part.Resources.Get(r.id).amount * r.hspu;
				if (this.intakes.ContainsKey(r))
				{
					ModuleResourceIntake[] l = this.intakes[r];
					for (int j = 0 ; j < l.Length ; ++j) if (l[j].intakeEnabled)
						// Heat Conductivity increases 9.8% each 5m/s for Air.
						// TODO: Parametrize this for different mediums, like atmos from other planets and water!
						energy *= Lib.Math.GeometricProgression(l[j].airFlow * l[j].intakeSpeed, 1.098, (int)(l[j].intakeSpeed / 5));
				}

				energy *= TimeWarp.fixedDeltaTime;
				Log.dbg("{0}:DoFixedUpate {1} {2}", this.ID, r.name, energy);

				{
					double demand = r.ratio * energy;
					if (demand > Lib.Physics.CUTOFF)
					{
						double consumed = this.part.RequestResource(r.id, demand, r.def.resourceFlowMode);
						energy *= (consumed/demand);
						Log.dbg("{0}:OnFixedUpdate {1} demand={2} ; consumed={3} ; energy = {4}", this.ID, r.name, demand, consumed, energy);
					}
				}
				this.availableEnergy += energy;
				Log.dbg("{0}:DoFixedUpate {1} {2} {3}", this.ID, r.name, energy, this.availableEnergy);
			}
			Log.dbg("{0}:OnFixedUpdate availableEnergy={1}", this.ID, this.availableEnergy);
		}

		#endregion

		internal void Announce(KerbalHeatDissipator kerbalHeatDissipator)
		{
			Log.dbg("{0}:Announce.in by {1}", this.ID, kerbalHeatDissipator.ID);

			this.targetModule = kerbalHeatDissipator;
			this.resources = Lib.Part.buildResourceList(this, this.targetModule.MaxEnergyTransfer);
			this.intakes = Lib.Part.buildIntakeList(this, this.resources);

			this.Active = 0 != this.resources.Length
							&& 0 != this.intakes.Count
					;

			this.setupMe();
			Log.dbg("{0}:Announce.out Active = {1}", this.ID, this.Active);
		}

		internal double WithdrawEnergy(double energy)
		{
			Log.dbg("{0}:WithdrawEnergy {1} {2}", this.ID, this.availableEnergy, energy);
			double r = Math.Min(this.availableEnergy, Math.Max(0, energy));
			this.availableEnergy -= r;
			return r;
		}

		private void setupMe()
		{
			{
				BaseField field = Fields["superChargerEnabled"];
				field.guiActive = field.guiActiveEditor = null != this.targetModule;
			}
			{
				BaseField field = Fields["status"];
				field.guiActive = field.guiActiveEditor = null == this.targetModule;
				this.status = "No Heat Dissipator found!";
				if (HighLogic.LoadedSceneIsEditor)
				{
					Part p = Lib.Part.LookForTarget(this);
					this.status = null == p
								? "No Heat Dissipator found!"
								: "Not available on Editor"
							;
				}
			}
		}

		private static new readonly KSPe.Util.Log.Logger Log = KSPe.Util.Log.Logger.CreateForType<KerbalHeatDissipatorSuperCharger>("L_Aerospace.Kerbal.HeatPump", "DissipatorSuperCharger", 0);
		protected override KSPe.Util.Log.Logger GetLogger() => Log;
	}
} } }