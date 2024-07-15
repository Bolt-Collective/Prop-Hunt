using Sandbox;
using System.Text.Json;

public enum HuntersPropGrabbing
{
	On,
	Off,
	Bleed
}

public class LobbySettings
{
	public int ForcedTauntTime { get; set; } = 60;
	public int TauntCoolDownTime { get; set; } = 10;
	public int PreRoundTime { get; set; } = 30;
	public int RoundTime { get; set; } = 360;
	public int PlayersNeededToStart { get; set; } = 64;
	public HuntersPropGrabbing HunterPropGrabMode { get; set; } = HuntersPropGrabbing.Off;
	public int BleedAmount { get; set; } = 10;
	public int RoundCount { get; set; } = 6;
	public bool AllowMapVoting { get; set; }


	public LobbySettings()
	{
		ForcedTauntTime = 60;
		TauntCoolDownTime = 10;
		RoundTime = 120;
		PlayersNeededToStart = 2;
		HunterPropGrabMode = HuntersPropGrabbing.Off;
		BleedAmount = 10;
		RoundCount = 6;
		AllowMapVoting = true;
		PreRoundTime = 30;
	}
	public LobbySettings( int forcedTauntTime, int tauntCoolDownTime, int roundTime, int playersNeededToStart, int bleedAmount, int roundCount, bool allowMapVoting, int preRoundTime )
	{
		ForcedTauntTime = forcedTauntTime;
		TauntCoolDownTime = tauntCoolDownTime;
		RoundTime = roundTime;
		PlayersNeededToStart = playersNeededToStart;
		HunterPropGrabMode = HuntersPropGrabbing.Off;
		PreRoundTime = preRoundTime;
		BleedAmount = bleedAmount;
		RoundCount = roundCount;
		AllowMapVoting = allowMapVoting;
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
