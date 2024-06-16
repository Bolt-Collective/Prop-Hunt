
[Title( "Game Manager" )]
[Description( "The brains of Prop Hunt. Controls rounds, teams, etc." )]
public class PropHuntManager : Component, Component.INetworkListener
{
	[Property] public GameState CurrentGameState { get; set; } = GameState.None;
	[Property] public int PlayersNeededToStart { get; set; } = 2;
	[Property] public int PropsWin { get; set; }
	[Property] public int HuntersWin { get; set; }
	[Property, Sync] public TimeUntil Countdown { get; set; }
	[Property] public List<GameObject> Props { get; set; } = new List<GameObject>();
	[Property] public List<GameObject> Hunters { get; set; } = new List<GameObject>();
	private Task GameLoopTask { get; set; }

	protected override void OnStart()
	{
		if ( IsProxy )
			return;
		_ = ResumeGame();
	}

	public Task ResumeGame()
	{
		return GameLoopTask ??= GameLoop();
	}
	void INetworkListener.OnBecameHost( Connection previousHost )
	{
		Log.Info( "Resuming game loop on second client" );
		_ = ResumeGame();
	}

	public async Task GameLoop()
	{
		while ( true )
		{
			switch ( CurrentGameState )
			{

				case GameState.None:
					CurrentGameState = GameState.WaitingForPlayers;
					break;

				case GameState.WaitingForPlayers:
					if ( Scene.GetAllComponents<Player>().Count() >= PlayersNeededToStart )
					{
						CurrentGameState = GameState.Preparing;
					}
					else
					{
						Countdown = 0;
						await Task.Frame();
					}
					break;

				case GameState.Preparing:
					CurrentGameState = GameState.Starting;
					await Task.Frame();
					break;

				case GameState.Starting:
					StartGame();
					CurrentGameState = GameState.Started;
					await Task.Frame();
					break;

				case GameState.Started:
					await Round();
					CurrentGameState = GameState.Ending;
					break;

				case GameState.Ending:
					await Ending();
					CurrentGameState = GameState.Ended;
					break;

				case GameState.Ended:
					await Ended();
					CurrentGameState = GameState.Voting;
					break;

				case GameState.Voting:
					Countdown = 5;
					await GameTask.DelaySeconds( 5 );
					CurrentGameState = GameState.WaitingForPlayers;
					break;
			}
		}
	}
	public string GetGameStateString()
	{
		switch ( CurrentGameState )
		{
			case GameState.None:
				return "None";
			case GameState.WaitingForPlayers:
				return "Waiting For Players";
			case GameState.Preparing:
				return "Preparing";
			case GameState.Starting:
				return "Starting";
			case GameState.Started:
				return "Started";
			case GameState.Ending:
				return "Ending";
			case GameState.Ended:
				return "Ended";
			case GameState.Voting:
				return "Voting";
			default:
				return "None";

		}
	}
	public void StartGame()
	{
		Log.Info( "Starting game" );
		var spawnList = Scene.GetAllComponents<SpawnPoint>().ToList();
		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			player.Transform.World = Game.Random.FromList( spawnList ).Transform.World;
			if ( player.Components.Get<PropShiftingMechanic>().IsProp )
			{
				player.Body.Components.Get<PropShiftingMechanic>().ExitProp();
			}
			var teamComponent = player.Components.Get<TeamComponent>();
			teamComponent.GetRandomTeam();
			Log.Info( "Assigned team" );
			if ( teamComponent.Team == Team.Props )
			{
				Props.Add( player.GameObject );
			}
			else
			{
				Hunters.Add( player.GameObject );
			}
			foreach ( var hunter in Hunters )
			{
				hunter.Components.Get<Inventory>().SpawnStartingItems();
			}
		}
		Log.Info( "Game started" );
	}

	public async Task Round()
	{
		if ( Props.Count == 0 )
		{
			HuntersWin++;
		}
		else if ( Hunters.Count == 0 )
		{
			PropsWin++;
		}
		Countdown = 2;
		await Task.DelayRealtimeSeconds( 2 );
	}

	public async Task Ending()
	{
		Countdown = 5;
		await Task.DelayRealtimeSeconds( 5 );
	}

	public async Task Ended()
	{
		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			player.ResetStats();
		}
		await Task.Frame();
	}

}
