using UnityEngine;

namespace NonStandard.Utility {
	public class TimedDestroy : MonoBehaviour {
		public float lifetime = 3;
		void Start() {
			if (lifetime >= 0) {
				Event.Wait(lifetime, DestroySelf, this);//Destroy(gameObject, lifetime);
			}
		}
		public void DestroySelf() { Destroy(gameObject); }
	}
}
