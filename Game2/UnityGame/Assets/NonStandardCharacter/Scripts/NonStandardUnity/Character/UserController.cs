using NonStandard.Inputs;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace NonStandard.Character {
	public class UserController : MonoBehaviour {
		[Tooltip("What character is being controlled (right-click to add default controls)")]
		[ContextMenuItem("Add default user controls", "CreateDefaultUserControls")]
		[SerializeField] protected Root target;
		protected Root lastTarget;
		[Tooltip("What camera is being controlled")]
		[SerializeField] protected CharacterCamera _camera;
		InputActionMap mouselookActionMap;
		public UnityEvent_Vector2 onMoveInput;
		public UnityEvent_float onJumpPowerProgress;
		public UnityEvent_float onJumpCountProgress;
		[System.Serializable] public class UnityEvent_Vector2 : UnityEvent<Vector2> { }
		[System.Serializable] public class UnityEvent_float : UnityEvent<float> { }
		public Transform MoveTransform {
			get { return target != null ? target.transform : null; }
			set {
				Target = value.GetComponent<Root>();
			}
		}
		public CharacterCamera CharacterCamera { get => _camera; set => _camera = value; }
		public Root Target {
			get { return target; }
			set {
				target = value;
				transform.SetParent(MoveTransform);
				transform.localPosition = Vector3.zero;
				transform.localRotation = Quaternion.identity;
			}
		}
		public float JumpInput {
			get { return target && target.jump ? target.JumpInput : 0; }
			set { if (target && target.jump) target.JumpInput = value; }
		}
		public float FireInput {
			get { return target && target.abilities ? target.abilities.FireInput : 0; }
			set {
				if (target && target.abilities) {
					target.abilities.FireInput = value;
				}
				else { Debug.LogWarning("missing "+nameof(Abilities)); }
			}
		}
		public float MoveSpeed {
			get { return target != null ? target.move.MoveSpeed : 0; }
			set { if (target != null) target.move.MoveSpeed = value; }
		}
		public float JumpHeight {
			get { return target != null ? target.jump.JumpHeight : 0; }
			set { if (target != null) target.jump.JumpHeight = value; }
		}
		public Vector2 MoveInput {
			get => new Vector2(target.move.StrafeRightMovement, target.move.MoveForwardMovement);
			set {
				target.move.StrafeRightMovement = value.x;
				target.move.MoveForwardMovement = value.y;
				onMoveInput?.Invoke(value);
			}
		}
		public void NotifyJumpPowerProgress(float power) {
			onJumpPowerProgress?.Invoke(power);
		}
		public void NotifyJumpCountProgress(float progress) {
			onJumpCountProgress?.Invoke(progress);
		}
		public bool IsAutoMoving => target != null && target.move != null && target.move.IsAutoMoving;
		public void SetAutoMovePosition(Vector3 position, float closeEnough = 0, System.Action whatToDoWhenTargetIsReached = null) {
			if (target != null) { target.move.SetAutoMovePosition(position, closeEnough, whatToDoWhenTargetIsReached); }
		}
		public void DisableAutoMove() { if (target != null) target.move.DisableAutoMove(); }
		public float GetJumpProgress() { return target != null ? target.jump.GetJumpProgress() : 0; }
		public bool IsStableOnGround() { return target != null ? target.move.IsStableOnGround : false; }
#if ENABLE_INPUT_SYSTEM
		public void SetMove(InputAction.CallbackContext context) {
			MoveInput = context.ReadValue<Vector2>();
		}
		public void SetJump(InputAction.CallbackContext context) {
			switch (context.phase) {
				case InputActionPhase.Started: JumpInput = float.PositiveInfinity; break;
				case InputActionPhase.Canceled: JumpInput = 0; break;
			}
		}
		public void SetFire(InputAction.CallbackContext context) {
			// ignore mouse when over UI
			if (context.control.path.StartsWith("/Mouse/") && UserInput.IsMouseOverUIObject()) { return; }
			switch (context.phase) {
				case InputActionPhase.Started: FireInput = float.PositiveInfinity; break;
				case InputActionPhase.Canceled: FireInput = 0; break;
			}
		}
		public void NotifyCameraRotation(InputAction.CallbackContext context) {
			_camera?.ProcessLookRotation(context);
		}
		public void NotifyCameraZoom(InputAction.CallbackContext context) {
			if (context.control.path.StartsWith("/Mouse/") && UserInput.IsMouseOverUIObject()) { return; }
			_camera?.ProcessZoom(context);
		}
		public Transform GetCameraTarget() {
			if (target.move != null && target.head != null) {
				return target.head;
			}
			return target.transform;
		}
		const string n_Player = "Player", n_MouseLook = "MouseLook", n_Move = "Move", n_Jump = "Jump", n_Look = "Look", n_Zoom = "Zoom", n_Fire = "Fire", n_ToggleML = "Toggle MouseLook";
		const string n_InputActionAsset = "FpsCharacterController", n_InputActionPath = "Assets/Resources";
#if UNITY_EDITOR
		public void CreateDefaultUserControls() {
			_camera = GetComponentInChildren<CharacterCamera>();
			if (_camera == null) { _camera = GetComponentInParent<CharacterCamera>(); }
			if (_camera == null && transform.parent) { _camera = transform.parent.GetComponentInChildren<CharacterCamera>(); }
			UserInput userInput = GetComponent<UserInput>();
			bool pleaseCreateInputActionAsset = false;
			if (userInput == null) {
				userInput = gameObject.AddComponent<UserInput>();
			}
			if (userInput == null) { userInput = gameObject.AddComponent<UserInput>(); }
			if (userInput.inputActionAsset == null) {
				pleaseCreateInputActionAsset = true;
				userInput.inputActionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
			}
			InputControlBinding[] bindings = new InputControlBinding[] {
				new InputControlBinding("move with player", n_Player+"/"+n_Move,    ControlType.Vector2, new EventBind(this, nameof(this.SetMove)), new string[] {"<Gamepad>/leftStick", "<XRController>/{Primary2DAxis}", "<Joystick>/stick",
					InputControlBinding.CompositePrefix+"WASD:"+"Up:<Keyboard>/w,<Keyboard>/upArrow;Down:<Keyboard>/s,<Keyboard>/downArrow;Left:<Keyboard>/a,<Keyboard>/leftArrow;Right:<Keyboard>/d,<Keyboard>/rightArrow"}),
				new InputControlBinding("jump with player", n_Player+"/"+n_Jump,    ControlType.Button,  new EventBind(this, nameof(this.SetJump)), new string[] {"<Keyboard>/space","<Gamepad>/buttonSouth"}),
				new InputControlBinding("shoot with player", n_Player + "/" + n_Fire,ControlType.Button,  new EventBind(this, nameof(this.SetFire)), new string[] {
				"<Gamepad>/rightTrigger","<Mouse>/leftButton","<Touchscreen>/primaryTouch/tap","<Joystick>/trigger","<XRController>/{PrimaryAction}","<Gamepad>/buttonWest"}),
				new InputControlBinding("toggle mouse look", n_Player+"/"+n_ToggleML,ControlType.Button,  new EventBind(this, nameof(BindMouselookInputMapToButton)), new string[] { "<Mouse>/rightButton" }),
				new InputControlBinding("rotate camera", n_Player+"/"+n_Look,    ControlType.Vector2, new EventBind(this, nameof(NotifyCameraRotation)), new string[] { "<Gamepad>/rightStick", "<Joystick>/{Hatswitch}" }),
				new InputControlBinding("\"mouse look\" rotate camera with mouse", n_MouseLook+"/"+n_Look, ControlType.Vector2, new EventBind(this, nameof(NotifyCameraRotation)), new string[] { "<VirtualMouse>/delta", "<Pointer>/delta", "<Mouse>/delta" }),
				new InputControlBinding("zoom camera", n_Player+"/"+n_Zoom,    ControlType.Vector2, new EventBind(this, nameof(NotifyCameraZoom)), new string[] { "<Mouse>/scroll" }),
			};
			foreach(InputControlBinding b in bindings) {
				userInput.AddBinding(b);
			}
			userInput.actionMapToBindOnStart = new string[] { n_Player };
			if (pleaseCreateInputActionAsset) {
				userInput.inputActionAsset.name = n_InputActionPath;
				userInput.inputActionAsset = ScriptableObjectUtility.SaveScriptableObjectAsAsset(userInput.inputActionAsset,
					n_InputActionAsset + "." + InputActionAsset.Extension, n_InputActionPath, userInput.inputActionAsset.ToJson()) as InputActionAsset;
			}
		}
#endif
		void BindMouselookInputMapToButton(InputAction.CallbackContext context) {
			if (mouselookActionMap == null) {
				UserInput userInput = GetComponent<UserInput>();
				mouselookActionMap = userInput.inputActionAsset.FindActionMap(n_MouseLook);
				if (mouselookActionMap == null) {
					throw new System.Exception($"character controls need a `{n_MouseLook}` action map");
				}
			}
			switch (context.phase) {
				case InputActionPhase.Started: mouselookActionMap.Enable(); break;
				case InputActionPhase.Canceled: mouselookActionMap.Disable(); break;
			}
		}
		public void RelinquishCharacter(Root target = null) {
			if (target == null) { target = this.target; }
			if (target.jump) {
				EventBind.Remove(target.jump.OnJumpPowerProgress, this, nameof(NotifyJumpPowerProgress));
				EventBind.Remove(target.jump.OnJumpCountChanged, this, nameof(NotifyJumpCountProgress));
			}
			if (target.move) {
				target.move.cameraTransform = null;
			}
			CharacterCamera.target = null;
		}
		public void AbsorbCharacter(Root target) {
			this.target = target;
			if (target.jump) {
				EventBind.On(target.jump.OnJumpPowerProgress, this, nameof(NotifyJumpPowerProgress));
				EventBind.On(target.jump.OnJumpCountChanged, this, nameof(NotifyJumpCountProgress));
			}
			if (target.move) {
				target.move.cameraTransform = CharacterCamera.transform;
			}
			CharacterCamera.target = GetCameraTarget();
		}
		private void Update() {
			if (lastTarget != target) {
				if (lastTarget) { RelinquishCharacter(lastTarget); }
				if (target) { AbsorbCharacter(target); }
				lastTarget = target;
			}
		}
#else
		CharacterCamera _camera;
		void Start() {
			_camera = CharacterCamera.FindCameraTargettingChildOf(target.transform);
		}
		void Update() {
			Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
			bool jump = Input.GetButton("Jump");
			MoveInput = input;
			JumpInput = jump ? 1 : 0;
			if (_camera != null && Input.GetMouseButton(1)) {
				_camera.ProcessLookRotation(new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")));
			}
		}
#endif
		public static UserController GetCharacterControllerOf(Transform t) {
			UserController fps = t.GetComponentInChildren<UserController>();
			if (fps == null) { t = t.parent; }
			return t != null ? GetCharacterControllerOf(t) : null;
		}
		/// <param name="whichUser">if negative, will return the only user. if there is more than one user, throws an error.</param>
		/// <returns></returns>
		/// <exception cref="System.Exception"></exception>
		public static UserController GetUserCharacterController(int whichUser = -1) {
			UserController[] ucc = FindObjectsOfType<UserController>();
			if (ucc == null || ucc.Length == 0) { throw new System.Exception($"Expecting {nameof(UserController)} in the scene"); }
			if (whichUser < 0 && ucc.Length > 1) { throw new System.Exception($"Expectiong ONE {nameof(UserController)} in the scene"); }
			int i = whichUser;
			if (i < 0) { i = 0; }
			if (i > ucc.Length) { throw new System.Exception($"Expecting at least {i+1} {nameof(UserController)}s in the scene"); }
			return ucc[i];
		}
	}
}