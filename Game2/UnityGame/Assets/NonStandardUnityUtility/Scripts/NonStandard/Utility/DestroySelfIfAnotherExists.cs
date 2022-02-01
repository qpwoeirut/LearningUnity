using UnityEngine;

namespace NonStandard.Utility {
	public class DestroySelfIfAnotherExists : MonoBehaviour {
		public Object targetKind;
		void Start() {
			Object[] objects = FindObjectsOfType(targetKind.GetType());
			bool foundAnotherOne = false;
			for (int i = 0; i < objects.Length; ++i) {
				if (objects[i] != targetKind) {
					foundAnotherOne = true;
					break;
				}
			}
			if (foundAnotherOne) {
				Destroy(gameObject);
			}
		}
	}
}
