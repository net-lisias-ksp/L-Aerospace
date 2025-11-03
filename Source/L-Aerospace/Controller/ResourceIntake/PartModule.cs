/*
	This file is part of L Aerospace
		© 2018-2025 LisiasT

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

namespace L_Aerospace.Controller.ResourceIntake
{
	public class ModuleResourceIntakeController : AbstractController<ModuleResourceIntake>
	{
	#region Dynamic Widgets
		public const int MAX_RESOURCES_GUI = 4;

		[KSPField (isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "")]
		public string resourceName_0 = "";

		[KSPField (isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "")]
		public float airFlow_0 = 0;

		[KSPField (isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "")]
		public string status_0 = "";

		[KSPField (isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "")]
		public float airSpeedGui_0 = 0;

		[KSPField (isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "")]
		public string resourceName_1 = "";

		[KSPField (isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "")]
		public float airFlow_1 = 0;

		[KSPField (isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "")]
		public string status_1 = "";

		[KSPField (isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "")]
		public float airSpeedGui_1 = 0;

		[KSPField (isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "")]
		public string resourceName_2 = "";

		[KSPField (isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "")]
		public float airFlow_2 = 0;

		[KSPField (isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "")]
		public string status_2 = "";

		[KSPField (isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "")]
		public float airSpeedGui_2 = 0;

		[KSPField (isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "")]
		public string resourceName_3 = "";

		[KSPField (isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "")]
		public float airFlow_3 = 0;

		[KSPField (isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "")]
		public string status_3 = "";

		[KSPField (isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "")]
		public float airSpeedGui_3 = 0;
	#endregion

		[KSPField (isPersistant = true)]
		public bool intakeEnabled = true;

		[KSPField (isPersistant = true)]
		public bool controlGenerators = false;

		private readonly List<ModuleGenerator> generators = new List<ModuleGenerator>();
		private readonly Dictionary<ModuleGenerator, bool> lastKnownGeneratorState = new Dictionary<ModuleGenerator, bool>();

		private class StatusData
		{
			internal readonly PartModule parent;
			internal readonly ModuleResourceIntake target;

			internal readonly BaseField resource;
			internal readonly BaseField airFlow;
			internal readonly BaseField status;
			internal readonly BaseField eas;

			internal StatusData(PartModule parent, ModuleResourceIntake target, int i)
			{
				this.parent = parent;
				this.target = target;

				this.resource = this.parent.Fields[string.Format("resourceName_{0}", i)];
				this.airFlow = this.parent.Fields[string.Format("airFlow_{0}", i)];
				this.status = this.parent.Fields[string.Format("status_{0}", i)];
				this.eas = this.parent.Fields[string.Format("airSpeedGui_{0}", i)];

				this.resource.guiName = this.target.resourceDef?.displayName??this.target.resourceName;
				this.resource.guiActive = true;

				this.setup(this.target.Fields["airFlow"], this.airFlow);
				this.setup(this.target.Fields["status"], this.status);
				this.setup(this.target.Fields["airSpeedGui"], this.eas);
			}

			internal void update()
			{
				this.airFlow.SetValue(this.target.airFlow, this.parent);
				this.status.SetValue(this.target.status, this.parent);
				this.eas.SetValue(this.target.airSpeedGui, this.parent);
			}

			private void setup(BaseField source, BaseField target)
			{
				source.guiActive = false;
				target.guiActive = true;
				target.guiName = "    " + source.guiName;
				target.guiFormat = source.guiFormat;
				target.guiUnits = source.guiUnits;
			}
		}

		private readonly StatusData[] status;

		public ModuleResourceIntakeController()
		{
			this.updateDelegate = this.dummyUpdate;

			this.status = new StatusData[MAX_RESOURCES_GUI];
		}

	#region PartModule life cycle

		public override void OnAwake()
		{
			base.OnAwake();
			this.firstSetupMyTargets();
			this.setupMe();
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			this.generators.AddRange(this.part.Modules.GetModules<ModuleGenerator>());
			foreach (ModuleGenerator mg in this.generators)
				this.lastKnownGeneratorState[mg] = mg.generatorIsActive;

			if (1 == this.targets.Count)
				Log.warn("Part {0} has only one ModuleResourceIntake! I'm useless, but still active...", this.part.partName);

			this.firstSetupMyTargets();
			this.setupMe();

			if (StartState.Editor == state)	this.updateDelegate = this.editorUpdate;
			else							this.updateDelegate = this.setupUpdate;
		}

	#endregion

	#region Update Delegates

		private void statusUpdate()
		{ 
			int size = Math.Min(this.status.Length, this.targets.Count);
			for (int i = 0; i < size; ++i)
				this.status[i].update();
		}

		private void editorUpdate()
		{
			Log.dbg("editorUpdate");
			// By some reason, Editor is (as usual) screwing with us.
			bool changed = false;
			foreach (ModuleResourceIntake m in this.targets)
			{
				changed =
					!(
						m.Fields["airFlow"].guiActive
						|| m.Fields["status"].guiActive
						|| m.Events["Activate"].active
						|| m.Events["Deactivate"].active
						|| m.Actions["ToggleAction"].active
					);

				this.setupTarget(m);
			}

			if (changed)		this.updateDelegate = this.editorUpdate;
			else				this.updateDelegate = this.dummyUpdate;
		}

		private void setupUpdate()
		{
			Log.dbg("setupUpdate");

			foreach (ModuleResourceIntake m in this.targets)
				this.setupTarget(m);

			{
				int size = Math.Min(this.status.Length, this.targets.Count);
				for (int i = 0; i < size; ++i)
					this.status[i] = new StatusData(this, this.targets[i], i);
			}

			this.updateDelegate = this.statusUpdate;
		}

	#endregion

		private void firstSetupMyTargets()
		{
			foreach (ModuleResourceIntake m in this.targets)
				this.firstSetupTarget(m);
		}

		private void firstSetupTarget(ModuleResourceIntake m)
		{
			this.setupTarget(m);
			// TODO: Inject a Delegate somehow in the BaseEvent.listParent of the target events so we can handle them if someone
			// access them directly by code. Idem for the Action. Idem for the intakeEnabled.
		}

		private void setupMe()
		{
			this.Events["Activate"].guiActive = !this.intakeEnabled;
			this.Events["Deactivate"].guiActive = this.intakeEnabled;
		}

		private void setupTarget(ModuleResourceIntake m)
		{
			m.intakeEnabled = this.intakeEnabled;
			{
				BaseField bf = m.Fields["airFlow"];
				bf.guiActive = false;
				bf.guiActiveEditor = false;
			}
			{
				BaseField bf = m.Fields["status"];
				bf.guiActive = false;
				bf.guiActiveEditor = false;
			}
			{
				BaseEvent be = m.Events["Activate"];
				be.guiActive = false;
				be.active = false;
			}
			{
				BaseEvent be = m.Events["Deactivate"];
				be.guiActive = false;
				be.active = false;
			}
			{
				BaseAction ba = m.Actions["ToggleAction"];
				if (null != ba)
				{ 
					ba.active = false;
					// m.Actions.Remove(ba); // Can't remove this or the ModuleResourceIntake will bork on OnStart
				}
			}
		}

		[KSPEvent (guiActive = true, guiActiveEditor = true, guiName = "#autoLOC_6001427")]
		public void Activate()
		{
			if (!this.active) return;

			this.intakeEnabled = true;

			this.ActivateIntakes();
			this.ActivateGeneratorsIfNeeded();

			this.setupMe();
		}

		[KSPEvent (guiActive = true, guiActiveEditor = true, guiName = "#autoLOC_6001426")]
		public void Deactivate()
		{
			if (!this.active) return;

			this.intakeEnabled = false;

			this.DeactivateIntakes();
			this.DeactivateGeneratorsIfNeeded();
			this.setupMe();
		}

		[KSPAction ("#autoLOC_6001425")]
		public void ToggleAction(KSPActionParam param)
		{
			if (!this.active) return;

			this.ToggleAction(this.intakeEnabled ? KSPActionType.Deactivate : KSPActionType.Activate);
			this.setupMe();
		}

		[KSPAction ("#autoLOC_6001427")]
		public void Activate(KSPActionParam param) => this.Activate();

		[KSPAction ("#autoLOC_6001426")]
		public void Deactivate(KSPActionParam param) => this.Deactivate();

		public void ToggleAction(KSPActionType action)
		{
			switch (action)
			{
				case KSPActionType.Activate:	this.Activate(); break;
				case KSPActionType.Deactivate:	this.Deactivate(); break;
				default:
					Log.err("Unknown KSPActionType {0}", action);
					break;
			}
		}

		private void ActivateIntakes()
		{
			foreach (ModuleResourceIntake m in this.targets) if (m.isEnabled && !m.intakeEnabled)
			{
				m.Activate();
				this.setupTarget(m);
			}
		}

		private void DeactivateIntakes()
		{
			foreach (ModuleResourceIntake m in this.targets) if (m.isEnabled && m.intakeEnabled)
			{
				m.Deactivate();
				this.setupTarget(m);
			}
		}

		private void ActivateGeneratorsIfNeeded()
		{
			if (!this.controlGenerators) return;

			foreach (ModuleGenerator mg in this.generators) if (mg.isEnabled && !mg.generatorIsActive)
			{ 
				Log.dbg("Gen On {0} {1} {2} {3}", mg.moduleName, this.lastKnownGeneratorState[mg], mg.generatorIsActive, this.intakeEnabled);
				if (!mg.generatorIsActive && this.lastKnownGeneratorState[mg] && this.intakeEnabled)
					mg.Activate();
			}
		}

		private void DeactivateGeneratorsIfNeeded()
		{
			if (!this.controlGenerators) return;

			foreach (ModuleGenerator mg in this.generators) if (mg.isEnabled)
			{
				Log.dbg("Gen Off {0} {1} {2} {3}", mg.moduleName, this.lastKnownGeneratorState[mg], mg.generatorIsActive, this.intakeEnabled);
				this.lastKnownGeneratorState[mg] = mg.generatorIsActive;
				if (mg.generatorIsActive && !this.intakeEnabled)
					mg.Shutdown();
			}
		}
	}
}
