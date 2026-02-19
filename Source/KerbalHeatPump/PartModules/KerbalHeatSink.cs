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
	public class KerbalHeatSink : PartModule
	{
		private bool active = false;
		public bool Active
		{
			get => Globals.Instance.KerbalHeatPump && this.active && this.enabled;
			internal set
			{
				this.enabled = this.isEnabled = value;
			}
		}

		[KSPField (isPersistant = true)]
		protected double maxEnergyTransfer = 7500;

		private Controller vesselModule;

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
			this.active = (fromModule as KerbalHeatSink).active;
			this.maxEnergyTransfer = (fromModule as KerbalHeatSink).maxEnergyTransfer;
		}

		public override void OnLoad(ConfigNode node)
		{
			Log.dbg("{0}:OnLoad {1}", this.ID, null != node);
			base.OnLoad(node);
			node.TryGetValue("active", ref this.active);
		}

		public override void OnSave(ConfigNode node)
		{
			Log.dbg("{0}:OnSave {1}", this.ID, null != node);
			base.OnSave(node);
			node.SetValue("active", this.active, true);
		}

		public override void OnInitialize()
		{
			Log.dbg("{0}:OnInitialize {1} {2} {3} {4}", this.ID, Globals.Instance.KerbalHeatPump, this.enabled, this.active, this.Active);
			base.OnInitialize();
			if (
					this.enabled
					&& !this.IsStageable()	// Rationale: stageable parts should obey the stage rules,
											// but we also need the "Active" life cycle nevertheless - so we force the activation
											// only if the part is not stageable.
				)
				this.part.force_activate(); // This will activate the OnFixedUpdate
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
			Log.dbg("{0}:OnInactive ", this.ID);
			base.OnInactive();
		}

		public override void OnStart(StartState state)
		{
			Log.dbg("{0}:OnStart.in {1} {2} {3} {4}", this.ID, state, this.enabled, this.active, this.Active);
			base.OnStart(state);

			this.enabled = state > StartState.Editor;
			this.vesselModule = Controller.GetModule(this.vessel);
			this.Active = this.maxEnergyTransfer > 0;

			Log.dbg("{0}:OnStart.out {1} {2}", this.ID, state, this.Active);
		}

		private string _getInfo = null;
		public override string GetInfo()
		{
			if (!this.active) return "Disabled.";
			if (null == this._getInfo)
			{
				this._getInfo = string.Format(
							"Max Energy Transfer : {0}kW"
						, this.maxEnergyTransfer
					);
			}
			return this._getInfo;
		}

		#endregion

		public double SinkHeat(double energy)
		{
			if (!this.Active) return 0;

			double maxEnergyToSink = this.maxEnergyTransfer * TimeWarp.fixedDeltaTime;
			energy = Math.Min(energy, maxEnergyToSink);

			double kelvins = energy / this.part.thermalMass;
			if (this.part.skinTemperature + kelvins >= this.part.skinMaxTemp) return 0;

			this.part.thermalInternalFlux += energy;
			Log.dbg("{0}:SinkHeat enegySunk={1} ; part.thermalInternalFlux = {2} ; part.temperature = {3}", this.ID, energy, this.part.thermalInternalFlux, this.part.temperature);
			return energy;
		}

		private String __ID = null;
		public String ID => __ID??(__ID = String.Format("{0}:{1:X}", this.name, this.part.GetInstanceID()));
		private static readonly KSPe.Util.Log.Logger Log = KSPe.Util.Log.Logger.CreateForType<KerbalHeatSink>("L_Aerospace.Kerbal.HeatPump", "Sink", 0);
	}
} } }