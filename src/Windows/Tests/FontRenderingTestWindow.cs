using SDL_Sharp;

public class FontRenderingTestWindow : SteamWindow
{
	public FontRenderingTestWindow(Steam steam, string title, int width, int height, bool resizable = false, int minimumWidth = 0, int minimumHeight = 0) : base(steam, title, width, height, resizable, minimumWidth, minimumHeight)
	{
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);
	}

	public override void Draw()
	{
		base.Draw();

		panel.DrawText("Hello, world!", 100, 40, new Color(255, 255, 255, 255));

		panel.DrawText("abcdefghijklmnopqrstuvwxyz", 100, 80, new Color(255, 255, 255, 255));

		panel.DrawText("0123456789", 100, 120, new Color(255, 255, 255, 255));

		panel.DrawText("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 100, 160, new Color(255, 255, 255, 255));

		panel.DrawText("!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~", 100, 200, new Color(255, 255, 255, 255));

		panel.DrawText("Hello, world! this is a test!", 100, 240, new Color(255, 255, 255, 255));

		panel.DrawText("A A - spacing", 100, 280, new Color(255, 255, 255, 255));

		panel.DrawText("Hello, world! this is a test!", 100, 320, new Color(255, 255, 255, 255), true, true);

		panel.DrawText("Hello, world! this is a test!", 100, 360, new Color(255, 255, 255, 255), true, false);

		panel.DrawText("Hello, world! this is a test!", 100, 400, new Color(255, 255, 255, 255), false, true);

		SDL.RenderPresent(renderer);
	}
}