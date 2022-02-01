#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NonStandard {
	public static class ScriptableObjectUtility {
		/// This makes it easy to create, name and place unique new ScriptableObject asset files.
		public static T CreateAsset<T>() where T : ScriptableObject { return CreateAsset(typeof(T)) as T; }
		public static ScriptableObject CreateAsset(System.Type t, string filename = "", string path = "", string content = "") {
			ScriptableObject asset = ScriptableObject.CreateInstance(t);
			return SaveScriptableObjectAsAsset(asset, filename, path, content);
		}

		public static ScriptableObject SaveScriptableObjectAsAsset(ScriptableObject asset, string filename = "", string path = "", string content = "") {
			System.Type t = asset.GetType();
			if (path == "") {
				path = AssetDatabase.GetAssetPath(Selection.activeObject);
				if (path == "") {
					path = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;//"Assets";
					Debug.Log(path);
					int idx = path.LastIndexOf("/");
					if (idx < 0) {
						path = "Assets";
					} else {
						path = path.Substring(0, idx);
						if (filename == "") {
							string typename = t.ToString();
							int idx2 = typename.LastIndexOf(".");
							if (idx > 0) { typename = typename.Substring(idx2); }
							filename = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + typename + ".asset";
						}
						Debug.Log(path + " //// " + filename);
					}
					//Debug.Log(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
				} else if (System.IO.Path.GetExtension(path) != "") {
					path = path.Replace(System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
				}
			}
			if (filename.Length == 0) { filename = "New " + t.ToString() + ".asset"; }
			string fullpath = path + "/" + filename;
			string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(fullpath);
			if (string.IsNullOrEmpty(content)) {
				AssetDatabase.CreateAsset(asset, assetPathAndName);
				AssetDatabase.SaveAssets();
			} else {
				StreamWriter writer = new StreamWriter(assetPathAndName, true);
				writer.WriteLine(content);
				writer.Close();
				AssetDatabase.ImportAsset(assetPathAndName);
			}
			AssetDatabase.Refresh();
			EditorUtility.FocusProjectWindow();
			Object loaded = AssetDatabase.LoadAssetAtPath(fullpath, t);
			if (loaded != null) {
				//Selection.activeObject = asset;
			}
			return loaded as ScriptableObject;
		}
	}
}
#endif
