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

namespace L_Aerospace.Lib
{
	/// <summary>
	/// To be used on PartModules that **dont** need to use Update or FixedUpdate before staging (if stageable at all)
	/// </summary>
	public abstract class AbstractPartModule : PartModule
	{
		protected bool hardActive = true;	// In Kraken, we trust! :)
		protected bool softActive { get; private set; }
		public bool Active
		{
			get => this.softActive && this.hardActive && this.enabled;
			internal protected set
			{
				this.enabled = this.isEnabled = value;
			}
		}

		#region KSP Life Cycle

		public sealed override void OnAwake()
		{
			Log.dbg("{0}:OnAwake", this.ID);
			base.OnAwake();
			this.DoAwake();
		}

		public sealed override void OnCopy(PartModule fromModule)
		{
			this.__ID = null;
			Log.dbg("{0}:OnCopy from {1:X}", this.ID, fromModule.part.GetInstanceID());
			base.OnCopy(fromModule);
			this.softActive = (fromModule as AbstractPartModule).softActive;
			this.DoCopy(fromModule);
		}

		public sealed override void OnLoad(ConfigNode node)
		{
			this.__ID = null;
			Log.dbg("{0}:OnLoad {1}", this.ID, null != node);
			base.OnLoad(node);
			{
				bool softActive = this.softActive;
				node.TryGetValue("active", ref softActive);
				this.softActive = softActive;
			}
			if (null == this.part.partInfo)
			{
				this.DoPrefabLoad(KSPe.ConfigNodeWithSteroids.from(node));
				return;
			}
			this.DoLoad(KSPe.ConfigNodeWithSteroids.from(node));
		}

		public sealed override void OnSave(ConfigNode node)
		{
			Log.dbg("{0}:OnSave {1}", this.ID, null != node);
			base.OnSave(node);
			node.SetValue("active", this.softActive, true);
			KSPe.ConfigNodeWithSteroids juicedNode = KSPe.ConfigNodeWithSteroids.from(node);
			this.DoSave(juicedNode);
		}

		public sealed override void OnStart(StartState state)
		{
			Log.dbg("{0}:OnStart.in {1} {2} {3} {4}", this.ID, state, this.enabled, this.hardActive, this.Active);
			base.OnStart(state);

			this.enabled = state > StartState.Editor;
			this.DoStart(state);

			Log.dbg("{0}:OnStart.out {1} {2}", this.ID, state, this.Active);
		}

		private string _getInfo = null;
		protected void ResetInfo() => this._getInfo = null;
		public sealed override string GetInfo()
		{
			if (!this.hardActive) return "Disabled.";
			return this._getInfo??(this._getInfo = this.DoGetInfo());
		}

		public sealed override void OnUpdate()
		{
			base.OnUpdate();
			if (!this.Active) return;

			this.DoUpdate();
		}

		public sealed override void OnFixedUpdate()
		{
			base.OnFixedUpdate();
			if (!this.Active) return;

			this.DoFixedUpdate();
		}

		#endregion

		#region My Internal Call Backs

		protected abstract void DoAwake();
		protected abstract void DoCopy(PartModule fromModule);
		protected abstract void DoLoad(KSPe.ConfigNodeWithSteroids node);
		protected abstract void DoPrefabLoad(KSPe.ConfigNodeWithSteroids node);
		protected abstract void DoSave(KSPe.ConfigNodeWithSteroids node);
		protected abstract void DoStart(StartState state);
		protected abstract string DoGetInfo();
		protected abstract void DoUpdate();
		protected abstract void DoFixedUpdate();

		#endregion

		private String __ID = null;
		public String ID => __ID??(__ID = String.Format("{0}:{1:X}", this.name, this.part.GetInstanceID()));
		protected readonly KSPe.Util.Log.Logger Log;
		protected abstract KSPe.Util.Log.Logger GetLogger();
		protected AbstractPartModule() : base() => this.Log = this.GetLogger();
	}
}
