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
				UrlDir.UrlConfig urlc = GameDatabase.Instance.GetConfigs("L-Aerospace")[0];
				Log.dbg("Globals: {0}", urlc.config);
				{ 
					ConfigNodeWithSteroids cn = ConfigNodeWithSteroids.from(urlc.config);
					try					{ this.PawEntries = cn.GetValue<bool>("PawEntries"); }
					catch (Exception e)	{ this.PawEntries = true; Log.err(e, this); }
				}
				{
					ConfigNodeWithSteroids cn = ConfigNodeWithSteroids.from(urlc.config.GetNode("INSTALLED"));

					try					{ this.KerbalHeatPump = cn.GetValue<bool>("KerbalHeatPump"); }
					catch (Exception e)	{ this.KerbalHeatPump = false; Log.err(e, this);}

					try					{ this.KerbalCrewMass = 0 != PhysicsGlobals.KerbalCrewMass && cn.GetValue<bool>("KerbalCrewMass"); }
					catch (Exception e)	{ this.KerbalCrewMass = false; Log.err(e, this);}

					try					{ this.KerbalCrewHeat = cn.GetValue<bool>("KerbalCrewHeat"); }
					catch (Exception e)	{ this.KerbalCrewHeat = false; Log.err(e, this);}
				}
			}
			catch (Exception e)
			{
				this.PawEntries = true;
				this.KerbalHeatPump =
					this.KerbalCrewMass =
					this.KerbalCrewHeat =
					false;
				Log.err(e, "Error reading L-Aerospace Config from GameDatabase. Some features were deactivated!", this);
			}
		}
	}
}
