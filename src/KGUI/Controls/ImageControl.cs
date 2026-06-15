using System.Drawing;
using System.Xml.Serialization;
using SDL;

namespace KGUI
{
	public class ImageControl : UIControl
    {
        unsafe SDL_Texture* imageTexture;
        public Color backgroundColor = Color.Transparent;

        public int imageWidth = 0;
        public int imageHeight = 0;

		public enum ImageScaleMode
		{
			Stretch,
			Fit,
			Fill,
			LoopX,
			LoopY,
			LoopBoth
		}


		[XmlAttribute("scaleMode")]
		ImageScaleMode scaleMode = ImageScaleMode.Stretch;


		[XmlAttribute("path")]
        string imageUrl = "";


        public ImageControl(UIControl parent) : base(parent)
		{
		}

		public override void OnAttributesDeserialized()
		{
			base.OnAttributesDeserialized();
			if (imageUrl != "")
			{
				SetImageUrl(Assets.GetAssetPath(imageUrl), scaleMode);
			}
		}

        public void SetImageUrl(string imageUrl, ImageScaleMode scaleMode = ImageScaleMode.Stretch)
        {
            string newUrl = imageUrl ?? "";
			if (string.IsNullOrEmpty(newUrl)) return;

            this.imageUrl = newUrl;
			this.scaleMode = scaleMode;

            unsafe
            {
                if (imageTexture != null)
                {
                    SDL3.SDL_DestroyTexture(imageTexture);
                    imageTexture = null;
                }
          
				imageTexture = LoadTexture(newUrl);
				if (imageTexture != null)
				{
					float widthF, heightF;
					SDL3.SDL_GetTextureSize(imageTexture, &widthF, &heightF);
					imageWidth = (int)widthF;
					imageHeight = (int)heightF;
				}
			}
		}

        public unsafe override void Draw()
        {
            base.Draw();

			if (backgroundColor != Color.Transparent)
			{
				DrawBox(0, 0, width, height, backgroundColor);
			}
			if (imageTexture == null)
				return;

			if (scaleMode == ImageScaleMode.Stretch)
			{
				DrawTexture(imageTexture, 0, 0);
			}
			else if (scaleMode == ImageScaleMode.Fit)
			{
				float imageAspect = (float)imageWidth / imageHeight;
				float controlAspect = (float)width / height;

				int drawWidth, drawHeight;
				if (controlAspect > imageAspect)
				{
					drawHeight = height;
					drawWidth = (int)(height * imageAspect);
				}
				else
				{
					drawWidth = width;
					drawHeight = (int)(width / imageAspect);
				}

				int drawX = (width - drawWidth) / 2;
				int drawY = (height - drawHeight) / 2;

				DrawTextureRect(imageTexture, drawX, drawY, drawWidth, drawHeight);
			}
			else if (scaleMode == ImageScaleMode.Fill)
			{
				float imageAspect = (float)imageWidth / imageHeight;
				float controlAspect = (float)width / height;

				int drawWidth, drawHeight;
				if (controlAspect < imageAspect)
				{
					drawHeight = height;
					drawWidth = (int)(height * imageAspect);
				}
				else
				{
					drawWidth = width;
					drawHeight = (int)(width / imageAspect);
				}

				int drawX = (width - drawWidth) / 2;
				int drawY = (height - drawHeight) / 2;

				DrawTextureRect(imageTexture, drawX, drawY, drawWidth, drawHeight);
			}
			else if (scaleMode == ImageScaleMode.LoopX || scaleMode == ImageScaleMode.LoopY || scaleMode == ImageScaleMode.LoopBoth)
			{
				int xRepeats = 1;
				int yRepeats = 1;

				if (scaleMode == ImageScaleMode.LoopX || scaleMode == ImageScaleMode.LoopBoth)
				{
					xRepeats = (int)Math.Ceiling((float)width / imageWidth);
				}
				if (scaleMode == ImageScaleMode.LoopY || scaleMode == ImageScaleMode.LoopBoth)
				{
					yRepeats = (int)Math.Ceiling((float)height / imageHeight);
				}

				for (int y = 0; y < yRepeats; y++)
				{
					for (int x = 0; x < xRepeats; x++)
					{
						DrawTexture(imageTexture, x * imageWidth, y * imageHeight);
					}
				}
			}
		}

        public override void Destroy()
        {
            base.Destroy();
            unsafe
			{
                SDL3.SDL_DestroyTexture(imageTexture);
            }
        }
    }
}