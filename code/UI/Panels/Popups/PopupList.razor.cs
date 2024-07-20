using Sandbox.UI;

public partial class PopupList : Panel
{
	public static PopupList Instance { get; set; }

	public PopupList() 
	{
		Instance = this;
	}

	public override void Tick()
	{
		base.Tick();

		// Very hacky solution to stop multiple popups from happening.. yuck
		if ( Children.Count() > 1 )
		{
			GetChild(1).Delete(true);
		}
	}
}

