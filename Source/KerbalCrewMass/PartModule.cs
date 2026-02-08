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
using UnityEngine;

namespace L_Aerospace { namespace KerbalCrewMass
{
	public class KerbalCrewMass : PartModule, IPartMassModifier
	{
		private const bool visibleOnFlight = false;
		private const bool visibleOnEditor = true;

		#region KSP UI

		[KSPField(isPersistant = true, guiActive = visibleOnFlight, guiActiveEditor = visibleOnEditor, guiName = "L/Aerospace::CrewMass")]
		[UI_Toggle(disabledText = "Disabled", enabledText = "Enabled", scene = UI_Scene.All)]
		public bool active = false;

		#endregion

		private float massSurplus;

		#region KSP Life Cycle

		public override void OnAwake()
		{
			Log.dbg("OnAwake {0}:{1:X}", this.name, this.part.GetInstanceID());
			base.OnAwake();
			this.active = Globals.Instance.KerbalCrewMass;
		}

		public override void OnStart(StartState state)
		{
			Log.dbg("OnStart {0}:{1:X} {2} {3}", this.name, this.part.GetInstanceID(), state, this.active);
			base.OnStart(state);
			{
				BaseField bf = this.Fields["active"];
				bf.guiActive = Globals.Instance.DebugMode || (visibleOnFlight && Globals.Instance.PawEntries);
				bf.guiActiveEditor = Globals.Instance.DebugMode || (visibleOnEditor && Globals.Instance.PawEntries);
			}
		}

		public override void OnCopy(PartModule fromModule)
		{
			Log.dbg("OnCopy {0}:{1:X} from {2:X}", this.name, this.part.GetInstanceID(), fromModule.part.GetInstanceID());
			base.OnCopy(fromModule);
		}

		public override void OnLoad(ConfigNode node)
		{
			Log.dbg("OnLoad {0}:{1:X} {2}", this.name, this.part.GetInstanceID(), null != node);
			base.OnLoad(node);
			this.CalculateCurrentMassSurplus();
		}

		public override void OnSave(ConfigNode node)
		{
			Log.dbg("OnSave {0}:{1:X} {2}", this.name, this.part.GetInstanceID(), null != node);
			base.OnSave(node);
		}

		public override void OnInitialize()
		{
			Log.dbg("OnInitialize {0}:{1:X}", this.name, this.part.GetInstanceID());
			base.OnInitialize();
		}

		public override void OnActive()
		{
			Log.dbg("OnActive {0}:{1:X}", this.name, this.part.GetInstanceID());
			base.OnActive();
			this.init();
		}

		// Needed because I had overriden OnActive.
		// See https://kerbalspaceprogram.com/api/class_part_module.html#a6f2dd76038326c527e64d2ce96bb45fe
		public override bool IsStageable() => false;

		public override void OnInactive()
		{
			Log.dbg("OnInactive {0}:{1:X}", this.name, this.part.GetInstanceID());
			base.OnInactive();
			this.deinit();
		}

		#endregion

		private void CalculateCurrentMassSurplus()
		{
			if (null == this.part.protoModuleCrew)
			{
				Log.dbg("ERROR: null == this.part.protoModuleCrew for {0}:{1:X} as {2}", this.name, this.part.GetInstanceID(), this.massSurplus);
				return;
			}
			// Switching Count with CrewCapacity saves a multiply to -1.
			this.massSurplus = (this.part.protoModuleCrew.Count - this.part.CrewCapacity) * PhysicsGlobals.KerbalCrewMass;
			Log.dbg("Recalculate mass surplus for {0}:{1:X} as {2} {3} = {4}", this.name, this.part.GetInstanceID(), this.part.protoModuleCrew.Count, this.part.CrewCapacity, this.massSurplus);
		}

		private void OnVesselCrewWasModified(Vessel data) => this.CalculateCurrentMassSurplus();

		private void init()
		{
			GameEvents.onVesselCrewWasModified.Add(this.OnVesselCrewWasModified);
		}

		private void deinit()
		{
			GameEvents.onVesselCrewWasModified.Remove(this.OnVesselCrewWasModified);
		}

		float IPartMassModifier.GetModuleMass(float defaultMass, ModifierStagingSituation sit) => this.massSurplus;
		ModifierChangeWhen IPartMassModifier.GetModuleMassChangeWhen() => ModifierChangeWhen.FIXED;

		private static readonly KSPe.Util.Log.Logger Log = KSPe.Util.Log.Logger.CreateForType<KerbalCrewMass>("L/Aerospace", "KerbalCrewMass", 0);
	}
} }