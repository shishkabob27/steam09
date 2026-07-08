using System.Drawing;
using System.Numerics;
using SDL;

namespace KGUI
{
	public class SDL3WindowRenderer : WindowRenderer
	{
		public unsafe SDL_Renderer* renderer;

		private Dictionary<string, nint> _textures = [];

		public SteamFont7? _steamFont7;
		public SteamFont8? _steamFont8;

		public unsafe SDL3WindowRenderer(SDL_Renderer* renderer)
		{
			this.renderer = renderer;
			
			unsafe
			{
				SDL_Surface* surface7 = SDL3_image.IMG_Load(Assets.GetAssetPath("fonts/steamfont7.png"));
				if (surface7 != null)
				{
					SDL_Texture* texture7 = SDL3.SDL_CreateTextureFromSurface(renderer, surface7);
					_steamFont7 = new SteamFont7(texture7);
					SDL3.SDL_DestroySurface(surface7);
				}

				SDL_Surface* surface8 = SDL3_image.IMG_Load(Assets.GetAssetPath("fonts/steamfont8.png"));
				if (surface8 != null)
				{
					SDL_Texture* texture8 = SDL3.SDL_CreateTextureFromSurface(renderer, surface8);
					_steamFont8 = new SteamFont8(texture8);
					SDL3.SDL_DestroySurface(surface8);
				}
			}
		}

		public override void RenderPoint(int x, int y, Color color)
		{
			unsafe
			{
				SDL3.SDL_SetRenderDrawColor(renderer, color.R, color.G, color.B, color.A);
				SDL3.SDL_RenderPoint(renderer, x, y);
			}
		}

		public override void RenderLine(int x1, int y1, int x2, int y2, Color color)
		{
			unsafe
			{
				SDL3.SDL_SetRenderDrawColor(renderer, color.R, color.G, color.B, 255);
				SDL3.SDL_RenderLine(renderer, x1, y1, x2, y2);
			}
		}
		public override void RenderBox(int x, int y, int width, int height, Color color)
		{
			unsafe
			{
				SDL3.SDL_SetRenderDrawColor(renderer, color.R, color.G, color.B, 255);
				SDL_FRect rect = new SDL_FRect { x = x, y = y, w = width, h = height };
				SDL3.SDL_RenderFillRect(renderer, &rect);
			}
		}
		public override void RenderBoxBorder(int x, int y, int width, int height, Color color)
		{
			unsafe
			{
				SDL3.SDL_SetRenderDrawColor(renderer, color.R, color.G, color.B, 255);
				SDL3.SDL_RenderLine(renderer, x, y, x + width - 1, y); // top
				SDL3.SDL_RenderLine(renderer, x, y + 1, x, y + height - 1); // left
				SDL3.SDL_RenderLine(renderer, x + width - 1, y + 1, x + width - 1, y + height - 1); // right
				SDL3.SDL_RenderLine(renderer, x + 1, y + height - 1, x + width - 1, y + height - 1); // bottom
			}
		}

		private unsafe SDL_Texture* GetOrLoadTexture(string texturePath)
		{
			if (!_textures.TryGetValue(texturePath, out nint texturePtr))
			{
				SDL_Surface* surface = SDL3_image.IMG_Load(Assets.GetAssetPath(texturePath));
				SDL_Texture* newTexture = SDL3.SDL_CreateTextureFromSurface(renderer, surface);
				SDL3.SDL_DestroySurface(surface);
				texturePtr = (nint)newTexture;
				_textures[texturePath] = texturePtr;
			}
			return (SDL_Texture*)texturePtr;
		}

		public override void DrawTexture(string texturePath, int x, int y)
		{
			unsafe
			{
				SDL_Texture* texture = GetOrLoadTexture(texturePath);
				float width, height;
				SDL3.SDL_GetTextureSize(texture, &width, &height);
				SDL_FRect destRect = new SDL_FRect { x = x, y = y, w = width, h = height };
				SDL3.SDL_RenderTexture(renderer, texture, null, &destRect);
			}
		}

