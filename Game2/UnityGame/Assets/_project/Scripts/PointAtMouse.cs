using NonStandard;
using NonStandard.Character;
using UnityEngine;
using UnityEngine.InputSystem;

public class PointAtMouse : MonoBehaviour {
	public enum LockAxis { None, XAxis, YAxis, ZAxis }
	public LockAxis lockAxis;
	public void UpdateDirection() {
		UpdateDirection(transform, lockAxis);
	}
	public static void UpdateDirection(Transform t, LockAxis lockAxis = LockAxis.None) {
		Ray ray = Global.GetComponent<CharacterCamera>().Camera.ScreenPointToRay(Mouse.current.position.ReadValue());
		if (Physics.Raycast(ray, out RaycastHit hitInfo)) {
			Vector3 p = hitInfo.point;
			switch (lockAxis) {
				case LockAxis.XAxis: p.x = t.position.x; break;
				case LockAxis.YAxis: p.y = t.position.y; break;
				case LockAxis.ZAxis: p.z = t.position.z; break;
			}
			t.LookAt(p);
		}
	}
}
