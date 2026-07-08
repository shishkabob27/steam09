using System.Drawing;
using System.Numerics;

namespace KGUI
{
    public abstract class WindowRenderer
    {
        public abstract void RenderPoint(int x, int y, Color color);
        public abstract void RenderLine(int x1, int y1, int x2, int y2, Color color);
        public abstract void RenderBox(int x, int y, int width, int height, Color color);
        public abstract void RenderBoxBorder(int x, int y, int width, int height, Color color);

        public abstract void DrawTexture(string texturePath, int x, int y);
        public abstract void DrawTextureRect(string texturePath, int x, int y, int width, int height);
		public abstract void DrawTexture9Grid(string texturePath, int x, int y, int width, int height, int top, int left, int bottom, int right, float scale);
		public abstract void DrawTextureSheet(string texturePath, int x, int y, int indexX, int indexY, int cellWidth, int cellHeight);
        public abstract Vector2 GetTextureSize(string texturePath);

        public abstract Rect GetClipRect();
        public abstract void SetClipRect(Rect? rect);

        public abstract void DrawText(string text, int x, int y, Color color, bool bold = false, bool underline = false, int fontSize = 8, FontAlignment alignment = FontAlignment.Left);
        public abstract void DrawTextWrapped(string text, int x, int y, int maxWidth, Color color, bool bold = false, bool underline = false, int fontSize = 8, FontAlignment alignment = FontAlignment.Left, int lineHeight = 12);
        public abstract int MeasureTextWrappedHeight(string text, int maxWidth, bool bold = false, bool underline = false, int fontSize = 8, int lineHeight = 12);
        public abstract int MeasureTextWidth(string text, bool bold = false, int fontSize = 8);
    }
}