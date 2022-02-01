using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor.Events;
#endif
using UnityEngine;
using UnityEngine.Events;

namespace NonStandard {
	public class EventBind {
		public object target;
		public string methodName;
		public object value;

		public EventBind(object target, string methodName, object value = null) {
			this.target = target; this.methodName = methodName; this.value = value;
		}
		public EventBind(object target, string methodName) {
			this.target = target; this.methodName = methodName; value = null;
		}
		public UnityAction<T> GetAction<T>(object target, string methodName) {
			System.Reflection.MethodInfo targetinfo = UnityEvent.GetValidMethodInfo(target, methodName, new Type[] { typeof(T) });
			if (targetinfo == null) {
				Debug.LogError("no method \"" + methodName + "\" (" + typeof(T).Name + ") in " + target.ToString());
				return null;
			}
			return Delegate.CreateDelegate(typeof(UnityAction<T>), target, targetinfo, false) as UnityAction<T>;
		}
		public static bool IfNotAlready<T>(UnityEvent<T> @event, UnityEngine.Object target, string methodName) {
			for(int i = 0; i < @event.GetPersistentEventCount(); ++i) {
				if (@event.GetPersistentTarget(i) == target && @event.GetPersistentMethodName(i) == methodName) { return false; }
			}
			On(@event, target, methodName);
			return true;
		}
		public static bool IfNotAlready(UnityEvent @event, UnityEngine.Object target, UnityAction action) {
			for (int i = 0; i < @event.GetPersistentEventCount(); ++i) {
				if (@event.GetPersistentTarget(i) == target && @event.GetPersistentMethodName(i) == action.Method.Name) { return false; }
			}
			On(@event, target, action);
			return true;
		}
		public static bool IfNotAlready<T>(UnityEvent<T> @event, UnityEngine.Object target, UnityAction<T> action) {
			for (int i = 0; i < @event.GetPersistentEventCount(); ++i) {
				if (@event.GetPersistentTarget(i) == target && @event.GetPersistentMethodName(i) == action.Method.Name) { return false; }
			}
			On(@event, target, action);
			return true;
		}
		public static void On(UnityEvent @event, object target, UnityAction action) {
#if UNITY_EDITOR
			if (target != null) {
				new EventBind(target, action.Method.Name).Bind(@event);
				return;
			}
#endif
			@event.AddListener(action.Invoke);
		}
		public static void On<T>(UnityEvent<T> @event, object target, UnityAction<T> action) {
#if UNITY_EDITOR
			if (target != null) {
				new EventBind(target, action.Method.Name).Bind(@event);
				return;
			}
#endif
			@event.AddListener(action.Invoke);
		}
		public static void On<T>(UnityEvent<T> @event, object target, string methodName) {
			new EventBind(target, methodName).Bind(@event);
		}
		public bool IsAlreadyBound(UnityEventBase @event) {
			UnityEngine.Object obj = target as UnityEngine.Object;
			return obj != null && IsAlreadyBound(@event, obj, methodName);
		}
		public static bool IsAlreadyBound(UnityEventBase @event, UnityEngine.Object target, string methodName) {
			return GetPersistentEventIndex(@event, target, methodName) >= 0;
		}
		public static int GetPersistentEventIndex(UnityEventBase @event, UnityEngine.Object target, string methodName) {
			for (int i = 0; i < @event.GetPersistentEventCount(); ++i) {
				if (@event.GetPersistentTarget(i) == target && @event.GetPersistentMethodName(i) == methodName) { return i; }
			}
			return -1;
		}
		public static bool Remove(UnityEventBase @event, UnityEngine.Object target, string methodName) {
			int index = GetPersistentEventIndex(@event, target, methodName);
			if (index >= 0) {
				UnityEventTools.RemovePersistentListener(@event, index);
				return true;
            }
			return false;
		}
		public static void Clear(UnityEventBase @event) {
			while (@event.GetPersistentEventCount() > 0) {
				UnityEventTools.RemovePersistentListener(@event, @event.GetPersistentEventCount() - 1);
			}
		}
		public static bool IfNotAlready(UnityEvent @event, UnityEngine.Object target, string methodName) {
			if (IsAlreadyBound(@event, target, methodName)) return false;
			On(@event, target, methodName);
			return true;
		}
		public static void On(UnityEvent @event, object target, string methodName) {
			new EventBind(target, methodName).Bind(@event);
		}
		public void Bind<T>(UnityEvent<T> @event) {
#if UNITY_EDITOR
			UnityEventTools.AddPersistentListener(@event, GetAction<T>(target, methodName));
#else
			System.Reflection.MethodInfo targetinfo = UnityEvent.GetValidMethodInfo(target, methodName, new Type[]{typeof(T)});
			@event.AddListener((val) => targetinfo.Invoke(target, new object[] { val }));
#endif
		}
		public void Bind(UnityEvent @event) {
#if UNITY_EDITOR
			if (value == null) {
				System.Reflection.MethodInfo targetinfo = UnityEvent.GetValidMethodInfo(target, methodName, new Type[0]);
				if (targetinfo == null) { Debug.LogError("no method " + methodName + "() in " + target.ToString()); }
				UnityAction action = Delegate.CreateDelegate(typeof(UnityAction), target, targetinfo, false) as UnityAction;
				UnityEventTools.AddVoidPersistentListener(@event, action);
			} else if (value is int) {
				UnityEventTools.AddIntPersistentListener(@event, GetAction<int>(target, methodName), (int)value);
			} else if (value is float) {
				UnityEventTools.AddFloatPersistentListener(@event, GetAction<float>(target, methodName), (float)value);
			} else if (value is string) {
				UnityEventTools.AddStringPersistentListener(@event, GetAction<string>(target, methodName), (string)value);
			} else if (value is bool) {
				UnityEventTools.AddBoolPersistentListener(@event, GetAction<bool>(target, methodName), (bool)value);
			} else if (value is GameObject) {
				Bind<GameObject>(@event);
			} else if (value is Transform) {
				Bind<Transform>(@event);
			} else {
				Debug.LogError("unable to assign " + value.GetType());
			}
#else
				System.Reflection.MethodInfo targetinfo = UnityEvent.GetValidMethodInfo(target, methodName, new Type[0]);
				@event.AddListener(() => targetinfo.Invoke(target, new object[] { value }));
#endif
		}
#if UNITY_EDITOR
		public void Bind<T>(UnityEvent @event) where T : UnityEngine.Object {
			if (value is T) {
				UnityEventTools.AddObjectPersistentListener(@event, GetAction<T>(target, methodName), (T)value);
			} else {
				Debug.LogError("unable to assign " + value.GetType());
			}
		}
#endif
		public static List<EventBind> GetList(UnityEventBase @event) {
			List<EventBind> eb = new List<EventBind>();
			for(int i = 0; i <@event.GetPersistentEventCount(); ++i) {
				eb.Add(new EventBind(@event.GetPersistentTarget(i), @event.GetPersistentMethodName(i)));
			}
			return eb;
		}
		public static string DebugPrint(UnityEventBase @event) {
			List<EventBind> eb = GetList(@event);
			return string.Join(", ", eb);
		}
		public override string ToString() {
			string t = null;
			if (target is UnityEngine.Object o) { t = o.name; }
			else if (target != null) { t = target.ToString(); }
			return t+"."+methodName;
		}
	}
}