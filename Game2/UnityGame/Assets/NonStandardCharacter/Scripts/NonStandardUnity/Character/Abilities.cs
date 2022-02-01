using UnityEngine;
using UnityEngine.Events;

namespace NonStandard.Character {
	// TODO list of abilities. abilities should have:
	//	callbacks for press, release, update
	//	an enum for dictionary access
	//	individual cooldown
	public class Abilities : MonoBehaviour {
		public float input;
		public float FireInput {
			get => input;
			set {
				if (input > 0) {
					if (value <= 0) { fireReleased?.Invoke(); }
				} else {
					if (value > 0) { firePressed?.Invoke(); }
				}
				input = value;
			}
		}
		[Tooltip("when player indicates the desire to 'fire'")]
		public UnityEvent firePressed;
		[Tooltip("when player indicates the desire to cease fire")]
		public UnityEvent fireReleased;

		private void Awake() {
			GetComponent<Root>().abilities = this;
		}
		public void InternalFixedUpdate(Root cm) {
			if (FireInput > 0) { FireInput -= Time.deltaTime; }
		}
	}
}
