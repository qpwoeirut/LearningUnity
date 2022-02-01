using System;

namespace MrV {
	// AABB : Axis Aligned Bounding Box
	public struct Rect : IPosition, IRect {
		public Coord min, max;

		public Rect(Coord min, Coord max) {
			this.min = min;
			this.max = max;
		}

		public Rect(int x, int y, int width, int height) {
			min = new Coord(x, y);
			max = new Coord(x + width, y + height);
		}

		public int X { get => min.col; set => min.col = (short)value; }
		public int Y { get => min.row; set => min.row = (short)value; }
		public int Width { get => max.col - min.col; set => max.col = (short)(min.col + value); }
		public int Height { get => max.row - min.row; set => max.row = (short)(min.row + value); }

		public int Top { get => min.row; set => min.row = (short)value; }
		public int Left { get => min.col; set => min.col = (short)value; }
		public int Bottom { get => max.row; set => max.row = (short)value; }
		public int Right { get => max.col; set => max.col = (short)value; }

		public Coord GetPosition() => min;
		public Coord GetSize() => max - min;
		public Rect GetRect() => this;
		public Coord Size => max - min;
		public bool Contains(Coord p) => p.X >= min.X && p.Y >= min.Y && p.X < max.X && p.Y < max.Y;
		public Coord GetOutOfBoundsDelta(Coord p) {
			if (Contains(p)) return Coord.Zero;
			Coord b = ClosestPoint(p);
			return p - b;
		}
		public Coord ClosestPoint(Coord p) {
			Coord b = p;
			if (p.X <= min.X) { b.X = min.X; }
			if (p.X >= max.X) { b.X = max.X; }
			if (p.Y <= min.Y) { b.Y = min.Y; }
			if (p.Y >= max.Y) { b.Y = max.Y; }
			return b;
		}

		public Rect Intersect(Rect r) {
			GetRectIntersect(min, max, r.min, r.max, out Coord iMin, out Coord iMax);
			return new Rect(iMin, iMax);
		}

		public bool TryGetIntersect(Rect r, out Rect intersection) => TryGetIntersect(this, r, out intersection);

		/// <summary>
		/// warning: delegates may cause memory leaks! be careful about allocated memory scope.
		/// </summary>
		/// <param name="locationAction"></param>
		public void ForEach(Action<Coord> locationAction) { Coord.ForEach(min, max, locationAction); }

		/// <summary>
		/// stops iterating as soon as action returns true.
		/// </summary>
		/// <param name="action">runs till the first return true</param>
		/// <returns>true if action returned true even once</returns>
		public bool ForEach(Func<Coord, bool> action) { return Coord.ForEach(min, max, action); }

		public bool IsIntersect(Rect other) { return IsRectIntersect(min, max, other.min, other.max); }

		public static Rect Sum(Rect a, Rect b) {
			b.Expand(a);
			return b;
		}

		public static bool IsRectIntersect(Coord aMin, Coord aMax, Coord bMin, Coord bMax) {
			return aMin.col < bMax.col && bMin.col < aMax.col && aMin.row < bMax.row && bMin.row < aMax.row;
		}

		public static bool IsSizeRectIntersect(Coord aMin, Coord aSize, Coord bMin, Coord bSize) {
			return IsRectIntersect(aMin, aMin + aSize, bMin, bMin + bSize);
		}

		public static bool GetRectIntersect(Coord aMin, Coord aMax, Coord bMin, Coord bMax, out Coord oMin, out Coord oMax) {
			oMin = new Coord { col = Math.Max(aMin.col, bMin.col), row = Math.Max(aMin.row, bMin.row) };
			oMax = new Coord { col = Math.Min(aMax.col, bMax.col), row = Math.Min(aMax.row, bMax.row) };
			return oMin.col < oMax.col && oMin.row < oMax.row;
		}

		public static bool TryGetIntersect(Rect a, Rect b, out Rect o) {
			return GetRectIntersect(a.min, a.max, b.min, b.max, out o.min, out o.max);
		}

		public static bool GetSizeRectIntersect(Coord aMin, Coord aSize, Coord bMin, Coord bSize, out Coord oMin, out Coord oSize) {
			bool result = GetRectIntersect(aMin, aMin + aSize, bMin, bMin + bSize, out oMin, out oSize);
			oSize -= oMin;
			return result;
		}

		/// <param name="nMin">needle min corner</param>
		/// <param name="nMax">needle max corner</param>
		/// <param name="hMin">haystack min corner</param>
		/// <param name="hMax">haystack max corner</param>
		/// <returns></returns>
		public static bool IsRectContained(Coord nMin, Coord nMax, Coord hMin, Coord hMax) {
			return nMin.col >= hMin.col && hMax.col >= nMax.col && nMin.row >= hMin.row && hMax.row >= nMax.row;
		}

		public static bool IsSizeRectContained(Coord nMin, Coord nSize, Coord hMin, Coord hSize) {
			return IsRectContained(nMin, nMin + nSize, hMin, hMin + hSize);
		}

		public static bool ExpandRectangle(Coord pMin, Coord pMax, ref Coord min, ref Coord max) {
			bool change = false;
			if (pMin.col < min.col) { min.col = pMin.col; change = true; }
			if (pMin.row < min.row) { min.row = pMin.row; change = true; }
			if (pMax.col > max.col) { max.col = pMax.col; change = true; }
			if (pMax.row > max.row) { max.row = pMax.row; change = true; }
			return change;
		}

		public bool Expand(Rect p) => ExpandRectangle(p.min, p.max, ref min, ref max);

		public bool Expand(Coord p) => ExpandRectangle(p, p, ref min, ref max);
	}
}