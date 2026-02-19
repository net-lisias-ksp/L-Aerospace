/*
	This file is part of L Aerospace
		© 2018-2026 LisiasT : http://lisias.net <support@lisias.net> : http://lisias.net <support@lisias.net>

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
using KSPe;

namespace L_Aerospace
{
	public class Globals
	{
		private static Globals INSTANCE = null;

		public static Globals Instance => INSTANCE ?? (INSTANCE = new Globals());

		public bool DebugMode => KSPe.Globals<Globals>.DebugMode;

		public readonly bool KerbalHeatPump;
		public readonly bool KerbalCrewMass;
		public readonly bool KerbalCrewHeat;
		public readonly double KerbalHeatPerCrew = 100;

		public readonly bool PawEntries;

		private Globals()
		{
			try
			{
				UrlDir.UrlConfig urlc = GameDatabase.Instance.GetConfigs("L_Aerospace")[0];
				{ 
					ConfigNodeWithSteroids cn = ConfigNodeWithSteroids.from(urlc.config);
					try					{ this.PawEntries = cn.GetValue<bool>("PawEntries"); }
					catch (Exception)	{ this.PawEntries = true; }
				}
				{
					ConfigNodeWithSteroids cn = ConfigNodeWithSteroids.from(urlc.config.GetNode("INSTALLED"));

					try					{ this.KerbalHeatPump = cn.GetValue<bool>("KerbalHeatPump"); }
					catch (Exception)	{ this.KerbalHeatPump = false; }

					try					{ this.KerbalCrewMass = 0 != PhysicsGlobals.KerbalCrewMass && cn.GetValue<bool>("KerbalCrewMass"); }
					catch (Exception)	{ this.KerbalCrewMass = false; }

					try					{ this.KerbalCrewHeat = cn.GetValue<bool>("KerbalCrewHeat"); }
					catch (Exception)	{ this.KerbalCrewHeat = false; }
				}
			}
			catch (Exception e)
			{
				this.PawEntries = true;
				this.KerbalHeatPump =
					this.KerbalCrewMass =
					this.KerbalCrewHeat =
					false;
				Log.err(e, this);
			}
		}
	}
}
