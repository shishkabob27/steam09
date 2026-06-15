namespace KGUI
{
	public class TabListControl : UIControl
	{
		private int childSpacing = 2;

		public TabListControl(UIControl parent) : base(parent)
		{
			AcceptMouseEvents = false;
		}

		public override void OnChildrenLayoutComplete()
		{
			base.OnChildrenLayoutComplete();

			foreach (var child in Children)
			{
				if (child is TabItemControl tab && tab.Default)
				{
					SetTabSelected(tab);
					break;
				}
			}
		}

		public override void Update()
		{
			base.Update();

			//layout children horizontally
			int totalWidth = 0;
			int maxHeight = 0;
			int cx = childSpacing;
			foreach (var child in Children)
			{
				child.y = 0;
				child.x = cx;
				cx += child.width + childSpacing;
				totalWidth += child.width + childSpacing;
				if (child.height > maxHeight) maxHeight = child.height;
			}

			width = totalWidth;
			height = maxHeight;
		}

		public void SetTabSelected(TabItemControl selectedTab)
		{
			foreach (var child in Children)
			{
				if (child is TabItemControl tab)
				{
					if (selectedTab == tab) tab.selected = true;
					else tab.selected = false;
				}
			}

			//OnTabSelected?.Invoke(tabName);
		}

		//public Action<string> OnTabSelected;
	}
}