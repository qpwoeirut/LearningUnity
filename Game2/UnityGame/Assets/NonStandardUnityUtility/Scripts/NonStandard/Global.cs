using System.Collections.Generic;
using UnityEngine;

namespace NonStandard {
	/// <summary>
	/// objects with this monobehavior on it will be given preference when searching for global components (sorted alphabetically)
	/// </summary>
	public partial class Global : MonoBehaviour {
		private static Global _instance;
		private static List<Global> globs = new List<Global>();
		public static bool IsQuitting { get; private set; }
		public static Global Instance() {
			if (_instance) { return _instance; }
			Global[] found = FindObjectsOfType<Global>(true);
			const string DefaultGlobalName = "<global>";
			if (found.Length > 0) {
				globs.AddRange(found);
				globs.Sort((a, b) => a.name.CompareTo(b.name));
				_instance = globs.Find(g => g.name == DefaultGlobalName);
				_instance = found[0];
			}
			if (!_instance) { _instance = new GameObject(DefaultGlobalName).AddComponent<Global>(); }
			return _instance;
		}
		public static GameObject Get() { return Instance().gameObject; }
		//public static T Get<T>(bool includeInactive = true) where T : Component {
		//	return null;
		//}
		public static T GetComponent<T>(bool includeInactive = true) where T : Component {
			T found = Get<T>(null);
			if (found != null) return found;
			T componentInstance = Instance().GetComponentInChildren<T>(includeInactive);
			if (componentInstance == null) {
				for(int i = 0; i < globs.Count; ++i) {
					Global g = globs[i];
					if (g == null) { globs.RemoveAt(i--); continue; }
					componentInstance = g.GetComponentInChildren<T>(includeInactive);
					if (componentInstance) { break; }
				}
			}
			if (componentInstance == null) { componentInstance = _instance.gameObject.AddComponent<T>(); }
			directory[typeof(T)] = componentInstance;
			return componentInstance;
		}
		public void TogglePause() { LifeCycle p = LifeCycle.Instance; if(p.isPaused) { p.Unpause(); } else { p.Pause(); } }
		public void Exit() => LifeCycle.Exit();
		public void ToggleActive(GameObject go) {
			if (go != null) {
				go.SetActive(!go.activeSelf);
				//Debug.Log(go+" "+go.activeInHierarchy);
			}
		}
		public void ToggleEnabled(MonoBehaviour m) { if (m != null) { m.enabled = !m.enabled; } }
		void Start() {
			Instance();
			if (globs.IndexOf(this) < 0) { globs.Add(this); }
			Component[] components = GetComponents<Component>();
			for(int i = 0; i < components.Length; ++i) {
				Component c = components[i];
				if (c == null) { continue; }
				System.Type t = c.GetType();
				if (!directory.TryGetValue(t, out object foundIt)) {
					directory[t] = c;
				}
			}
		}
		void OnApplicationQuit() {
			if (IsQuitting) { return; }
			IsQuitting = true;
			string c = "color", a = "#84f", b = "#48f";
			Debug.Log("<" + c + "=" + a + ">"+nameof(Global)+"</" + c + ">.IsQuitting = <" + c + "=" + b + ">true</" + c + ">;");
		}
		private void OnDestroy() {
			if (IsQuitting) { return; }
			globs.Remove(this);
		}
	}
}