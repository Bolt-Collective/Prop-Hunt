using Editor;
using Sandbox;
[CustomEditor( typeof( LobbySettings ) )]
public class LobbySettingsControlWidget : ControlWidget
{
	public LobbySettingsControlWidget( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Column();
		PaintBackground = false;
		Layout.Spacing = 2;
		if ( property.IsNull )
		{
			property.SetValue( new LobbySettings() );
		}

		var serializedObject = property.GetValue<LobbySettings>()?.GetSerialized();
		if ( serializedObject is null ) return;

		var controlSheet = new ControlSheet();
		controlSheet.AddObject( serializedObject );
		Layout.Add( controlSheet );


		var button = new Button( "Save" );
		button.Clicked += () =>
		{
			var lobbySettings = property.GetValue<LobbySettings>();
			LobbySettings.SetLobbySettings( lobbySettings );
		};
		Layout.Add( button );
	}
}
