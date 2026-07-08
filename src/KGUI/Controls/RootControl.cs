using System.Drawing;
using System.Numerics;

namespace KGUI.Controls
{
	public class RootControl : UIControl
	{
		public UIPanel panel;
		
		public RootControl(UIControl parent) : base(parent)
		{
		}

		public void SetPanel(UIPanel panel)
		{
			this.panel = panel;
		}
		
		public override void DrawTexture(string texturePath, int x, int y)
		{
			panel.window.backendWindow.Renderer.DrawTexture(texturePath, this.x + x, this.y + y);
		}

		public override void DrawTextureRect(string texturePath, int x, int y, int width, int height)
		{
			panel.window.backendWindow.Renderer.DrawTextureRect(texturePath, this.x + x, this.y + y, width, height);
		}

		public override void DrawTexture9Grid(string texturePath, int x, int y, int width, int height, int top, int left, int bottom, int right, float scale)
		{
			panel.window.backendWindow.Renderer.DrawTexture9Grid(texturePath, this.x + x, this.y + y, width, height, top, left, bottom, right, scale);
		}

		public override void DrawTextureSheet(string texturePath, int x, int y, int indexX, int indexY, int cellWidth, int cellHeight)
		{
			panel.window.backendWindow.Renderer.DrawTextureSheet(texturePath, this.x + x, this.y + y, indexX, indexY, cellWidth, cellHeight);
		}

		public override Vector2 GetTextureSize(string texturePath)
		{
			return panel.window.backendWindow?.Renderer.GetTextureSize(texturePath) ?? Vector2.Zero;
		}

		public override void DrawPoint(int x, int y, Color color)
		{
			panel.window.backendWindow.Renderer.RenderPoint(this.x + x, this.y + y, color);
		}

		public override void DrawLine(int x1, int y1, int x2, int y2, Color color)
		{
			panel.window.backendWindow.Renderer.RenderLine(this.x + x1, this.y + y1, this.x + x2, this.y + y2, color);
		}

		public override void DrawBox(int x, int y, int width, int height, Color color)
		{
			panel.window.backendWindow.Renderer.RenderBox(this.x + x, this.y + y, width, height, color);
		}

		public override void DrawBoxBorder(int x, int y, int width, int height, Color color)
		{
			panel.window.backendWindow.Renderer.RenderBoxBorder(this.x + x, this.y + y, width, height, color);
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
			return panel.MeasureTextWidth(text, bold, fontSize);
		}

		public override void Reposition(int x, int y)
		{
		}

		public override int GetRelativeMouseX() { return panel.MouseX - x; }
		public override int GetRelativeMouseY() { return panel.MouseY - y; }
	}
}