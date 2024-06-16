using Sandbox.Utility;
[Title( "Game Manager" )]
[Description( "The brains of Prop Hunt. Controls rounds, teams, etc." )]
public partial class PropHuntManager : Component, Component.INetworkListener
{
	[HostSync] public GameState RoundState { get; set; } = GameState.None;
	[HostSync] public string RoundStateText { get; set; }

	[HostSync] public TimeSince TimeSinceRoundStateChanged { get; set; } = 0;
	[HostSync] public int RoundLength { get; set; } = 0;

	public static int PreRoundTime { get; set; } = 30;

	public static int RoundTime { get; set; } = 6 * 60;

	/// <summary>
	/// How many rounds to play before map voting
	/// </summary>
	public static int RoundCount { get; set; } = 6;


	/// <summary>
	/// All players, both assigned to a team and spectating.
	/// </summary>
	public static IEnumerable<Player> AllPlayers => Game.ActiveScene.GetAllComponents<Player>();

	/// <summary>
	/// Players assigned to a team, so not spectating.
	/// </summary>
	public static IEnumerable<Player> ActivePlayers => AllPlayers.Where( x => x.TeamComponent.Team != Team.Unassigned );

	/// <summary>
	/// Players not assigned to a team, or spectating.
	/// </summary>
	public static IEnumerable<Player> InactivePlayers => AllPlayers.Where( x => x.TeamComponent.Team == Team.Unassigned );

	/// <summary>
	/// Players assigned to a particular team.
	/// </summary>
	public static IEnumerable<Player> GetPlayers( Team team ) => AllPlayers.Where( x => x.TeamComponent.Team == team );

	public static int MaxPlayersToStart { get; set; } = 2;

	/// <summary>
	/// Next map chosen by RTV
	/// </summary>
	public string NextMap { get; set; } = null;
	public static bool IsFirstRound { get; set; } = true;

	protected override void OnUpdate()
	{
		// Network spawn all props and gibs
		foreach ( var prop in Scene.GetAllComponents<Prop>() )
		{
			if ( prop.GameObject.NetworkMode != NetworkMode.Object )
			{
				prop.GameObject.NetworkSpawn( null );
			}
		}

		foreach ( var gib in Scene.GetAllComponents<Gib>() )
		{
			if ( gib.GameObject.NetworkMode != NetworkMode.Object )
			{
				gib.GameObject.NetworkSpawn( null );
			}
		}

		switch ( RoundState )
		{
			case GameState.None:
				RoundStateText = "";
				RoundState = GameState.WaitingForPlayers;
				break;
			case GameState.WaitingForPlayers:
				RoundStateText = "Waiting For Players";

				if ( AllPlayers.Count() >= MaxPlayersToStart )
					OnRoundPreparing();

				break;
			case GameState.Preparing:
				RoundStateText = "Intermission";
				if ( TimeSinceRoundStateChanged > RoundLength )
					OnRoundStarting();
				break;
			case GameState.Starting:
				RoundStateText = "Preparing";
				if ( TimeSinceRoundStateChanged > RoundLength )
					OnRoundStart();
				break;
			case GameState.Started:
				RoundStateText = "Ongoing";
				RoundTick();
				break;
			case GameState.Ending:
				RoundStateText = "Ending";
				if ( TimeSinceRoundStateChanged > RoundLength )
					OnRoundEnd();
				break;
			case GameState.Ended:
				RoundStateText = "Ended";

				break;
			case GameState.Voting:
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	protected void OnRoundPreparing()
	{
		RoundState = GameState.Preparing;
		RoundLength = PreRoundTime;
		TimeSinceRoundStateChanged = 0;
	}

	protected void OnRoundStarting()
	{
		RoundState = GameState.Starting;
		RoundLength = 30;
		TimeSinceRoundStateChanged = 0;

		if ( IsFirstRound )
		{
			// Make intermission time 1 second after round 1
			PreRoundTime = 1;
			IsFirstRound = false;
		}

		PopupSystem.DisplayPopup( "Hide or die", "The seekers will be unblinded in 30 seconds", 30f );
	}

	protected void OnRoundStart()
	{
		RoundState = GameState.Started;
		RoundLength = RoundTime; // 360 seconds
		TimeSinceRoundStateChanged = 0;
	}


	public virtual void OnRoundEnding()
	{
		RoundState = GameState.Ending;
		TimeSinceRoundStateChanged = 0;
		RoundLength = 15;


	}

	[HostSync] public int RoundNumber { get; set; } = 0;
	public virtual void OnRoundEnd()
	{
		RoundState = GameState.Ended;
		TimeSinceRoundStateChanged = 0;

		GetPlayers( Team.Props ).ToList().Clear();
		GetPlayers( Team.Hunters ).ToList().Clear();

		// TODO: implement RTV and map votes


		if ( NextMap != null )
		{
			var mapChanger = Game.ActiveScene.GetAllComponents<MapChanger>().FirstOrDefault();
			mapChanger?.LoadMap( NextMap );

			return;
		}

		if ( RoundNumber >= RoundCount )
		{
			DoMapVote();
		}
		else
		{
			RoundNumber++;
			ResetRound();
		}
	}

	public void DoMapVote()
	{
		// TODO: do map vote
		Log.Info( "map vote" );
	}

	public void ResetRound()
	{
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToList();

		foreach ( var player in AllPlayers )
		{
			player?.ResetStats();
			player.Transform.World = Game.Random.FromList( spawnPoints ).Transform.World;
		}

		RoundState = GameState.None;
		TimeSinceRoundStateChanged = 0;
	}

	/// <summary>
	/// Logic happening every update
	/// </summary>
	protected void RoundTick()
	{
		if ( TimeSinceRoundStateChanged > RoundLength && GetPlayers( Team.Hunters ).Count( x => x.Health <= 0 ) <= 0 )
		{
			ForceWin( Team.Props );
		}
		else if ( GetPlayers( Team.Props ).Count( x => x.Health > 0 ) <= 0 )
		{
			ForceWin( Team.Hunters );
		}

	}

	private Team WinningTeam { get; set; }
	[HostSync] public string WinningTeamName { get; set; }


	public void ForceWin( Team team )
	{
		WinningTeam = team;
		WinningTeamName = team.GetName();

		Log.Info( WinningTeamName + " win!" );

		OnRoundEnding();
	}
}
