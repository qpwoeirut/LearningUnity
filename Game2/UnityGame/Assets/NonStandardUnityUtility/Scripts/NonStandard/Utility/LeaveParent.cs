using UnityEngine;

namespace NonStandard.Utility {
	public class LeaveParent : MonoBehaviour {
		public Transform whereToGo = null;
		public bool worldPositionStays;
		public void DoActivateTrigger() { transform.SetParent(whereToGo, worldPositionStays); }
		void Start () { DoActivateTrigger(); }
	}
}