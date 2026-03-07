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
#if DEBUG
using System.Linq;
#endif

namespace L_Aerospace.Controller.AnimateGeneric
{
	public class AnimateGenericController : AbstractController<ModuleAnimateGeneric>
	{
		[UI_Toggle (disabledText = "Off", scene = UI_Scene.All, enabledText = "On", affectSymCounterparts = UI_Scene.All)]
		[KSPField (isPersistant = true, guiActive = true, guiActiveEditor = true)]
		public bool Active;

		[KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false)]
		protected string AnimationTargetNames = "";
		protected readonly List<string> animationTargetNames = new List<string>();
		private readonly List<ModuleAnimateGeneric> animatingTargets = new List<ModuleAnimateGeneric>();

		[KSPField (isPersistant = true, guiActive = false, guiActiveEditor = false)]
		protected string TriggerTargetNames = "";
		protected readonly List<string> triggerTargetNames = new List<string>();
		private readonly List<ModuleAnimateGeneric> triggerTargets = new List<ModuleAnimateGeneric>();

		#region PartModule life cycle

		public override void OnAwake()
		{
			base.OnAwake();
			{
				BaseField bf = this.Fields["Active"];
				bf.OnValueModified += this.OnActiveModified;
			}
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
			//{ 
			//	KSPe.ConfigNodeWithSteroids n = KSPe.ConfigNodeWithSteroids.from(node);
			//	Log.dbg("OnLoad {0}", n);
			//	this.animationTargetNames.AddRange(n.GetArrayOf<string>("animationTargetNames"));
			//	this.triggerTargetNames.AddRange(n.GetArrayOf<string>("triggerTargetNames"));
			//}
			{
				foreach (string s in this.AnimationTargetNames.Split(','))
					this.animationTargetNames.Add(s.Trim());
				foreach (string s in this.TriggerTargetNames.Split(','))
					this.triggerTargetNames.Add(s.Trim());
			}
		}

		public override void OnSave(ConfigNode node)
		{
			base.OnLoad(node);
			//{ 
			//	KSPe.ConfigNodeWithSteroids n = KSPe.ConfigNodeWithSteroids.from(node);
			//	Log.dbg("OnSave {0}", n);
			//	n.SetArrayOf("animationTargetNames", this.animationTargetNames.ToArray());
			//	n.SetArrayOf("triggerTargetNames", this.triggerTargetNames.ToArray());
			//	n.Commit();
			//}
		}

		public override void OnStart(StartState state)
		{
			if (StartState.None == state) return; // Do nothing on Loading.
			base.OnStart(state);
			this.setupMyTargets();
			this.setupMe();
		}

	#endregion

		private void OnActiveModified(object arg1)
		{
			this.setupMe();
		}

		private void setupMe()
		{
			if (this.Active)	this.updateDelegate = this.startAnimations;
			else				this.updateDelegate = this.finishAnimations;
		}

		private void setupMyTargets()
		{
			this.build(this.animationTargetNames, this.animatingTargets);
			this.build(this.triggerTargetNames, this.triggerTargets);

			for (int i = 0 ; i < this.targets.Count; ++i)
			{
				{ 
					BaseField bf = this.targets[i].Fields["status"];
					bf.guiActive = false;
					bf.guiActiveEditor = false;
				}
				{
					BaseEvent a = this.targets[i].Events["Toggle"];
					a.guiActive = false;
					a.guiActiveEditor = false;
				}
			}

			for (int i = 0 ; i < this.animatingTargets.Count; ++i)
			{
				this.targets[i].isOneShot = false;
			}

			for (int i = 0 ; i < this.triggerTargets.Count; ++i)
			{

			}
		}

		private void build(List<string> targetNames, List<ModuleAnimateGeneric> targets)
		{
			targets.Clear();
			foreach (ModuleAnimateGeneric m in this.targets) if (targetNames.Contains(m.animationName))
				targets.Add(m);
		#if DEBUG
			Log.dbg("build {0} {1}", string.Join(",", targetNames.ToArray()), string.Join(",", (from t in targets select t.animationName).ToArray()));
		#endif
		}

		private void startAnimations()
		{
			Log.dbg("startAnimations");
			for (int i = 0 ; i < this.triggerTargets.Count; ++i)
			{
				ModuleAnimateGeneric m = this.targets[i];
				if (!m.enabled) continue;
				if (m.IsMoving()) continue;
				if (1 == m.animTime) continue;
				this.ExecuteAsCoroutine(() => m.Toggle() );
			}

			for (int i = 0 ; i < this.animatingTargets.Count; ++i)
			{
				ModuleAnimateGeneric m = this.targets[i];
				if (!m.enabled) continue;
				if (m.IsMoving()) continue;
				this.ExecuteAsCoroutine(() => m.Toggle() );
			}

			this.updateDelegate = this.updateAnimation;
		}

		private void updateAnimation()
		{
			Log.dbg("updateAnimation");
			for (int i = 0 ; i < this.animatingTargets.Count; ++i)
			{
				ModuleAnimateGeneric m = this.targets[i];
				Log.dbg("updateAnimation {0} {1} {2}", m.name, m.enabled, m.aniState);
				if (!m.enabled) continue;
				if (ModuleAnimateGeneric.animationStates.MOVING == m.aniState) continue;
				if (1.0 == m.animTime)
				{
					m.animSpeed = Math.Abs(m.animSpeed);
					this.ExecuteAsCoroutine(() => m.Toggle() );
				}
			}
		}

		private void finishAnimations()
		{
			Log.dbg("finishAnimations");
			for (int i = 0 ; i < this.triggerTargets.Count; ++i)
			{
				ModuleAnimateGeneric m = this.targets[i];
				if (!m.enabled) continue;
				if (0 == m.animTime) continue;
				if (ModuleAnimateGeneric.animationStates.MOVING == m.aniState) continue;
				this.ExecuteAsCoroutine(() => m.Toggle() );
			}

			this.updateDelegate = this.dummyUpdate;
		}
	}
}
