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

using UnityEngine;

namespace L_Aerospace { namespace Kerbal { namespace CrewMass
{
	public class KerbalCrewMass : PartModule, IPartMassModifier
	{
		private bool active = false;
		public bool Active
		{
			get => Globals.Instance.KerbalCrewMass & this.active && this.enabled;
			internal set
			{
				this.enabled = this.isEnabled = value;
			}
		}

		private float massSurplus;

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
			this.active = (fromModule as KerbalCrewMass).active;
		}

		public override void OnLoad(ConfigNode node)
		{
			Log.dbg("{0}:OnLoad {1}", this.ID, null != node);
			base.OnLoad(node);
			node.TryGetValue("active", ref this.active);

			// Without this, there's no reason to waste our time here!
			// For future reference:
			//		kerbalCrewMass = 0.09375  // Full equiped Kerbal (Parachute and JetPack)
			//		kerbalCrewMass = 0.045    // Only the Kerbal
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
		}

		public override void OnActive()
		{
			Log.dbg("{0}:OnActive", this.ID);
			base.OnActive();
			this.init();
		}

		// Needed because I had overriden OnActive.
		// See https://kerbalspaceprogram.com/api/class_part_module.html#a6f2dd76038326c527e64d2ce96bb45fe
		public override bool IsStageable() => false;

		public override void OnInactive()
		{
			Log.dbg("{0}:OnInactive", this.ID);
			base.OnInactive();
			this.deinit();
		}

		public override void OnStart(StartState state)
		{
			Log.dbg("{0}:OnStart.in {1} {2} {3} {4}", this.ID, state, this.enabled, this.active, this.Active);
			base.OnStart(state);

			this.enabled = state > StartState.Editor;
			this.CalculateCurrentMassSurplus();

			Log.dbg("{0}:OnStart.out {1} {2}", this.ID, state, this.Active);
		}

		private string _getInfo = null;
		public override string GetInfo()
		{
			if (!this.active) return "Disabled.";
			if (null == this._getInfo)
			{
				this._getInfo = string.Format(
							"Mass per Kerbal: {0}kG\n"
							+ "Max Crew Mass: {1}kG"
						, PhysicsGlobals.KerbalCrewMass
						, PhysicsGlobals.KerbalCrewMass * this.part.CrewCapacity
					);
			}
			return this._getInfo;
		}

		#endregion

		private void CalculateCurrentMassSurplus()
		{
			if (null == this.part.protoModuleCrew)
			{
				Log.dbg("ERROR: null == this.part.protoModuleCrew for {0} as {2}", this.ID, this.massSurplus);
				return;
			}
			// Switching Count with CrewCapacity saves a multiply to -1.
			this.massSurplus = (this.part.protoModuleCrew.Count - this.part.CrewCapacity) * PhysicsGlobals.KerbalCrewMass;
			Log.dbg("Recalculate mass surplus for {0} as {2} {3} = {4}", this.ID, this.part.protoModuleCrew.Count, this.part.CrewCapacity, this.massSurplus);
#if DEBUG
			{
				List<string> crew = new List<string>();
				for (int i = 0; i < this.part.protoModuleCrew.Count; ++i)
					crew.Add(this.part.protoModuleCrew[i].name);
				Log.dbg("Part {0} have {2} crew : {3}", this.ID, crew.Count, String.Join(", ", crew.ToArray()));
			}
#endif
			this.Active = this.massSurplus > Lib.Physics.CUTOFF;
		}

		private void OnVesselCrewWasModified(Vessel data) => this.CalculateCurrentMassSurplus();
		private void OnEditorShipModified(ShipConstruct data) => this.CalculateCurrentMassSurplus();
		private void init()
		{
			GameEvents.onVesselCrewWasModified.Add(this.OnVesselCrewWasModified);
			GameEvents.onEditorShipModified.Add(this.OnEditorShipModified);
		}

		private void deinit()
		{
			GameEvents.onEditorShipModified.Remove(this.OnEditorShipModified);
			GameEvents.onVesselCrewWasModified.Remove(this.OnVesselCrewWasModified);
		}

		float IPartMassModifier.GetModuleMass(float defaultMass, ModifierStagingSituation sit) => this.Active ? this.massSurplus : 0;
		ModifierChangeWhen IPartMassModifier.GetModuleMassChangeWhen() => ModifierChangeWhen.FIXED;

		private String __ID = null;
		public String ID => __ID??(__ID = String.Format("{0}:{1:X}", this.name, this.part.GetInstanceID()));
		private static readonly KSPe.Util.Log.Logger Log = KSPe.Util.Log.Logger.CreateForType<KerbalCrewMass>("L_Aerospace.Kerbal.Kerbal.CrewMass", "Module", 0);
	}
} } }