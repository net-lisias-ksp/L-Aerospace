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

namespace L_Aerospace { namespace Kerbal { namespace HeatPump
{
	public class KerbalHeatExchanger : L_Aerospace.Lib.AbstractPartModuleAutoActivable
	{
		[KSPField (isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Heat Exchange")]
		[UI_Toggle (disabledText = "#autoLOC_900890", scene = UI_Scene.All, enabledText = "#autoLOC_900889", affectSymCounterparts = UI_Scene.All)]
		public bool heatExchangeEnabled = false;

		[KSPField( isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Cooland Usage Threshold", guiFormat = "P0")]
		[UI_FloatRange(scene = UI_Scene.All, minValue = 0.00f, maxValue = 1f, stepIncrement = 0.01f)]
		public float coollantThresholdRatio = 1f;

		[KSPField (isPersistant = true)]
		protected double maxEnergyTransfer = 7500;

		private Controller vesselModule;
		private ResourceDef[] resources = new ResourceDef[0];

		#region KSP Life Cycle

		protected override void DoAwake()
		{
			this.hardActive = Globals.Instance.KerbalHeatPump;
		}

		protected override void DoEditorPartEvent(ConstructionEventType eventType, Part part) { }
		protected override void DoWillBeCopied (bool asSymCounterpart) { }
		protected override void DoCopy(PartModule fromModule)
		{
			this.heatExchangeEnabled = (fromModule as KerbalHeatExchanger).heatExchangeEnabled;
			this.maxEnergyTransfer = (fromModule as KerbalHeatExchanger).maxEnergyTransfer;
			this.coollantThresholdRatio = (fromModule as KerbalHeatExchanger).coollantThresholdRatio;
			this.resources = (fromModule as KerbalHeatExchanger).resources;
		}
		protected override void DoWasCopied(PartModule fromModule, bool asSymCounterpart) { }

		protected override void DoSave(KSPe.ConfigNodeWithSteroids node) { }
		protected override void DoPrefabLoad(KSPe.ConfigNodeWithSteroids node) { }
		protected override void DoLoad(KSPe.ConfigNodeWithSteroids node)
		{
			this.resources = ResourceDef.readList(this.maxEnergyTransfer, this.part.partInfo.partConfig, this.GetType().Name).ToArray();
			if (0 == this.resources.Length)
				Log.warn("{0}:OnLoad No Resources found! Deactivating myself...", this.ID);
			else
				Log.dbg("{0}:OnLoad Found {1} Resources", this.ID, this.resources.Length);
#if DEBUG
			{ 
				for (int i = 0; i < this.resources.Length; ++i)
					Log.dbg("{0}:OnLoad {1}", this.ID, this.resources[i]);
			}
#endif
		}

		protected override void DoStart(StartState state) { }

		protected override void DoStartFinished(StartState state)
		{
			this.vesselModule = Controller.GetVesselModule(this);  // this don't work on DoStart??
			this.Active = 0 != this.resources.Length;
			this.vesselModule?.Announce(this); // VesselModule is not available on Editor!
		}

		protected override string DoGetInfo()
		{
			string r = string.Format(
						"Max Energy Transfer : {0}kW"
					, this.maxEnergyTransfer
				);
			if (this.resources.Length > 0)
			{
				r += "\n<b>Consumables</b>";
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
			if (!this.heatExchangeEnabled) return;

			// pegar a temperatura da parte, multiplicar pela thermal mass.
			double energyCurrent =  this.part.thermalMass * this.part.temperature;
			double energyGoal = this.part.thermalMass * this.vessel.atmosphericTemperature; // This is a Heat Pump, not a HVAC! Wec can't excange more heat than available on the environment!

			double energyWeWantToSink = energyCurrent - energyGoal;
			Log.dbg("{0}:OnFixedUpdate energyWeWantToSink={1}", this.ID, energyWeWantToSink);
			if (energyWeWantToSink < 1) return;

			double energyWeCanSink = Math.Min(energyWeWantToSink, this.maxEnergyTransfer);

			// Only the coolant in the part is accountable for thermal transfer!
			double energyAvailable = 0;
			for (int i = 0; i < this.resources.Length; ++i)
				energyAvailable += this.resources[i].hspu * this.part.Resources.Get(resources[i].id).amount;
			energyAvailable *= TimeWarp.fixedDeltaTime;
			if (energyAvailable < Lib.Physics.CUTOFF)
			{
				Log.dbg("{0}:OnFixedUpdate NOT ENOUGH OUT OF COOLANTS!", this.ID);
				// Any already consumed resouces are lost.
				this.heatExchangeEnabled = false;
				//Lib.UI.PostScreenWarning(Localizer.Format("#SOMETHING", this.vessel.vesselName, this.resources[i].name));
				Lib.UI.PostScreenWarning(string.Format("Vessel {0} run out of Coolants. Heat Exchanger is disabled!", this.vessel.vesselName));
				return;
			}

			Log.dbg("{0}:OnFixedUpdate energyWeWantToSink={1} ; energyWeCanSink={2} ; energyAvailable={3} ; maxEnergyTransfer {4}", this.ID, energyWeWantToSink, energyWeCanSink, energyAvailable, this.maxEnergyTransfer);

			double energy = Math.Min(energyWeCanSink, energyAvailable) * TimeWarp.fixedDeltaTime;
			for (int i = 0; i < this.resources.Length; ++i)
			{
				ResourceDef r = this.resources[i];

				double demand = r.ratio * (energyWeCanSink / this.maxEnergyTransfer) * this.coollantThresholdRatio;
				if (demand < Lib.Physics.CUTOFF) continue;
				double consumed = this.part.RequestResource(r.id, demand, r.def.resourceFlowMode);
				if (consumed < Lib.Physics.CUTOFF && demand > Lib.Physics.CUTOFF)
				{
					Log.dbg("{0}:OnFixedUpdate {1} NOT ENOUGH!: demand={2} ; consumed={3}", this.ID, r.name, demand, consumed);
					// Any already consumed resouces are lost.
					this.heatExchangeEnabled = false;
					//Lib.UI.PostScreenWarning(Localizer.Format("#SOMETHING", this.vessel.vesselName, this.resources[i].name));
					Lib.UI.PostScreenWarning(string.Format("Vessel {0} run out of {1}. Heat Exchanger is disabled!", this.vessel.vesselName, r.name));
					return;
				}
				energy *= (consumed/demand);
				Log.dbg("{0}:OnFixedUpdate {1}: demand={2} ; consumed={3} ; energy = {4}", this.ID, r.name, demand, consumed, energy);
			}

			double energySunk = this.vesselModule.PumpHeat(energy);
			if (double.IsNaN(energySunk))
			{
				this.heatExchangeEnabled = false;
				//Lib.UI.PostScreenError(Localizer.Format("#SOMETHING");
				Lib.UI.PostScreenWarning("No active Heat Sinks! Heat Exchanger is disabled!");
				Log.dbg("{0}:OnFixedUpdate {1}: No active Heat Sinks! Heat Exchanger is disabled!", this.ID);
				return;
			}
			this.part.thermalInternalFlux -= energySunk;
			Log.dbg("{0}:OnFixedUpdate energyToBeSunk={1} ; enegySunk={2} ; part.thermalInternalFlux = {3} ; part.temperature = {4}", this.ID, energy, energySunk, this.part.thermalInternalFlux, this.part.temperature);
		}

		#endregion

		private static new readonly KSPe.Util.Log.Logger Log = KSPe.Util.Log.Logger.CreateForType<KerbalHeatExchanger>("L_Aerospace.Kerbal.HeatPump", "Exchanger", 0);
		protected override KSPe.Util.Log.Logger GetLogger() => Log;
	}
} } }