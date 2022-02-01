using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace NonStandard.Character {
	public class CharacterCamera : MonoBehaviour {
		[Tooltip("which transform to follow with the camera")]
		public Transform _target;
		[Tooltip("if false, camera can pass through walls")]
		public bool clipAgainstWalls = true;

		/// <summary>how the camera should be rotated, calculated in Update, to keep LateUpdate as light as possible</summary>
		internal Quaternion targetRotation;
		/// <summary>how far the camera wants to be from the target</summary>
		public float targetDistance = 10;
		public float maxDistance = 50;
		/// <summary>calculate how far to clip the camera in the Update, to keep LateUpdate as light as possible
		protected float distanceBecauseOfObstacle;

		[System.Serializable] public class UnityEvent_Vector2 : UnityEvent<Vector2> { }
		[Tooltip("notified if the look rotation is changed, like from a mouse or joystick adjustment")]
		public UnityEvent_Vector2 OnLookInputChange;
		[Tooltip("notified if zoom is changed, like from a mouse or joystick adjustment")]
		public UnityEvent_Vector2 OnZoomInputChange;
		/// <summary>for fast access to transform</summary>
		internal Transform t;

		private Camera _camera;

		/// <summary>keep track of rotation, so it can be un-rotated and cleanly re-rotated</summary>
		private float pitch, yaw;

		public float maxVerticalAngle = 100, minVerticalAngle = -100;
		public Vector2 inputMultiplier = Vector2.one;

		public Camera Camera => _camera;
		public Transform target { get { return _target; } 
			set {
				//Debug.Log("target! "+Show.GetStack(10));
				if (_target == transform.parent) {
					transform.SetParent(value);
				}
				_target = value; 
			}
		}

		public Vector2 LookInput {
			get => new Vector2(horizontalRotateInput, verticalRotateInput);
			set {
				horizontalRotateInput = value.x;
				verticalRotateInput = value.y;
				OnLookInputChange?.Invoke(value);
			}
		}
		/// publicly accessible variables that can be modified by external scripts or UI
		[HideInInspector] public float horizontalRotateInput, verticalRotateInput, zoomInput;
		public float ZoomInput { get { return zoomInput; }
			set {
				zoomInput = value;
				OnZoomInputChange?.Invoke(new Vector2(0,value));
			}
		}
		public float DistanceBecauseOfObstacle { get => distanceBecauseOfObstacle; set => distanceBecauseOfObstacle = value; }
		public void AddToTargetDistance(float value) {
			targetDistance += value;
			if (targetDistance < 0) { targetDistance = 0; }
			if (targetDistance > maxDistance) { targetDistance = maxDistance; }
			OrthographicCameraDistanceChangeLogic();
		}
		public void OrthographicCameraDistanceChangeLogic() {
			if (_camera == null || !_camera.orthographic) { return; }
			if (targetDistance < 1f / 128) { targetDistance = 1f / 128; }
			if (targetDistance > maxDistance) { targetDistance = maxDistance; }
			_camera.orthographicSize = targetDistance / 2;
		}

		public bool IsTargettingChildOf(Transform targetRoot) {
			if (_target == null && targetRoot != null) return false;
			Transform t = _target;
			do {
				if (targetRoot == t) { return true; }
				t = t.parent;
			} while (t != null);
			return false;
        }

		/// <summary>
		/// does linear exhaustive search through all <see cref="CharacterCamera"/>s and looks at parent transform hierarchy
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public static CharacterCamera FindCameraTargettingChildOf(Transform t) {
			CharacterCamera[] ccs = FindObjectsOfType<CharacterCamera>();
			for(int i = 0; i < ccs.Length; ++i) {
				if (ccs[i].IsTargettingChildOf(t)) { return ccs[i]; }
            }
			return null;
        }

#if ENABLE_INPUT_SYSTEM
		public void ProcessLookRotation(InputAction.CallbackContext context) {
			ProcessLookRotation(context.ReadValue<Vector2>());
		}
		public void ProcessZoom(InputAction.CallbackContext context) {
			ProcessZoom(context.ReadValue<Vector2>());
		}
#endif
		public void ProcessLookRotation(Vector2 lookRot) {
			LookInput = lookRot;
		}
		public void ProcessZoom(Vector2 lookRot) {
			ZoomInput = lookRot.y;
		}
		public void ToggleOrthographic() { _camera.orthographic = !_camera.orthographic; }
		public void SetCameraOrthographic(bool orthographic) { _camera.orthographic = orthographic; }
	
#if UNITY_EDITOR
		/// called when created by Unity Editor
		void Reset() {
			if (target == null) {
				Root r = null;
				if (r == null) { r = transform.GetComponentInParent<Root>(); }
				if (r == null) { r = FindObjectOfType<Root>(); }
				if (r != null) { target = r.GetCameraTarget(); }
			}
		}
	#endif

		public void SetMouseCursorLock(bool a_lock) {
			Cursor.lockState = a_lock ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !a_lock;
		}
		public void LockCursor() { SetMouseCursorLock(true); }
		public void UnlockCursor() { SetMouseCursorLock(false); }

		public void Awake() { t = transform; }

		public void Start() {
			RecalculateDistance();
			RecalculateRotation();
			_camera = GetComponent<Camera>();
		}

		public bool RecalculateDistance() {
			float oldDist = targetDistance;
			if (target != null) {
				Vector3 delta = t.position - target.position;
				targetDistance = delta.magnitude;
			}
			return oldDist != targetDistance;
		}
		public bool RecalculateRotation() {
			float oldP = pitch, oldY = yaw;
			targetRotation = t.rotation;
			Vector3 right = Vector3.Cross(t.forward, Vector3.up);
			if(right == Vector3.zero) { right = -t.right; }
			Vector3 straightForward = Vector3.Cross(Vector3.up, right).normalized;
			pitch = Vector3.SignedAngle(straightForward, t.forward, -right);
			yaw = Vector3.SignedAngle(Vector3.forward, straightForward, Vector3.up);
			return oldP != pitch || oldY != yaw;
		}

		public void Update() {
			const float anglePerSecondMultiplier = 100;
			float rotH = horizontalRotateInput * anglePerSecondMultiplier * inputMultiplier.x * Time.unscaledDeltaTime,
				rotV = verticalRotateInput * anglePerSecondMultiplier * inputMultiplier.y * Time.unscaledDeltaTime,
				zoom = zoomInput * Time.unscaledDeltaTime;
			targetDistance -= zoom;
			if (zoom != 0) {
				if (targetDistance < 0) { targetDistance = 0; }
				if (targetDistance > maxDistance) { targetDistance = maxDistance; }
				if (target == null) {
					t.position += t.forward * zoom;
				}
				OrthographicCameraDistanceChangeLogic();
			}
			if (rotH != 0 || rotV != 0) {
				targetRotation = Quaternion.identity;
				yaw += rotH;
				pitch -= rotV;
				if (yaw < -180) { yaw += 360; }
				if (yaw >= 180) { yaw -= 360; }
				if (pitch < -180) { pitch += 360; }
				if (pitch >= 180) { pitch -= 360; }
				pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
				targetRotation *= Quaternion.Euler(pitch, yaw, 0);
			}
			if (target != null) {
				RaycastHit hitInfo;
				bool usuallyHitsTriggers = Physics.queriesHitTriggers;
				Physics.queriesHitTriggers = false;
				if (clipAgainstWalls && Physics.Raycast(target.position, -t.forward, out hitInfo, targetDistance)) {
					distanceBecauseOfObstacle = hitInfo.distance;
				} else {
					distanceBecauseOfObstacle = targetDistance;
				}
				Physics.queriesHitTriggers = usuallyHitsTriggers;
			}
		}
		public void LateUpdate() {
			t.rotation = targetRotation;
			if(target != null) {
				t.position = target.position - (t.rotation * Vector3.forward) * distanceBecauseOfObstacle;
			}
		}
	}
}
