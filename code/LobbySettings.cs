using Sandbox;
using System.Text.Json;
public class LobbySettings
{
	public int ForcedTauntTime { get; set; } = 60;
	public int TauntCoolDownTime { get; set; } = 10;
	public int RoundTime { get; set; } = 120;
	public int PlayersNeededToStart { get; set; } = 2;


	public LobbySettings()
	{
		ForcedTauntTime = 60;
		TauntCoolDownTime = 10;
		RoundTime = 120;
	}
	public LobbySettings( int forcedTauntTime, int tauntCoolDownTime, int roundTime, int playersNeededToStart )
	{
		ForcedTauntTime = forcedTauntTime;
		TauntCoolDownTime = tauntCoolDownTime;
		RoundTime = roundTime;
		PlayersNeededToStart = playersNeededToStart;
	}
	public static void SetLobbySettings( LobbySettings lobbySettings )
	{
		string jsonString = JsonSerializer.Serialize( lobbySettings );
		FileSystem.Data.WriteJson( "lobbysettings.json", jsonString );

		string jsonFromFile = FileSystem.Data.ReadJson<string>( "lobbysettings.json" );
		LobbySettings lobbySettingsFromFile = JsonSerializer.Deserialize<LobbySettings>( jsonFromFile );
		Log.Info( $"ForcedTauntTime: {lobbySettingsFromFile.ForcedTauntTime}" );
		Log.Info( $"TauntCoolDownTime: {lobbySettingsFromFile.TauntCoolDownTime}" );
		Log.Info( $"RoundTime: {lobbySettingsFromFile.RoundTime}" );
		Log.Info( $"PlayersNeededToStart: {lobbySettingsFromFile.PlayersNeededToStart}" );
	}
}
