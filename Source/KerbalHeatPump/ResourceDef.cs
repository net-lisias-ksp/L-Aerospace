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

using KSPe;

namespace L_Aerospace { namespace Kerbal { namespace HeatPump
{
	internal struct ResourceDef
	{
		internal readonly int id;
		internal readonly string name;
		internal readonly double consuption;

		internal PartResourceDefinition def;
		internal readonly double hspu;	// Joules per Unit per °K
		internal readonly double hspuK;	// Joules per Unit per °K in kiloJoules (to optimize a bit calculations)
		internal readonly double hsp;	// Joules per Kg per °K
		internal readonly double ratio;	// How many units (consuption) consumed per Unit

		private ResourceDef(double autonomy, string name, double consuption) : this()
		{
			PartResourceDefinition r = PartResourceLibrary.Instance.GetDefinition(name);
			this.id = r.id;
			this.name = name;
			this.consuption = consuption;

			this.def = r;

			// density : Tons per Unit
			// hsp : Joules per (Kg * °K) J/(kg*K)

			this.hsp = 1000 * r.density * r.specificHeatCapacity; // HSP per kG, not per U.
			this.hspu = r.specificHeatCapacity; // HSP per U, not per kG.
			this.hspuK = this.hspu / 1000;
			this.ratio = consuption / autonomy;

			Log.dbg("{0} {1} {2} {3} {4} {5}", this.id, this.name, this.consuption, this.hsp, this.hspu, this.ratio);
		}

		public override string ToString() =>
				this.ratio > 0
					? string.Format("ResourceDef:{{id:{0} name:{1} ratio:{2} density:{3} hsp:{4}}}", this.id, this.name, this.ratio, this.def.density, this.def.specificHeatCapacity)
					: string.Format("ResourceDef:{{id:{0} name:{1} density:{2} hsp:{3} hspu:{4}}}", this.id, this.name, this.def.density, this.def.specificHeatCapacity, this.hspu)
				;

		internal static ResourceDef from(double autonomy, ConfigNode node) => from(autonomy, ConfigNodeWithSteroids.from(node));
		internal static ResourceDef from(double autonomy, ConfigNodeWithSteroids node)
		{
			return new ResourceDef(
						autonomy,
						node.GetValue("name"),
						node.GetValue<double>("consuption", 0d)
					);;
		}

		internal static ResourceDef from(double autonomy, ResourceRatio r)
		{
			return new ResourceDef(
						autonomy,
						r.ResourceName,
						r.Ratio
					);
		}

		internal ConfigNode toConfigNode()
		{
			ConfigNode r = new ConfigNode("RESOURCE");
			r.AddValue("name", this.name);
			r.AddValue("consuption", this.consuption);
			return r;
		}

		internal static List<ResourceDef> readList(double autonomy, ConfigNode partConfig, string name)
		{
			ConfigNode moduleConfig = null;
			{
				ConfigNode[] modulesConfig = partConfig.GetNodes("MODULE");
				for (int i = 0; i < modulesConfig.Length; ++i) if (name.Equals(modulesConfig[i].GetValue("name")))
					moduleConfig = modulesConfig[i];
			}

			ConfigNode[] nodes = moduleConfig.GetNodes("RESOURCE");
			List<ResourceDef> r = new List<ResourceDef>(nodes.Length);

			if (null == nodes) return r;

			for (int i = 0; i < nodes.Length; ++i)
				r.Add(ResourceDef.from(autonomy, nodes[i]));

			return r;
		}
	}
} } }
