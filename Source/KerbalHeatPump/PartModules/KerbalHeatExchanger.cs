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

		[KSPField (isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Cabin Temperature")]
		public string cabinTempStatus = "";

		[KSPField( isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Heat EXCH THR", guiFormat = "P1")]
		[UI_FloatRange(scene = UI_Scene.All, minValue = 0.001f, maxValue = 1f, stepIncrement = 0.001f)]
		public float heatExchangeThresholdRatio = 1f;

		[KSPField (isPersistant = true)]
		protected double maxEnergyTransfer = 7500;

		private Controller vesselModule;
		private ResourceDef[] resources = new ResourceDef[0];

		#region KSP Life Cycle

		protected override void DoAwake()
		{
			this.hardActive = Globals.Instance.KerbalHeatPump;
			{
				BaseField field = Fields["heatExchangeThresholdRatio"];
				field.OnValueModified += this.OnHeatExchangeThresholdRatio;
				//UI_FloatRange range = (UI_FloatRange)field.uiControlEditor;
			}
			{
				BaseField field = Fields["cabinTempStatus"];
				field.guiActive = field.guiActiveEditor = HighLogic.LoadedSceneIsFlight;
			}
		}

		protected override void DoEditorPartEvent(ConstructionEventType eventType, Part part) { }
		protected override void DoWillBeCopied (bool asSymCounterpart) { }
		protected override void DoCopy(PartModule fromModule)
		{
			this.heatExchangeEnabled = (fromModule as KerbalHeatExchanger).heatExchangeEnabled;
			this.maxEnergyTransfer = (fromModule as KerbalHeatExchanger).maxEnergyTransfer;
			this.heatExchangeThresholdRatio = (fromModule as KerbalHeatExchanger).heatExchangeThresholdRatio;
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

		protected override void DoUpdate()
		{
			this.cabinTempStatus = Lib.UI.Format(this.part.temperature, 0, "°K");
		}

		protected override void DoFixedUpdate()
		{
			if (!this.heatExchangeEnabled) return;

			// Note: Everything on KSP is computed in kW

			Log.dbg("{0}:OnFixedUpdate vessel.atmosphericTemperature = {1} ; part.thermalMass = {2} ; part.temperature = {3}", this.ID, this.vessel.atmosphericTemperature, this.part.thermalMass, this.part.temperature);

			// pegar a temperatura da parte, multiplicar pela thermal mass.
			double energyCurrent =  this.part.thermalMass * this.part.temperature;
			double energyGoal = this.part.thermalMass * this.vessel.atmosphericTemperature; // This is a Heat Pump, not a HVAC! Wec can't excange more heat than available on the environment!

			double energyWeWantToSink = energyCurrent - energyGoal;
			Log.dbg("{0}:OnFixedUpdate energyWeWantToSink={1}", this.ID, energyWeWantToSink);
			if (energyWeWantToSink < Lib.Physics.CUTOFF)
			{	// Standby energy consuption.
				for (int i = 0; i < this.resources.Length; ++i)
				{
					ResourceDef r = this.resources[i];

					if (r.hspu < Lib.Physics.CUTOFF)
					{
						double demand = 0.01 * r.ratio * this.maxEnergyTransfer * TimeWarp.fixedDeltaTime; // 1% energy penalty.
						if (demand > 0)
						{ 
							double consumed = this.part.RequestResource(r.id, demand, r.def.resourceFlowMode);
							if (consumed < Lib.Physics.CUTOFF)
							{
								Log.dbg("{0}:OnFixedUpdate (standby) {1} NOT ENOUGH!: demand={2} ; consumed={3}", this.ID, r.name, demand, consumed);
								// Any already consumed resouces are lost.
								this.turnMeOffDueExhaustedResources(r.name);
								return;
							}
							Log.dbg("{0}:OnFixedUpdate (standby) {1}: demand={2} ; consumed={3}", this.ID, r.name, demand, consumed);
						}
						continue;
					}
				}
				return;
			}

			double maxEnergyTransfer = this.maxEnergyTransfer * this.heatExchangeThresholdRatio;
			double energyWeCanSink = Math.Min(energyWeWantToSink, maxEnergyTransfer);

			// Only the coolant in the part is accountable for thermal transfer!
			double energyAvailable = 0;
			for (int i = 0; i < this.resources.Length; ++i)
				energyAvailable += this.resources[i].hspuK * this.part.Resources.Get(resources[i].id).amount;
			if (energyAvailable < Lib.Physics.CUTOFF)
			{
				Log.dbg("{0}:OnFixedUpdate NOT ENOUGH OUT OF COOLANTS!", this.ID);
				// Any already consumed resouces are lost.
				this.heatExchangeEnabled = false;
				//Lib.UI.PostScreenWarning(Localizer.Format("#SOMETHING", this.vessel.vesselName, this.resources[i].name));
				Lib.UI.PostScreenWarning(string.Format("Vessel {0} run out of Coolants. Heat Exchanger is disabled!", this.vessel.vesselName));
				return;
			}

			// Now we need to cope with the event in which want to pump out more energy than we have in the budget (due lack of coolant)
			// We "salvage" the situation by stressing the cooland pumps to make them flow faster into the dissipator.
			// We calculate that onus by merely dividing the energy we want to ditch by the energy budget. The onus is a multipier
			// to be applied as some kind of penalty depending of the resource.
			// Note: both values are guaranteed not to be zero at this point.
			double resourceOnus = energyWeCanSink / energyAvailable;

			Log.dbg("{0}:OnFixedUpdate energyWeWantToSink={1} ; energyWeCanSink={2} ; energyAvailable={3} ; maxEnergyTransfer {4} ; resourceOnus = {5}", this.ID, energyWeWantToSink, energyWeCanSink, energyAvailable, maxEnergyTransfer, resourceOnus);

			// From this point, we want to work on the timeslice KSP is running.
			double energy = energyWeCanSink *= TimeWarp.fixedDeltaTime;
			maxEnergyTransfer *= TimeWarp.fixedDeltaTime;
			for (int i = 0; i < this.resources.Length; ++i)
			{
				ResourceDef r = this.resources[i];

				if (r.hspu < Lib.Physics.CUTOFF)
				{
					// https://www.desmos.com/calculator : y=a\log_{1.5}\left(x-h\right)+k
					double thisResourceOnus = Math.Max(1, 0.5 * (0.3 * Math.Log(resourceOnus - -4.06, 1.5) + -0.2));
					double demand = r.ratio * Math.Min(maxEnergyTransfer, energy) * thisResourceOnus;
					if (demand > Lib.Physics.CUTOFF)
					{ 
						double consumed = this.part.RequestResource(r.id, demand, r.def.resourceFlowMode);
						energy *= consumed/demand;
						double diff = consumed-demand;
						if (diff < -Lib.Physics.CUTOFF)
						{
							Log.dbg("{0}:OnFixedUpdate {1} NOT ENOUGH! demand={2} ; consumed={3} ; diff = {4} ; thisResourceOnus = {5}", this.ID, r.name, demand, consumed, diff, thisResourceOnus);
							// Any already consumed resouces are lost.
							this.turnMeOffDueExhaustedResources(r.name);
							return;
						}
						Log.dbg("{0}:OnFixedUpdate {1}: demand={2} ; consumed={3} ; diff = {4} ; energy = {5} ; thisResourceOnus = {6}", this.ID, r.name, demand, consumed, diff, energy, thisResourceOnus);
					}
					continue;
				}

				{
					double demand = r.ratio * (energyWeCanSink / maxEnergyTransfer);
					if (demand < Lib.Physics.CUTOFF) continue;
					double consumed = this.part.RequestResource(r.id, demand, r.def.resourceFlowMode);
					if (consumed < Lib.Physics.CUTOFF)
					{
						Log.dbg("{0}:OnFixedUpdate {1} NOT ENOUGH!: demand={2} ; consumed={3}", this.ID, r.name, demand, consumed);
						// Any already consumed resouces are lost.
						this.turnMeOffDueExhaustedResources(r.name);
						return;
					}
					energy *= (consumed/demand);
					Log.dbg("{0}:OnFixedUpdate {1}: demand={2} ; consumed={3} ; energy = {4} ; resourceOnus = {5}", this.ID, r.name, demand, consumed, energy, resourceOnus);
				}
			}

			double energySunk = this.vesselModule.PumpHeat(energy);
			if (double.IsNaN(energySunk))
			{
				this.turnMeOffDueNoSkinkers();
				Log.dbg("{0}:OnFixedUpdate {1}: No active Heat Sinks! Heat Exchanger is disabled!", this.ID);
				return;
			}
			this.part.thermalInternalFlux -= energySunk;
			Log.dbg("{0}:OnFixedUpdate energyToBeSunk={1} ; enegySunk={2} ; part.thermalInternalFlux = {3} ; part.temperature = {4}", this.ID, energy, energySunk, this.part.thermalInternalFlux, this.part.temperature);
		}

		private void OnHeatExchangeThresholdRatio(object value)
		{
			float v = (float)this.maxEnergyTransfer * (float)value;
			BaseField field = Fields["heatExchangeThresholdRatio"];
			field.guiName = string.Format("Heat EXCH THR: {0}", Lib.UI.Format(1000*v, 0, "W"));
		}

		#endregion

		private void turnMeOffDueExhaustedResources(string resName)
		{
			this.heatExchangeEnabled = false;
			//Lib.UI.PostScreenWarning(Localizer.Format("#SOMETHING", this.vessel.vesselName, this.resources[i].name));
			Lib.UI.PostScreenWarning(string.Format("Vessel {0} run out of {1}. Heat Exchanger is disabled!", this.vessel.vesselName, resName));
		}

		private void turnMeOffDueNoSkinkers()
		{
			this.heatExchangeEnabled = false;
			//Lib.UI.PostScreenError(Localizer.Format("#SOMETHING");
			Lib.UI.PostScreenWarning("No active Heat Sinks! Heat Exchanger is disabled!");
		}

		private static new readonly KSPe.Util.Log.Logger Log = KSPe.Util.Log.Logger.CreateForType<KerbalHeatExchanger>("L_Aerospace.Kerbal.HeatPump", "Exchanger", 0);
		protected override KSPe.Util.Log.Logger GetLogger() => Log;
	}
} } }