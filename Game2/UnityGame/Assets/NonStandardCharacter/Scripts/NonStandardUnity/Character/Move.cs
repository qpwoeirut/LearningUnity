using UnityEngine;

namespace NonStandard.Character {
	[RequireComponent(typeof(Rigidbody))]
	public class Move : MonoBehaviour {
		private CapsuleCollider capsule;
		[HideInInspector] public Root root;

		public float speed = 5;
		[Tooltip("anything steeper than this cannot be moved on")]
		public float maxStableAngle = 60;
		[Tooltip("Set this to enable movement based on how a camera is looking")]
		public Transform cameraTransform;

		[Tooltip("If true, keys must be held while jumping to move, and also, direction can be changed in air.")]
		public bool canMoveInAir;
		public bool lookForwardMoving = true;
		public bool maintainSpeedAgainstWall;
		[Tooltip("Force movement, ignoring physics system. Allows movement during paused game.")]
		public bool systemMovement;
		protected bool isStableOnGround;
		protected float strafeRightMovement;
		protected float moveForwardMovement;

		[HideInInspector] public Vector3 MoveDirection;
		[HideInInspector] public Vector3 GroundNormal;
		[HideInInspector] public Vector3 OppositionDirection;
		[HideInInspector] public Vector3 LastVelocity;
		[HideInInspector] public Vector3 LastOppositionDirection;

		public static ulong Now => (ulong)System.Environment.TickCount;
		public float MoveSpeed { get { return speed; } set { speed = value; } }
		public float StrafeRightMovement { get { return strafeRightMovement; } set { strafeRightMovement = value; } }
		public float MoveForwardMovement { get { return moveForwardMovement; } set { moveForwardMovement = value; } }
		public bool IsStableOnGround { get => isStableOnGround; internal set => isStableOnGround = value; }
		public bool IsAutoMoving => root.automove != null && root.automove.enabled;
		private void Awake() {
			root = GetComponent<Root>();
			root.move = this;
			capsule = GetComponentInChildren<CapsuleCollider>();
		}
		void Start() {
			root.rb.freezeRotation = true;
		}
		Vector3 ConvertIntentionToRealDirection(Vector3 intention, Transform playerTransform, out float speed, bool onGround) {
			speed = intention.magnitude;
			if (cameraTransform) {
				intention = cameraTransform.TransformDirection(intention);
				if (!onGround) { return intention / speed; }
				Vector3 lookForward = cameraTransform.forward;
				Vector3 lookRight = cameraTransform.right;
				Vector3 groundNormal = Vector3.up;
				Vector3 groundForward = Vector3.ProjectOnPlane(lookForward, groundNormal);
				if (groundForward == Vector3.zero) { groundForward = cameraTransform.up; }
				else { groundForward.Normalize(); }
				float a = Vector3.SignedAngle(groundForward, lookForward, lookRight);
				intention = Quaternion.AngleAxis(-a, lookRight) * intention;
			} else {
				intention = playerTransform.transform.TransformDirection(intention);
			}
			intention /= speed;
			return intention;
		}
		public Vector3 AccountForBlocks(Vector3 moveVelocity) {
			if (OppositionDirection != Vector3.zero) {
				float opposition = -Vector3.Dot(MoveDirection, OppositionDirection);
				if (opposition > 0) {
					float s = speed;
					if (maintainSpeedAgainstWall) { s = moveVelocity.magnitude; }
					moveVelocity += opposition * OppositionDirection;
					if (maintainSpeedAgainstWall) { moveVelocity.Normalize(); moveVelocity *= s; }
				}
			}
			return moveVelocity;
		}
		public void ApplyMoveFromInput(Root r) {
			Vector3 moveVelocity = Vector3.zero;
			Transform t = r.rootTransform;
			Vector3 oldDirection = MoveDirection;
			MoveDirection = new Vector3(strafeRightMovement, 0, moveForwardMovement);
			float intendedSpeed = 1;
			if (MoveDirection != Vector3.zero) {
				MoveDirection = ConvertIntentionToRealDirection(MoveDirection, t, out intendedSpeed, r.rb.useGravity);
				if (intendedSpeed > 1) { intendedSpeed = 1; }
				// else { Debug.Log(intendedSpeed); }
			}
			if (r.automove != null && r.automove.enabled) {
				if (MoveDirection == Vector3.zero) {
					if (!r.automove.arrived) {
						MoveDirection = r.automove.CalculateMoveDirection(t.position, speed * intendedSpeed, Vector3.up, ref r.automove.arrived);
						if (r.automove.arrived) { r.callbacks?.arrived?.Invoke(r.automove.targetPosition); }
					}
				} else {
					r.automove.arrived = true; // if the player is providing input, stop calculating automatic movement
				}
			}
			if (MoveDirection != Vector3.zero) {
				moveVelocity = AccountForBlocks(MoveDirection);
				// apply the direction-adjusted movement to the velocity
				moveVelocity *= (speed * intendedSpeed);
			}
			if (MoveDirection != oldDirection) { r.callbacks?.moveDirectionChanged?.Invoke(MoveDirection); }
			float gravity = r.rb.velocity.y; // get current gravity
			if (r.rb.useGravity) {
				moveVelocity.y = gravity; // apply to new velocity
			}
			if(lookForwardMoving && MoveDirection != Vector3.zero && cameraTransform != null)
			{
				r.rootTransform.rotation = Quaternion.LookRotation(MoveDirection, Vector3.up);
				if(r.head != null && r.head != r.transform) { r.head.localRotation = Quaternion.identity; } // turn head straight while walking
			}
			if (!systemMovement) {
				r.rb.velocity = moveVelocity;
			} else {
				r.rootTransform.position += moveVelocity * Time.unscaledDeltaTime;
			}
			LastVelocity = moveVelocity;
			if(OppositionDirection == Vector3.zero && LastOppositionDirection != Vector3.zero)
			{
				r.callbacks?.wallCollisionStopped?.Invoke(); // done colliding
				LastOppositionDirection = Vector3.zero;
			}
			OppositionDirection = Vector3.zero;
		}
		public void InternalFixedUpdate(Root r) {
			if (IsStableOnGround || canMoveInAir) {
				if(IsStableOnGround && r.jump != null) {
					r.jump.MarkStableJumpPoint(r.rootTransform.position); 
				}
				ApplyMoveFromInput(r);
			}
		}
		public void InternalLateUpdate(Root r) {
			if (!IsStableOnGround && (r.jump == null || !r.jump.isJumping) && GroundNormal != Vector3.zero) {
				GroundNormal = Vector3.zero;
				r.callbacks?.fall?.Invoke();
			}
			IsStableOnGround = false; // invalidate stability *after* jump state is calculated
		}
		public void SetAutoMovePosition(Vector3 position, float closeEnough = 0, System.Action whatToDoWhenTargetIsReached = null) {
			if (root.automove == null) {
				root.automove = root.gameObject.AddComponent<AutoMove>();
			}
			root.automove.SetAutoMovePosition(position, closeEnough);
			root.automove.whatToDoWhenTargetIsReached = whatToDoWhenTargetIsReached;
		}
		public void DisableAutoMove() {
			if (root.automove == null) { return; }
			root.automove.DisableAutoMove();
			MoveForwardMovement = StrafeRightMovement = 0;
			MoveDirection = OppositionDirection;
		}
		public void CalculateLocalCapsule(out Vector3 top, out Vector3 bottom, out float rad) {
			float h = capsule.height / 2f;
			Vector3 r = Vector3.zero;
			switch (capsule.direction) {
				case 0: r = new Vector3(h, 0); break;
				case 1: r = new Vector3(0, h); break;
				case 2: r = new Vector3(0, 0, h); break;
			}
			top = capsule.center + r;
			bottom = capsule.center - r;
			top = root.rootTransform.rotation * top;
			bottom = root.rootTransform.rotation * bottom;
			rad = capsule.radius;
		}

