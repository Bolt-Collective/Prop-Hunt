
[Title("Game Manager")]
[Description("The brains of Prop Hunt. Controls rounds, teams, etc.")]
public class PropHuntManager : Component, Component.INetworkListener
{
	[Property] public GameState CurrentGameState { get; set; } = GameState.None;
	[Property] public int PlayersNeededToStart { get; set; } = 2;
	[Property] public int CurrentRound { get; set; } = 0;
	[Property] public int RoundsToWin { get; set; } = 3;
	[Property] public int PropsWin { get; set; }
	[Property] public int HuntersWin { get; set; } 
	[Property, Sync] public TimeUntil Countdown { get; set; }
	[Property] public List<GameObject> Props { get; set; } = new List<GameObject>();
	[Property] public List<GameObject> Hunters { get; set; } = new List<GameObject>();
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
					break;

				case GameState.Starting:
					StartGame();
					break;

				case GameState.Started:
					await Round();
					break;

				case GameState.Ending:
					CurrentGameState = GameState.Ended;
					break;

				case GameState.Ended:
					Log.Info("Game has ended");
					await GameTask.DelaySeconds(5);
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
	public string GetGameStateString()
	{
		switch (CurrentGameState)
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
	Log.Info("Starting game");
	CurrentRound++;
	var spawnList = Scene.GetAllComponents<SpawnPoint>().ToList();
	foreach (var player in Scene.GetAllComponents<Player>())
	{
    player.Transform.World = Game.Random.FromList(spawnList).Transform.World;
    if (player.Components.Get<PropShiftingMechanic>().IsProp)
    {
        player.Body.Components.Get<PropShiftingMechanic>().ExitProp();
    }
    var teamComponent = player.Components.Get<TeamComponent>();
    teamComponent.GetRandomTeam();
    Log.Info("Assigned team");
    if (teamComponent.Team == Team.Props)
    {
        Props.Add(player.GameObject);
    }
    else
    {
        Hunters.Add(player.GameObject);
    }
	}
		Log.Info("Game started");
		CurrentGameState = GameState.Started;
	}

	public async Task Round()
	{
		Countdown = 120;
		await Task.DelayRealtimeSeconds(120);
	}
	
}
