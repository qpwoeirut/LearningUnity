using System;
using System.Collections.Generic;

namespace Game1 {
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

        public char MapValue(char[,] map) {
            return map[Position.Row, Position.Col];
        }
    }

    internal class Program {
        private static readonly Random Rand = new Random();

        private const string MapStr =
            "..............................\n" +
            "..............................\n" +
            "..............................\n" +
            "..............................\n" +
            ".................########.....\n" +
            "................#.............\n" +
            "................#..#####......\n" +
            "................#......###....\n" +
            "................#####....###..\n" +
            "................#..........#..\n" +
            "................#..........#..\n" +
            "................#..........#..\n" +
            "................#..........#..\n" +
            "................############..\n" +
            "..............................\n";


        private static readonly int Cols = MapStr.IndexOf('\n');
        private static readonly int Rows = MapStr.Length / Cols;

        private static readonly Coord MapBoundaries = new Coord(Rows, Cols); // not really a coord but whatever

        public static void OldMain(string[] args) {
            Console.WriteLine(
                "You control the Player (P). To win, pick up the item (I) and then collide with the NPC (N).");
            Console.WriteLine("If you collide with the NPC before picking up the item, you lose!");
            Console.WriteLine("Hit any key to continue.");
            Console.ReadKey();

            // convert the mapStr into an easily editable 2d char grid
            char[,] map = new char[Rows, Cols];
            for (int row = 0; row < Rows; ++row) {
                for (int col = 0; col < Cols; ++col) {
                    map[row, col] = MapStr[row * (Cols + 1) + col];
                }
            }

            var player = new Entity(RandCoord(), 'P', ConsoleColor.Green);
            var npc = new Entity(RandCoord(), 'N', ConsoleColor.Red);
            var item = new Entity(RandCoord(), 'I', ConsoleColor.Yellow);
            var entities = new List<Entity> {player, npc, item};

            var running = true;
            var playerHasItem = false;
            while (running) {
                Console.Clear();
                for (int row = 0; row < Rows; ++row) {
                    for (int col = 0; col < Cols; ++col) {
                        Console.Write(map[row, col]);
                    }

                    Console.WriteLine();
                }

                foreach (var entity in entities) {
                    entity.Draw();
                }

                Console.SetCursorPosition(0, Rows);
                ConsoleKeyInfo userInput = Console.ReadKey();
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
                        running = false;
                        break;
                }

                npc.Direction = Coord.Directions[Math.Abs(Environment.TickCount) % Coord.Directions.Length];

                var oldPlayerPosition = player.Position;
                var oldNpcPosition = npc.Position;

                foreach (var entity in entities) {
                    Coord previousPos = entity.Position;
                    entity.Move();
                    if (!entity.Position.IsInside(MapBoundaries) || entity.MapValue(map) == '#') {
                        entity.Position = previousPos;
                    }
                }

                if (player.Position == item.Position) {
                    playerHasItem = true;
                    entities.Remove(item);
                    npc.Color = ConsoleColor.Blue;
                }

                if (player.Position == npc.Position ||
                    (player.Position == oldNpcPosition && oldPlayerPosition == npc.Position)) {
                    Console.ForegroundColor = ConsoleColor.White;
                    var middleRow = Rows / 2;
                    var middleCol = Cols / 2 - ("You lose!".Length / 2);
                    Console.SetCursorPosition(middleCol, middleRow);
                    Console.Write(playerHasItem ? "You win!" : "You lose!");
                    Console.SetCursorPosition(0, Rows);

                    running = false;
                }
            }
        }

        private static Coord RandCoord() {
            return new Coord(Rand.Next(MapBoundaries.Row - 1), Rand.Next(MapBoundaries.Col - 1));
        }
    }
}