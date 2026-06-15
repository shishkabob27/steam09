using System.Collections.Generic;
using System.Drawing;
using SDL;

namespace KGUI
{
	public class FontRenderer
	{
		public unsafe SDL_Texture* texture;

		public unsafe FontRenderer(SDL_Texture* fontTexture)
		{
			this.texture = fontTexture;
		}

		//table of characters and their widths
		public static char[] chars = new char[]
		{
			'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u',
			'v','w','x','y','z','A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P',
			'Q','R','S','T','U','V','W','X','Y','Z','0','1','2','3','4','5','6','7','8','9','\\',
			'!','@','#','$','%','^','&','*','(',')','_','+','-','=','`','~','[',']','{','}','|',
			';',':','<','>','?','/','.',',','\'','"',
		};

		public virtual int[] widths { get; set; }

		public int CharAdvance(char c, bool bold = false)
		{
			int index = Array.IndexOf(chars, c);
			if (index != -1)
				return widths[index] + (bold ? 1 : 0);
			return 3;
		}

		public int MeasureRange(string text, int start, int end, bool bold = false)
		{
			if (text == null || start >= end) return 0;
			int width = 0;
			for (int i = start; i < end; i++)
				width += CharAdvance(text[i], bold);
			return width;
		}

		public unsafe void RenderText(SDL_Renderer* renderer, string text, int x, int y, Color color, bool bold = false, bool underline = false, FontAlignment alignment = FontAlignment.Left)
		{
			if (text == null || string.IsNullOrEmpty(text)) return;

			int xOriginal = x;

			int textWidth = MeasureText(text, bold);

			switch (alignment)
			{
				case FontAlignment.Center:
					x = xOriginal - (textWidth / 2);
					break;
				case FontAlignment.Right:
					x = xOriginal - textWidth;
					break;
			}

			for (int i = 0; i < text.Length; i++)
				DrawGlyph(renderer, text[i], ref x, y, color, bold);

			if (underline)
			{
				SDL3.SDL_SetRenderDrawColor(renderer, color.R, color.G, color.B, color.A);
				SDL3.SDL_RenderLine(renderer, xOriginal, y + 10, xOriginal + textWidth - 2, y + 10);
				SDL3.SDL_SetRenderDrawColor(renderer, 240, 240, 240, 255);
			}
		}

		public unsafe void RenderTextWrapped(SDL_Renderer* renderer, string text, int x, int y, int maxWidth, Color color, bool bold = false, bool underline = false, FontAlignment alignment = FontAlignment.Left, int lineHeight = 12)
		{
			if (text == null || maxWidth <= 0) return;

			BuildWrappedLayout(text, maxWidth, bold, out List<List<(int start, int end)>> lines, out List<int> lineWidths);

			for (int li = 0; li < lines.Count; li++)
			{
				int lineY = y + li * lineHeight;
				int lineW = lineWidths[li];
				int penX = x;
				switch (alignment)
				{
					case FontAlignment.Center:
						penX = x - (lineW / 2);
						break;
					case FontAlignment.Right:
						penX = x - lineW;
						break;
				}

				bool firstSpan = true;
				foreach (var (s, e) in lines[li])
				{
					if (!firstSpan)
						penX += CharAdvance(' ', bold);
					firstSpan = false;
					for (int i = s; i < e; i++)
						DrawGlyph(renderer, text[i], ref penX, lineY, color, bold);
				}

				if (underline)
				{
					int lineStartX = alignment switch
					{
						FontAlignment.Center => x - (lineW / 2),
						FontAlignment.Right => x - lineW,
						_ => x,
					};
					SDL3.SDL_SetRenderDrawColor(renderer, color.R, color.G, color.B, color.A);
					SDL3.SDL_RenderLine(renderer, lineStartX, lineY + 10, lineStartX + lineW - 2, lineY + 10);
					SDL3.SDL_SetRenderDrawColor(renderer, 240, 240, 240, 255);
				}
			}
		}

