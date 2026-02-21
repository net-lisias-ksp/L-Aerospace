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

namespace L_Aerospace { namespace Kerbal { namespace CrewHeat
{
	public class KerbalCrewHeat : L_Aerospace.Lib.AbstractPartModuleAutoActivable
	{
		private double totalKerbalHeat = 0;

		#region KSP Life Cycle

		protected override void DoAwake()
		{
			this.hardActive = Globals.Instance.KerbalCrewHeat;
		}

		protected override void DoCopy(PartModule fromModule)
		{
			this.totalKerbalHeat = (fromModule as KerbalCrewHeat).totalKerbalHeat;
		}

		protected override void DoSave(KSPe.ConfigNodeWithSteroids node) { }
		protected override void DoPrefabLoad(KSPe.ConfigNodeWithSteroids node) { }
		protected override void DoLoad(KSPe.ConfigNodeWithSteroids node) { }

		protected override void DoStart(StartState state)
		{
			this.CalculateCurrentHeatSurplus();
		}

		protected override string DoGetInfo()
		{
			string r = string.Format(
						"Heat per Kerbal: {0}W\n" +
						"Max Heat: {1}"
					, Globals.Instance.KerbalHeatPerCrew
					, Lib.UI.Format(Globals.Instance.KerbalHeatPerCrew * this.part.CrewCapacity, 0, "J")
				);
			return r;
		}

		protected override void DoUpdate() { }

		protected override void DoFixedUpdate()
		{
			this.Active = this.totalKerbalHeat < Lib.Physics.CUTOFF;

			double energyPushed = this.totalKerbalHeat * TimeWarp.fixedDeltaTime;
			this.part.thermalInternalFlux += energyPushed;
			Log.dbg("{0}:OnFixedUpdate energyPushed = {1} ; part.thermalInternalFlux = {2} ; part.temperature = {3}", this.ID, energyPushed, this.part.thermalInternalFlux, this.part.temperature);
		}

		#endregion

		private void CalculateCurrentHeatSurplus()
		{
			this.totalKerbalHeat = 0;
			if (!this.Active) return;
			if (null == this.part.protoModuleCrew)
			{
				Log.dbg("ERROR: null == this.part.protoModuleCrew for {0}. No Kerbal Heat today.", this.ID);
				this.Active = false;
				return;
			}

			this.totalKerbalHeat = (this.part.CrewCapacity - this.part.protoModuleCrew.Count) * Globals.Instance.KerbalHeatPerCrew;
			Log.dbg("Recalculate total Kerbal heat for {0} as {1} {2} = {3}", this.ID, this.part.protoModuleCrew.Count, this.part.CrewCapacity, this.totalKerbalHeat);
#if DEBUG
			{
				List<string> crew = new List<string>();
				for (int i = 0; i < this.part.protoModuleCrew.Count; ++i)
					crew.Add(this.part.protoModuleCrew[i].name);
				Log.dbg("Part {0} have {1} crew : {2}", this.ID, crew.Count, String.Join(", ", crew.ToArray()));
			}
#endif
			this.Active = this.totalKerbalHeat > Lib.Physics.CUTOFF;
		}

		private void OnVesselCrewWasModified(Vessel data) => this.CalculateCurrentHeatSurplus();
		private void OnEditorShipModified(ShipConstruct data) => this.CalculateCurrentHeatSurplus();
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

		private static new readonly KSPe.Util.Log.Logger Log = KSPe.Util.Log.Logger.CreateForType<KerbalCrewHeat>("L_Aerospace.Kerbal.Kerbal.CrewHeat", "Module", 0);
		protected override KSPe.Util.Log.Logger GetLogger() => Log;
	}
} } }