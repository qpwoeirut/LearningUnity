using UnityEngine;
using UnityEngine.Events;

namespace NonStandard.Utility {
	public class OnActiveChange : MonoBehaviour {
		public UnityEvent onEnable, onDisable;
		void OnEnable() { onEnable.Invoke(); }
		void OnDisable() { onDisable.Invoke(); }
	}
}