/*
	This file is part of L Aerospace
		© 2018-2025 LisiasT

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

using L_Aerospace.Lib;

namespace L_Aerospace.Controller
{
	public abstract class AbstractController<T> : PartModule where T: class
	{
		protected bool active = true;
		protected readonly List<T> targets = new List<T>();
		protected UpdateDelegate updateDelegate;

		public override void OnAwake()
		{
			this.updateDelegate = this.dummyUpdate;

			bool foundme = false;
			Type myself = this.GetType();
			for (int i = 0;i < this.part.Modules.Count;++i)
			{
				if (this.part.Modules[i] is ModulePartVariants)
				{
					Log.err("{0} can't be used together {1}", myself.Name, this.part.Modules[i].GetType().Name);
					this.active = this.enabled = false;
				}
				if (!foundme && this.part.Modules[i] is T)
					foundme = true;

				if (!foundme && this.part.Modules[i] is T)
					Log.warn("Ideally {0} should be declared on the Part config before {1}", myself.Name, this.part.Modules[i].moduleName);
			}
		}

		public override void OnStart(StartState state)
		{
			Log.dbg("{0}.OnStart({1})", this.GetType().Name, state);
			base.OnStart(state);

			this.targets.AddRange(this.part.Modules.GetModules<T>());
			if (0 == this.targets.Count)
			{
				Log.warn("Part {0} has no {1}! Disabling {2}...", this.part.partName, typeof(T).GetType().Name, this.GetType().Name);
				this.active = this.enabled = false;
				return;
			}
		}

		public sealed override void OnUpdate()
		{
			base.OnUpdate();
			this.updateDelegate();
		}

		protected void dummyUpdate() {}
	}
}
