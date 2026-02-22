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
	public class Controller : L_Aerospace.Lib.AbstractVesselModule
	{
		private readonly List<KerbalHeatSink> list = new List<KerbalHeatSink>();

		#region KSP Life Cycle

		protected override void DoLoadVessel() => this.hardActive = Globals.Instance.KerbalCrewMass;

		protected override void DoStart() => this.populate();

		protected override void DoGoOnRails()
		{
			this.partsWithSinker.Clear();
			this.heatSinkers.Clear();
			this.registry.Clear();
		}

		protected override void DoGoOffRails() => this.populate();
		protected override void DoVesselChange(Vessel data) => this.repopulate();
		protected override void DoEditorShipModified(ShipConstruct data) => this.repopulate();

		#endregion

		public double PumpHeat(double energy)
		{
			int count = this.heatSinkers.Count;

			Log.dbg("{0}:PumpHeat Trying {1} on {2} sinkers.", this.ID, energy, count);
			if (count < 1) return double.NaN; // Ugly hack, but returning negative numbers are even worst due collateral effects. Better to blow up things early.

			int passes = count;
			double sunkEnergy = 0;
			while (passes > 0 && energy - sunkEnergy > Lib.Physics.CUTOFF)
			{	// If some sinker fail us, let's keep trying the other ones hoping they can absorb the remaining energy.
				double energyPerSink = energy / passes;
				for (int i = 0; i < count; ++i)
				{
					sunkEnergy += this.heatSinkers[i].SinkHeat(energyPerSink);	// Ignore if the Sink is active or not, inactive Sinks will just return 0
					passes -= 1;
				}
			}	// At this point, there's nothing left to be tried. Whatever happens, happens.
			Log.dbg("{0}:PumpHeat Sunk {1} on {2} sinkers.", this.ID, sunkEnergy, count);
			return sunkEnergy;
		}

		private void populate()
		{
			this.list.Clear(); // Better safer then sorrier.
			for (int i = 0; i < this.vessel.parts.Count; ++i)
			{
				Part p = this.vessel.parts[i];
				KerbalHeatSink m = p.FindModuleImplementing<KerbalHeatSink>();
				if (null == m) continue;
				this.list.Add(m);
			}
			Log.dbg("{0}:populate Found {1} Heat Sinkers.", this.ID, this.list.Count);
		}

		internal static Controller GetVesselModule(PartModule partModule)
		{
			if (null == partModule.part.vessel) return null; // Usefull to save some code from the caller when there's no vessel active, as on LoadingScreen.

			int count = partModule.part.vessel.vesselModules.Count;
			for (int i = 0; i < count; ++i) if (partModule.part.vessel.vesselModules[i] is Controller)
				return partModule.part.vessel.vesselModules[i] as Controller;
			throw new EntryPointNotFoundException(typeof(Controller).FullName);
		}

		private static new readonly KSPe.Util.Log.Logger Log = KSPe.Util.Log.Logger.CreateForType<KerbalHeatExchanger>("L_Aerospace.Kerbal.HeatPump", "Controller", 0);
		protected override KSPe.Util.Log.Logger GetLogger() => Log;
	}
} } }
