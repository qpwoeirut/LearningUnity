using System;

namespace MrV {
	public class Game : CommandLineGameEngine {
		EntityMobileObject player, mrv;
		EntityBasic goal;
		protected override void InitScreen(out Coord size, out char defaultCharacter) {
			size = new Coord(60, 20);
			defaultCharacter = '\0';
		}
		protected override void InitInput(InputSystem appInput) {
			InputMap[] controlSchema = new InputMap[] {
			new InputMap(
				new KBind(ConsoleKey.W, () => PlayerMove(Coord.Up), "move player up"),
				new KBind(ConsoleKey.A, () => PlayerMove(Coord.Left), "move player left"),
				new KBind(ConsoleKey.S, () => PlayerMove(Coord.Down), "move player down"),
				new KBind(ConsoleKey.D, () => PlayerMove(Coord.Right), "move player right"),
				new KBind(ConsoleKey.UpArrow, () => PlayerMove(Coord.Up), "move player up"),
				new KBind(ConsoleKey.LeftArrow, () => PlayerMove(Coord.Left), "move player left"),
				new KBind(ConsoleKey.DownArrow, () => PlayerMove(Coord.Down), "move player down"),
				new KBind(ConsoleKey.RightArrow, () => PlayerMove(Coord.Right), "move player right")),
			new InputMap(
				new KBind(ConsoleKey.UpArrow, () => ScreenScroll(Coord.Up), "pan map up"),
				new KBind(ConsoleKey.LeftArrow, () => ScreenScroll(Coord.Left), "pan map left"),
				new KBind(ConsoleKey.DownArrow, () => ScreenScroll(Coord.Down), "pan map down"),
				new KBind(ConsoleKey.RightArrow, () => ScreenScroll(Coord.Right), "pan map right"))
			};
			int controlSchemeIndex = 0;
			InputMap system = new InputMap(
				new KBind(ConsoleKey.H, PrintKeyBindings, "show key bindings"),
				new KBind(ConsoleKey.M, () => {
					appInput.DisableInputMap(controlSchema[controlSchemeIndex]);
					++controlSchemeIndex; controlSchemeIndex %= controlSchema.Length;
					appInput.EnableInputMap(controlSchema[controlSchemeIndex]);
				}, "switch input mode"));
			appInput.EnableInputMap(system);
			appInput.EnableInputMap(controlSchema[controlSchemeIndex]);
		}
		void PlayerMove(Coord dir) {
			player.SetVelocity(dir);
			base.KeepPointOnScreen(player.position + dir);
		}
		protected override void InitData() {
			InitMaze();
			InitEntities();
			InitCollisionRules();
		}
		void InitMaze() {
			string mazeFile = "maze.txt";
			MazeGen.WriteMaze(99, 51, 1, 1, 123, mazeFile);
			map = Map2d.LoadFromFile(mazeFile);
			drawList.Add(map);
			collidableList.Add(map);
		}
		private Coord lastValidPlayerDirection = Coord.Zero;
		void InitEntities() {
			player = new EntityMobileObject("player", new ConsoleTile('@', ConsoleColor.Green), new Coord(1, 1), () => {
				if (player.velocity != Coord.Zero) {
					lastValidPlayerDirection = player.velocity;
				}
				player.velocity = Coord.Zero;
			});
			int nextMove = 0;
			Random randomNumberGenerator = new Random();
			mrv = new EntityMobileObject("Mr.V", new ConsoleTile('V', ConsoleColor.Cyan), new Coord(3, 3), () => {
				mrv.velocity = Coord.Zero;
				if (Environment.TickCount > nextMove) {
					Coord dir = Coord.CardinalDirections[randomNumberGenerator.Next() % Coord.CardinalDirections.Length];
					mrv.SetVelocity(dir);
					nextMove = Environment.TickCount + 100;
				}
			});
			goal = new EntityBasic("goal", new ConsoleTile('g', ConsoleColor.Yellow), map.GetSize() - Coord.Two);
			base.AddToLists(player);
			base.AddToLists(mrv);
			base.AddToLists(goal);
		}
		void InitCollisionRules() {
			base.AddCollisionRule(new CollisionRule("Map/Entity", typeof(Map2d), typeof(EntityMobileObject), EntityOnMap));
			base.AddCollisionRule(new CollisionRule("Entity/Entity", typeof(EntityMobileObject), typeof(EntityMobileObject), EntityToEntity));
			base.AddCollisionRule(new CollisionRule("Entity/EntityBasic", typeof(EntityMobileObject), typeof(EntityBasic), EntityToBasicEntity));
		}
		void EntityOnMap(object mapObject, object mobObject) {
			EntityMobileObject mob = (EntityMobileObject)mobObject;
			Map2d map = (Map2d)mapObject;
			char squareValue = map[mob.position].letter;
			if (IsPlayer(mob)) {
				switch (squareValue) {
					case ' ': map[mob.position] = '.'; return;
				}
			} else if (IsFireball(mob)) {
				switch (squareValue) {
					case '#':
						map[mob.position] = ',';
						Destroy(mob);
						return;
				}
			}
			switch (squareValue) {
				case '#': mob.position = mob.lastValidPosition; break;
			}
		}
		public bool IsPlayer(EntityMobileObject mob) => mob == player;
		public bool IsFireball(EntityMobileObject mob) => mob.icon.letter == '*';
		private bool wizardGranted = false;
		void EntityToEntity(object mobObject0, object mobObject1) {
			EntityMobileObject mob0 = (EntityMobileObject)mobObject0;
			EntityMobileObject mob1 = (EntityMobileObject)mobObject1;
			if (!wizardGranted && mob0 == mrv && mob1 == player) {
				SimpleMessageBox("You're a Wizard!\n\nPress space to shoot magic missiles!", ConsoleColor.Cyan);
				wizardGranted = true;
				player.icon = new ConsoleTile('W', ConsoleColor.Green, ConsoleColor.DarkGreen);
				inputSystem.EnableInputMap(new InputMap(new KBind(ConsoleKey.Spacebar, () => {
					ShootMagicMissile(new ConsoleTile('*', ConsoleColor.Red), player.position, lastValidPlayerDirection);
				}, "shoot magic missile")));
			}
		}
		void ShootMagicMissile(ConsoleTile tile, Coord position, Coord direction) {
			int nextMove = 0;
			long timelimit = Environment.TickCount + 3000;
			EntityMobileObject missile = null;
			missile = new EntityMobileObject("magic missile", tile, position, () => {
				if (Environment.TickCount > timelimit) { Destroy(missile); }
				if (Environment.TickCount > nextMove) {
					nextMove = Environment.TickCount + 50;
					missile.SetVelocity(direction);
				} else {
					missile.SetVelocity(Coord.Zero);
				}
			});
			base.AddToLists(missile);
		}
		void EntityToBasicEntity(object mobObject, object basicObject) {
			EntityMobileObject mob = (EntityMobileObject)mobObject;
			EntityBasic basic = (EntityBasic)basicObject;
			if (mob == player && basic == goal) {
				SimpleMessageBox("You Won!", ConsoleColor.Yellow);
				status = GameStatus.Ended;
			}
		}
		void PrintKeyBindings() {
			SimpleMessageBox(GetInputDescription());
		}
		void SimpleMessageBox(string message, ConsoleColor textColor = ConsoleColor.White) {
			Coord screenSize = screen.GetSize();
			const int insetSize = 2, width = 40;
			Rect area = new Rect(insetSize, insetSize, width, screenSize.row - insetSize * 2);
			Coord inset = new Coord(insetSize, insetSize / 2);
			ConsoleTile messageBack = new ConsoleTile('.', ConsoleColor.Black, ConsoleColor.DarkGray);
			base.MessageBox(message, textColor, area, messageBack, inset);
		}
	}
}
