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

namespace L_Aerospace { namespace KerbalHeatPump
{
	public class ModuleKerbalHeatSink : PartModule
	{
		public bool Active
		{
			get => this.isEnabled;
			private set
			{
				this.enabled = this.isEnabled = value;
			}
		}

		private Controller vesselModule;
		private ResourceDef[] resources = new ResourceDef[0];
		private Dictionary<string, ModuleResourceIntake[]> intakes = new Dictionary<string, ModuleResourceIntake[]>();

		#region KSP Life Cycle

		public override void OnAwake()
		{
			Log.dbg("OnAwake {0}", this.ID);
			base.OnAwake();
		}

		public override void OnCopy(PartModule fromModule)
		{
			Log.dbg("OnCopy {0} from {1:X}", this.ID, fromModule.part.GetInstanceID());
			base.OnCopy(fromModule);
			this.resources = (fromModule as ModuleKerbalHeatSink).resources;
			this.intakes = (fromModule as ModuleKerbalHeatSink).intakes;
		}

		public override void OnLoad(ConfigNode node)
		{
			Log.dbg("OnLoad {0} {1}", this.ID, null != node);
			base.OnLoad(node);

			if (null == this.part.partInfo) return;

			{ 
				this.resources = ResourceDef.readList(this.part.partInfo.partConfig, this.GetType().Name, this.ID).ToArray();
				if (0 == this.resources.Length)
					Log.warn("{0}:OnLoad No Resources found! Deactivating myself...", this.ID);
				else
					Log.dbg("{0}:OnLoad Found {1} Resources", this.ID, this.resources.Length);
			}
			this.Active = 0 != this.resources.Length;

			{
				int count = 0;
				List<ModuleResourceIntake> all = this.part.FindModulesImplementing<ModuleResourceIntake>();
				for (int i = 0; i < this.resources.Length; ++i)
				{
					string resourceName = this.resources[i].name;
					List<ModuleResourceIntake> intakes = new List<ModuleResourceIntake>(this.part.Modules.Count);
					for (int j = 0; j < all.Count; ++j)
						if (resourceName.Equals(all[j].resourceName))
							intakes.Add(all[j]);
					ModuleResourceIntake[] intakeArray = intakes.ToArray();
					count += intakeArray.Length;
					this.intakes[resourceName] = intakeArray;
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
			Log.dbg("OnSave {0} {1}", this.ID, null != node);
			base.OnSave(node);
		}

		public override void OnStart(StartState state)
		{
			Log.dbg("OnStart {0} {1} {2}", this.ID, state, this.enabled);
			base.OnStart(state);
			this.Active &= StartState.Editor != state;
		}

		public override void OnInitialize()
		{
			Log.dbg("OnInitialize {0}", this.ID);
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
			Log.dbg("OnActive {0}", this.ID);
			base.OnActive();
		}

		// Needed because I had overriden OnActive.
		// See https://kerbalspaceprogram.com/api/class_part_module.html#a6f2dd76038326c527e64d2ce96bb45fe
		public override bool IsStageable() => false;

		public override void OnInactive()
		{
			Log.dbg("OnInactive {0}}", this.ID);
			base.OnInactive();
		}

		#endregion

		public double SinkHeat(double energy)
		{
			if (!this.Active) return 0;

			double kelvins = energy / this.part.thermalMass;
			if (this.part.temperature + kelvins >= this.part.maxTemp) return 0;

			for (int i = 0; i < this.resources.Length; ++i)
			{
				ResourceDef resource = this.resources[i];
				ModuleResourceIntake[] intakes = this.intakes[resource.name];
				double demand = energy * resource.rate;
				double flow = 0;
				for (int j = 0; j < intakes.Length; ++j)
					flow += intakes[j].airFlow;
				double supply = Math.Min(demand, flow);

				//energy *= supply / demand;
			}

			this.part.thermalInternalFlux += energy;
			return energy;
		}

		private String __ID = null;
		public String ID => __ID??(__ID = String.Format("{0}:{1:X}", this.name, this.part.GetInstanceID()));
		private static readonly KSPe.Util.Log.Logger Log = KSPe.Util.Log.Logger.CreateForType<ModuleKerbalHeatSink>("L_Aerospace", "KerbalHeatSink", 0);
	}
} }