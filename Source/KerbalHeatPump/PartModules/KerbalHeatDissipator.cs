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
	public class KerbalHeatDissipator : L_Aerospace.Lib.AbstractPartModuleAutoActivable
	{
		public bool Ready => null != this.vesselModule && this.Active;

		[KSPField (isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Heat Exchange")]
		[UI_Toggle (disabledText = "#autoLOC_900890", scene = UI_Scene.All, enabledText = "#autoLOC_900889", affectSymCounterparts = UI_Scene.All)]
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

		protected override void DoAwake()
		{
			this.hardActive = Globals.Instance.KerbalHeatPump;
			{
				BaseField field = Fields["maxEnergyTransfer"];
				field.OnValueModified += this.OnMaxEnergyTransfer;
				//UI_FloatRange range = (UI_FloatRange)field.uiControlEditor;
			}
		}

		protected override void DoCopy(PartModule fromModule)
		{
			this.heatExchangeEnabled = (fromModule as KerbalHeatDissipator).heatExchangeEnabled;
			this.maxEnergyTransfer = (fromModule as KerbalHeatDissipator).maxEnergyTransfer;
			this.resources = (fromModule as KerbalHeatDissipator).resources;

			// These guys are tied to their original Part, I can't just copy them to this one!
			this.intakes = Lib.Part.buildIntakeList(this, this.resources);
		}

		protected override void DoSave(KSPe.ConfigNodeWithSteroids node) { }
		protected override void DoPrefabLoad(KSPe.ConfigNodeWithSteroids node) { }
		protected override void DoLoad(KSPe.ConfigNodeWithSteroids node)
		{
			this.resources = Lib.Part.buildResourceList(this, this.maxEnergyTransfer);
			this.intakes = Lib.Part.buildIntakeList(this, this.resources);
		}

		protected override void DoStart(StartState state)
		{
			this.vesselModule = Controller.GetModule(this.vessel);
			this.Active = 0 != this.resources.Length;
			this.Active &= 0 != this.intakes.Count;
			if (this.Ready) this.vesselModule.Announce(this);
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

			// Pegar temperatura do ambiente. Essa eh temperatura do resource being scoped.
			// multiplicar pela thermalmass para saber qual o bottom line que a parte pode chegar
			double intakeResourceTemp = this.vessel.atmosphericTemperature;
			double energyGoal = this.part.thermalMass * intakeResourceTemp;

			// pegar a temperatura interna da parte, multiplicar pela thermal mass.
			double energyCurrent = this.part.thermalMass * this.part.temperature;

			// subtrair os dois, essa eh a energia que queremos remover do circuito
			double energyWeWantToDissipate = energyCurrent - energyGoal;
			double energyWeCanDissipate = Math.Min(this.maxEnergyTransfer, energyWeWantToDissipate);

			Log.dbg("{0}:OnFixedUpdate energyWeCanDissipate={1} ; energyWeWantToDissipate={2}", this.ID, energyWeCanDissipate, energyWeWantToDissipate);
			if (energyWeCanDissipate < 1) return;

			Log.dbg("{0}:OnFixedUpdate energyGoal = {1} ; energyCurrent = {2} ; energyWeCanDissipate = {3} ; energyWeWantToDissipate = {4}", this.ID, energyGoal, energyCurrent, energyWeCanDissipate, energyWeWantToDissipate);

			energyWeWantToDissipate *= TimeWarp.fixedDeltaTime;
			double maxEnergyTransfer = this.maxEnergyTransfer * TimeWarp.fixedDeltaTime;
			for (int i = 0; i < this.resources.Length; ++i)
			{
				ResourceDef r = this.resources[i];

				// Only the coolant in the part is accountable for thermal transfer! Heat Dissipators don't work remotely! :)
				double energy;
				double coollantThresholdRatio = this.coollantThresholdRatio;
				if (this.intakes.ContainsKey(r))
				{
					energy = 0;
					coollantThresholdRatio = 1.0;
					ModuleResourceIntake[] l = this.intakes[r];
					for (int j = 0 ; j < l.Length ; ++j) if (l[j].intakeEnabled)
						energy += l[j].airFlow * l[j].intakeSpeed * r.hspu;
				}
				else
				{
					energy = this.part.Resources.Get(r.id).amount * r.hspu;
					coollantThresholdRatio = this.coollantThresholdRatio;
				}

				energy *= TimeWarp.fixedDeltaTime;
				energy = Math.Min(energyWeWantToDissipate, energy);

				Log.dbg("{0}:OnFixedUpdate {1} energyWeWantToDissipate={2} ; energy={3} ; maxEnergyTransfer={4}", this.ID, r.name, energyWeWantToDissipate, energy, maxEnergyTransfer);

				double demand = r.ratio * Math.Min(coollantThresholdRatio, energyWeWantToDissipate / maxEnergyTransfer);
				energy *= TimeWarp.fixedDeltaTime;
				if (demand > Lib.Physics.CUTOFF)
				{
					double consumed = this.part.RequestResource(r.id, demand, r.def.resourceFlowMode);
					energy *= (consumed/demand);
					Log.dbg("{0}:OnFixedUpdate {1} demand={2} ; consumed={3} ; energy = {4} ; energyToDissipate = {5}", this.ID, r.name, demand, consumed, energy, energyWeWantToDissipate);
				}
				this.part.thermalInternalFlux -= energy;
				energyWeWantToDissipate = Math.Max(0, energyWeWantToDissipate - energy);
				Log.dbg("{0}:OnFixedUpdate part.temperature={1} ; part.thermalInternalFlux={2} ; energy={3} ; (left)energyToDissipate={4}", this.ID, this.part.temperature, this.part.thermalInternalFlux, energy, energyWeWantToDissipate);
				if (energyWeWantToDissipate < Lib.Physics.CUTOFF) break;
			}
			Log.dbg("{0}:OnFixedUpdate enegyNotDissipated={1} ; this.part.thermalInternalFlux = {2} ; this.part.temperature = {3} ; intakeResourceTemp = {4}", this.ID, energyWeWantToDissipate, this.part.thermalInternalFlux, this.part.temperature, intakeResourceTemp);
		}

		private void OnMaxEnergyTransfer(object value)
		{
			float v = (float)this.maxEnergyTransfer * (float)value;
			BaseField field = Fields["maxEnergyTransfer"];
			field.guiName = string.Format("Heat EXCH THR: {0}", L_Aerospace.Lib.UI.Format(v, 0, "J"));
			this.ResetInfo(); // Forces GetInfo to be regenerated.
		}

		#endregion

		private static new readonly KSPe.Util.Log.Logger Log = KSPe.Util.Log.Logger.CreateForType<KerbalHeatDissipator>("L_Aerospace.Kerbal.HeatPump", "Dissipator", 0);
		protected override KSPe.Util.Log.Logger GetLogger() => Log;
	}
} } }