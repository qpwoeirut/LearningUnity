using System;
using System.Collections.Generic;
using System.Text;

namespace MrV {
	public abstract class CommandLineGameEngine {
		public enum GameStatus { None, Running, Ended }
		public GameStatus status;
		protected InputSystem inputSystem;
		protected Map2d screen, backbuffer;
		protected Map2d map;
		protected Coord drawOffset = Coord.Zero;
		protected List<IDrawable> drawList = new List<IDrawable>();
		protected List<IUpdatable> updateList = new List<IUpdatable>();
		protected List<IRect> collidableList = new List<IRect>();
		protected List<ConsoleKeyInfo> inputQueue = new List<ConsoleKeyInfo>();
		protected List<object> destroyList = new List<object> ();
		private List<CollisionRule> collisionRules = new List<CollisionRule>();
		private Dictionary<Type, Dictionary<Type, List<CollisionRule>>> collisionRuleLookupTables = 
			new Dictionary<Type, Dictionary<Type, List<CollisionRule>>>();

		public virtual void Init() {
			InitScreen(out Coord size, out char defaultChar);
			InitScreen(size, defaultChar);
			inputSystem = new InputSystem();
			InitInput(inputSystem);
			InitData();
			UpdateRuleLookupDictionary();
			status = GameStatus.Running;
		}
		public virtual void Release() {
		}
		public virtual void Draw() {
			screen.Fill(ConsoleTile.DefaultTile);
			ConsoleTile[,] drawBuffer = screen.GetRawMap();
			Render(drawBuffer);
			screen.Render(Coord.Zero, backbuffer);
			Map2d temp = screen; screen = backbuffer; backbuffer = temp;
			ConsoleTile.DefaultTile.ApplyColor();
			Console.SetCursorPosition(0, screen.Height);
		}
		public void ScreenScroll(Coord dir) { drawOffset -= dir; }
		public void KeepPointOnScreen(Coord position) {
			Coord screenMargin = new Coord(3, 2);
			Rect viewArea = new Rect(screenMargin, screen.GetSize() - Coord.One - screenMargin);
			Coord distanceFromScreen = viewArea.GetOutOfBoundsDelta(position + drawOffset);
			drawOffset -= distanceFromScreen;
		}
		protected virtual void Render(ConsoleTile[,] drawBuffer) {
			for (int i = 0; i < drawList.Count; ++i) {
				drawList[i].Draw(drawBuffer, drawOffset);
			}
		}
		protected void InitScreen(Coord size, char initialCharacter) {
			screen = new Map2d(size, initialCharacter);
			backbuffer = new Map2d(size, initialCharacter);
		}
		public void Update() {
			ServiceInputQueue();
			for (int i = 0; i < updateList.Count; ++i) {
				updateList[i].Update();
			}
			CollisionUpdate();
			for (int i = 0; i < updateList.Count; ++i) {
				EntityMobileObject mob = updateList[i] as EntityMobileObject;
				if (mob != null) {
					mob.lastValidPosition = mob.position;
				}
			}
			for (int i = 0; i < destroyList.Count; ++i) {
				object obj = destroyList[i];
				if (obj is IDrawable drawable) { drawList.Remove(drawable); }
				if (obj is IUpdatable updatable) { updateList.Remove(updatable); }
				if (obj is IRect collidable) { collidableList.Remove(collidable); }
			}
		}
		public void Destroy(object obj) {
			destroyList.Add(obj);
		}
		public void AddToLists(object obj) {
			if (obj is IDrawable drawable) { drawList.Add(drawable); }
			if (obj is IUpdatable updatable) { updateList.Add(updatable); }
			if (obj is IRect collidable) { collidableList.Add(collidable); }
		}
		public void Input() {
			ConsoleKeyInfo input;
			while (Console.KeyAvailable) {
				input = Console.ReadKey();
				Console.Write("\b "); // backspace and overwrite typed character
				inputQueue.Add(input);
				if (input.Key == ConsoleKey.Escape) { status = GameStatus.Ended; return; }
			}
		}
		public void ServiceInputQueue() {
			do {
				if (inputQueue.Count > 0) {
					ConsoleKeyInfo input = inputQueue[0];
					inputQueue.RemoveAt(0);
					if (!inputSystem.DoKeyPress(input)) {
						// Console.WriteLine(input.Key); // uncomment to show unexpected key presses
					}
				}
			} while (inputQueue.Count > 0);
		}
		public string GetInputDescription() {
			StringBuilder sb = new StringBuilder();
			foreach (var kbindEntry in inputSystem.currentKeyBinds.keyBinds) {
				List<KBind> kbinds = kbindEntry.Value;
				for (int i = 0; i < kbinds.Count; ++i) {
					KBind kbind = kbinds[i];
					sb.Append(kbind.key.ToString()).Append(" ").Append(kbind.description).Append("\n");
				}
			}
			return sb.ToString();
		}
		public void MessageBox(string message, ConsoleColor txtColor, Rect area, ConsoleTile back, Coord inset) {
			screen.Fill(back, area);
			screen.Render(Coord.Zero, backbuffer);
			Console.BackgroundColor = back.Back;
			Console.ForegroundColor = txtColor;
			Coord cursor = area.min + inset;
			bool spaceLeftToWrite = true;
			for (int i = 0; spaceLeftToWrite && i < message.Length; ++i) {
				if (cursor.X >= area.max.X - inset.X) {
					for (; i < message.Length && i != '\n'; ++i) ; // no wrap. skip all chars till new line
					if (i > message.Length) { break; }
				}
				char c = message[i];
				switch (c) {
					case '\n':
						cursor.X = area.min.X + inset.X;
						cursor.Y++;
						if (cursor.Y >= area.max.Y - inset.Y) {
							spaceLeftToWrite = false;
						}
						break;
					default:
						Console.SetCursorPosition(cursor.X, cursor.Y);
						Console.Write(c);
						cursor.X++;
						break;
				}
			}
			ConsoleTile.DefaultTile.ApplyColor();
			Console.ReadKey();
			screen.Fill(ConsoleTile.DefaultTile);
			backbuffer.Fill(ConsoleTile.DefaultTile);
		}
		public class CollisionRule {
			public string name;
			public Type collider, collidee;
			public NotifyCollision onCollision;
			public delegate void NotifyCollision(object a, object b);
			public CollisionRule(string name, Type collider, Type collidee, NotifyCollision onCollision) {
				this.name = name; this.collider = collider; this.collidee = collidee; this.onCollision = onCollision;
			}
			public bool AppliesTo(Type collider, Type collidee) {
				return this.collidee == collidee && this.collider == collider;
			}
		}
		public void AddCollisionRule(CollisionRule r) {
			collisionRules.Add(r);
			UpdateRuleLookupDictionary(r);
		}
		public void UpdateRuleLookupDictionary() {
			for (int i = 0; i < collisionRules.Count; i++) {
				UpdateRuleLookupDictionary(collisionRules[i]);
			}
		}
		private void UpdateRuleLookupDictionary(CollisionRule r) {
			if (!collisionRuleLookupTables.TryGetValue(r.collider, out Dictionary<Type, List<CollisionRule>> subDictionary)) {
				subDictionary = new Dictionary<Type, List<CollisionRule>>();
				collisionRuleLookupTables[r.collider] = subDictionary;
			}
			if (!subDictionary.TryGetValue(r.collidee, out List<CollisionRule> ruleSet)) {
				ruleSet = new List<CollisionRule>();
				subDictionary[r.collidee] = ruleSet;
			}
			ruleSet.Add(r);
		}
		public List<CollisionRule> GetRules(Type a, Type b) {
			List<CollisionRule> foundRules = null;
			// O(n)
			//for (int i = 0; i < collisionRules.Count; i++) {
			//	CollisionRule r = collisionRules[i];
			//	if (r.AppliesTo(a, b)) {
			//		if (foundRules == null) { foundRules = new List<CollisionRule>(); }
			//		foundRules.Add(r);
			//	}
			//}

			// O(1)
			if (collisionRuleLookupTables.TryGetValue(a, out var subDictionary)) {
				if (subDictionary.TryGetValue(b, out var ruleset)) {
					foreach (CollisionRule r in ruleset) {
						if (r.AppliesTo(a, b)) {
							if (foundRules == null) { foundRules = new List<CollisionRule>(); }
							foundRules.Add(r);
						}
					}
				}
			}
			return foundRules;
		}
		public void CollisionUpdate() {
			// O(n^2) --TODO create Quadtree?
			for (int i = 0; i < collidableList.Count; ++i) {
				object a = collidableList[i];
				Rect rectA = collidableList[i].GetRect();
				for (int j = i + 1; j < collidableList.Count; ++j) {
					object b = collidableList[j];
					Rect rectB = collidableList[j].GetRect();
					if (rectA.TryGetIntersect(rectB, out Rect intersect)) {
						CollisionAttempt(a, rectA, b, rectB);
						CollisionAttempt(b, rectB, a, rectA);
					}
				}
			}
		}
		private void CollisionAttempt(object a, Rect rectA, object b, Rect rectB) {
			List<CollisionRule> foundRules = GetRules(a.GetType(), b.GetType());
			if (foundRules == null) { return; }
			for (int i = 0; i < foundRules.Count; ++i) {
				CollisionRule rule = foundRules[i];
				rule.onCollision(a, b);
			}
		}
		protected abstract void InitInput(InputSystem appInput);
		protected abstract void InitScreen(out Coord size, out char defaultCharacter);
		protected abstract void InitData();
	}
}
