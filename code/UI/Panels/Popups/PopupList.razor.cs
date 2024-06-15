using Sandbox.UI;

public partial class PopupList : Panel
{
	public static PopupList Instance { get; set; }

	public PopupList() 
	{
		Instance = this;
	}
}

