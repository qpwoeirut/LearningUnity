using System.Collections.Generic;
using UnityEngine;

public class CollisionTagged : MonoBehaviour {
	public CollisionTag kind;
	private void OnCollisionEnter(Collision collision) {
		CollisionTagged[] collidables = collision.collider.GetComponents<CollisionTagged>();
		if (collidables != null && collidables.Length > 0) {
			for (int i = 0; i < collidables.Length; i++) {
				CollisionTagged collidable = collidables[i];
				List<CollisionRules.Rule> rules = CollisionRules.Instance.GetRules(this, collidable);
				if (rules != null) {
					rules.ForEach(r => r.onCollision.Invoke(new CollisionTagged[] { this, collidable }));
				}
			}
		}
	}
}
