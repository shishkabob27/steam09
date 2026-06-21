using System.Drawing;

namespace KGUI.Controls
{
	public class TreeViewItem : UIControl
    {
        bool _isCategory = false;
        protected bool _expanded = true;

		protected virtual int GetInitialHeight() => 30; //change this to whatever height you want your category to be

        public TreeViewItem(UIControl parent) : base(parent)
        {
        }

        public void SetIsCategory(bool category = false)
        {
            this._isCategory = category;
            if (_isCategory)
            {
                OnClick += TestClickExpandButton;
            
                OnDoubleClick += (control) =>
                {
                    _expanded = !_expanded;
                };
            }
        }


        public override void Update()
        {
            base.Update();

			if (!_isCategory)
			{
				return;
			}

			int totalHeight = GetInitialHeight();

			int childY = GetInitialHeight();
            foreach (var child in _children)
            {
                if (!_expanded)
                {
                    child.visible = false;
                    continue;
                }
                child.visible = true;
                child.y = childY;
                childY += child.height;
                child.width = width;
                child.x = x;
				totalHeight += child.height;
            }

			this.height = totalHeight;
        }

        public override void Draw()
        {
            base.Draw();

            int textX = 21;
        
            if (focused)
            {
                DrawBox(0, 0, width, height, Color.FromArgb(244, 244, 254));
            }

            if (_isCategory)
            {
                DrawBox(6, 3, 9, 9, Color.White);
                DrawBoxBorder(6, 3, 9, 9, Color.FromArgb(160, 160, 160));

                DrawBox(8, 7, 5, 1, Color.Black);
                if (!_expanded) DrawBox(10, 5, 1, 5, Color.Black);
            }

            //draw text
            DrawText(text, textX, (height / 2) - 5, Color.Black, bold: _isCategory);
        }
        
        public virtual void TestClickExpandButton(UIControl control)
        {
            if (GetRelativeMouseX() < 6 || GetRelativeMouseX() > 15 || GetRelativeMouseY() < 3 || GetRelativeMouseY() > 12) return;
            _expanded = !_expanded;
        }

		public void SetExpanded(bool expanded)
		{
			if (!_isCategory) return;
			_expanded = expanded;
		}

		public bool IsExpanded()
		{
			if (!_isCategory) return true;
			return _expanded;
		}
    }
}