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
	public class KerbalHeatDissipator : PartModule
	{
		[KSPField(isPersistant = true)]
		private bool active = false;
		public bool Active
		{
			get => this.active && this.isEnabled;
			internal set
			{
				this.enabled = this.isEnabled = value;
			}
		}

		[UI_Toggle (disabledText = "#autoLOC_900890", scene = UI_Scene.All, enabledText = "#autoLOC_900889", affectSymCounterparts = UI_Scene.All)]
		[KSPField (isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Heat Exchange")]
		public bool heatExchangeEnabled = false;

		[KSPField( isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Cooland Usage Threshold", guiFormat = "P0")]
		[UI_FloatRange(scene = UI_Scene.All, minValue = 0.00f, maxValue = 1f, stepIncrement = 0.01f)]
		public float coollantThresholdRatio = 1f;

		[KSPField (isPersistant = true)]
		protected double maxEnergyTransfer = 7500;

		private Controller vesselModule;
		private ResourceDef[] resources = new ResourceDef[0];
		private Dictionary<ResourceDef,ModuleResourceIntake[]> intakes = new Dictionary<ResourceDef, ModuleResourceIntake[]>();

		#region KSP Life Cycle

		public override void OnAwake()
		{
			Log.dbg("{0}:OnAwake", this.ID);
			base.OnAwake();
		}

		public override void OnCopy(PartModule fromModule)
		{
			Log.dbg("{0}:OnCopy from {1:X}", this.ID, fromModule.part.GetInstanceID());
			base.OnCopy(fromModule);
			this.heatExchangeEnabled = (fromModule as KerbalHeatDissipator).heatExchangeEnabled;
			this.maxEnergyTransfer = (fromModule as KerbalHeatDissipator).maxEnergyTransfer;
			this.resources = (fromModule as KerbalHeatDissipator).resources;
		}

		public override void OnLoad(ConfigNode node)
		{
			Log.dbg("{0}:OnLoad {1}", this.ID, null != node);
			base.OnLoad(node);
			this.active &= Globals.Instance.KerbalHeatPump;

			if (null == this.part.partInfo) return;

			{ 
				this.resources = ResourceDef.readList(this.part.partInfo.partConfig, this.GetType().Name).ToArray();
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

			this.Active = 0 != this.resources.Length;

			{
				int count = 0;
				List<ModuleResourceIntake> all = this.part.FindModulesImplementing<ModuleResourceIntake>();
				for (int i = 0; i < this.resources.Length; ++i)
				{
					ResourceDef r = this.resources[i];
					string resourceName = r.name;
					List<ModuleResourceIntake> intakes = new List<ModuleResourceIntake>(this.part.Modules.Count);
					for (int j = 0; j < all.Count; ++j)
						if (resourceName.Equals(all[j].resourceName))
							intakes.Add(all[j]);
					if (intakes.Count > 0)
					{
						ModuleResourceIntake[] intakeArray = intakes.ToArray();
						this.intakes[r] = intakeArray;
						count += intakeArray.Length;
					}
				}

				if (0 == count)
					Log.warn("{0}:OnLoad No Resource Intakes found! Deactivating myself...", this.ID);
				else
					Log.dbg("{0}:OnLoad Found {1} Resource Intakes", this.ID, count);

				this.Active &= 0 != count;
			}
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
			this.Active &= state > StartState.Editor;
		}

		public override void OnInitialize()
		{
			Log.dbg("{0}:OnInitialize {1}", this.ID, this.Active);
			base.OnInitialize();
			if (
					this.Active
					&& !this.IsStageable()	// Rationale: stageable parts should obey the stage rules,
											// but we also need the "Active" life cycle nevertheless - so we force the activation
											// only if the part is not stageable.
				)
				this.part.force_activate(); // This will activate the OnFixedUpdate
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

		private string _getInfo = null;
		public override string GetInfo()
		{
			if (!Globals.Instance.KerbalHeatPump) return "Disabled.";
			if (null == this._getInfo)
			{
				this._getInfo = string.Format(
							"Max Energy Transfer : {0}kW"
						, this.maxEnergyTransfer
					);
				if (this.resources.Length > 0)
				{
					this._getInfo += "\n<b>Consumables</b>";
					for (int i = 0; i < this.resources.Length; ++i)
					{
						if (this.resources[i].ratio > 0)
							this._getInfo += string.Format(
									"\n\t{0} : {1} {2}"
								, this.resources[i].name
								, this.resources[i].ratio*this.resources[i].def.density*1000
								, "kG/J" 
							);
						if (this.resources[i].hsp > 0)
							this._getInfo += string.Format(
									"\n\t{0} : {1} {2}"
								, this.resources[i].name
								, this.resources[i].hsp
								, "J/(kG°K)" 
							);
					}
				}
			}
			return this._getInfo;
		}

		public override void OnFixedUpdate()
		{
			base.OnFixedUpdate();
			if (!(this.Active && this.heatExchangeEnabled)) return;

			// Pegar temperatura do ambiente. Essa eh temperatura do resource being scoped.
			// multiplicar pela thermalmass para saber qual o bottom line que a parte pode chegar
			double intakeResourceTemp = this.vessel.atmosphericTemperature;
			double energyGoal = this.part.thermalMass * intakeResourceTemp;

			// pegar a temperatura interna da parte, multiplicar pela thermal mass.
			double energyCurrent = this.part.thermalMass * this.part.temperature;

			// subtrair os dois, essa eh a energia que queremos remover do circuito
			double energyToDissipate = energyCurrent - energyGoal;
			double maxEnergyToDissipate = Math.Min(this.maxEnergyTransfer, energyToDissipate);

			Log.dbg("{0}:OnFixedUpdate maxEnergyToDissipate={1} ; energyToDissipate={2}", this.ID, maxEnergyToDissipate, energyToDissipate);
			if (maxEnergyToDissipate < 1) return;

			double maxEnergyTransfer = this.maxEnergyTransfer * TimeWarp.fixedDeltaTime;
			energyToDissipate *= TimeWarp.fixedDeltaTime;

			Log.dbg("{0}:OnFixedUpdate energyGoal = {1} ; energyCurrent = {2} ; maxEnergyToDissipate = {3} ; energyToDissipate = {4}", this.ID, energyGoal, energyCurrent, maxEnergyToDissipate, energyToDissipate);

			for (int i = 0; i < this.resources.Length; ++i)
			{
				ResourceDef r = this.resources[i];

				// Only the coolant in the part is accountable for thermal transfer! Heat Dissipators don't work remotely! :)
				double energy = 0;
				if (this.intakes.ContainsKey(r))
				{
					energy = 0;
					ModuleResourceIntake[] l = this.intakes[r];
					for (int j = 0 ; j < l.Length ; ++j) if (l[j].intakeEnabled)
						energy += l[j].airFlow * l[j].intakeSpeed * r.hspu;
				}
				else
					energy = this.part.Resources.Get(r.id).amount * r.hspu * this.coollantThresholdRatio;

				energy = Math.Min(energyToDissipate, energy * TimeWarp.fixedDeltaTime);

				Log.dbg("{0}:OnFixedUpdate {1} energyToDissipate={2} ; energy={3}", this.ID, r.name, energyToDissipate, energy);

				double demand = energy * r.ratio * energyToDissipate / maxEnergyTransfer;
				if (demand > Lib.Physics.CUTOFF)
				{
					double consumed = this.part.RequestResource(r.id, demand, r.def.resourceFlowMode);
					energy *= (consumed/demand);
					Log.dbg("{0}:OnFixedUpdate {1} demand={2} ; consumed={3} ; energy = {4} ; energyToDissipate = {5}", this.ID, r.name, demand, consumed, energy, energyToDissipate);
				}
				this.part.thermalInternalFlux -= energy;
				energyToDissipate = Math.Max(0, energyToDissipate - energy);
				Log.dbg("{0}:OnFixedUpdate part.temperature={1} ; part.thermalInternalFlux={2} ; energy={3} ; (left)energyToDissipate={4}", this.ID, this.part.temperature, this.part.thermalInternalFlux, energy, energyToDissipate);
				if (energyToDissipate < Lib.Physics.CUTOFF) break;
			}
			Log.dbg("{0}:OnFixedUpdate enegyNotDissipated={1} ; this.part.thermalInternalFlux = {2} ; this.part.temperature = {3} ; intakeResourceTemp = {4}", this.ID, energyToDissipate, this.part.thermalInternalFlux, this.part.temperature, intakeResourceTemp);
		}

		private void OnThresholdRatioChanged(object value)
		{
			float v = (float)this.part.maxTemp * (float)value;
			BaseField field = Fields["thresholdRatio"];
			field.guiName = string.Format("Heat EXCH THR: {0}", L_Aerospace.Lib.UI.Format(v, 0, "°K"));
			this._getInfo = null; // Forces GetInfo to be regenerated.
		}

		#endregion

		private String __ID = null;
		public String ID => __ID??(__ID = String.Format("{0}:{1:X}", this.name, this.part.GetInstanceID()));
		private static readonly KSPe.Util.Log.Logger Log = KSPe.Util.Log.Logger.CreateForType<KerbalHeatDissipator>("L_Aerospace.Kerbal.HeatPump", "Dissipator", 0);
	}
} } }