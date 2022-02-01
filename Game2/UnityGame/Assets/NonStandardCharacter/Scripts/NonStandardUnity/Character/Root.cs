using UnityEngine;
using UnityEngine.InputSystem;

namespace NonStandard.Character {
	/// <summary>
	/// a facade class to control all of the subcomponents of a character
	/// </summary>
	public class Root : MonoBehaviour {
		[Tooltip("Intended for defining information about this character")]
		public Object data;
		[HideInInspector] public Transform rootTransform;
		[HideInInspector] public Rigidbody rb;
		public Transform head;
		public Body body;
		public Abilities abilities;
		public Jump jump;
		public Move move;
		public AutoMove automove;
		public Callbacks callbacks;
#if UNITY_EDITOR
		private void OnValidate() {
			UnityEditorRefreshExpectedComponents();
		}
		public void UnityEditorRefreshExpectedComponents() {
			if (rootTransform == null) { rootTransform = transform; }
			if (body == null) { body = GetComponent<Body>(); }
			if (head == null && body != null) { head = body.head; }
			if (rb == null) { rb = rootTransform.GetComponent<Rigidbody>(); }
			if (abilities == null) { abilities = GetComponent<Abilities>(); }
			if (jump == null) { jump = GetComponent<Jump>(); }
			if (move == null) { move = GetComponent<Move>(); }
			if (automove == null) { automove = GetComponent<AutoMove>(); }
			if (callbacks == null) { callbacks = GetComponent<Callbacks>(); }
		}
#endif
		public void Init() {
			rootTransform = transform;
			rb = GetComponent<Rigidbody>();
			if (move == null) { move = GetComponentInChildren<Move>(); }
		}
		public void Awake() { Init(); }
		public void Start() { Init(); }
		public Transform GetCameraTarget() { return head != null ? head : rootTransform; }
		public void TakeControlOfUserInterface() {
			UserController user = UserController.GetUserCharacterController();
			user.RelinquishCharacter();
			user.AbsorbCharacter(this);
		}
		/// <summary>
		/// how many seconds to hold down the fire button. if a non-zero value, a fire impulse will be applied. if zero, stop firing.
		/// </summary>
		public float FireInput {
			get => abilities ? abilities.FireInput : 0;
			set {
				if (abilities) {
					abilities.FireInput = value;
				} else { Debug.LogWarning("receiving FireInput, but missing "+nameof(Abilities)+" component"); }
			}
		}
		public void SetFireAction(InputAction.CallbackContext context) {
			switch (context.phase) {
				case InputActionPhase.Started: FireInput = float.PositiveInfinity; break;
				case InputActionPhase.Canceled: FireInput = 0; break;
			}
		}
		/// <summary>
		/// how many seconds to hold down the jump button. if a non-zero value, a jump impulse will be applied. if zero, stop jumping.
		/// </summary>
		public float JumpInput {
			get => jump?jump.JumpInput:0;
			set {
				if (jump) {
					jump.JumpInput = value;
				}
			}
		}
		public void SetJump(InputAction.CallbackContext context) {
			switch (context.phase) {
				case InputActionPhase.Started: JumpInput = float.PositiveInfinity; break;
				case InputActionPhase.Canceled: JumpInput = 0; break;
			}
		}
		public Vector2 MoveInput {
			get => new Vector2(move.StrafeRightMovement, move.MoveForwardMovement);
			set {
				move.StrafeRightMovement = value.x;
				move.MoveForwardMovement = value.y;
			}
		}
		public void SetMove(InputAction.CallbackContext context) {
			MoveInput = context.ReadValue<Vector2>();
		}
		private void Update() {
			if (move.systemMovement && move.enabled && Time.timeScale == 0) { move.InternalFixedUpdate(this); }
		}
		void FixedUpdate() {
			if (jump && jump.enabled) { jump.InternalEarlyUpdate(); }
			if (abilities != null && abilities.enabled) { abilities.InternalFixedUpdate(this); }
			if (move && move.enabled) { move.InternalFixedUpdate(this); }
			if (jump && jump.enabled) { jump.InternalFixedUpdate(this); }
			if (move && move.enabled) { move.InternalLateUpdate(this); }
		}
		private void OnCollisionStay(Collision collision) {
			if (collision.impulse != Vector3.zero && move.MoveDirection != Vector3.zero && Vector3.Dot(collision.impulse.normalized, move.MoveDirection) < -.75f) {
				rb.velocity = move.LastVelocity; // on a real collision, very much intentionally against a wall, maintain velocity
			}
			int contactThatMakesStability = move.CollisionStabilityCheck(this, collision);
			if (contactThatMakesStability >= 0) {
				Vector3 standingNormal = collision.contacts[contactThatMakesStability].normal;
				if (standingNormal != move.GroundNormal) {
					callbacks?.stand?.Invoke(standingNormal);
				}
				move.GroundNormal = standingNormal;
			}
		}
		private void OnCollisionEnter(Collision collision) {
			if (collision.impulse != Vector3.zero && move.CollisionStabilityCheck(this, collision) < 0) {
				rb.velocity = move.LastVelocity; // on a real collision, where the player is unstable, maintain velocity
			}
			jump?.Interrupt(); //jump.collided = true;
		}
	}
}
