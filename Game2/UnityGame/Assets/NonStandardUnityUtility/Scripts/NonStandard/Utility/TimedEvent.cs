using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace NonStandard.Utility {
	public class TimedEvent : MonoBehaviour {
		public float time = 3;
		public UnityEvent _event;
		private void Reset() {
			//Event.Bind(_event, this, nameof(DestroySelf));
		}
		void Start() {
			StartCoroutine(DoTimedEventCoroutine(time, _event.Invoke));
		}
		public static void Do(float timeInSeconds, Action action) {
			FindObjectOfType<MonoBehaviour>().StartCoroutine(DoTimedEventCoroutine(timeInSeconds, action));
		}
		public static IEnumerator DoTimedEventCoroutine(float timeInSeconds, Action action) {
			yield return new WaitForSeconds(timeInSeconds);
			action.Invoke();
		}
		public void DestroySelf() { Destroy(gameObject); }
	}
}
