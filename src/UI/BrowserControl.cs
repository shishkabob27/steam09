using System.Xml.Serialization;
using SDL;
using KGUI.Controls;

public class BrowserControl : UIControl
{
	[XmlAttribute("initialURL")]
	string _initialURL = "http://store.steampowered.com/";
	Browser _browser;

	RootControl _rootControl;
	
	public BrowserControl(UIControl parent) : base(parent)
	{
		_browser = new Browser();
		_browser.Initialize();
		_browser.LoadURL(_initialURL);

		this.OnMouseDown += (button) =>
		{
			_browser.OnMouseDown(button);
		};
		this.OnMouseUp += (button) =>
		{
			_browser.OnMouseUp(button);
		};
		this.OnScroll += (control, scrollY) =>
		{
			_browser.OnMouseScroll(0, scrollY);
		};
		this.OnKeyDown += (control, key, mod) =>
		{
			_browser.OnKeyDown(key, mod);
		};
		this.OnKeyUp += (control, key, mod) =>
		{
			_browser.OnKeyUp(key, mod);
		};
	}
	

	public override void Update()
	{
		base.Update();
		if (!enabled || !visible) return;

		_browser.Resize(width, height);

		_browser.OnMouseMove(GetRelativeMouseX(), GetRelativeMouseY());


		_browser.Update();
	}

	public override void Draw()
	{
		if (!enabled) return;

		if (_rootControl == null)
		{
			UIControl currentParent = parent;
			while (currentParent.parent != null)
			{
				currentParent = currentParent.parent;
			}
			this._rootControl = currentParent as RootControl;
			if (this._rootControl == null) return;
		}
		
		unsafe {
			_browser.Draw(_rootControl.panel.window.renderer, new SDL_FRect { x = GetAbsoluteX(), y = GetAbsoluteY(), w = width, h = height });
		}
	}

	public void LoadURL(string url)
	{
		_browser.LoadURL(url);
	}
}