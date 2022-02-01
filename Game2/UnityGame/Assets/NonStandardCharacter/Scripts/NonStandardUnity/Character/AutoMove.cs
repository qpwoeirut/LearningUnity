using UnityEngine;

namespace NonStandard.Character {
	public class AutoMove : MonoBehaviour {
		public Vector3 targetPosition;
		public System.Action whatToDoWhenTargetIsReached;
		public float closeEnough;
		public bool jumpAtObstacle;
		public bool arrived;
		private void Awake() {
			Root r = GetComponent<Root>();
			r.automove = this;
		}
		public static bool GetClickedLocation(Camera camera, out Vector3 targetPosition) {
			Ray ray = camera.ScreenPointToRay(UnityEngine.Input.mousePosition);
			RaycastHit rh;// = new RaycastHit();
			if (Physics.Raycast(ray, out rh)) {
				targetPosition = rh.point;
				return true;
			}
			targetPosition = Vector3.zero;
			return false;
		}
		public Vector3 CalculateMoveDirection(Vector3 position, float speed, Vector3 upNormal, ref bool arrived) {
			if (arrived) return Vector3.zero;
			Vector3 delta = targetPosition - position;
			if (upNormal != Vector3.zero) {
				delta = Vector3.ProjectOnPlane(delta, upNormal);
			}
			float dist = delta.magnitude;
			if (dist <= closeEnough || dist <= closeEnough + Time.deltaTime * speed) {
				arrived = true;
				if (whatToDoWhenTargetIsReached != null) { whatToDoWhenTargetIsReached.Invoke(); }
				return Vector3.zero;
			}
			return delta / dist; // normalized vector indicating direciton
		}
		public void SetAutoMovePosition(Vector3 position, float closeEnough) {
			targetPosition = position;
			this.closeEnough = closeEnough;
			enabled = true;
			arrived = false;
		}
		public void DisableAutoMove() { enabled = false; }
	}
}
