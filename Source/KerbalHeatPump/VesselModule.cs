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
		private readonly List<KerbalHeatSink> heatSinkers = new List<KerbalHeatSink>();
		private readonly Dictionary<Part, KerbalHeatDissipator> heatDissipators = new Dictionary<Part, KerbalHeatDissipator>();
		private readonly Dictionary<Part, List<KerbalHeatDissipatorSuperCharger>> superChargers = new Dictionary<Part, List<KerbalHeatDissipatorSuperCharger>>();

		#region KSP Life Cycle

		protected override void DoLoadVessel() => this.hardActive = Globals.Instance.KerbalCrewMass;

		protected override void DoStart() => this.populate();

		protected override void DoGoOnRails() => this.nukeMe();
		protected override void DoGoOffRails() => this.repopulate();

		protected override void DoVesselWasModified(Vessel data) => this.repopulate();
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

		public double WithdrawEnergy(double requestedEnergy, KerbalHeatDissipator owner)
		{
			Log.dbg("{0}:WithdrawEnergy {1} asked for {2}", this.ID, owner.ID, requestedEnergy);
			if (!this.superChargers.ContainsKey(owner.part)) return 0;

			List<KerbalHeatDissipatorSuperCharger> superChargers = this.superChargers[owner.part];
			int count = superChargers.Count;
			int passes = count;
			double withdrawnEnergy = 0;
			while (passes > 0 && requestedEnergy - withdrawnEnergy > Lib.Physics.CUTOFF)
			{	// If some Charger fail us, let's keep trying the other ones hoping they can absorb the remaining energy.
				double energyPerCharger = (requestedEnergy - withdrawnEnergy) / passes;
				for (int j = 0; j < count; ++j)
					withdrawnEnergy += superChargers[j].WithdrawEnergy(energyPerCharger);	// Ignore if the Charger is active or not, inactive Sinks will just return 0
				--passes;
			}	// At this point, there's nothing left to be tried. Whatever happens, happens.
			Log.dbg("{0}:WithdrawEnergy from {1},  {2} was granted to {3}", this.ID, requestedEnergy, withdrawnEnergy, owner.ID);
			return withdrawnEnergy;
		}

		internal void Announce(KerbalHeatExchanger heatExchanger)
		{
			Log.dbg("{0}:Announce by Heat Exchanger {1}", this.ID, heatExchanger.ID);
		}

		internal void Announce(KerbalHeatSink heatsink)
		{
			Log.dbg("{0}:Announce by Heat Sinker {1}", this.ID, heatsink.ID);
			if (!this.heatSinkers.Contains(heatsink)) this.heatSinkers.Add(heatsink);
		}

		internal void Announce(KerbalHeatDissipator heatDissipator)
		{
			Log.dbg("{0}:Announce by Heat Dissipator {1}", this.ID, heatDissipator.ID);
			if (!this.heatDissipators.ContainsKey(heatDissipator.part)) this.heatDissipators[heatDissipator.part] = heatDissipator;
		}

		internal void Announce(KerbalHeatDissipatorSuperCharger heatDissipatorSuperCharger)
		{
			Log.dbg("{0}:Announce by Heat Dissipator Super Charger {1}", this.ID, heatDissipatorSuperCharger.ID);
			Part dissipatorPart = Lib.Part.LookForTarget(heatDissipatorSuperCharger);
			if (null == dissipatorPart) return;

			if (!this.heatDissipators.ContainsKey(dissipatorPart))
			{
				KerbalHeatDissipator m = dissipatorPart.FindModuleImplementing<KerbalHeatDissipator>();
				if (null == m) return; // No dissipator? Super Charger can't work!
				this.heatDissipators[dissipatorPart] = m;
			}
			if (!this.superChargers.ContainsKey(dissipatorPart)) this.superChargers[dissipatorPart] = new List<KerbalHeatDissipatorSuperCharger>();
			if (!this.superChargers[dissipatorPart].Contains(heatDissipatorSuperCharger)) this.superChargers[dissipatorPart].Add(heatDissipatorSuperCharger);
			heatDissipatorSuperCharger.Announce(this.heatDissipators[dissipatorPart]);
		}

		internal static Controller GetVesselModule(PartModule partModule)
		{
			if (null == partModule.part.vessel) return null; // Usefull to save some code from the caller when there's no vessel active, as on LoadingScreen.

			List<VesselModule> vesselModules = partModule.part.vessel.vesselModules;
			int count = vesselModules.Count;
			for (int i = 0; i < count; ++i) if (vesselModules[i] is Controller)
				return vesselModules[i] as Controller;
			throw new EntryPointNotFoundException(typeof(Controller).FullName);
		}

		private void nukeMe()
		{
			this.heatSinkers.Clear();
			this.heatDissipators.Clear();
			this.superChargers.Clear();
		}

		private void populate()
		{
			// Better safer then sorrier.
			this.nukeMe();
		}

		private void repopulate()
		{
			this.populate();

			int exchangers = 0;
			int sinkers = 0;
			int dissipators = 0;
			int superChargers = 0;

			// A Repopulation happens middle game, when all the parts are already Initialized and alive, and they will not announce
			// themselves again. On the bright side, no race conditions so we can just go horse on the problem.
			for (int i = 0; i < this.vessel.parts.Count; ++i)
			{
				Part p = this.vessel.parts[i];
				{
					KerbalHeatExchanger m = p.FindModuleImplementing<KerbalHeatExchanger>();
					if (m)
					{
						this.Announce(m);
						++exchangers;
					}
				}
				{
					KerbalHeatSink m = p.FindModuleImplementing<KerbalHeatSink>();
					if (m)
					{
						this.Announce(m);
						++sinkers;
					}
				}
				{
					KerbalHeatDissipator m = p.FindModuleImplementing<KerbalHeatDissipator>();
					if (m)
					{
						this.Announce(m);
						++dissipators;
					}
				}
				{
					List<KerbalHeatDissipatorSuperCharger> lm = p.FindModulesImplementing<KerbalHeatDissipatorSuperCharger>();
					for (int j = 0; j < lm.Count; ++j)
					{ 
						this.Announce(lm[j]);
						++superChargers;
					}
				}
			}

			Log.detail("{0}:populate Found {1} Heat Exchangers ; {2} Heat Sinkers; {2} Heat Dissipators; {1} Heat Dissipator Super Chargers", this.ID, exchangers, sinkers, dissipators, superChargers);
		}

		private static new readonly KSPe.Util.Log.Logger Log = KSPe.Util.Log.Logger.CreateForType<KerbalHeatExchanger>("L_Aerospace.Kerbal.HeatPump", "Controller", 0);
		protected override KSPe.Util.Log.Logger GetLogger() => Log;
	}
} } }
