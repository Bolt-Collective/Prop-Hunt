
[Title("Game Manager")]
[Description("The brains of Prop Hunt. Controls rounds, teams, etc.")]
public class PropHuntManager : Component
{
	[Property] public GameState CurrentGameState { get; set; } = GameState.None;
	[Property] public int PlayersNeededToStart { get; set; } = 2;
	[Property] public int CurrentRound { get; set; } = 0;
	
	public async Task GameLoop()
	{
		while (CurrentGameState != GameState.Ended)
		{
			switch (CurrentGameState)
			{
				case GameState.None:
					break;
				case GameState.WaitingForPlayers:
					if (Scene.GetAllComponents<Player>().Count() >= PlayersNeededToStart)
					{
						StartGame();
					}
					break;
				case GameState.Preparing:
					await GameTask.DelaySeconds(5);
					CurrentGameState = GameState.Starting;
					break;
				case GameState.Starting:
					await GameTask.DelaySeconds(5);
					CurrentGameState = GameState.Started;
					break;
				case GameState.Started:
					break;
				case GameState.Ending:
					await GameTask.DelaySeconds(5);
					CurrentGameState = GameState.Ended;
					break;
				case GameState.Ended:
					break;
				case GameState.Voting:
					break;
				default:
					break;
			}
		}
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
}
