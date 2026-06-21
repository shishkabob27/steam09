using System.Drawing;
using SDL;

namespace KGUI.Controls
{
	public class RootControl : UIControl
	{
		public UIPanel panel;
		
		public unsafe RootControl(UIControl parent) : base(parent)
		{
		}

		public void SetPanel(UIPanel panel)
		{
			this.panel = panel;
		}

		public override unsafe SDL_Texture* LoadTexture(string path)
		{
			SDL_Texture* texture = null;
			unsafe {
				texture = SDL3_image.IMG_LoadTexture(panel.window.renderer, (Utf8String)path);
			}
			if (texture == null)
			{
				Console.WriteLine("Failed to load texture: " + path);
			}
			return texture;
		}
		
		public override unsafe void DrawTexture(SDL_Texture* texture, int x, int y)
		{
			float width, height;
			SDL3.SDL_GetTextureSize(texture, &width, &height);
			SDL_FRect destRect = new SDL_FRect { x = this.x + x, y = this.y + y, w = width, h = height };
			SDL3.SDL_RenderTexture(panel.window.renderer, texture, null, &destRect);
		}

		public override unsafe void DrawTextureRect(SDL_Texture* texture, int x, int y, int width, int height)
		{
			SDL_FRect destRect = new SDL_FRect { x = this.x + x, y = this.y + y, w = width, h = height };
			SDL3.SDL_RenderTexture(panel.window.renderer, texture, null, &destRect);
		}

		public override unsafe void DrawTexture9Grid(SDL_Texture* texture, int x, int y, int width, int height, int top, int left, int bottom, int right, float scale)
		{
			SDL_FRect destRect = new SDL_FRect { x = this.x + x, y = this.y + y, w = width, h = height };
			SDL3.SDL_RenderTexture9Grid(panel.window.renderer, texture, null, top, left, bottom, right, scale, &destRect);
		}

		public override unsafe void DrawTextureSheet(SDL_Texture* texture, int x, int y, int indexX, int indexY, int cellWidth, int cellHeight)
		{
			float width, height;
			SDL3.SDL_GetTextureSize(texture, &width, &height);

			SDL_FRect sourceRect = new SDL_FRect { x = indexX * cellWidth, y = indexY * cellHeight, w = cellWidth, h = cellHeight };
			SDL_FRect destRect = new SDL_FRect { x = this.x + x, y = this.y + y, w = cellWidth, h = cellHeight };
			
			SDL3.SDL_RenderTexture(panel.window.renderer, texture, &sourceRect, &destRect);
		}

		public override void DrawPoint(int x, int y, Color color)
		{
			unsafe {
				SDL3.SDL_SetRenderDrawColor(panel.window.renderer, color.R, color.G, color.B, 255);
				SDL3.SDL_RenderPoint(panel.window.renderer, this.x + x, this.y + y);
			}
		}

		public override void DrawLine(int x1, int y1, int x2, int y2, Color color)
		{
			unsafe {
				SDL3.SDL_SetRenderDrawColor(panel.window.renderer, color.R, color.G, color.B, 255);
				SDL3.SDL_RenderLine(panel.window.renderer, this.x + x1, this.y + y1, this.x + x2, this.y + y2);
			}
		}

		public override void DrawBox(int x, int y, int width, int height, Color color)
		{
			unsafe {
				SDL3.SDL_SetRenderDrawColor(panel.window.renderer, color.R, color.G, color.B, 255);
				SDL_FRect rect = new SDL_FRect { x = this.x + x, y = this.y + y, w = width, h = height };
				SDL3.SDL_RenderFillRect(panel.window.renderer, &rect);
			}
		}

		public override void DrawBoxBorder(int x, int y, int width, int height, Color color)
		{
			unsafe {
				SDL3.SDL_SetRenderDrawColor(panel.window.renderer, color.R, color.G, color.B, 255);
				SDL3.SDL_RenderLine(panel.window.renderer, this.x + x, this.y + y, this.x + x + width - 1, this.y + y); // top
				SDL3.SDL_RenderLine(panel.window.renderer, this.x + x, this.y + y + 1, this.x + x, this.y + y + height - 1); // left
				SDL3.SDL_RenderLine(panel.window.renderer, this.x + x + width - 1, this.y + y + 1, this.x + x + width - 1, this.y + y + height - 1); // right
				SDL3.SDL_RenderLine(panel.window.renderer, this.x + x + 1, this.y + y + height - 1, this.x + x + width - 1, this.y + y + height - 1); // bottom
			}
		}

		public override void DrawText(string text, int x, int y, Color color, bool bold = false, bool underline = false, int fontSize = 8, FontAlignment alignment = FontAlignment.Left)
		{
			panel.DrawText(text, this.x + x, this.y + y, color, bold, underline, fontSize, alignment);
		}

		public override void DrawTextWrapped(string text, int x, int y, int maxWidth, Color color, bool bold = false, bool underline = false, int fontSize = 8, FontAlignment alignment = FontAlignment.Left, int lineHeight = 12)
		{
			panel.DrawTextWrapped(text, this.x + x, this.y + y, maxWidth, color, bold, underline, fontSize, alignment, lineHeight);
		}
		
		public override int MeasureTextWrappedHeight(string text, int maxWidth, bool bold = false, bool underline = false, int fontSize = 8, int lineHeight = 12)
		{
			return panel.MeasureTextWrappedHeight(text, maxWidth, bold, underline, fontSize, lineHeight);
		}

		public override int MeasureTextWidth(string text, bool bold = false, int fontSize = 8)
		{
			return panel.MeasureText(text, bold, fontSize);
		}

		public override void Reposition(int x, int y)
		{
		}

		public override int GetRelativeMouseX() { return panel.MouseX - x; }
		public override int GetRelativeMouseY() { return panel.MouseY - y; }
	}
}