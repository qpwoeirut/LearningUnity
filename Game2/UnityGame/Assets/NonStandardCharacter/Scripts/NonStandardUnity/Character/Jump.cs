using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NonStandard.Character {
	[RequireComponent(typeof(Move))]
	public class Jump : MonoBehaviour {
		[HideInInspector] public Root root;
		internal float lastJump = -1;
		public static ulong Now => (ulong)System.Environment.TickCount;
		/// <summary>if true, the jump is intentionally happening and hasn't been interrupted</summary>
		[HideInInspector] public bool isJumping;
		/// <summary>if true, the jump has passed it's apex</summary>
		[HideInInspector] public bool peaked;
		/// <summary>if true, the jump is no longer adjusting it's height based on Pressed value</summary>
		[HideInInspector] public bool heightSet;
		/// <summary>while this is true, the jump module is trying to jump</summary>
		[HideInInspector] public bool pressed;
#if NONSTANDARD_LINES
		/// <summary>for debugging: shows the jump arc, and how it grows as Pressed is held</summary>
		public bool showJumpArc = false;
#endif
		/// <summary>allows ret-con of a missed jump (user presses jump a bit late after walking off a ledge)</summary>
		[HideInInspector] public bool forgiveLateJumps = true;
		//[Tooltip("Enable or disable jumping")]
		//public bool enabled = true;
		[Tooltip("Tapping the jump button for the shortest amount of time possible will result in this height")]
		public float min = .125f;
		[Tooltip("Holding the jump button for fullJumpPressDuration seconds will result in this height")]
		public float max = 1.5f;
		[Tooltip("How long the jump button must be pressed to jump the maximum height")]
		public float fullPressDuration = .25f;
		[Tooltip("For double-jumping, put a 2 here. To eliminate jumping, put a 0 here.")]
		public int doubleJumps = 0;
		[Tooltip("Used for AI driven jumps of different height")]
		public float timedJumpPress = 0; // TODO just set targetJumpHeight?
		/// <summary>how long to wait for a jump after walking off a ledge</summary>
		public const long jumpLagForgivenessMs = 200;
		/// <summary>how long to wait to jump if press happens while still in the air</summary>
		public const long jumpTooEarlyForgivenessMs = 500;
		/// <summary>calculated target jump height</summary>
		[HideInInspector] public float targetJumpHeight;
		[HideInInspector] public Vector3 position;
		/// <summary>when jump was started, ideally when the button was pressed</summary>
		protected ulong jumpTime;
		/// <summary>when jump should reach apex</summary>
		protected ulong peakTime;
		/// <summary>when jump start position was last recognized as stable</summary>
		protected ulong stableTime;
		/// <summary>when the jump button was pressed</summary>
		protected ulong timePressed;
		/// <summary>How many double jumps have happend since being on the ground</summary>
		[HideInInspector] public int usedDoubleJumps;
		public UnityEvent_float OnJumpPowerProgress;
		public UnityEvent_float OnJumpCountChanged;
		[System.Serializable] public class UnityEvent_float : UnityEvent<float> { }

#if NONSTANDARD_LINES
		/// <summary>debug artifact, for seeing the jump arc</summary>
		[HideInInspector] Wire jumpArc;
#endif
		public bool Pressed {
			get { return pressed; }
			set { if (value && !pressed) { timePressed = Now; } pressed = value; }
		}
		/// <summary>
		/// how many seconds to hold down the jump button. if a non-zero value, a jump impulse will be applied. decays to zero with time.
		/// </summary>
		public float TimedJumpPress { get => timedJumpPress; set => timedJumpPress = value; }
		public float JumpHeight { get { return max; } set { max = value; } }
		public float JumpInput { get; set; }

		private void Awake() {
			root = GetComponent<Root>();
			//if (root.jump != null) {
			//	Debug.LogError(nameof(Root)+" should not have more than one peer "+nameof(Jump));
			//}
			root.jump = this;
		}
		public void StartJump(Vector3 p) {
			jumpTime = Now;
			peakTime = 0;
			isJumping = true;
			peaked = false;
			heightSet = false;
			position = p;
			root.move.IsStableOnGround = false;
			OnJumpCountChanged?.Invoke(GetJumpProgress());
		}
		public void Interrupt() {
			isJumping = false;
			heightSet = true;
			targetJumpHeight = 0;
		}
		public float GetJumpProgress() {
			return root.move.IsStableOnGround ? 1f : ((usedDoubleJumps + 1f) / (doubleJumps + 1f));
		}
		public void InternalEarlyUpdate() {
			if (JumpInput != lastJump) {
				Pressed = JumpInput > 0;//jump.PressJump = Jump;
				lastJump = JumpInput;
			}
		}
		public void InternalFixedUpdate(Root r) {
			if (!enabled) return;
			bool peakedAtStart = peaked, jumpingAtStart = isJumping;
			bool jpress = pressed;
			ulong now = Now;
			if (TimedJumpPress > 0) {
				jpress = true; TimedJumpPress -= Time.deltaTime; if (TimedJumpPress < 0) { TimedJumpPress = 0; }
			}
			bool lateButForgiven = false;
			ulong late = 0;
			if (r.move.IsStableOnGround) { usedDoubleJumps = 0; } else if (jpress && forgiveLateJumps && (late = Now - stableTime) < jumpLagForgivenessMs) {
				stableTime = 0;
				r.move.IsStableOnGround = lateButForgiven = true;
			}
			if (jpress && r.move.IsStableOnGround && !isJumping && now - timePressed < jumpTooEarlyForgivenessMs) {
				timePressed = 0;
				if (!lateButForgiven) { StartJump(r.rootTransform.position); } else { StartJump(position); jumpTime -= late; }
			}
			float gForce = Mathf.Abs(Physics.gravity.y);
			if (isJumping) {
				Vector3 vel = r.rb.velocity;
				JumpUpdate(now, gForce, r.move.speed, jpress, r.rootTransform, ref vel);
				r.rb.velocity = vel;
			} else {
				peaked = now >= peakTime;
			}
			if (!isJumping && jpress && usedDoubleJumps < doubleJumps) {
				DoubleJump(r.rootTransform, r.move.speed, gForce, peaked && !peakedAtStart && jumpingAtStart);
			}
			if (isJumping && !jumpingAtStart) {
				r.callbacks?.jumped?.Invoke(Vector3.up);
			}
		}

		private void JumpUpdate(ulong now, float gForce, float speed, bool jpress, Transform t, ref Vector3 vel) {
			if (!heightSet) {
				CalcJumpOverTime(now - jumpTime, gForce, out float y, out float yVelocity);
				if (float.IsNaN(y)) {
					Debug.Log("if you see this error message, there might be a timing problem\n" +
						(now - jumpTime) + " bad y value... " + yVelocity + "  " + peakTime + " vs " + now); // TODO why bad value happens sometimes?
					y = 0;
					yVelocity = 0;
				}
				Vector3 p = t.position;
				p.y = position.y + y;
				t.position = p;
				vel.y = yVelocity;
#if NONSTANDARD_LINES
				if (showJumpArc) {
					if (jumpArc == null) { jumpArc = Lines.MakeWire("jump arc").Line(Vector3.zero); }
					jumpArc.Line(CalcPath(position, t.forward, speed, targetJumpHeight, gForce), Color.red);
				}
#endif
			}
			peaked = heightSet && now >= peakTime;
			isJumping = !peaked && jpress;
		}
		private void CalcJumpOverTime(ulong jumpMsSoFar, float gForce, out float yPos, out float yVel) {
			float jumptiming = jumpMsSoFar / 1000f;
			float jumpP = Mathf.Min(jumptiming / fullPressDuration, 1);
			OnJumpPowerProgress?.Invoke(jumpP);
			if (jumpP >= 1) { heightSet = true; }
			targetJumpHeight = (max - min) * jumpP + min;
			float jVelocity = CalcJumpVelocity(targetJumpHeight, gForce);
			float jtime = 500 * CalcStandardDuration_WithJumpVelocity(jVelocity, gForce);
			peakTime = jumpTime + (uint)jtime;
			yPos = CalcHeightAt_WithJumpVelocity(jVelocity, jumptiming, targetJumpHeight, gForce);
			yVel = CalcVelocityAt_WithJumpVelocity(jVelocity, jumptiming, gForce);
		}
		private void DoubleJump(Transform t, float speed, float gForce, bool justPeaked) {
			if (justPeaked) {
				float peakHeight = position.y + targetJumpHeight;
				Vector3 delta = t.position - position;
				delta.y = 0;
				float dist = delta.magnitude;
				float peakTime = CalcStandardJumpDuration(targetJumpHeight, gForce) / 2;
				float expectedDist = peakTime * speed;
				if (dist > expectedDist) {
					Vector3 p = position + delta * expectedDist / dist;
					p.y = peakHeight;
					t.position = p;
				}
				position = t.position;
				position.y = peakHeight;
			} else {
				position = t.position;
			}
			StartJump(position);
			++usedDoubleJumps;
			OnJumpCountChanged?.Invoke(GetJumpProgress());
		}
		public void MarkStableJumpPoint(Vector3 position) {
			this.position = position;
			stableTime = Now;
		}
		/// <param name="pos">starting position of the jump</param>
		/// <param name="dir"></param>
		/// <param name="speed"></param>
		/// <param name="jHeight"></param>
		/// <param name="gForce"></param>
		/// <returns></returns>
		public static List<Vector3> CalcPath(Vector3 pos, Vector3 dir, float speed, float jHeight, float gForce, float timeIncrement = 1f / 32) {
			return CalcPath_WithVelocity(CalcJumpVelocity(jHeight, gForce), pos, dir, speed, jHeight, gForce, timeIncrement);
		}
		static List<Vector3> CalcPath_WithVelocity(float jVelocity, Vector3 p, Vector3 dir, float speed, float jHeight, float gForce, float timeIncrement) {
			List<Vector3> points = new List<Vector3>();
			float stdJumpDuration = 2 * jVelocity / gForce;
			for (float t = 0; t < stdJumpDuration; t += timeIncrement) {
				float vAtPoint = t * gForce - jVelocity;
				float y = -(vAtPoint * vAtPoint) / (gForce * 2) + jHeight;
				Vector3 pos = p + dir * (speed * t) + Vector3.up * y;
				points.Add(pos);
			}
			points.Add(p + dir * speed * stdJumpDuration);
			return points;
		}
		static float CalcJumpVelocity(float jumpHeight, float gForce) { return Mathf.Sqrt(2 * jumpHeight * gForce); }
		static float CalcJumpHeightAt(float time, float jumpHeight, float gForce) {
			return CalcHeightAt_WithJumpVelocity(CalcJumpVelocityAt(time, jumpHeight, gForce), time, jumpHeight, gForce);
		}
		static float CalcHeightAt_WithJumpVelocity(float jumpVelocity, float time, float jumpHeight, float gForce) {
			float vAtPoint = CalcVelocityAt_WithJumpVelocity(jumpVelocity, time, gForce);
			float y = -(vAtPoint * vAtPoint) / (gForce * 2) + jumpHeight;
			return y;
		}
		static float CalcJumpVelocityAt(float time, float jumpHeight, float gForce) {
			return CalcVelocityAt_WithJumpVelocity(CalcJumpVelocity(jumpHeight, gForce), time, gForce);
		}
		static float CalcVelocityAt_WithJumpVelocity(float jumpVelocity, float time, float gForce) {
			return -(time * gForce - jumpVelocity);
		}
		static float CalcStandardJumpDuration(float jumpHeight, float gForce) {
			return CalcStandardDuration_WithJumpVelocity(CalcJumpVelocity(jumpHeight, gForce), gForce);
		}
		static float CalcStandardDuration_WithJumpVelocity(float jumpVelocity, float gForce) {
			return 2 * jumpVelocity / gForce;
		}
	}
}
