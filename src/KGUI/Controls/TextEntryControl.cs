using System.Drawing;
using System.Xml.Serialization;
using SDL;

namespace KGUI
{
	public class TextEntryControl : UIControl
	{
		[XmlAttribute("maxChars")]
		public int maxChars = 64;

		[XmlAttribute("textHidden")]
		public bool textHidden = false;

		int cursorPosition = 0;

		bool isSelecting = false;
		int selectionStart = 0;
		int selectionEnd = 0;

		public TextEntryControl(UIControl parent) : base(parent)
		{
			//double clicking a button should still trigger a click event
			OnDoubleClick += (control) =>
			{
				OnClick?.Invoke(control);
			};

			OnKeyDown += (control, key, modifier) =>
			{
				bool isShiftPressed = modifier.HasFlag(SDL_Keymod.SDL_KMOD_SHIFT) || modifier.HasFlag(SDL_Keymod.SDL_KMOD_LSHIFT) || modifier.HasFlag(SDL_Keymod.SDL_KMOD_RSHIFT);
				bool isCapsLockPressed = modifier.HasFlag(SDL_Keymod.SDL_KMOD_CAPS);
				bool shouldBeCap = isCapsLockPressed || isShiftPressed;

				string keyString = key.ToString().Replace("SDLK_", "").ToLower();

				if (isShiftPressed && key != SDL_Keycode.SDLK_LSHIFT && key != SDL_Keycode.SDLK_RSHIFT)
				{
					if (!isSelecting)
					{
						selectionStart = cursorPosition;
						selectionEnd = cursorPosition;
						isSelecting = true;
					}
				}
				if (key == SDL_Keycode.SDLK_RETURN)
				{
					OnEnterPressed?.Invoke(control);
				}
				else if (key == SDL_Keycode.SDLK_BACKSPACE)
				{
					DeleteText();
					isSelecting = false;
				}
				else if (key == SDL_Keycode.SDLK_LEFT)
				{
					if (!isShiftPressed) isSelecting = false;
					cursorPosition = Math.Max(cursorPosition - 1, 0);

					if (isSelecting)
					{
						selectionEnd = cursorPosition;
					}
				}
				else if (key == SDL_Keycode.SDLK_RIGHT)
				{
					if (!isShiftPressed) isSelecting = false;
					cursorPosition = Math.Min(cursorPosition + 1, this.text.Length);

					if (isSelecting)
					{
						selectionEnd = cursorPosition;
					}
				}
				else if (key == SDL_Keycode.SDLK_COMMA)
				{
					if (shouldBeCap) InsertText("<");
					else InsertText(",");
				}
				else if (key == SDL_Keycode.SDLK_PERIOD)
				{
					if (shouldBeCap) InsertText(">");
					else InsertText(".");
				}
				else if (key == SDL_Keycode.SDLK_SLASH)
				{
					if (shouldBeCap) InsertText("?");
					else InsertText("/");
				}
				else if (key == SDL_Keycode.SDLK_MINUS)
				{
					if (shouldBeCap) InsertText("_");
					else InsertText("-");
				}
				else if (key == SDL_Keycode.SDLK_EQUALS)
				{
					if (shouldBeCap) InsertText("+");
					else InsertText("=");
				}
				else if (key == SDL_Keycode.SDLK_SEMICOLON)
				{
					if (shouldBeCap) InsertText(":");
					else InsertText(";");
				}
				else if (key == SDL_Keycode.SDLK_COLON)
				{
					InsertText(":");
				}
				else if (key == SDL_Keycode.SDLK_APOSTROPHE)
				{
					if (shouldBeCap) InsertText("\"");
					else InsertText("'");
				}
				else if (key == SDL_Keycode.SDLK_LEFTBRACKET)
				{
					if (shouldBeCap) InsertText("{");
					else InsertText("[");
				}
				else if (key == SDL_Keycode.SDLK_RIGHTBRACKET)
				{
					if (shouldBeCap) InsertText("}");
					else InsertText("]");
				}
				else if (key == SDL_Keycode.SDLK_GRAVE)
				{
					if (shouldBeCap) InsertText("~");
					else InsertText("`");
				}
				else if (key == SDL_Keycode.SDLK_BACKSLASH)
				{
					if (shouldBeCap) InsertText("|");
					else InsertText("\\");
				}
				else if (key == SDL_Keycode.SDLK_SPACE)
				{
					InsertText(" ");
				}
				else if (key >= SDL_Keycode.SDLK_0 && key <= SDL_Keycode.SDLK_9)
				{
					if (isShiftPressed)
					{
						string text = (key - SDL_Keycode.SDLK_0).ToString().Replace("SDLK_", "");
						if (text == "0") text = ")";
						else if (text == "1") text = "!";
						else if (text == "2") text = "@";
						else if (text == "3") text = "#";
						else if (text == "4") text = "$";
						else if (text == "5") text = "%";
						else if (text == "6") text = "^";
						else if (text == "7") text = "&";
						else if (text == "8") text = "*";
						else if (text == "9") text = "(";
						InsertText(text);
					}
					else
					{
						InsertText((key - SDL_Keycode.SDLK_0).ToString().Replace("SDLK_", ""));
					}
				}
				else if (key >= SDL_Keycode.SDLK_A && key <= SDL_Keycode.SDLK_Z)
				{
					if (modifier.HasFlag(SDL_Keymod.SDL_KMOD_CTRL) || modifier.HasFlag(SDL_Keymod.SDL_KMOD_LCTRL))
					{
						if (key == SDL_Keycode.SDLK_V)
						{
							string clipboardText = SDL3.SDL_GetClipboardText();
							if (clipboardText != null)
							{
								InsertText(clipboardText);
								cursorPosition += clipboardText.Length;
								if (cursorPosition > maxChars)
								{
									cursorPosition = maxChars;
								}
							}
						}
						else if (key == SDL_Keycode.SDLK_A)
						{
							isSelecting = true;
							selectionStart = 0;
							selectionEnd = this.text.Length;
						}
						else if (key == SDL_Keycode.SDLK_C)
						{
							int start = Math.Min(selectionStart, selectionEnd);
							int end = Math.Max(selectionStart, selectionEnd);
							string textToCopy = this.text.Substring(start, end - start);
							SDL3.SDL_SetClipboardText(textToCopy);
						}
						else if (key == SDL_Keycode.SDLK_X)
						{
							int start = Math.Min(selectionStart, selectionEnd);
							int end = Math.Max(selectionStart, selectionEnd);
							string textToCopy = this.text.Substring(start, end - start);
							SDL3.SDL_SetClipboardText(textToCopy);

							DeleteText();
							isSelecting = false;
						}
					}
					else
					{
						if ((isCapsLockPressed && !isShiftPressed) || (!isCapsLockPressed && isShiftPressed)) keyString = keyString.ToUpper();
						InsertText(keyString);
					}
				}
			};
		}

