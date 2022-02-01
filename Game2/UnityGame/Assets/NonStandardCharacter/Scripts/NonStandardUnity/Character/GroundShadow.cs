using UnityEngine;

namespace NonStandard.Character {
	public class GroundShadow : MonoBehaviour {
		public Transform shadowSource;
		public Vector3 direction = Vector3.down;
		public float maxShadowDistance = 100;
		public void LateUpdate() {
			UpdateGroundShadow();
		}
		public void UpdateGroundShadow() {
			if (Physics.Raycast(shadowSource.position, direction, out RaycastHit rh, maxShadowDistance)) {
				transform.position = rh.point;
				if (rh.normal != -direction) {
					Vector3 r = Vector3.Cross(rh.normal, Vector3.up);
					Vector3 f = Vector3.Cross(rh.normal, r);
					transform.rotation = Quaternion.LookRotation(f, rh.normal);
				} else {
					transform.rotation = Quaternion.identity;
				}
				//groundShadow.SetActive(true);
			} else {
				//groundShadow.SetActive(false);
			}
		}
	}
}
