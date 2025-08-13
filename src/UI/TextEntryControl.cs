using SDL_Sharp;

public class TextEntryControl : UIControl
{
	public int maxLength = 64;
	public bool isPassword = false;

	int cursorPosition = 0;

	bool isSelecting = false;
	int selectionStart = 0;
	int selectionEnd = 0;

	public TextEntryControl(UIPanel parent, Renderer renderer, string controlName, int x, int y, int width = 0, int height = 0, int maxLength = 64) : base(parent, renderer, controlName, x, y, width, height)
	{
		this.maxLength = maxLength;

		//double clicking a button should still trigger a click event
		OnDoubleClick += () =>
		{
			OnClick?.Invoke();
		};

		OnKeyDown += (key, modifier) =>
		{
			bool isShiftPressed = modifier.HasFlag(KeyModifier.Shift) || modifier.HasFlag(KeyModifier.LeftShift) || modifier.HasFlag(KeyModifier.RightShift);
			bool shouldBeCap = modifier.HasFlag(KeyModifier.Caps) || modifier.HasFlag(KeyModifier.LeftShift) || modifier.HasFlag(KeyModifier.RightShift);

			if (key == Keycode.LShift || key == Keycode.RShift)
			{
				if (!isSelecting)
				{
					selectionStart = cursorPosition;
					selectionEnd = cursorPosition;
					isSelecting = true;
				}
			}
			if (key == Keycode.Return)
			{
				OnEnterPressed?.Invoke();
			}
			else if (key == Keycode.Backspace)
			{
				DeleteText();
				isSelecting = false;
			}
			else if (key == Keycode.Left)
			{
				if (!isShiftPressed) isSelecting = false;
				cursorPosition = Math.Max(cursorPosition - 1, 0);

				if (isSelecting)
				{
					selectionEnd = cursorPosition;
				}
			}
			else if (key == Keycode.Right)
			{
				if (!isShiftPressed) isSelecting = false;
				cursorPosition = Math.Min(cursorPosition + 1, this.text.Length);

				if (isSelecting)
				{
					selectionEnd = cursorPosition;
				}
			}
			else if (key == Keycode.Comma)
			{
				if (isShiftPressed) InsertText("<");
				else InsertText(",");
			}
			else if (key == Keycode.Period)
			{
				if (isShiftPressed) InsertText(">");
				else InsertText(".");
			}
			else if (key == Keycode.Slash)
			{
				if (isShiftPressed) InsertText("?");
				else InsertText("/");
			}
			else if (key == Keycode.Minus)
			{
				if (isShiftPressed) InsertText("_");
				else InsertText("-");
			}
			else if (key == Keycode.Equals)
			{
				if (isShiftPressed) InsertText("+");
				else InsertText("=");
			}
			else if (key == Keycode.SemiColon)
			{
				if (isShiftPressed) InsertText(":");
				else InsertText(";");
			}
			else if (key == Keycode.Colon)
			{
				InsertText(":");
			}
			else if (key == Keycode.Quote)
			{
				if (isShiftPressed) InsertText("\"");
				else InsertText("'");
			}
			else if (key == Keycode.LeftBracket)
			{
				if (isShiftPressed) InsertText("{");
				else InsertText("[");
			}
			else if (key == Keycode.RightBracket)
			{
				if (isShiftPressed) InsertText("}");
				else InsertText("]");
			}
			else if (key == Keycode.BackQuote)
			{
				if (isShiftPressed) InsertText("~");
				else InsertText("`");
			}
			else if (key == Keycode.Backslash)
			{
				if (isShiftPressed) InsertText("|");
				else InsertText("\\");
			}
			else if (key == Keycode.Space)
			{
				InsertText(" ");
			}
			else if (key >= Keycode.D0 && key <= Keycode.D9)
			{
				if (isShiftPressed)
				{
					string text = (key - Keycode.D0).ToString();
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
					InsertText((key - Keycode.D0).ToString());
				}
			}
			else if (key >= Keycode.A && key <= Keycode.Z)
			{
				if (modifier.HasFlag(KeyModifier.Ctrl) || modifier.HasFlag(KeyModifier.RightCtrl) || modifier.HasFlag(KeyModifier.LeftCtrl))
				{
					if (key == Keycode.V)
					{
						InsertText(SDL.GetClipboardText());
						if (cursorPosition > maxLength)
						{
							cursorPosition = maxLength;
						}
					}
					else if (key == Keycode.A)
					{
						isSelecting = true;
						selectionStart = 0;
						selectionEnd = this.text.Length;
					}
					else if (key == Keycode.C)
					{
						int start = Math.Min(selectionStart, selectionEnd);
						int end = Math.Max(selectionStart, selectionEnd);
						string textToCopy = this.text.Substring(start, end - start);
						SDL.SetClipboardText(textToCopy);
					}
					else if (key == Keycode.X)
					{
						int start = Math.Min(selectionStart, selectionEnd);
						int end = Math.Max(selectionStart, selectionEnd);
						string textToCopy = this.text.Substring(start, end - start);
						SDL.SetClipboardText(textToCopy);

						DeleteText();
						isSelecting = false;
					}
				}
				else
				{
					string text = key.ToString().ToLower();
					if (shouldBeCap) text = text.ToUpper();
					InsertText(text);
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

		if (this.text.Length > maxLength)
		{
			this.text = this.text.Substring(0, maxLength);
		}

		isSelecting = false;
	}

	int TextDrawOffset()
	{
		int textWidth = parent.steamFont8.MeasureText(text);
		if (textWidth <= width - 20) return 0;
		return textWidth - width + 20;
	}

	public override void Draw()
	{
		base.Draw();

		//content
		if (enabled) SDL.SetRenderDrawColor(renderer, 85, 85, 85, 255);
		else SDL.SetRenderDrawColor(renderer, 80, 80, 80, 255);
		Rect rect = new Rect(x + 1, y + 1, width - 2, height - 2);
		SDL.RenderFillRect(renderer, ref rect);

		//border
		if (enabled) SDL.SetRenderDrawColor(renderer, 20, 20, 20, 255);
		else SDL.SetRenderDrawColor(renderer, 56, 56, 56, 255);
		SDL.RenderDrawLine(renderer, x + 1, y, x + width - 2, y); // top
		SDL.RenderDrawLine(renderer, x, y + 1, x, y + height - 2); // left
		SDL.RenderDrawLine(renderer, x + 1, y + height - 1, x + width - 2, y + height - 1); // bottom
		SDL.RenderDrawLine(renderer, x + width - 1, y + 1, x + width - 1, y + height - 2); // right
	

		//corners
		if (enabled) SDL.SetRenderDrawColor(renderer, 65, 66, 66, 255);
		else SDL.SetRenderDrawColor(renderer, 91, 93, 89, 255);
		SDL.RenderDrawPoint(renderer, x + 1, y + 1);
		SDL.RenderDrawPoint(renderer, x + width - 2, y + 1);
		SDL.RenderDrawPoint(renderer, x + 1, y + height - 2);
		SDL.RenderDrawPoint(renderer, x + width - 2, y + height - 2);

		//Clip draw
		Rect clipRect = new Rect(x + 1, y + 1, width - 2, height - 2);
		SDL.RenderSetClipRect(renderer, ref clipRect);

		string textToDraw = isPassword ? string.Empty.PadRight(text.Length, '*') : text;

		//selection
		if (isSelecting && text.Length > 0)
		{
			SDL.SetRenderDrawColor(renderer, 196, 181, 80, 255);

			//measure text
			int smallerIndex = Math.Min(selectionStart, selectionEnd);
			int largerIndex = Math.Max(selectionStart, selectionEnd);

			smallerIndex = Math.Max(smallerIndex, 0);
			largerIndex = Math.Min(largerIndex, textToDraw.Length);

			int upToSmallerIndexWidth = parent.steamFont8.MeasureText(textToDraw.Substring(0, smallerIndex));
			int upToLargerIndexWidth = parent.steamFont8.MeasureText(textToDraw.Substring(smallerIndex, largerIndex - smallerIndex));
			//draw selection
			Rect selectionRect = new Rect(x + 10 - TextDrawOffset() + upToSmallerIndexWidth, y + 4, upToLargerIndexWidth, height - 8);
			SDL.RenderFillRect(renderer, ref selectionRect);
		}

		//text
		Color textColor;
		if (enabled) textColor = new Color(255, 255, 255, 255);
		else textColor = new Color(121, 126, 121, 255);
		int textX = x + 10 - TextDrawOffset();
		int textY = y + (height / 2) - 5;

		parent.DrawText(textToDraw, textX, textY, textColor);

		//cursor
		if (focused && enabled)
		{
			SDL.SetRenderDrawColor(renderer, textColor.R, textColor.G, textColor.B, 255);

			int cursorX = parent.steamFont8.MeasureText(textToDraw.Substring(0, Math.Min(cursorPosition, textToDraw.Length)));
			SDL.RenderDrawLine(renderer, textX + cursorX, textY, textX + cursorX, textY + 10);
		}

		//Unclip draw
		unsafe
		{
			SDL.RenderSetClipRect(renderer, null);
		}
	}

	public Action OnEnterPressed;
}