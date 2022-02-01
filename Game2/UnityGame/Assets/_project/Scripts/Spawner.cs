using UnityEngine;

public class Spawner : MonoBehaviour {
    public GameObject prefab;
    public void Spawn() {
        if (!gameObject.activeInHierarchy) { return; }
        Instantiate(prefab, transform.position, transform.rotation);
    }
}
