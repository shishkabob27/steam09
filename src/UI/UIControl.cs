using SDL_Sharp;

public class UIControl
{
	public UIPanel parent;
	public Renderer renderer;

	public string ControlName;

	public Dictionary<string, object> Metadata = new Dictionary<string, object>();

	public int x;
	public int y;
	public int zIndex = 0;
	public int width;
	public int height;

	//if false, mouse events will not be sent to this control, except for scroll events
	public bool acceptMouseButtons = true;

	public bool ManualDraw = false;

	public bool visible = true;
	public bool enabled = true;

	public bool focused = false;

	public bool mouseOver = false;
	public bool mouseDown = false;

	public string text = "";

	public UIControl(UIPanel parent, Renderer renderer, string controlName, int x, int y, int width = 0, int height = 0)
	{
		this.parent = parent;
		this.renderer = renderer;
		this.ControlName = controlName;
		this.x = x;
		this.y = y;
		this.width = width;
		this.height = height;
	}

	public virtual void Update()
	{

	}

	public virtual void Draw()
	{
	}

	public Action OnClick;
	public Action OnDoubleClick;
	public Action OnRightClick;
	public Action<int> OnScroll;
	public Action<Keycode, KeyModifier> OnKeyDown;
	public Action<Keycode, KeyModifier> OnKeyUp;
}