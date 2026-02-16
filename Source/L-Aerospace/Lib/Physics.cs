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

namespace L_Aerospace.Lib
{
	public static class Physics
	{
		public const double CUTOFF = 0.00001;

		public static double GetLocalTemperature(CelestialBody body, Vector3d position)
		{
			// Brute force, half baked, local temperature calculation.
			double zeroAltitude = body.position.magnitude - body.Radius;
			double currentAltitude = position.magnitude - zeroAltitude;

			if (body.atmosphere && (currentAltitude < body.atmosphereDepth))
				return body.GetTemperature(currentAltitude);

			return PhysicsGlobals.SpaceTemperature;
		}
	}
}
