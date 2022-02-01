using System;
using System.Collections;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

namespace NonStandard.Utility {
	public class Event : MonoBehaviour {
		public UnityEvent _event;
		public void Invoke() { _event.Invoke(); }
		public void Bind(object target, string methodName) {
			Bind(_event, target, methodName);
		}
		public void Set(object target, string methodName) {
			Set(_event, target, methodName);
		}
		public static void Bind(UnityEvent @event, object target, string methodName) {
			System.Reflection.MethodInfo targetinfo = UnityEvent.GetValidMethodInfo(target, methodName, Type.EmptyTypes);
			UnityAction action = Delegate.CreateDelegate(typeof(UnityAction), target, targetinfo, false) as UnityAction;
			UnityEventTools.AddVoidPersistentListener(@event, action);
		}
		public static void Set(UnityEvent @event, object target, string methodName) {
			Clear(@event);
			Bind(@event, target, methodName);
		}
		public static void Clear(UnityEventBase @event) {
			while (@event.GetPersistentEventCount() > 0) {
				UnityEventTools.RemovePersistentListener(@event, @event.GetPersistentEventCount() - 1);
			}
		}
		public static IEnumerator WaitCoroutine(float timeInSeconds, Action action) {
			yield return new WaitForSeconds(timeInSeconds);
			action.Invoke();
		}
		public static IEnumerator WaitFrameCoroutine(int frames, Action action) {
			for (int i = 0; i < frames; i++) { yield return new WaitForEndOfFrame(); }
			action.Invoke();
		}
		public static void Wait(float timeInSeconds, Action action, MonoBehaviour whoExecutesTheCoroutine = null) {
			if (whoExecutesTheCoroutine == null) {
				whoExecutesTheCoroutine = FindObjectOfType<MonoBehaviour>();
			}
			whoExecutesTheCoroutine.StartCoroutine(WaitCoroutine(timeInSeconds, action));
		}
		public static void WaitFrames(int frameCount, Action action, MonoBehaviour whoExecutesTheCoroutine = null) {
			if (whoExecutesTheCoroutine == null) {
				whoExecutesTheCoroutine = FindObjectOfType<MonoBehaviour>();
			}
			whoExecutesTheCoroutine.StartCoroutine(WaitFrameCoroutine(frameCount, action));
		}
	}
}