using Editor;
using Sandbox;
[CustomEditor( typeof( LobbySettings ) )]
public class LobbySettingsControlWidget : ControlWidget
{
	public LobbySettingsControlWidget( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Column();
		Layout.Spacing = 2;
		if ( property.IsNull )
		{
			property.SetValue( new LobbySettings() );
		}

		var serializedObject = property.GetValue<LobbySettings>()?.GetSerialized();
		if ( serializedObject is null ) return;

		serializedObject.TryGetProperty( nameof( LobbySettings.RoundTime ), out var roundTime );
		serializedObject.TryGetProperty( nameof( LobbySettings.TauntCoolDownTime ), out var tauntCoolDownTime );
		serializedObject.TryGetProperty( nameof( LobbySettings.ForcedTauntTime ), out var forcedTauntTime );
		serializedObject.TryGetProperty( nameof( LobbySettings.PlayersNeededToStart ), out var playersNeededToStart );

		Layout.Add( new Label( "Round Time" ) );
		Layout.Add( new IntegerControlWidget( roundTime ) );
		Layout.Add( new Label( "Taunt Cooldown" ) { } );
		Layout.Add( new IntegerControlWidget( tauntCoolDownTime ) );
		Layout.Add( new Label( "Forced Taunt Time" ) { } );
		Layout.Add( new IntegerControlWidget( forcedTauntTime ) );
		Layout.Add( new Label( "Players needed to start" ) );
		Layout.Add( new IntegerControlWidget( playersNeededToStart ) );
		var button = new Button( "Save" );
		button.Clicked += () =>
		{
			var lobbySettings = new LobbySettings( forcedTauntTime.GetValue<int>(), tauntCoolDownTime.GetValue<int>(), roundTime.GetValue<int>(), playersNeededToStart.GetValue<int>() );
			LobbySettings.SetLobbySettings( lobbySettings );
		};
		Layout.Add( button );
	}
}
