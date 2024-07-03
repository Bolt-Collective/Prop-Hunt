using Sandbox;
using System.Text.Json;

public class LobbySettings
{
	public int ForcedTauntTime { get; set; } = 60;
	public int TauntCoolDownTime { get; set; } = 10;
	public int RoundTime { get; set; } = 120;
	public int PlayersNeededToStart { get; set; } = 2;
	public bool Bleed { get; set; } = false;
	public int BleedAmount { get; set; } = 10;


	public LobbySettings()
	{
		ForcedTauntTime = 60;
		TauntCoolDownTime = 10;
		RoundTime = 120;
		PlayersNeededToStart = 2;
		Bleed = false;
		BleedAmount = 10;
	}
	public LobbySettings( int forcedTauntTime, int tauntCoolDownTime, int roundTime, int playersNeededToStart, bool bleed, int bleedAmount )
	{
		ForcedTauntTime = forcedTauntTime;
		TauntCoolDownTime = tauntCoolDownTime;
		RoundTime = roundTime;
		PlayersNeededToStart = playersNeededToStart;
		Bleed = bleed;
		BleedAmount = bleedAmount;
	}
	public static void SetLobbySettings( LobbySettings lobbySettings )
	{
		string jsonString = JsonSerializer.Serialize( lobbySettings );
		FileSystem.Data.WriteJson( "lobbysettings.json", jsonString );

		string jsonFromFile = FileSystem.Data.ReadJson<string>( "lobbysettings.json" );
		LobbySettings lobbySettingsFromFile = JsonSerializer.Deserialize<LobbySettings>( jsonFromFile );
		Log.Info( jsonFromFile );
	}
}
