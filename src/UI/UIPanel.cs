//Holds and manages UIControls
using SDL_Sharp;
using SDL_Sharp.Image;
using SDL_Sharp.Ttf;

public class UIPanel
{
	public SteamWindow window;
	public SteamFont7 steamFont7;
	public SteamFont8 steamFont8;

	public List<UIControl> controls = new List<UIControl>();

	//track double clicks
	float timeSinceLastClick = 0;
	UIControl lastClickedControl = null;
	const float DOUBLE_CLICK_TIME = 0.5f;

	bool leftMouseDown = false;
	bool rightMouseDown = false;

	int mouseX;
	int mouseY;
	public int MouseX { get { return mouseX; } }
	public int MouseY { get { return mouseY; } }

	//TEXTURES
	public Texture radioButtonTexture;

	public Texture categoryIconTexture;

	public Texture checkboxTexture;

	public UIPanel(SteamWindow window)
	{
		this.window = window;

		unsafe
		{
			Surface* surface = IMG.Load("resources/fonts/steamfont7.png");
			Texture texture = SDL.CreateTextureFromSurface(window.renderer, surface);
			steamFont7 = new SteamFont7(texture);
			SDL.FreeSurface(surface);
		}

		unsafe
		{
			Surface* surface = IMG.Load("resources/fonts/steamfont8.png");
			Texture texture = SDL.CreateTextureFromSurface(window.renderer, surface);
			steamFont8 = new SteamFont8(texture);
			SDL.FreeSurface(surface);
		}

		unsafe
		{
			Surface* surface = IMG.Load("resources/graphics/radiobutton.png");
			radioButtonTexture = SDL.CreateTextureFromSurface(window.renderer, surface);
			SDL.FreeSurface(surface);
		}

		unsafe
		{
			Surface* surface = IMG.Load("resources/graphics/category_icon.png");
			categoryIconTexture = SDL.CreateTextureFromSurface(window.renderer, surface);
			SDL.FreeSurface(surface);
		}

		unsafe
		{
			Surface* surface = IMG.Load("resources/graphics/checkbox.png");
			checkboxTexture = SDL.CreateTextureFromSurface(window.renderer, surface);
			SDL.FreeSurface(surface);
		}
	}

	public void DrawText(string text, int x, int y, Color color, bool bold = false, bool underline = false, int fontSize = 8, FontAlignment alignment = FontAlignment.Left)
	{
		if (fontSize == 7)
		{
			steamFont7.RenderText(window.renderer, text, x, y, color, bold, underline, alignment);
		}
		else if (fontSize == 8)
		{
			steamFont8.RenderText(window.renderer, text, x, y, color, bold, underline, alignment);
		}
	}

	public void DrawTexture(Texture texture, int x, int y)
	{
		int width, height;
		unsafe
		{
			SDL.QueryTexture(texture, null, null, &width, &height);
			Rect rect = new Rect(x, y, width, height);
			SDL.RenderCopy(window.renderer, texture, null, &rect);
		}
	}

	public void DrawTextureSheet(Texture texture, int x, int y, int indexX, int indexY, int cellWidth, int cellHeight)
	{
		int width, height;
		unsafe
		{
			SDL.QueryTexture(texture, null, null, &width, &height);
		}

		Rect sourceRect = new Rect(indexX * cellWidth, indexY * cellHeight, cellWidth, cellHeight);
		Rect destRect = new Rect(x, y, cellWidth, cellHeight);
		SDL.RenderCopy(window.renderer, texture, ref sourceRect, ref destRect);
	}

	public void DrawBox(int x, int y, int width, int height, Color color)
	{
		SDL.SetRenderDrawColor(window.renderer, color.R, color.G, color.B, color.A);
		unsafe
		{
			Rect rect = new Rect(x, y, width, height);
			SDL.RenderFillRect(window.renderer, &rect);
		}
	}

	public void AddControl(UIControl control)
	{
		controls.Add(control);
	}

	public void RemoveControl(UIControl control)
	{
		control.focused = false;
		control.mouseOver = false;
		control.mouseDown = false;

		if (lastClickedControl == control)
		{
			lastClickedControl = null;
		}

		controls.Remove(control);
	}

	public void SetFocus(UIControl control)
	{
		if (lastClickedControl != null)
		{
			lastClickedControl.focused = false;
		}
		lastClickedControl = control;
		control.focused = true;
	}

	public virtual void Update(float deltaTime)
	{
		timeSinceLastClick += deltaTime;

		MouseButtonMask mouseState = SDL.GetGlobalMouseState(out int x, out int y);

		leftMouseDown = mouseState.HasFlag(MouseButtonMask.Left) && window.MouseFocus;
		rightMouseDown = mouseState.HasFlag(MouseButtonMask.Right) && window.MouseFocus;

		//check if mouse is over the control
		foreach (UIControl control in controls)
		{
			if (control.enabled == false)
				continue;


			//check if mouse is over the control
			if (mouseX >= control.x && mouseX <= control.x + control.width && mouseY >= control.y && mouseY <= control.y + control.height)
			{
				if (!control.mouseOver)
				{
					control.mouseOver = true;
				}
			}
			else
			{
				control.mouseOver = false;
				control.mouseDown = false;
			}


			//mouse down
			if (control.mouseOver && leftMouseDown && control.acceptMouseButtons)
			{
				control.mouseDown = true;
			}

			//mouse up
			if (control.mouseDown && !leftMouseDown && control.acceptMouseButtons)
			{
				control.mouseDown = false;

				//if right click, invoke right click
				if (rightMouseDown)
				{
					control.OnRightClick?.Invoke();
				}
				else
				{
					if (timeSinceLastClick < DOUBLE_CLICK_TIME && lastClickedControl == control)
					{
						timeSinceLastClick = 0;
						control.OnDoubleClick?.Invoke();
					}
					else //single click
					{
						lastClickedControl = control;
						timeSinceLastClick = 0;
						control.OnClick?.Invoke();
					}
				}
			}

			control.focused = lastClickedControl == control;

			control.Update();
		}
	}

	public void HandleSDLEvent(SDL_Sharp.Event e)
	{
		//handle mouse events
		if (e.Type == SDL_Sharp.EventType.MouseMotion)
		{
			mouseX = e.Motion.X;
			mouseY = e.Motion.Y;

		}
		else if (e.Type == SDL_Sharp.EventType.KeyDown)
		{
			lastClickedControl?.OnKeyDown?.Invoke(e.Keyboard.Keysym.Sym, e.Keyboard.Keysym.Mod);
		}
		else if (e.Type == SDL_Sharp.EventType.KeyUp)
		{
			lastClickedControl?.OnKeyUp?.Invoke(e.Keyboard.Keysym.Sym, e.Keyboard.Keysym.Mod);
		}
		else if (e.Type == SDL_Sharp.EventType.MouseWheel)
		{
			foreach (UIControl control in controls)
			{
				if (control.mouseOver)
				{
					control.OnScroll?.Invoke(e.Wheel.Y);
				}
			}
		}
	}
}