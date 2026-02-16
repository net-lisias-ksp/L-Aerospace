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
		internal readonly string name;
		internal readonly double rate;
		internal readonly int id;

		internal  ResourceDef(string name, double rate) : this()
		{
			this.name = name;
			this.rate = rate;
			this.id = PartResourceLibrary.Instance.GetDefinition(name).id;
		}

		internal static ResourceDef from(ConfigNode node)
		{
			return new ResourceDef(
						node.GetValue("name"),
						Double.Parse(node.GetValue("rate"))
					);
			}

		internal ConfigNode toConfigNode()
		{
			ConfigNode r = new ConfigNode("RESOURCE");
			r.AddValue("name", this.name);
			r.AddValue("rate", this.rate);
			return r;
		}

		internal static List<ResourceDef> readList(ConfigNode partConfig, string name, string owner)
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
				r.Add(ResourceDef.from(nodes[i]));

			return r;
		}
	}
} } }
