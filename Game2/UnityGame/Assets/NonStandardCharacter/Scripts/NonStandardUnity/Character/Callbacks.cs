using UnityEngine;
using UnityEngine.Events;

namespace NonStandard.Character {
	public class Callbacks : MonoBehaviour {
		[System.Serializable] public class UnityEvent_Vector3 : UnityEvent<Vector3> { }
		[Tooltip("when player changes direction, passes the new direction")]
		public UnityEvent_Vector3 moveDirectionChanged;
		[Tooltip("when player changes their standing angle, passes the new ground normal")]
		public UnityEvent_Vector3 stand;
		[Tooltip("when player jumps, passes the direction of the jump")]
		public UnityEvent_Vector3 jumped;
		[Tooltip("when player starts to fall")]
		public UnityEvent fall;
		[Tooltip("when player collides with a wall, passes the wall's normal")]
		public UnityEvent_Vector3 wallCollisionStart;
		[Tooltip("when player is no longer colliding with a wall")]
		public UnityEvent wallCollisionStopped;
		[Tooltip("when auto-moving player reaches their goal, passes absolute location of the goal")]
		public UnityEvent_Vector3 arrived;
		private void Awake() {
			Root cm = GetComponent<Root>();
			cm.callbacks = this;
		}
		public void Initialize() {
			moveDirectionChanged = new UnityEvent_Vector3();
			stand = new UnityEvent_Vector3();
			jumped = new UnityEvent_Vector3();
			fall = new UnityEvent();
			wallCollisionStart = new UnityEvent_Vector3();
			wallCollisionStopped = new UnityEvent();
			arrived = new UnityEvent_Vector3();
		}
	}
}
