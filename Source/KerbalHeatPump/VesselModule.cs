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

		protected override void OnStart()
		{
			Log.dbg("OnStart {0}:{1:X} {2}", this.name, this.GetInstanceID(), this.enabled);
			base.OnStart();
			this.populate();
			this.enabled = this.Enabled;
		}

		public override void OnGoOnRails()
		{
			Log.dbg("OnGoOnRails {0}:{1:X} {2}", this.name, this.GetInstanceID(), this.enabled);
			base.OnGoOnRails();
			this.list.Clear();
		}

		public override void OnGoOffRails()
		{
			Log.dbg("OnGoOffRails {0}:{1:X} {2}", this.name, this.GetInstanceID(), this.enabled);
			base.OnGoOffRails();
			this.populate();
			this.enabled = this.Enabled;
		}

		public override void OnLoadVessel()
		{
			Log.dbg("OnLoadVessel {0}:{1:X}", this.name, this.vessel.GetInstanceID());
			base.OnLoadVessel();
			GameEvents.onVesselChange.Add(this.OnVesselChange);
			GameEvents.onEditorShipModified.Add(this.OnEditorShipModified);
		}

		public override void OnUnloadVessel()
		{
			Log.dbg("OnUnloadVessel {0}:{1:X}", this.name, this.vessel.GetInstanceID());
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
			int count = 0;
			for (int i = 0; i < this.list.Count; ++i)
				count += (this.list[i].Active ? 1 : 0);
			if (count < 1) return energy;
			double energyPerSink = energy / this.list.Count;
			double sunkEnergy = 0;
			for (int i = 0; i < count; ++i) // Ignore if the Sink is active or not, inactive Sinks will just return 0
				sunkEnergy += this.list[i].SinkHeat(energyPerSink);
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
		}

		internal static Controller GetModule(Vessel vessel)
		{
			int count = vessel.vesselModules.Count;
			for (int i = 0; i < count; ++i) if (vessel.vesselModules[i] is Controller)
				return vessel.vesselModules[i] as Controller;
			throw new EntryPointNotFoundException(typeof(Controller).FullName);
		}
	}
} } }