		public void DeleteText()
		{
			if (!isSelecting)
			{
				if (cursorPosition > 0)
				{
					this.text = this.text.Remove(cursorPosition - 1, 1);
					cursorPosition--;
				}
			}
			else
			{
				int start = Math.Min(selectionStart, selectionEnd);
				int end = Math.Max(selectionStart, selectionEnd);
				this.text = this.text.Substring(0, start) + this.text.Substring(end);
				cursorPosition = start;
			}
		}

		public void InsertText(string text)
		{
			if (isSelecting && selectionStart != selectionEnd) //remove selection
			{
				int start = Math.Min(selectionStart, selectionEnd);
				int end = Math.Max(selectionStart, selectionEnd);
				this.text = this.text.Substring(0, start) + this.text.Substring(end);
				cursorPosition = start;
			}

			this.text = this.text.Insert(Math.Min(cursorPosition, this.text.Length), text);
			cursorPosition += text.Length;

			if (this.text.Length > maxChars)
			{
				this.text = this.text.Substring(0, maxChars);
			}

			isSelecting = false;
		}

		int TextDrawOffset()
		{
			int textWidth = MeasureTextWidth(text);
			if (textWidth <= width - 20) return 0;
			return textWidth - width + 20;
		}

		public override void Draw()
		{
			base.Draw();

			Color backgroundColor;
			if (enabled) backgroundColor = Color.FromArgb(85, 85, 85);
			else backgroundColor = Color.FromArgb(80, 80, 80);
			DrawBox(1, 1, width - 2, height - 2, backgroundColor);

			//border
			Color borderColor;
			if (enabled) borderColor = Color.FromArgb(20, 20, 20);
			else borderColor = Color.FromArgb(56, 56, 56);
			DrawBoxBorder(1, 0, width - 2, 0, borderColor); // top
			DrawBoxBorder(0, 1, 0, height - 2, borderColor); // left
			DrawBoxBorder(1, height - 1, width - 2, height - 1, borderColor); // bottom
			DrawBoxBorder(width - 1, 1, width - 1, height - 2, borderColor); // right


			//corners
			Color cornerColor;
			if (enabled) cornerColor = Color.FromArgb(65, 66, 66);
			else cornerColor = Color.FromArgb(91, 93, 89);
			DrawPoint(1, 1, cornerColor);
			DrawPoint(width - 2, 1, cornerColor);
			DrawPoint(1, height - 2, cornerColor);
			DrawPoint(width - 2, height - 2, cornerColor);

			//content
			//DrawBox(1, 1, width - 2, height - 2, Color.FromArgb(255, 255, 255));

			//border
			//DrawBoxBorder(0, 0, width, height, Color.FromArgb(179, 184, 188));

			//Clip draw
			// SDL_Rect clipRect = new SDL_Rect { x = x + 1, y = y + 1, w = width - 2, h = height - 2 };
			// SDL3.SDL_SetRenderClipRect(renderer, &clipRect);

			string textToDraw = textHidden ? string.Empty.PadRight(text.Length, '*') : text;

			//selection
			if (isSelecting && text.Length > 0)
			{
				//SDL3.SDL_SetRenderDrawColor(renderer, 196, 181, 80, 255);

				//measure text
				int smallerIndex = Math.Min(selectionStart, selectionEnd);
				int largerIndex = Math.Max(selectionStart, selectionEnd);

				smallerIndex = Math.Max(smallerIndex, 0);
				largerIndex = Math.Min(largerIndex, textToDraw.Length);
				int upToSmallerIndexWidth = 0;
				int upToLargerIndexWidth = 0;
				try {
					upToSmallerIndexWidth = MeasureTextWidth(textToDraw.Substring(0, smallerIndex));
					upToLargerIndexWidth = MeasureTextWidth(textToDraw.Substring(smallerIndex, largerIndex - smallerIndex));
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
				//draw selection
				SDL_Rect selectionRect = new SDL_Rect { x = 10 - TextDrawOffset() + upToSmallerIndexWidth, y = 4, w = upToLargerIndexWidth, h = height - 8 };
				DrawBox(selectionRect.x, selectionRect.y, selectionRect.w, selectionRect.h, Color.FromArgb(196, 181, 80));
			}

			//text
			Color textColor;
			if (enabled) textColor = Color.White;
			else textColor = Color.FromArgb(121, 126, 121);
			int textX = 10 - TextDrawOffset();
			int textY = (height / 2) - 5;

			DrawText(textToDraw, textX, textY, textColor);

			//cursor
			if (focused && enabled)
			{

				int cursorX = MeasureTextWidth(textToDraw.Substring(0, Math.Min(cursorPosition, textToDraw.Length)));
				DrawLine(textX + cursorX, textY, textX + cursorX, textY + 10, textColor);
			}

			//Unclip draw
			// SDL3.SDL_SetRenderClipRect(renderer, null);
		}

		public Action<UIControl> OnEnterPressed;
	}
}