		public override void DrawTextureRect(string texturePath, int x, int y, int width, int height)
		{
			SDL_FRect destRect = new SDL_FRect { x = x, y = y, w = width, h = height };
			unsafe
			{
				SDL3.SDL_RenderTexture(renderer, GetOrLoadTexture(texturePath), null, &destRect);
			}
		}

		public override void DrawTexture9Grid(string texturePath, int x, int y, int width, int height, int top, int left, int bottom, int right, float scale)
		{
			SDL_FRect destRect = new SDL_FRect { x = x, y = y, w = width, h = height };

			unsafe
			{
				SDL3.SDL_RenderTexture9Grid(renderer, GetOrLoadTexture(texturePath), null, top, left, bottom, right, scale, &destRect);
			}
		}

		public override void DrawTextureSheet(string texturePath, int x, int y, int indexX, int indexY, int cellWidth, int cellHeight)
		{
			unsafe
			{
				SDL_Texture* texture = GetOrLoadTexture(texturePath);
				float width, height;
				SDL3.SDL_GetTextureSize(texture, &width, &height);

				SDL_FRect sourceRect = new SDL_FRect { x = indexX * cellWidth, y = indexY * cellHeight, w = cellWidth, h = cellHeight };
				SDL_FRect destRect = new SDL_FRect { x = x, y = y, w = cellWidth, h = cellHeight };
				
				SDL3.SDL_RenderTexture(renderer, texture, &sourceRect, &destRect);
			}
		}

		public override Vector2 GetTextureSize(string texturePath)
		{
			unsafe
			{
				SDL_Texture* texture = GetOrLoadTexture(texturePath);
				float width, height;
				SDL3.SDL_GetTextureSize(texture, &width, &height);
				return new Vector2(width, height);
			}
		}

		public override Rect GetClipRect()
		{
			unsafe
			{
				SDL_Rect clipRect;
				SDL3.SDL_GetRenderClipRect(renderer, &clipRect);
				return new Rect(clipRect.x, clipRect.y, clipRect.w, clipRect.h);
			}
		}

		public override void SetClipRect(Rect? rect)
		{
			unsafe
			{
				if (rect == null)
				{
					SDL3.SDL_SetRenderClipRect(renderer, null);
					return;
				}

				SDL_Rect clipRect = new SDL_Rect { x = rect.Value.X, y = rect.Value.Y, w = rect.Value.W, h = rect.Value.H };
				SDL3.SDL_SetRenderClipRect(renderer, &clipRect);
			}
		}

		public override void DrawText(string text, int x, int y, Color color, bool bold = false, bool underline = false, int fontSize = 8, FontAlignment alignment = FontAlignment.Left)
		{
			unsafe
			{
				if (fontSize == 7 && _steamFont7 != null)
				{
					_steamFont7.RenderText(renderer, text, x, y, color, bold, underline, alignment);
				}
				else if (fontSize == 8 && _steamFont8 != null)
				{
						_steamFont8.RenderText(renderer, text, x, y, color, bold, underline, alignment);
				}
			}
		}

		public override void DrawTextWrapped(string text, int x, int y, int maxWidth, Color color, bool bold = false, bool underline = false, int fontSize = 8, FontAlignment alignment = FontAlignment.Left, int lineHeight = 12)
		{
			FontRenderer fontRenderer = fontSize == 7 ? _steamFont7 : _steamFont8;
			if (fontRenderer == null) return;
			unsafe {
				fontRenderer.RenderTextWrapped(renderer, text, x, y, maxWidth, color, bold, underline, alignment, lineHeight);
			}
		}

		public override int MeasureTextWrappedHeight(string text, int maxWidth, bool bold = false, bool underline = false, int fontSize = 8, int lineHeight = 12)
		{
			FontRenderer fontRenderer = fontSize == 7 ? _steamFont7 : _steamFont8;
			if (fontRenderer == null) return 0;
			return fontRenderer.MeasureWrappedHeight(text, maxWidth, bold, lineHeight);
		}

		public override int MeasureTextWidth(string text, bool bold = false, int fontSize = 8)
		{
			FontRenderer fontRenderer = fontSize == 7 ? _steamFont7 : _steamFont8;
			if (fontRenderer == null) return 0;
			return fontRenderer.MeasureText(text, bold);
		}
	}
}