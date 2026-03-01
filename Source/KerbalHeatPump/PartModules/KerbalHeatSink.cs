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
	public class KerbalHeatSink : L_Aerospace.Lib.AbstractPartModule
	{
		[KSPField (isPersistant = true)]
		protected double maxEnergyTransfer = 7500;

		private Controller vesselModule;

		#region KSP Life Cycle

		protected override void DoAwake()
		{
			this.hardActive = Globals.Instance.KerbalHeatPump;
		}

		protected override void DoEditorPartEvent(ConstructionEventType eventType, Part part) { }
		protected override void DoWillBeCopied (bool asSymCounterpart) { }
		protected override void DoCopy(PartModule fromModule)
		{
			this.maxEnergyTransfer = (fromModule as KerbalHeatSink).maxEnergyTransfer;
		}
		protected override void DoWasCopied(PartModule fromModule, bool asSymCounterpart) { }

		protected override void DoSave(KSPe.ConfigNodeWithSteroids node) { }
		protected override void DoPrefabLoad(KSPe.ConfigNodeWithSteroids node) { }
		protected override void DoLoad(KSPe.ConfigNodeWithSteroids node) { }

		protected override void DoStart(StartState state) { }
		protected override void DoStartFinished(StartState state)
		{
			this.vesselModule = Controller.GetVesselModule(this);  // this don't work on DoStart??
			bool crewable = this.part.CrewCapacity > 0;
			if (crewable)
				Log.error("Are you nuts? Shoving a Heat Sinker on a crewable part? No Val barnecuing, please!!! :) (Sinker is permanently disabled)");

			this.Active = !crewable && this.maxEnergyTransfer > 0;
		}

		protected override string DoGetInfo()
		{
			string r = string.Format(
						"Max Energy Transfer : {0}kW"
					, this.maxEnergyTransfer
				);
			return r;
		}

		protected override void DoUpdate() { }
		protected override void DoFixedUpdate() { }

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

		private static new readonly KSPe.Util.Log.Logger Log = KSPe.Util.Log.Logger.CreateForType<KerbalHeatSink>("L_Aerospace.Kerbal.HeatPump", "Sink", 0);
		protected override KSPe.Util.Log.Logger GetLogger() => Log;
	}
} } }