		public int MeasureWrappedHeight(string text, int maxWidth, bool bold = false, int lineHeight = 12)
		{
			if (text == null || maxWidth <= 0) return 0;
			BuildWrappedLayout(text, maxWidth, bold, out List<List<(int start, int end)>> lines, out _);
			return lines.Count * lineHeight;
		}

		public int MeasureWrappedLineCount(string text, int maxWidth, bool bold = false)
		{
			if (text == null || maxWidth <= 0) return 0;
			BuildWrappedLayout(text, maxWidth, bold, out List<List<(int start, int end)>> lines, out _);
			return lines.Count;
		}

		void BuildWrappedLayout(string text, int maxWidth, bool bold, out List<List<(int start, int end)>> lines, out List<int> lineWidths)
		{
			var linesOut = new List<List<(int start, int end)>>();
			var widthsOut = new List<int>();
			var current = new List<(int start, int end)>();
			int curWidth = 0;
			int i = 0;
			int n = text.Length;

			void flushLine()
			{
				if (current.Count == 0) return;
				linesOut.Add(current);
				widthsOut.Add(curWidth);
				current = new List<(int start, int end)>();
				curWidth = 0;
			}

			while (i < n)
			{
				if (curWidth == 0)
				{
					while (i < n && char.IsWhiteSpace(text[i]))
						i++;
					if (i >= n)
						break;
				}

				int wordStart = i;
				while (i < n && !char.IsWhiteSpace(text[i]))
					i++;
				int wordEnd = i;
				int wordW = MeasureRange(text, wordStart, wordEnd, bold);
				int spaceW = curWidth > 0 ? CharAdvance(' ', bold) : 0;

				if (curWidth > 0 && curWidth + spaceW + wordW > maxWidth)
				{
					flushLine();
					continue;
				}

				if (curWidth == 0 && wordW > maxWidth)
				{
					for (int j = wordStart; j < wordEnd; j++)
					{
						int cw = CharAdvance(text[j], bold);
						if (cw > maxWidth)
						{
							if (curWidth > 0)
								flushLine();
							current.Add((j, j + 1));
							curWidth = cw;
							flushLine();
							continue;
						}
						if (curWidth > 0 && curWidth + cw > maxWidth)
							flushLine();
						current.Add((j, j + 1));
						curWidth += cw;
					}
					while (i < n && char.IsWhiteSpace(text[i]))
						i++;
					continue;
				}

				if (curWidth > 0)
					curWidth += spaceW;
				current.Add((wordStart, wordEnd));
				curWidth += wordW;

				while (i < n && char.IsWhiteSpace(text[i]))
					i++;
			}

			flushLine();
			lines = linesOut;
			lineWidths = widthsOut;
		}

		unsafe void DrawGlyph(SDL_Renderer* renderer, char c, ref int penX, int penY, Color color, bool bold)
		{
			int index = Array.IndexOf(chars, c);
			if (index != -1)
			{
				int xPos = index % 21;
				int yPos = index / 21;

				SDL_FRect sourceRect = new SDL_FRect { x = xPos * 12, y = yPos * 12, w = 11, h = 11 };
				SDL_FRect destRect = new SDL_FRect { x = penX, y = penY, w = 11, h = 11 };

				byte r, g, b;
				SDL3.SDL_GetTextureColorMod(texture, &r, &g, &b);
				SDL3.SDL_SetTextureColorMod(texture, color.R, color.G, color.B);
				SDL3.SDL_RenderTexture(renderer, texture, &sourceRect, &destRect);

				if (bold)
				{
					destRect.x += 1;
					SDL3.SDL_RenderTexture(renderer, texture, &sourceRect, &destRect);
				}

				SDL3.SDL_SetTextureColorMod(texture, r, g, b);

				penX += widths[index] + (bold ? 1 : 0);
			}
			else
			{
				penX += 3;
			}
		}

		public int MeasureText(string text, bool bold = false)
		{
			if (text == null || string.IsNullOrEmpty(text)) return 0;

			int width = 0;
			for (int i = 0; i < text.Length; i++)
				width += CharAdvance(text[i], bold);
			return width;
		}
	}


	public enum FontAlignment
	{
		Left,
		Center,
		Right
	}
}