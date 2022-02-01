using System.Collections.Generic;

namespace NonStandard.Utility {
	/// <summary>
	/// can only be used in the Unity Editor
	/// </summary>
	public static class File {
#if UNITY_EDITOR
		public static List<string> FindFile(string path, string filename, int allowedRecursion = 0, string[] ignoreFolders = null) {
			string[] list = System.IO.Directory.GetFiles(path);
			List<string> result = null;
			string n = System.IO.Path.DirectorySeparatorChar + filename;
			for (int i = 0; i < list.Length; ++i) {
				if (list[i].EndsWith(n)) {
					//Debug.Log("f " + list[i]);
					if (result == null) { result = new List<string>(); }
					result.Add(list[i]);
				}
			}
			if (allowedRecursion != 0) {
				bool ShouldBeIgnored(string path) {
					if (ignoreFolders == null) return false;
					int index = path.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
					string folder = path.Substring(index + 1);
					for (int i = 0; i < ignoreFolders.Length; ++i) {
						if (folder.StartsWith(ignoreFolders[i])) return true;
					}
					return false;
				}
				list = System.IO.Directory.GetDirectories(path);
				for (int i = 0; i < list.Length; ++i) {
					if (ShouldBeIgnored(list[i])) { continue; }
					//Debug.Log("d "+list[i]);
					List<string> r = FindFile(list[i], filename, allowedRecursion - 1);
					if (r != null) {
						if (result == null) { result = new List<string>(); }
						result.AddRange(r);
					}
				}
			}
			return result;
		}
		/// <param name="filename">do not include the path, do include the ".cs"</param>
		/// <param name="fileData"></param>
		public static void RewriteAssetCSharpFile(string filename, string fileData) {
			string startPath = System.IO.Path.GetFullPath(".");
			char dir = System.IO.Path.DirectorySeparatorChar;
			string fileBranch = startPath + dir + "Assets";
			//Debug.Log(assetPath);
			List<string> found = FindFile(fileBranch, filename, -1, null);
			if (found == null) {
				fileBranch = startPath + dir + "Packages";
				found = FindFile(fileBranch, filename, -1, null);
			}
			if (found == null) {
				fileBranch = startPath + dir + "Library" + dir + "PackageCache";
				found = FindFile(fileBranch, filename, -1, null);
				if (found != null) {
					UnityEngine.Debug.LogWarning("modifying "+filename+" in Library"+dir+"PackageCache");
				}
			}
			if (found != null) {
				//Debug.Log(string.Join("\n", found));
				string path = found[0];
				System.IO.File.WriteAllText(path, fileData);
				string relativePath = path.Substring(startPath.Length + 1);
				//Debug.Log(relativePath);
				Event.Wait(0, () => {
					UnityEditor.AssetDatabase.ImportAsset(relativePath);
					UnityEditor.EditorGUIUtility.PingObject(UnityEditor.AssetDatabase.LoadMainAssetAtPath(relativePath));
					//UnityEditor.Selection.activeObject = UnityEditor.AssetDatabase.LoadMainAssetAtPath(relativePath);
				});
			} else {
				UnityEngine.Debug.LogError("Could not find C# file "+filename);
			}
		}
#endif
	}
}
