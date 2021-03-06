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

            public List<Coord> Neighbors() {
                return new List<Coord> { this + Up, this + Left, this + Down, this + Right };
            }

            public static Coord RandomDirection() {
                return Directions[Math.Abs(Environment.TickCount) % Directions.Length];
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
            private readonly int _moveRate;
            private int _lastMoveTime;

            public Entity(Coord p, char i, ConsoleColor c, int m) {
                Position = p;
                _icon = i;
                Color = c;
                _moveRate = m;
                _lastMoveTime = Environment.TickCount;
            }

            public void Draw() {
                Console.SetCursorPosition(Position.Col, Position.Row);
                ConsoleColor currentColor = Console.ForegroundColor;
                Console.ForegroundColor = Color;
                Console.Write(_icon);
                Console.ForegroundColor = currentColor;
            }

            public void Move() {
                if (Environment.TickCount >= _lastMoveTime + _moveRate) {
                    Position += Direction;
                    Direction = Coord.Zero;
                    _lastMoveTime = Environment.TickCount;
                }
            }
        }
        
        private enum Difficulty {  // npc intelligence out of 100, limit in seconds, delay before npc moves in milliseconds
            Easy = (70 * 100 + 60) * 1000 + 500,
            Medium = (75 * 100 + 45) * 1000 + 300,
            Hard = (80 * 100 + 30) * 1000 + 100
        }
        
        private enum GameState {
            Running, Won, BlendedWithHopperAndThatRandomFrogWhichIsGreen, RanOutOfTime, Quit
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
            private static readonly int LargeDistance = Rows * Cols;  // this number is larger than the length of any simple path through the grid
            
            private static readonly Random Rand = new Random();
            
            private char[,] _map;
            private Entity _player, _npc, _item;
            private List<Entity> _entities;
            
            private int _lastDraw;
            private const int DrawPause = 100;

            private int _startTime;
            private int _timeLimit;
            private int _npcIntelligence;

            private int[,] _dist;

            public GameState State;
            private bool _playerHasItem;
            private ConsoleKeyInfo _userInput;

            public void Init() {
                SendInstructions();
                Difficulty difficulty = ReadDifficultyInput();
                SendInformation(difficulty);
                
                _npcIntelligence = (int) difficulty / (1000 * 100);
                _timeLimit = ((int) difficulty / 1000) % 100;
                var npcPause = (int) difficulty % 1000;

                _map = new char[Rows, Cols];
                for (int row = 0; row < Rows; ++row) {
                    for (int col = 0; col < Cols; ++col) {
                        _map[row, col] = MapStr[row * (Cols + 1) + col];
                    }
                }

                _player = new Entity(RandValidCoord(), 'P', ConsoleColor.Green, 0);
                _npc = new Entity(RandValidCoord(), 'N', ConsoleColor.Red, npcPause);
                _item = new Entity(RandValidCoord(), 'I', ConsoleColor.Yellow, 0);
                _entities = new List<Entity> {_item, _npc, _player};

                State = GameState.Running;
                _playerHasItem = false;

                _startTime = Environment.TickCount;
            }

            private static void SendInstructions() {
                Console.WriteLine(
                    "You control the Player (P). To win, pick up the item (I) and then collide with the NPC (N).");
                Console.WriteLine("If you collide with the NPC before picking up the item, you lose!");
            }

            private void SendInformation(Difficulty difficulty) {
                Console.WriteLine($"\nYou are on difficulty {difficulty}, so you have {_timeLimit} seconds to win.");
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
                for (var row = 0; row < Rows; ++row) {
                    // maybe this will speed up the buffering thing
                    var tmp = "";
                    for (var col = 0; col < Cols; ++col) {
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
                        State = GameState.Quit;
                        break;
                }
                // npc either finds shortest path or picks random move
                _npc.Direction = _npcIntelligence >= Rand.Next(1, 101) ? CalculateBestMove(_npc.Position) : Coord.RandomDirection();
            }

            private Coord CalculateBestMove(Coord start) {
                CalculateDistanceGrid(_player.Position);
                
                // run away if player has item, chase if player doesn't
                var bestDistance = _playerHasItem ? 0 : LargeDistance;
                var bestDirection = Coord.Zero;
                foreach (var direction in Coord.Directions) {
                    var newCoord = start + direction;
                    if (!PositionIsValid(newCoord)) continue;
                    if ((_playerHasItem && bestDistance < _dist[newCoord.Row, newCoord.Col]) || 
                        (!_playerHasItem && bestDistance > _dist[newCoord.Row, newCoord.Col])) {
                        bestDistance = _dist[newCoord.Row, newCoord.Col];
                        bestDirection = direction;
                    }
                }

                return bestDirection;
            }

            private void CalculateDistanceGrid(Coord start) {  // bfs go brrr
                _dist = new int[Rows, Cols];
                for (var row = 0; row < Rows; ++row) {
                    for (var col = 0; col < Cols; ++col) {
                        _dist[row, col] = LargeDistance;
                    }
                }

                _dist[start.Row, start.Col] = 0;
                var queue = new Queue<Coord>();
                queue.Enqueue(start);
                while (queue.Count > 0) {
                    var cur = queue.Dequeue();
                    var curDist = _dist[cur.Row, cur.Col];
                    foreach (var neighbor in cur.Neighbors()) {
                        if (PositionIsValid(neighbor) && _dist[neighbor.Row, neighbor.Col] > curDist + 1) {
                            _dist[neighbor.Row, neighbor.Col] = curDist + 1;
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            private bool PositionIsValid(Coord position) {
                return position.IsInside(MapBoundaries) && MapValueAt(position) != '#';
            }

            public void Pause() {
                while (Environment.TickCount < _lastDraw + DrawPause) Thread.Sleep(5);
                _lastDraw = Environment.TickCount;
            }

            public void Update() {
                var oldPlayerPosition = _player.Position;
                var oldNpcPosition = _npc.Position;

                foreach (var entity in _entities) {
                    Coord previousPos = entity.Position;
                    entity.Move();
                    if (!PositionIsValid(entity.Position)) {
                        entity.Position = previousPos;
                    }
                }
                
                if (_player.Position == _item.Position) {
                    _playerHasItem = true;
                    _entities.Remove(_item);
                    _npc.Color = ConsoleColor.Blue;
                }

                if (_player.Position == _npc.Position ||
                    (_player.Position == oldNpcPosition && oldPlayerPosition == _npc.Position)) {
                    WriteToCenter(_playerHasItem ? "You win!" : "You lose!");
                    State = _playerHasItem ? GameState.Won : GameState.BlendedWithHopperAndThatRandomFrogWhichIsGreen;
                }

                if (_startTime + _timeLimit * 1000 <= Environment.TickCount) {
                    WriteToCenter("Ran out of time!");
                    State = GameState.RanOutOfTime;
                }
            }

            private void WriteToCenter(string message) {
                var middleRow = Rows / 2;
                var middleCol = (Cols - message.Length) / 2;
                Console.SetCursorPosition(middleCol, middleRow);
                Console.Write(message);
                Console.SetCursorPosition(0, Console.BufferHeight - 1);
            }

            public void WriteFinalMessage() {
                var currentColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                switch (State) {
                    case GameState.Won:
                        WriteToCenter("You won!");
                        break;
                    case GameState.RanOutOfTime:
                        WriteToCenter("Ran out of time!");
                        break;
                    case GameState.BlendedWithHopperAndThatRandomFrogWhichIsGreen:
                        WriteToCenter("Enjoy the blender!");
                        break;
                }
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
            while (g.State == GameState.Running) {
                g.Pause();
                g.GetUserInput();
                g.Update();
                g.Draw();
            }
            g.WriteFinalMessage();
        }
    }
}