using System;

namespace MrV {
	public class Map2d : IRect, IDrawable {
		private ConsoleTile[,] map;
		public ConsoleTile transparentLetter = new ConsoleTile('\0', ConsoleColor.Black, ConsoleColor.Black);

		public Map2d() => SetSize(Coord.Zero);

		public Map2d(Map2d toCopy) => Copy(toCopy);

		public Map2d(Coord size, ConsoleTile fill) {
			SetSize(size);
			Fill(fill);
		}
		public Map2d(Coord size) : this(size, ' ') { }

		public Map2d(string fileData) { LoadFromString(fileData); }

		public void SetSize(Coord newSize) {
			if (IsSize(newSize)) return;
			ConsoleTile[,] oldMap = map;
			map = new ConsoleTile[newSize.row, newSize.col];
			if (oldMap != null) {
				for (int row = 0; row < oldMap.GetLength(0) && row < Height; ++row) {
					for (int col = 0; col < oldMap.GetLength(1) && col < Width; ++col) {
						map[row, col] = oldMap[row, col];
					}
				}
			}
		}

		public void SetEach(System.Func<Coord, ConsoleTile> rowColumnValue) {
			for (int r = 0; r < Height; ++r) {
				for (int c = 0; c < Width; ++c) {
					map[r, c] = rowColumnValue(new Coord(c, r));
				}
			}
		}

		public void Fill(ConsoleTile fill) { map.Fill(fill); }

		public void Fill(ConsoleTile fill, Rect where) {
			map.SetAt(where.GetPosition(), where.GetSize(), fill);
		}

		public void Copy(Map2d m) {
			SetSize(m.GetSize());
			SetEach(coord => m[coord]);
		}

		public Coord GetSize() {
			if (map != null) {
				return Coord.SizeOf(map);
			}
			return Coord.Zero;
		}

		public int Height => map != null ? map.GetLength(0) : 0;
		public int Width => map?.GetLength(1) ?? 0;

		public ConsoleTile this[int row, int col] {
			get { return map[row, col]; }
			set { map[row, col] = value; }
		}

		public ConsoleTile this[Coord position] {
			get => map[position.row, position.col];
			set => map[position.row, position.col] = value;
		}

		//public void ForEach(Action<Coord> action) { Size.ForEach(action); }

		public bool Contains(Coord position) { return position.IsWithin(GetSize()); }

		public ConsoleTile[,] GetRawMap() => map;

		public void Release() { map = null; }

		public bool IsSize(Coord size) { return Height == size.row && Width == size.col; }

		public void Draw(ConsoleTile[,] drawBuffer, Coord position) {
			if (!Rect.GetSizeRectIntersect(Coord.Zero, Coord.SizeOf(drawBuffer), position, Coord.SizeOf(map), out Coord min, out Coord size)) {
				return;
			}
			Coord cursor, max = min + size;
			for (cursor.row = min.row; cursor.row < max.row; ++cursor.row) {
				for (cursor.col = min.col; cursor.col < max.col; ++cursor.col) {
					int y = cursor.row - position.row, x = cursor.col - position.col;
					ConsoleTile ct = map[y, x];
					if (transparentLetter == -1 || ct.letter != transparentLetter) {
						drawBuffer[cursor.row, cursor.col] = map[y, x];
					}
				}
			}
		}

		/// <summary>
		/// draw maps: https://notimetoplay.itch.io/ascii-mapper
		/// random generator: https://thenerdshow.com/amaze.html, https://www.dcode.fr/maze-generator, http://www.delorie.com/game-room/mazes/genmaze.cgi
		/// https://codepen.io/MittenedWatchmaker/pen/xpEvXd, https://raw.githubusercontent.com/dragonsploder/ascii-map-generator/master/AMG.py,
		/// https://rosettacode.org/wiki/Maze_generation#C.23
		/// </summary>
		/// <param name="filePathAndName"></param>
		public void LoadFile(string filePathAndName) {
			string text = TextUtil.StringFromFile(filePathAndName);
			if (text.Length > 0 && text[text.Length-1] == '\n') {
				text = text.Substring(0, text.Length - 1); // remove trailing endline
			}
			LoadFromString(text);
		}

		public static Map2d LoadFromFile(string filePathAndName) {
			Map2d m2d = new Map2d();
			m2d.LoadFile(filePathAndName);
			return m2d;
		}

		public void LoadFromString(string text) {
			Coord size = new Coord { row = 1, col = 0 };
			int lineWidth = 0;
			for (int i = 0; i < text.Length; ++i) {
				char c = text[i];
				switch (c) {
					case '\r': continue; // ignore linefeed
					case '\n': if (i == text.Length - 1) { continue; } // ignore EOF trailing newline
						size.row++;
						lineWidth = 0;
						break;
					default:
						lineWidth++;
						break;
				}
				if (lineWidth > size.col) {
					size.col = (short)lineWidth;
				}
			}
			SetSize(size);
			Coord cursor = Coord.Zero;
			for (int i = 0; i < text.Length; ++i) {
				char c = text[i];
				switch (c) {
					case '\r': continue; // ignore linefeed
					case '\n': if (i == text.Length - 1) { continue; } // ignore EOF trailing newline
						cursor.row++;
						cursor.col = 0;
						break;
					default:
						this[cursor] = c;
						cursor.col++;
						break;
				}
			}
		}
		public override string ToString() {
			Coord c = Coord.Zero, size = GetSize();
			if (!size.IsGreaterThan(Coord.Zero)) return "";
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			do {
				if (c.col == 0 && c.row > 0) { sb.Append('\n'); }
				sb.Append(this[c].letter);
			} while (c.Iterate(size));
			return sb.ToString();
		}

		public Rect GetRect() { return new Rect(0, 0, Width, Height); }

		public Coord GetPosition() { return Coord.Zero; }

		public void Render(Coord offset, Map2d backBuffer = null) {
			Coord cursor = Coord.Zero, size = GetSize();
			do {
				ConsoleTile c = this[cursor];
				if (backBuffer == null || backBuffer[cursor] != c) {
					Coord position = cursor + offset;
					if (!position.IsGreaterThanOrEqualTo(Coord.Zero)) continue;
					position.SetCursorPosition();
					c.Write();
				}
			} while (cursor.Iterate(size));
		}
	}
}
