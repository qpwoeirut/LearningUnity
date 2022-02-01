using UnityEngine;

namespace NonStandard.Character {
	public class CharacterInputLegacy : MonoBehaviour {
		Root r;
		CharacterCamera cc;
		void Start() {
			r = GetComponent<Root>();
			cc = CharacterCamera.FindCameraTargettingChildOf(r.transform);
			if (r == null) { enabled = false; Debug.LogWarning("Missing " + nameof(Move) + " on " + name); }
		}
		void Update() {
			UpdateCharacterMove(r);
			if (cc != null && Input.GetMouseButton(1)) {
				cc.ProcessLookRotation(new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")));
			}
		}
		public static void UpdateCharacterMove(Root r) {
			Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
			bool jump = Input.GetButton("Jump");
			r.MoveInput = input;
			r.JumpInput = jump ? 1 : 0;
		}
	}
}