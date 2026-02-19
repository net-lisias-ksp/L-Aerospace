using System;
using System.Collections.Generic;

namespace L_Aerospace.Kerbal.HeatPump
{
	internal static class Lib
	{
		internal static class Physics
		{
			internal const double CUTOFF = L_Aerospace.Lib.Physics.CUTOFF;
		}

		internal static class UI
		{
			internal static void PostScreenWarning(string v) => L_Aerospace.Lib.UI.PostScreenWarning(v);

			internal static string Format(float value, int decimals, string unit = null) =>  L_Aerospace.Lib.UI.Format(value, decimals, unit);
			internal static string Format(double value, int decimals, string unit = null) =>  L_Aerospace.Lib.UI.Format(value, decimals, unit);
		}

		internal static class Part
		{
			internal static ResourceDef[] buildResourceList(PartModule owner, double maxEnergyTransfer, string ID)
			{
				ResourceDef[] r = ResourceDef.readList(maxEnergyTransfer, owner.part.partInfo.partConfig, owner.GetType().Name).ToArray();
				if (0 == r.Length)
					Log.warn("{0}:buildResourceList No Resources found! Deactivating myself...", ID);
				else
					Log.dbg("{0}:buildResourceList Found {1} Resources", ID, r.Length);
	#if DEBUG
				{
					for (int i = 0; i < r.Length; ++i)
						Log.dbg("{0}:buildResourceList {1}", ID, r[i]);
				}
	#endif
				return r;
			}

			internal static Dictionary<ResourceDef, ModuleResourceIntake[]> buildIntakeList(PartModule owner, ResourceDef[] resources, string ID)
			{
				Dictionary<ResourceDef, ModuleResourceIntake[]> r = new Dictionary<ResourceDef, ModuleResourceIntake[]>();

				List<ModuleResourceIntake> all = owner.part.FindModulesImplementing<ModuleResourceIntake>();
				for (int i = 0; i < resources.Length; ++i)
				{
					ResourceDef resource = resources[i];
					string resourceName = resource.name;
					List<ModuleResourceIntake> intakes = new List<ModuleResourceIntake>(owner.part.Modules.Count);
					for (int j = 0; j < all.Count; ++j)
						if (resourceName.Equals(all[j].resourceName))
							intakes.Add(all[j]);
					if (intakes.Count > 0)
					{
						ModuleResourceIntake[] intakeArray = intakes.ToArray();
						r[resource] = intakeArray;
					}
				}

				if (0 == r.Count)
					Log.warn("{0}:buildIntakeList No Resource Intakes found! Deactivating myself...", ID);
				else
					Log.dbg("{0}:buildIntakeList Found {1} Resource Intakes", ID, r.Count);

				return r;
			}
		}
	}
}
