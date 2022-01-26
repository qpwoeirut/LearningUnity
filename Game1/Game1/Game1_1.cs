using System;
using System.Collections.Generic;
using System.Threading;

namespace Game1_1 {
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

        private class Entity {
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
        
        private enum Difficulty {
            Easy = 60 * 1000 + 500,
            Medium = 45 * 1000 + 100,
            Hard = 30 * 1000 + 10
        }

        private class Game {
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
            
            private static readonly Random Rand = new Random();
            
            private char[,] _map;
            private Entity _player, _npc, _item;
            private List<Entity> _entities;
            
            private Difficulty _difficulty;
            private int _lastDraw;
            private int _lastNpcMove;
            private bool _moveNpcOk = true;
            private const int DrawPause = 100;

            private int _startTime;
            private int _timeLimit;
            private int _npcPause;
            
            public bool Running;
            private bool _playerHasItem;
            private ConsoleKeyInfo _userInput;

            public void Init() {
                SendWelcome();

                _map = new char[Rows, Cols];
                for (int row = 0; row < Rows; ++row) {
                    for (int col = 0; col < Cols; ++col) {
                        _map[row, col] = MapStr[row * (Cols + 1) + col];
                    }
                }

                _player = new Entity(RandValidCoord(), 'P', ConsoleColor.Green);
                _npc = new Entity(RandValidCoord(), 'N', ConsoleColor.Red);
                _item = new Entity(RandValidCoord(), 'I', ConsoleColor.Yellow);
                _entities = new List<Entity> {_item, _npc, _player};
                
                Running = true;
                _playerHasItem = false;

                _startTime = Environment.TickCount;
            }

            private void SendWelcome() {
                Console.WriteLine(
                    "You control the Player (P). To win, pick up the item (I) and then collide with the NPC (N).");
                Console.WriteLine("If you collide with the NPC before picking up the item, you lose!");
                _difficulty = ReadDifficultyInput();
                _timeLimit = (int)_difficulty / 1000;
                _npcPause = (int)_difficulty % 1000;
                Console.WriteLine($"\nYou are on difficulty {_difficulty}, so you have {_timeLimit} seconds to win.");
                Console.WriteLine("Hit any key to continue.");
                Console.ReadKey();
                Console.Clear();
            }

            private static Difficulty ReadDifficultyInput() {
                Console.WriteLine("Choose the [E]asy, [M]edium, or [H]ard difficulty.");
                var difficultyChar = char.ToLower(Console.ReadKey().KeyChar);
                while (!"emh".Contains(difficultyChar.ToString())) difficultyChar = char.ToLower(Console.ReadKey().KeyChar);
                switch (difficultyChar) {
                    case 'e': return Difficulty.Easy;
                    case 'm': return Difficulty.Medium;
                    case 'h': return Difficulty.Hard;
                }
                throw new Exception();
            }

            public void Draw() {
                Console.SetCursorPosition(0, 0);
                for (int row = 0; row < Rows; ++row) {
                    // maybe this will speed up the buffering thing
                    string tmp = "";
                    for (int col = 0; col < Cols; ++col) {
                        tmp += _map[row, col];
                    }
                    Console.WriteLine(tmp);
                }
                var timeElapsed = (Environment.TickCount - _startTime) / 1000;
                Console.WriteLine($"Time: {timeElapsed}");

                foreach (var entity in _entities) {
                    entity.Draw();
                }

                Console.SetCursorPosition(0, Console.BufferHeight - 1);
            }

            public void GetUserInput() {
                _userInput = Console.KeyAvailable ? Console.ReadKey() : new ConsoleKeyInfo();
                switch (_userInput.KeyChar) {
                    case 'w':
                        _player.Direction = Coord.Up;
                        break;
                    case 'a':
                        _player.Direction = Coord.Left;
                        break;
                    case 's':
                        _player.Direction = Coord.Down;
                        break;
                    case 'd':
                        _player.Direction = Coord.Right;
                        break;
                    case 'q':
                    case (char) 27:
                        Running = false;
                        break;
                }

                _npc.Direction = Coord.Directions[Math.Abs(Environment.TickCount) % Coord.Directions.Length];
            }

            public void Pause() {
                while (Environment.TickCount < _lastDraw + DrawPause) Thread.Sleep(5);
                _lastDraw = Environment.TickCount;
                _moveNpcOk = _lastDraw >= _lastNpcMove + _npcPause;
                if (_moveNpcOk) _lastNpcMove = _lastDraw;
            }

            public void Update() {
                var oldPlayerPosition = _player.Position;
                var oldNpcPosition = _npc.Position;

                foreach (var entity in _entities) {
                    Coord previousPos = entity.Position;
                    entity.Move();
                    if (!entity.Position.IsInside(MapBoundaries) || MapValueAt(entity.Position) == '#') {
                        entity.Position = previousPos;
                    }
                }

                if (!_moveNpcOk) _npc.Position = oldNpcPosition;

                if (_player.Position == _item.Position) {
                    _playerHasItem = true;
                    _entities.Remove(_item);
                    _npc.Color = ConsoleColor.Blue;
                }

                if (_player.Position == _npc.Position ||
                    (_player.Position == oldNpcPosition && oldPlayerPosition == _npc.Position)) {
                    WriteToCenter(_playerHasItem ? "You win!" : "You lose!");
                    Running = false;
                }

                if (_startTime + _timeLimit * 1000 <= Environment.TickCount) {
                    WriteToCenter("Ran out of time!");
                    Running = false;
                }
            }

            private void WriteToCenter(string message) {
                var currentColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                var middleRow = Rows / 2;
                var middleCol = (Cols - message.Length) / 2;
                Console.SetCursorPosition(middleCol, middleRow);
                Console.Write(message);
                Console.SetCursorPosition(0, Console.BufferHeight - 1);
                Console.ForegroundColor = currentColor;
            }

            private static Coord RandCoord() {
                return new Coord(Rand.Next(Rows), Rand.Next(Cols));
            }

            // TODO make sure things dont spawn on top of each other
            private Coord RandValidCoord() {
                var c = RandCoord();
                while (MapValueAt(c) != '.') c = RandCoord();
                return c;
            }

            private char MapValueAt(Coord coord) {
                return _map[coord.Row, coord.Col];
            }
        }

        public static void Main(string[] args) {
            var g = new Game();
            g.Init();
            g.Draw();
            while (g.Running) {
                g.Pause();
                g.GetUserInput();
                g.Update();
                g.Draw();
            }
        }
    }
}