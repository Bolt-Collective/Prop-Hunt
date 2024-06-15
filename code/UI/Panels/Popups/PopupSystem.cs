
public partial class PopupSystem 
{
	public static void DisplayPopup( string text, string title = "", float duration = 8f )
	{
		var Popup = new Popup();
		Popup.Text = text;
		Popup.Title = title;
		Popup.Duration = duration;
		PopupList.Instance.AddChild( Popup );
	}
}
