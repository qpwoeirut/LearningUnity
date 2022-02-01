using UnityEngine;
using NonStandard.Character;

public class RandomWalk : MonoBehaviour {
	Root r;
	long whenToChangeMoveTarget = 0;
	[Tooltip("How many seconds max to go in the randomwalk desired direction")]
	public float moveAttentionSpan = 3;
	[Tooltip("Vision arc to choose the next randomwalk position from")]
	public float randomWalkTurnArc = 180;
	void Start() {
		r = GetComponent<Root>();
		SelectNextMoveTarget();
	}
	void SelectNextMoveTarget() {
		float angleTurn = Random.Range(-randomWalkTurnArc / 2, randomWalkTurnArc / 2);
		float horizonAdjust = Random.Range(-45, 0);
		Quaternion gaze = transform.rotation;
		gaze *= Quaternion.AngleAxis(horizonAdjust, transform.right);
		gaze *= Quaternion.AngleAxis(angleTurn, Vector3.up);
		Vector3 dir = gaze * Vector3.forward;
		NonStandard.Lines.Make("gaze").Arrow(r.head.position, r.head.position + dir, Color.cyan);
		Vector3 p = transform.position;
		if (Physics.Raycast(r.head.position, dir, out RaycastHit hitInfo)) {
			p = hitInfo.point;
			transform.Rotate(Vector3.up, angleTurn);
		}
		r.move.SetAutoMovePosition(p, 1);
		whenToChangeMoveTarget = System.Environment.TickCount + (long)(moveAttentionSpan * 1000);
	}
	void Update() {
		if ((r.automove.arrived && r.move.IsStableOnGround) || System.Environment.TickCount > whenToChangeMoveTarget) {
			SelectNextMoveTarget();
		}
	}
}
