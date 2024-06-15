
[Title("Game Manager")]
[Description("The brains of Prop Hunt. Controls rounds, teams, etc.")]
public class PropHuntManager : Component, Component.INetworkListener
{
	[Property] public GameState CurrentGameState { get; set; } = GameState.None;
	[Property] public int PlayersNeededToStart { get; set; } = 2;
	[Property] public int CurrentRound { get; set; } = 0;
	[Property, Sync] public TimeUntil Countdown { get; set; }
	[Property] public List<GameObject> Players { get; set; } = new List<GameObject>();
	private Task GameLoopTask { get; set; }
	
	protected override void OnStart()
	{
		if (IsProxy)
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
		while (CurrentGameState != GameState.Ended)
		{
			switch (CurrentGameState)
			{

				case GameState.None:
					CurrentGameState = GameState.WaitingForPlayers;
					break;
					
				case GameState.WaitingForPlayers:
					if (Scene.GetAllComponents<Player>().Count() >= PlayersNeededToStart)
					{
						await Started();
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
					CurrentGameState = GameState.Started;
					await Task.Frame();
					break;

				case GameState.Started:
					await Round();
					break;

				case GameState.Ending:
					await Task.Frame();
					CurrentGameState = GameState.Ended;
					break;

				case GameState.Ended:
					Log.Info("Game has ended");
					break;

				case GameState.Voting:
					await GameTask.DelaySeconds(5);
					break;

				default:
					await GameTask.DelaySeconds(5);
					break;
			}
		}
	}

	public async Task Started()
	{
		Countdown = 10;
		await Task.DelayRealtimeSeconds(10);
		StartGame();
		CurrentGameState = GameState.Started;
	}
	public void StartGame()
	{
		//Change the gamestate to preparing
		CurrentGameState = GameState.Preparing;
		CurrentRound = 1;
		var players = Scene.GetAllComponents<Player>();
		var spawnList = Scene.GetAllComponents<SpawnPoint>().ToList();
		foreach (var player in players)
		{
			player.Transform.World = Game.Random.FromList(spawnList).Transform.World;
		}
	}

	public async Task Round()
	{
		Countdown = 120;
		await Task.DelayRealtimeSeconds(120);
	}
	
}
