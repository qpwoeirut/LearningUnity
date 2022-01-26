using System;
using System.Collections.Generic;

namespace Game1_1 {
    // TODO: stop npc from moving more than 2x/sec
    class Program {
        public readonly struct Coord {
            public readonly short Row, Col;

            public Coord(int row, int col) {
                this.Row = (short) row;
                this.Col = (short) col;
            }

            public bool IsInside(Coord limit) {
                return 0 <= Row && Row < limit.Row && 0 <= Col && Col < limit.Col;
            }

            public static bool operator ==(Coord a, Coord b) {
                return a.Row == b.Row && a.Col == b.Col;
            }

            public static bool operator !=(Coord a, Coord b) {
                return a.Row != b.Row || a.Col != b.Col;
            }

            public static Coord operator +(Coord a, Coord b) {
                return new Coord(a.Row + b.Row, a.Col + b.Col);
            }

            public static Coord
                Zero = new Coord(0, 0),
                Up = new Coord(-1, 0),
                Left = new Coord(0, -1),
                Down = new Coord(1, 0),
                Right = new Coord(0, 1);

            public static readonly Coord[] Directions = {Up, Left, Down, Right};
        }

        public class Entity {
            public Coord Position;
            public Coord Direction;
            private readonly char _icon;
            public ConsoleColor Color;

            public Entity(Coord p, char i, ConsoleColor c) {
                Position = p;
                _icon = i;
                Color = c;
            }

            public void Draw() {
                Console.SetCursorPosition(Position.Col, Position.Row);
                ConsoleColor currentColor = Console.ForegroundColor;
                Console.ForegroundColor = Color;
                Console.Write(_icon);
                Console.ForegroundColor = currentColor;
            }

            public void Move() {
                Position += Direction;
                Direction = Coord.Zero;
            }
        }

        private class Game {
            private static readonly Random Rand = new Random();
            char[,] map;
            Entity player, npc, item;
            List<Entity> entities;
            public bool Running;
            public bool PlayerHasItem;
            ConsoleKeyInfo userInput;

            private const string MapStr =
                "..............................\n" +
                "..............................\n" +
                "..............................\n" +
                "..............................\n" +
                ".................########.....\n" +
                "................#.............\n" +
                "................#..#####......\n" +
                "....######......#......###....\n" +
                ".........#......#####....###..\n" +
                ".........#......#..........#..\n" +
                "...#######......#..........#..\n" +
                "...#######......#..........#..\n" +
                "................#..........#..\n" +
                "................############..\n" +
                "..............................\n";

            private static readonly int Cols = MapStr.IndexOf('\n');
            private static readonly int Rows = MapStr.Length / (Cols + 1);
            private static readonly Coord MapBoundaries = new Coord(Rows, Cols); // not really a coord but whatever

            public void Init() {
                Console.WriteLine(
                    "You control the Player (P). To win, pick up the item (I) and then collide with the NPC (N).");
                Console.WriteLine("If you collide with the NPC before picking up the item, you lose!");
                Console.WriteLine("Hit any key to continue.");
                Console.ReadKey();

                map = new char[Rows, Cols];
                for (int row = 0; row < Rows; ++row) {
                    for (int col = 0; col < Cols; ++col) {
                        map[row, col] = MapStr[row * (Cols + 1) + col];
                    }
                }

                player = new Entity(RandValidCoord(), 'P', ConsoleColor.Green);
                npc = new Entity(RandValidCoord(), 'N', ConsoleColor.Red);
                item = new Entity(RandValidCoord(), 'I', ConsoleColor.Yellow);
                entities = new List<Entity> {player, npc, item};
                
                Running = true;
                PlayerHasItem = false;
            }

            public void Draw() {
                Console.SetCursorPosition(0, 0);
                for (int row = 0; row < Rows; ++row) {
                    // maybe this will speed up the buffering thing
                    string tmp = "";
                    for (int col = 0; col < Cols; ++col) {
                        tmp += map[row, col];
                    }
                    Console.WriteLine(tmp);
                }

                foreach (var entity in entities) {
                    entity.Draw();
                }

                Console.SetCursorPosition(0, Rows);
            }

            public void GetUserInput() {
                userInput = Console.KeyAvailable ? Console.ReadKey() : new ConsoleKeyInfo();
                switch (userInput.KeyChar) {
                    case 'w':
                        player.Direction = Coord.Up;
                        break;
                    case 'a':
                        player.Direction = Coord.Left;
                        break;
                    case 's':
                        player.Direction = Coord.Down;
                        break;
                    case 'd':
                        player.Direction = Coord.Right;
                        break;
                    case 'q':
                    case (char) 27:
                        Running = false;
                        break;
                }

                npc.Direction = Coord.Directions[Math.Abs(Environment.TickCount) % Coord.Directions.Length];
            }

            public void Update() {
                var oldPlayerPosition = player.Position;
                var oldNpcPosition = npc.Position;

                foreach (var entity in entities) {
                    Coord previousPos = entity.Position;
                    entity.Move();
                    if (!entity.Position.IsInside(MapBoundaries) || MapValueAt(entity.Position) == '#') {
                        entity.Position = previousPos;
                    }
                }

                if (player.Position == item.Position) {
                    PlayerHasItem = true;
                    entities.Remove(item);
                    npc.Color = ConsoleColor.Blue;
                }

                if (player.Position == npc.Position ||
                    (player.Position == oldNpcPosition && oldPlayerPosition == npc.Position)) {
                    Console.ForegroundColor = ConsoleColor.White;
                    var middleRow = Rows / 2;
                    var middleCol = Cols / 2 - ("You lose!".Length / 2);
                    Console.SetCursorPosition(middleCol, middleRow);
                    Console.Write(PlayerHasItem ? "You win!" : "You lose!");
                    Console.SetCursorPosition(0, Rows);

                    Running = false;
                }
            }

            private static Coord RandCoord() {
                return new Coord(Rand.Next(MapBoundaries.Row - 1), Rand.Next(MapBoundaries.Col - 1));
            }

            private Coord RandValidCoord() {
                var c = RandCoord();
                return MapValueAt(c) == '.' ? c : RandValidCoord();
            }

            private char MapValueAt(Coord coord) {
                return map[coord.Row, coord.Col];
            }
        }

        public static void Main(string[] args) {
            var g = new Game();
            g.Init();
            while (g.Running) {
                g.Draw();
                System.Threading.Thread.Sleep(100);
                g.GetUserInput();
                g.Update();
            }
        }
    }
}