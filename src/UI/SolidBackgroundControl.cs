using SDL_Sharp;

public class SolidBackgroundControl : UIControl
{
	public Color color = new Color(0, 0, 0, 0);

	public SolidBackgroundControl(UIPanel parent, Renderer renderer, string controlName, int x, int y, int width = 0, int height = 0, Color color = default) : base(parent, renderer, controlName, x, y, width, height)
	{
		this.color = color;
		acceptMouseButtons = false;
	}

	public override void Draw()
	{
		base.Draw();
		
		SDL.SetRenderDrawColor(renderer, color.R, color.G, color.B, color.A);
		Rect rect = new Rect(x, y, width, height);
		SDL.RenderFillRect(renderer, ref rect);
	}
}