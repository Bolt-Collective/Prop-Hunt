
using System.Text.Json.Serialization;

public enum RoundState
{
	None,
	Waiting,
	Active,
	Ended
}

public partial class GameManager : Component, Component.INetworkListener
{
	public static GameManager Instance { get; set; } 

	[Sync(), Property]
	public RoundState GameState { get; set; } = RoundState.Waiting;
	
	[Property, Category( "Actions" )] public Action OnRoundStart { get; set; }
	[Property, Category( "Actions" )] public Action OnRoundEnd { get; set; }
	[Property, Category( "Actions" )] public Action RoundUpdate { get; set; }
	[Property, Category( "Actions" )] public Action<GameObject> OnPlayerJoin { get; set; }

	[InlineEditor, Property, Sync, JsonIgnore] public TimeUntil RoundTimer { get; set; }
	[Sync, InlineEditor, Property, JsonIgnore, Category( "Game State" )] public TimeSince TimeSinceStateSwitch { get; set; } = 0;
	[Property, Category( "Game Settings" )] public int PlayersToStart { get; set; } = 2;

	[Sync]
	public string RoundName { get; set; } = "Waiting";

	protected override void OnAwake()
	{
		base.OnAwake();
		
		Instance = this;
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}
		
		if ( Application.IsHeadless && !Connection.All.Any() && GameState == RoundState.Active )
		{
			//EndGame();
			Log.Info( "No players connected, ending game" );
		}
		
		if ( GameState == RoundState.Waiting && CanStartGame() )
		{
			RoundName = "Active";
			GameState = RoundState.Active;
			TimeSinceStateSwitch = 0;
			
			Log.Info( "active" );
		}
	}
	
	public bool CanStartGame()
	{
		return (Scene.GetAll<Player>().Count() >= (Application.IsHeadless ? 1 : PlayersToStart)) && TimeSinceStateSwitch > 5;
	}
}