		/// <param name="cm"></param>
		/// <param name="collision"></param>
		/// <returns>the index of collision that could cause stability</returns>
		public int CollisionStabilityCheck(Root r, Collision collision) {
			float biggestOpposition = -Vector3.Dot(MoveDirection, OppositionDirection);
			int stableIndex = -1, wallCollisions = -1;
			Vector3 standingNormal = Vector3.zero;
			// identify that the character is on the ground if it's colliding with something that is angled like ground
			for (int i = 0; i < collision.contacts.Length; ++i) {
				Vector3 surfaceNormal = collision.contacts[i].normal;
				float a = Vector3.Angle(Vector3.up, surfaceNormal);
				if (a <= maxStableAngle) {
					IsStableOnGround = true;
					stableIndex = i;
					standingNormal = surfaceNormal;
				} else {
					float opposition = -Vector3.Dot(MoveDirection, surfaceNormal);
					if(opposition > biggestOpposition) {
						biggestOpposition = opposition;
						wallCollisions = i;
						OppositionDirection = surfaceNormal;
					}
					if(r.automove != null && r.jump != null && r.automove.jumpAtObstacle) {
						r.jump.TimedJumpPress = r.jump.fullPressDuration;
					}
				}
			}
			if(wallCollisions != -1) {
				if (LastOppositionDirection != OppositionDirection) {
					r.callbacks?.wallCollisionStart?.Invoke(OppositionDirection);
				}
				LastOppositionDirection = OppositionDirection;
			}
			return stableIndex;
		}
	}
}
