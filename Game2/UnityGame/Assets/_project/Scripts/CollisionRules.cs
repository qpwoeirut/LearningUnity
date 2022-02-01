using NonStandard;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

public class CollisionRules : MonoBehaviour {
	private static CollisionRules _instance;
	public static CollisionRules Instance {
		get {
			if (_instance) return _instance;
			_instance = FindObjectOfType<CollisionRules>();
			return _instance;
		}
	}
	private void Awake() { _instance = this; }

	[System.Serializable] public class Rule {
		public string name;
		public CollisionTag collider, collidee;
		public bool deactivated;
		public UnityEvent_Collidable_Array onCollision = new UnityEvent_Collidable_Array();
		[System.Serializable] public class UnityEvent_Collidable_Array : UnityEvent<CollisionTagged[]> { }
		public Rule(string name, CollisionTag collider, CollisionTag collidee, EventBind eventBind) :
			this(name, collider, collidee, new EventBind[] { eventBind }) { }
		public Rule(string name, CollisionTag collider, CollisionTag collidee, UnityAction<CollisionTagged[]> action) :
			this(name, collider, collidee, new UnityAction<CollisionTagged[]>[] { action }) { }
		public Rule(string name, CollisionTag collider, CollisionTag collidee, EventBind[] eventBinds) {
			this.name = name; this.collider = collider; this.collidee = collidee; Array.ForEach(eventBinds, e=>e.Bind(onCollision));
		}
		public Rule(string name, CollisionTag collider, CollisionTag collidee, UnityAction<CollisionTagged[]>[] actions) {
			this.name = name; this.collider = collider; this.collidee = collidee;
			Array.ForEach(actions, action=>onCollision.AddListener(action));
		}
		public bool AppliesTo(CollisionTagged collider, CollisionTagged collidee) { return AppliesTo(collider.kind, collidee.kind) && !deactivated; }
		public bool AppliesTo(CollisionTag collider, CollisionTag collidee) {
			return collider == this.collider && collidee == this.collidee;
		}
	}
	public List<Rule> collisionRules = new List<Rule>();
	Dictionary<CollisionTag, Dictionary<CollisionTag, HashSet<Rule>>> collisionRuleLookupTables = 
		new Dictionary<CollisionTag, Dictionary<CollisionTag, HashSet<Rule>>>();
	public void Start() {
		UpdateRuleLookupDictionary();
	}
	void UpdateRuleLookupDictionary() {
		for (int i = 0; i < collisionRules.Count; i++) {
			Rule r = collisionRules[i];
			if (!collisionRuleLookupTables.TryGetValue(r.collider, out Dictionary<CollisionTag, HashSet<Rule>> subLookupTable)) {
				subLookupTable = new Dictionary<CollisionTag, HashSet<Rule>>();
				collisionRuleLookupTables[r.collider] = subLookupTable;
			}
			if (!subLookupTable.TryGetValue(r.collidee, out HashSet<Rule> ruleSet)) {
				ruleSet = new HashSet<Rule>();
				subLookupTable[r.collidee] = ruleSet;
			}
			ruleSet.Add(r);
		}
	}
	public void Add(Rule rule) {
		collisionRules.Add(rule);
		UpdateRuleLookupDictionary();
	}
	/// <summary>
	/// if a rule for these two colliding objects exist, don't add it.
	/// </summary>
	/// <param name="rule"></param>
	/// <returns>true if added, false if not added</returns>
	public bool AddIfMissing(Rule rule) {
		List<Rule> rules = GetRules(rule.collider, rule.collidee);
		if (rules != null && rules.Count > 0) {
			return false;
		}
		Add(rule);
		return true;
	}
	/// <summary>
	/// if a rule for these two colliding objects exist *with the same name*, don't add it.
	/// </summary>
	/// <param name="rule"></param>
	/// <returns>true if added, false if not added</returns>
	public bool AddIfMissingNamed(Rule rule) {
		List<Rule> rules = GetRules(rule.collider, rule.collidee);
		if (rules != null && rules.Count > 0) {
			Rule found = rules.Find(r=>r.name == rule.name);
			if (found != null) {
				return false;
			}
		}
		Add(rule);
		return true;
	}
	public List<Rule> GetRules(CollisionTagged a, CollisionTagged b) { return GetRules(a.kind, b.kind); }
	public List<Rule> GetRules(CollisionTag a, CollisionTag b) {
		List<Rule> foundRules = null;
		if (collisionRuleLookupTables.TryGetValue(a, out var subDictionary)) {
			if (subDictionary.TryGetValue(b, out var ruleset)) {
				foreach (Rule r in ruleset) {
					if (r.AppliesTo(a, b)) {
						if (foundRules == null) { foundRules = new List<Rule>(); }
						foundRules.Add(r);
					}
				}
			}
		}
		return foundRules;
	}
	/// <summary>
	/// if the rule listing is changed in the editor, the dictionaries need to update to match.
	/// </summary>
	private void OnValidate() {
		collisionRuleLookupTables.Clear();
		UpdateRuleLookupDictionary();
#if UNITY_EDITOR
		collisionTagEditor.OnValidate();
#endif
	}
	public void DebugCollision(CollisionTagged[] collidables) {
		Debug.Log("Collision between "+ collidables[0]+" and "+ collidables[1]);
	}
	public void DestroyCollider(CollisionTagged[] collidables) {
		Destroy(collidables[0].gameObject);
	}
	public void DestroyCollidee(CollisionTagged[] collidables) {
		Destroy(collidables[1].gameObject);
	}
	public void DestroyBoth(CollisionTagged[] collidables) {
		Destroy(collidables[0].gameObject);
		Destroy(collidables[1].gameObject);
	}
#if UNITY_EDITOR
	/// ---------------------------------------------------------------------------------------------------------------
	const string TagEnumName = nameof(CollisionTag);
	[ContextMenuItem("Write Enum", nameof(__Write)), ContextMenuItem("Refresh List", nameof(__Refresh)),
		Tooltip("Convenience class for re-writing "+TagEnumName+".cs")]
	[SerializeField] public CollisionTagEditor collisionTagEditor = new CollisionTagEditor();
	private void __Refresh() { collisionTagEditor.__Refresh(); }
	private void __Write() { collisionTagEditor.__Write(); }
	[System.Serializable] public class CollisionTagEditor {
		public string[] collisionTags = new string[0];
		public bool refreshList;
		public bool writeList;
		public void OnValidate() {
			if (refreshList) { __Refresh(); refreshList = false; }
			if (writeList) { __Write(); writeList = false; }
		}
		public void __Refresh() { collisionTags = Enum.GetNames(typeof(CollisionTag)); }
		public void __Write() {
			if (collisionTags == null || collisionTags.Length == 0) {
				Debug.LogError("There should always be at least one value in the CollisionTag enumeration!");
				__Refresh();
				return;
			}
			string fileData = CalculateNewCollisionTagFile();
			NonStandard.Utility.File.RewriteAssetCSharpFile(TagEnumName + ".cs", fileData);
		}
		public string CalculateNewCollisionTagFile() {
			StringBuilder sb = new StringBuilder();
			sb.Append("public enum "+ TagEnumName+" {");
			for (int i = 0; i < collisionTags.Length; ++i) {
				if (i > 0) { sb.Append(", "); }
				sb.Append(collisionTags[i]);
			}
			sb.Append("}");
			return sb.ToString();
		}
	}
#endif
}
