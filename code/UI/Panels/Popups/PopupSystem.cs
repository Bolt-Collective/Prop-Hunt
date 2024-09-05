public partial class PopupSystem
{
	[ConCmd( "popup" )]
	public static void DisplayPopup( string text, string title = "", float duration = 8f )
	{
		var popup = new Popup();
		popup.Text = text;
		popup.Title = title;
		popup.Duration = duration;
		PopupList.Instance?.AddChild( popup );
	}

	public static void ClearPopups()
	{
		PopupList.Instance?.DeleteChildren();
	}
}
