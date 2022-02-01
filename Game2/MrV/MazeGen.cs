using System;
using System.Collections.Generic;
using System.Text;

namespace MrV {
	public class MazeGen {
		const char Filled = '#', Walkable = ' ';
		public static bool show = false;
		public static void WriteMaze(int width, int height, int startx, int starty, int seed, string filename) {
			string maze = CreateMaze(new Coord(width, height), new Coord(startx, starty), seed);
			System.IO.File.WriteAllText(filename, maze);
		}
		public static string CreateMaze(Coord size, Coord start, int seed) {
			MazeGen mg = new MazeGen(seed);
			char[,] generatedMap = mg.Generate(size, start);
			return ToString(generatedMap);
		}
		public static string ToString(char[,] map) {
			StringBuilder sb = new StringBuilder();
			Coord size = map.GetSize(), cursor;
			for (cursor.row = 0; cursor.row < size.row; ++cursor.row) {
				for (cursor.col = 0; cursor.col < size.col; ++cursor.col) {
					sb.Append(map.At(cursor));
				}
				sb.Append('\n');
			}
			return sb.ToString();
		}
		Random random;
		MazeGen(int seed) {
			random = new Random(seed);
		}
		char[,] Generate(Coord size, Coord start) {
			char[,] map = new char[size.row, size.col];
			map.Fill(Filled);
			if (start.IsWithin(size)) {
				map.SetAt(start, Walkable);
			}
			MazeWalk(map, start);
			return map;
		}
		public static void Show(char[,] map) {
			Console.SetCursorPosition(0, 0);
			Console.Write(ToString(map));
			Console.ReadKey();
		}
		void MazeWalk(char[,] map, Coord startingPoint) {
			List<Coord> possibleIntersections = new List<Coord>();
			possibleIntersections.Add(startingPoint);
			bool userNeedsToSeeMaze = true;
			while (possibleIntersections.Count > 0) {
				if (show && userNeedsToSeeMaze) { Show(map); }
				userNeedsToSeeMaze = MazeWalkOneStep(map, possibleIntersections);
			}
		}
		bool MazeWalkOneStep(char[,] map, List<Coord> possibleIntersections) {
			int intersectionToTry = random.Next(possibleIntersections.Count);
			Coord position = possibleIntersections[intersectionToTry];
			List<Coord> possibleNextSteps = PossibleNextSteps(position, map);
			if (possibleNextSteps.Count == 0) {
				possibleIntersections.RemoveAt(intersectionToTry);
				return false;
			}
			int whichStepToTake = random.Next(possibleNextSteps.Count);
			Coord dir = possibleNextSteps[whichStepToTake];
			position += dir;
			map.SetAt(position, Walkable);
			position += dir;
			map.SetAt(position, Walkable);
			possibleIntersections.Add(position);
			return true;
		}
		static readonly Coord[] _directions = new Coord[] { Coord.Up, Coord.Left, Coord.Down, Coord.Right };
		public List<Coord> PossibleNextSteps(Coord start, char[,] map) {
			List<Coord> possibleNext = new List<Coord>();
			for (int i = 0; i < _directions.Length; ++i) {
				if (IsAbleToDigMazeTunnel(start, _directions[i], map)) {
					possibleNext.Add(_directions[i]);
				}
			}
			return possibleNext;
		}
		bool IsAbleToDigMazeTunnel(Coord position, Coord direction, char[,] map) {
			Coord size = map.GetSize();
			Coord next = position + direction;
			if (!next.IsWithin(size)) return false;
			Coord end = next + direction;
			if (!end.IsWithin(size)) return false;
			char n = map.At(next), e = map.At(end);
			return n==Filled && e==Filled;
		}
	}
}
