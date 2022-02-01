using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NonStandard.Utility {
	public class NextScene : MonoBehaviour {
		public string sceneName;
		public Image progressBar;
		public void DoActivateTrigger() {
			AsyncOperation ao = SceneManager.LoadSceneAsync(sceneName);
			if (progressBar != null) {
				StartCoroutine(UpdateProgressVisual()); IEnumerator UpdateProgressVisual() {
					while (!ao.isDone) {
						progressBar.fillAmount = ao.progress;
						yield return null;
					}
				}
			}
		}
	}
}