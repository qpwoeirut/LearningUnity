using MrV;

namespace Game2 {
	public class Program {
		public static void Main(string[] args) {
			Game g = new Game();
			g.Init();
			while(g.status == Game.GameStatus.Running) {
				g.Draw();
				g.Input();
				g.Update();
			}
			g.Release();
		}
	}
}