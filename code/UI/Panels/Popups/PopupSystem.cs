﻿
public partial class PopupSystem 
{
	public static void DisplayPopup( string text, string title = "", float duration = 8f )
	{
		var popup = new Popup();
		popup.Text = text;
		popup.Title = title;
		popup.Duration = duration;
		PopupList.Instance.AddChild( popup );
	}
}
