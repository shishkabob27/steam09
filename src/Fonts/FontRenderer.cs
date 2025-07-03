using SDL_Sharp;

public class FontRenderer
{
	public Texture texture;

	public FontRenderer(Texture fontTexture)
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

	public void RenderText(Renderer renderer, string text, int x, int y, SDL_Sharp.Color color, bool bold = false, bool underline = false, FontAlignment alignment = FontAlignment.Left)
	{
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
		{
			char c = text[i];
			int index = Array.IndexOf(chars, c);
			if (index != -1)
			{
				int xPos = index % 21;
				int yPos = index / 21;

				Rect sourceRect = new Rect((xPos * 12), (yPos * 12), 11, 11);
				Rect destRect = new Rect(x, y, 11, 11);

				unsafe
				{
					//get color from texture, this is dumb but it works
					SDL.GetTextureColorMod(texture, out byte r, out byte g, out byte b);
					SDL.SetTextureColorMod(texture, color.R, color.G, color.B);
					SDL.RenderCopy(renderer, texture, &sourceRect, &destRect);

					//if bold, render again with x offset 1
					if (bold)
					{
						destRect.X += 1;
						SDL.RenderCopy(renderer, texture, &sourceRect, &destRect);
					}

					SDL.SetTextureColorMod(texture, r, g, b);
				}

				x += widths[index] + (bold ? 1 : 0);
			}
			else // unknown character or space
			{
				x += 3;
			}
		}

		if (underline)
		{
			SDL.SetRenderDrawColor(renderer, color.R, color.G, color.B, color.A);
			SDL.RenderDrawLine(renderer, xOriginal, y + 10, xOriginal + textWidth - 2, y + 10);
			SDL.SetRenderDrawColor(renderer, 0, 0, 0, 255);
		}
	}

	public int MeasureText(string text, bool bold = false)
	{
		int width = 0;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			int index = Array.IndexOf(chars, c);
			if (index != -1)
			{
				width += widths[index] + (bold ? 1 : 0);
			}
			else // unknown character or space
			{
				width += 3;
			}
		}
		return width;
	}
}


public enum FontAlignment
{
	Left,
	Center,
	Right
}