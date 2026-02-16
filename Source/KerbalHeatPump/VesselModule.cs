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
	public class Controller : VesselModule
	{
		private readonly List<KerbalHeatSink> list = new List<KerbalHeatSink>();

		#region KSP Life Cycle

		public override void OnLoadVessel()
		{
			Log.dbg("{0}:OnLoadVessel", this.ID);
			base.OnLoadVessel();
			GameEvents.onVesselChange.Add(this.OnVesselChange);
			GameEvents.onEditorShipModified.Add(this.OnEditorShipModified);
		}

		protected override void OnStart()
		{
			Log.dbg("{0}:OnStart {1}", this.ID, this.enabled);
			base.OnStart();
			this.populate();
			this.enabled = this.Enabled;
		}

		public override void OnGoOnRails()
		{
			Log.dbg("{0}:OnGoOnRails {1}", this.ID, this.enabled);
			base.OnGoOnRails();
			this.list.Clear();
		}

		public override void OnGoOffRails()
		{
			Log.dbg("{0}OnGoOffRails", this.ID);
			base.OnGoOffRails();
			this.populate();
			this.enabled = this.Enabled;
		}

		public override void OnUnloadVessel()
		{
			Log.dbg("{0}:OnUnloadVessel", this.ID);
			base.OnUnloadVessel();
			GameEvents.onEditorShipModified.Remove(this.OnEditorShipModified);
			GameEvents.onVesselChange.Remove(this.OnVesselChange);
		}

		private void OnVesselChange(Vessel data) => this.populate();
		private void OnEditorShipModified(ShipConstruct data) => this.populate();

		#endregion

		public bool Enabled => Globals.Instance.KerbalCrewMass && 0 != PhysicsGlobals.KerbalCrewMass;

		public double PumpHeat(double energy)
		{
			int count = this.list.Count;
			Log.dbg("{0}:PumpHeat Trying {1} on {2} sinkers.", this.ID, energy, count);
			if (count < 1) return double.NaN; // Ugly hack, but returning negative numbers are even worst due collateral effects. Better to blow up things early.

			int passes = this.list.Count;
			double sunkEnergy = 0;
			while (passes > 0 && energy - sunkEnergy > Lib.Physics.CUTOFF)
			{	// If some sinker fail us, let's keep trying the other ones hoping they can absorb the remaining energy.
				double energyPerSink = energy / passes;
				for (int i = 0; i < count; ++i)
				{
					sunkEnergy += this.list[i].SinkHeat(energyPerSink);	// Ignore if the Sink is active or not, inactive Sinks will just return 0
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

		internal static Controller GetModule(Vessel vessel)
		{
			if (null == vessel) return null; // Usefull to save some code from the caller when there's no vessel active, as on LoadingScreen.

			int count = vessel.vesselModules.Count;
			for (int i = 0; i < count; ++i) if (vessel.vesselModules[i] is Controller)
				return vessel.vesselModules[i] as Controller;
			throw new EntryPointNotFoundException(typeof(Controller).FullName);
		}
		private String __ID = null;
		public String ID => __ID??(__ID = String.Format("{0}:{1:X}", this.name, this.vessel.GetInstanceID()));
		private static readonly KSPe.Util.Log.Logger Log = KSPe.Util.Log.Logger.CreateForType<KerbalHeatExchanger>("L_Aerospace.Kerbal.HeatPump", "Controller", 0);
	}
} } }
