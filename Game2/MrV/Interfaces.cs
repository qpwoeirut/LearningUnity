namespace MrV {
	public interface IUpdatable {
		void Update();
	}
	public interface IPosition {
		Coord GetPosition();
	}
	public interface IRect : IPosition {
		Rect GetRect();
	}
	public interface IDrawable {
		void Draw(ConsoleTile[,] screen, Coord offset);
	}
}
