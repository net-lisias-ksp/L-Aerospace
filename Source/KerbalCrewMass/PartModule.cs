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
	public class KerbalCrewMass : L_Aerospace.Lib.AbstractPartModule, IPartMassModifier
	{
		private float massSurplus;

		#region KSP Life Cycle

		protected override void DoAwake()
		{
			this.hardActive = Globals.Instance.KerbalCrewMass;
		}

		protected override void DoCopy(PartModule fromModule)
		{
			this.massSurplus = (fromModule as KerbalCrewMass).massSurplus;
		}

		protected override void DoSave(KSPe.ConfigNodeWithSteroids node) { }
		protected override void DoPrefabLoad(KSPe.ConfigNodeWithSteroids node) { }
		protected override void DoLoad(KSPe.ConfigNodeWithSteroids node)
		{
			// Without this, there's no reason to waste our time here!
			this.Active = this.part.CrewCapacity > 0;
			// For future reference:
			//		kerbalCrewMass = 0.09375  // Full equiped Kerbal (Parachute and JetPack)
			//		kerbalCrewMass = 0.045    // Only the Kerbal
		}

		protected override void DoStart(StartState state)
		{
			this.CalculateCurrentMassSurplus();
		}

		protected override string DoGetInfo()
		{
			string r = string.Format(
						"Mass per Kerbal: {0}kG\n"
						+ "Max Crew Mass: {1}kG"
					, PhysicsGlobals.KerbalCrewMass
					, PhysicsGlobals.KerbalCrewMass * this.part.CrewCapacity
				);
			return r;
		}

		protected override void DoUpdate() { }
		protected override void DoFixedUpdate() { }

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

		private static new readonly KSPe.Util.Log.Logger Log = KSPe.Util.Log.Logger.CreateForType<KerbalCrewMass>("L_Aerospace.Kerbal.Kerbal.CrewMass", "Module", 0);
		protected override KSPe.Util.Log.Logger GetLogger() => Log;
	}
} } }