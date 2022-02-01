using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NonStandard.Extension {
	public static class TransformExtension {
		public static string HierarchyPath(this Transform t, string separator = "/") {
			StringBuilder sb = new StringBuilder();
			sb.Append(t.name);
			t = t.parent;
			while (t != null) {
				string str = t.name;
				//if (str.Contains("/")) { str = "\""+str.Escape()+"\""; }
				sb.Insert(0, str + separator);
				t = t.parent;
			}
			return sb.ToString();
		}

		public static int IndexOfChild(this Transform t, Func<Transform, bool> predicate) {
			for(int i = 0; i < t.childCount; ++i) {
				Transform c = t.GetChild(i);
				if (c != null && predicate.Invoke(c)) { return i; }
			}
			return -1;
		}

		private static string _TransformName(Transform t) { return t.name; }
		public static string JoinToString(this Transform t, string separator = ", ", Func<Transform, string> toString = null) {
			if(toString == null) { toString = _TransformName; }
			string[] children = new string[t.childCount];
			for(int i = 0; i < t.childCount; ++i) {
				children[i] = toString.Invoke(t.GetChild(i));
			}
			return string.Join(separator, children);
		}

		public static void PopulateManifest(this Transform self, List<object> manifest) {
			manifest.Add(self);
			for (int i = 0; i < self.childCount; ++i) {
				Transform child = self.GetChild(i);
				if (child != null) {
					PopulateManifest(child, manifest);
				}
			}
		}
	}
}