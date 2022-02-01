using UnityEngine;
using UnityEngine.Events;

namespace NonStandard.Utility {
	public class OnDestroyed : MonoBehaviour {
		[System.Serializable] public class UnityEvent_Transform : UnityEvent<Transform> { }
		public UnityEvent_Transform onDestroy;
		public Object argument;

		private void OnDestroy() {
			if (Global.IsQuitting) return;
			onDestroy?.Invoke(transform);
		}

		public void Replace(Transform t) {
			Instantiate(argument as GameObject, t.position, t.rotation);
		}
	}
}
