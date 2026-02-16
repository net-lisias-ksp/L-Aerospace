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
	public class KerbalHeatExchanger : PartModule
	{
		[UI_Toggle (disabledText = "#autoLOC_900890", scene = UI_Scene.All, enabledText = "#autoLOC_900889", affectSymCounterparts = UI_Scene.All)]
		[KSPField (isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Heat Exchange")]
		public bool heatExchangeEnabled = false;

		[KSPField (isPersistant = true)]
		protected double maxEnergyTransfer = 7500;

		[KSPField( isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Heat Exchange Threshold", guiFormat = "P0")]
		[UI_FloatRange(scene = UI_Scene.All, minValue = 0.01f, maxValue = 1f, stepIncrement = 0.01f)]
		public float thresholdRatio = 0.5f;

		private Controller vesselModule;
		private ResourceDef[] resources = new ResourceDef[0];

		public bool Active
		{
			get => this.isEnabled;
			private set
			{
				this.enabled = this.isEnabled = value;
			}
		}

		#region KSP Life Cycle

		public override void OnAwake()
		{
			Log.dbg("OnAwake {0}", this.ID);
			base.OnAwake();
			{
				BaseField field = Fields["thresholdRatio"];
				field.OnValueModified += this.OnThresholdRatioChanged;
				//UI_FloatRange range = (UI_FloatRange)field.uiControlEditor;
			}
		}

		public override void OnCopy(PartModule fromModule)
		{
			Log.dbg("OnCopy {0} from {1:X}", this.ID, fromModule.part.GetInstanceID());
			base.OnCopy(fromModule);
			this.heatExchangeEnabled = (fromModule as KerbalHeatExchanger).heatExchangeEnabled;
			this.maxEnergyTransfer = (fromModule as KerbalHeatExchanger).maxEnergyTransfer;
			this.thresholdRatio = (fromModule as KerbalHeatExchanger).thresholdRatio;
			this.resources = (fromModule as KerbalHeatExchanger).resources;
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
			Log.dbg("OnLoad {0} {1}", this.ID, null != node);

			if (null == this.part.partInfo) return;
			this.resources = ResourceDef.readList(this.part.partInfo.partConfig, this.GetType().Name, this.ID).ToArray();
			if (0 == this.resources.Length)
				Log.warn("{0}:OnLoad No Resources found! Deactivating myself...", this.ID);
			else
				Log.dbg("{0}:OnLoad Found {1} Resources", this.ID, this.resources.Length);
			this.Active = 0 != this.resources.Length;
		}

		public override void OnSave(ConfigNode node)
		{
			Log.dbg("{0}:OnSave {1}", this.ID, null != node);
			base.OnSave(node);
		}

		public override void OnStart(StartState state)
		{
			Log.dbg("{0}:OnStart {1} {2}", this.ID, state, this.enabled);
			base.OnStart(state);
			this.Active &= StartState.Editor != state;
		}

		public override void OnInitialize()
		{
			Log.dbg("{0}:OnInitialize {1} {1}", this.ID, this.enabled, this.isActiveAndEnabled);
			base.OnInitialize();
			if (
					this.Active
					&& !this.IsStageable()	// Rationale: stageable parts should obey the stage rules,
											// but we also need the "Active" life cycle nevertheless - so we force the activation
											// only if the part is not stageable.
				)
				this.part.force_activate(); // This will activate the OnFixedUpdate
			this.OnThresholdRatioChanged(this.thresholdRatio);
			this.vesselModule = Controller.GetModule(this.vessel);
		}

		public override void OnActive()
		{
			Log.dbg("{0}:OnActive", this.ID);
			base.OnActive();
		}

		// Needed because I had overriden OnActive.
		// See https://kerbalspaceprogram.com/api/class_part_module.html#a6f2dd76038326c527e64d2ce96bb45fe
		public override bool IsStageable() => false;

		public override void OnInactive()
		{
			Log.dbg("{0}:OnInactive", this.ID);
			base.OnInactive();
		}

		public override void OnFixedUpdate()
		{
			base.OnFixedUpdate();
			if (!this.heatExchangeEnabled) return;

			double maxTempToSink = this.part.temperature - (this.part.maxTemp * ((double)this.thresholdRatio));
			Log.dbg("{0}:OnFixedUpdate maxTempToSink={1}", this.ID, maxTempToSink);
			if (maxTempToSink < 1) return;

			double totalEnergy = this.part.thermalMass * maxTempToSink;
			double energyToSink = Math.Min(totalEnergy, this.maxEnergyTransfer) * TimeWarp.fixedDeltaTime;
			Log.dbg("{0}:OnFixedUpdate totalEnergy={1} ; energyToSink={2}", this.ID, totalEnergy, energyToSink);

			for (int i = 0; i < this.resources.Length; ++i)
			{
				double toConsume = energyToSink * this.resources[i].rate ;
				double consumed = this.part.RequestResource(this.resources[i].id, toConsume, ResourceFlowMode.ALL_VESSEL);
				if ((toConsume - consumed) > 0.001)
				{
					Log.dbg("{0}:OnFixedUpdate {1} NOT ENOUGH!: toConsume={2} ; consumed={3}", this.ID, this.resources[i].name, toConsume, consumed);
					// Any already consumed resouces are lost.
					this.heatExchangeEnabled = false;
					//Lib.UI.PostScreenWarning(Localizer.Format("#SOMETHING", this.vessel.vesselName, this.resources[i].name));
					Lib.UI.PostScreenWarning(string.Format("Vessel {0} run out of {1}. Heat Exchanger is disabled!", this.vessel.vesselName, this.resources[i].name));
					return;
				}
				double ratio = consumed / toConsume;
				energyToSink *= ratio;
				Log.dbg("{0}:OnFixedUpdate {1}: toConsume={2} ; consumed={3} ; ratio = {4}", this.ID, this.resources[i].name, toConsume, consumed, ratio);
			}

			double energySunk = this.vesselModule.PumpHeat(energyToSink);
			if (energySunk < 1)
			{
				this.heatExchangeEnabled = false;
				//Lib.UI.PostScreenError(Localizer.Format("#SOMETHING");
				Lib.UI.PostScreenWarning("No active Heat Sinks! Heat Exchanger is disabled!");
			}
			this.part.thermalInternalFlux -= energySunk;
			Log.dbg("{0}:OnFixedUpdate enegySunk={1} ; this.part.temperature = {2}", this.ID, energySunk, this.part.temperature);
		}

		private void OnThresholdRatioChanged(object value)
		{
			float v = (float)this.part.maxTemp * (float)value;
			BaseField field = Fields["thresholdRatio"];
			field.guiName = string.Format("Heat EXCH THR: {0}", Lib.UI.Format(v, 0, "°K"));
			this._getInfo = null; // Forces GetInfo to be regenerated.
		}

		#endregion

		private String __ID = null;
		public String ID => __ID??(__ID = String.Format("{0}:{1:X}", this.name, this.part.GetInstanceID()));
		private static readonly KSPe.Util.Log.Logger Log = KSPe.Util.Log.Logger.CreateForType<KerbalHeatExchanger>("L_Aerospace.Kerbal.HeatPump", "Exchange", 0);
	}
} } }