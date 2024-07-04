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

		serializedObject.TryGetProperty( nameof( LobbySettings.RoundTime ), out var roundTime );
		serializedObject.TryGetProperty( nameof( LobbySettings.TauntCoolDownTime ), out var tauntCoolDownTime );
		serializedObject.TryGetProperty( nameof( LobbySettings.ForcedTauntTime ), out var forcedTauntTime );
		serializedObject.TryGetProperty( nameof( LobbySettings.PlayersNeededToStart ), out var playersNeededToStart );
		serializedObject.TryGetProperty( nameof( LobbySettings.Bleed ), out var bleed );
		serializedObject.TryGetProperty( nameof( LobbySettings.BleedAmount ), out var bleedAmount );

		var roundTimeSheet = new ControlSheet();
		roundTimeSheet.AddRow( roundTime );
		Layout.Add( roundTimeSheet );

		var tauntCooldownSheet = new ControlSheet();
		tauntCooldownSheet.AddRow( tauntCoolDownTime );
		Layout.Add( tauntCooldownSheet );

		var forcedTauntTimeSheet = new ControlSheet();
		forcedTauntTimeSheet.AddRow( forcedTauntTime );
		Layout.Add( forcedTauntTimeSheet );

		var playersNeededToStartSheet = new ControlSheet();
		playersNeededToStartSheet.AddRow( playersNeededToStart );
		Layout.Add( playersNeededToStartSheet );

		var bleedAmountSheet = new ControlSheet();
		bleedAmountSheet.AddRow( bleed );
		bleedAmountSheet.AddRow( bleedAmount );
		Layout.Add( bleedAmountSheet );


		var button = new Button( "Save" );
		button.Clicked += () =>
		{
			var lobbySettings = new LobbySettings( forcedTauntTime.GetValue<int>(), tauntCoolDownTime.GetValue<int>(), roundTime.GetValue<int>(), playersNeededToStart.GetValue<int>(), bleed.GetValue<bool>(), bleedAmount.GetValue<int>() );
			LobbySettings.SetLobbySettings( lobbySettings );
		};
		Layout.Add( button );
	}
}