using System.Data;
using System.Net.Http.Headers;
using Sandbox.Utility;
namespace PropHunt;

[Title( "Game Manager" )]
[Description( "The brains of Prop Hunt. Controls rounds, teams, etc." )]
public partial class PropHuntManager : Component, Component.INetworkListener
{
	[HostSync, Property] public GameState RoundState { get; set; } = GameState.None;
	[HostSync] public string RoundStateText { get; set; }

	[HostSync] public TimeSince TimeSinceRoundStateChanged { get; set; } = 0;
	[HostSync] public int RoundLength { get; set; } = 120;

	public static int PreRoundTime { get; set; } = 5;

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
	public static IEnumerable<Player> ActivePlayers => AllPlayers.Where( x => x.TeamComponent.TeamName != Team.Unassigned.ToString() );

	/// <summary>
	/// Players not assigned to a team, or spectating.
	/// </summary>
	public static IEnumerable<Player> InactivePlayers => AllPlayers.Where( x => x.TeamComponent.TeamName == Team.Unassigned.ToString() );

	/// <summary>
	/// Players assigned to a particular team.
	/// </summary>
	public static IEnumerable<Player> GetPlayers( Team team ) => AllPlayers.Where( x => x.TeamComponent.TeamName == team.ToString() );

	[Property, HostSync] public int MaxPlayersToStart { get; set; } = 2;

	/// <summary>
	/// Next map chosen by RTV
	/// </summary>
	public string NextMap { get; set; } = null;
	public static bool IsFirstRound { get; set; } = true;
	public static PropHuntManager Instance { get; set; }
	public List<(string, int)> Votes { get; set; } = new();
	[Property, HostSync] public bool OnGoingRound { get; set; } = false;

	[Property, Sync] public TimeSince TimeSinceLastForceTaunt { get; set; }

	[Property, Sync] public int ForceTauntCooldown { get; set; } = 60;

	protected override void OnStart()
	{
		Instance = this;
	}
	protected override void OnUpdate()
	{
		if ( !IsProxy && AllPlayers.Count() > 2 )
		{
			MaxPlayersToStart = Connection.All.Count;
		}
		else
		{
			MaxPlayersToStart = 2;
		}
		if ( !Networking.IsHost ) return;

		if ( RoundState != GameState.Started )
		{
			ResetForceTauntValues();
		}

		GameStateManager();

	}
	[Broadcast]
	void GameStateManager()
	{
		switch ( RoundState )
		{
			case GameState.None:
				RoundStateText = "";
				RoundState = GameState.WaitingForPlayers;
				break;
			case GameState.WaitingForPlayers:
				RoundStateText = $"{"Waiting For Players"} {AllPlayers.Count()} / {MaxPlayersToStart}";

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
	public void OnRoundPreparing()
	{
		OnGoingRound = true;
		RoundState = GameState.Preparing;
		RoundLength = PreRoundTime;
		TimeSinceRoundStateChanged = 0;

	}

	private void ResetForceTauntValues()
	{
		TimeSinceLastForceTaunt = 0;
	}

	// Method to check if a taunt can be played based on a cooldown
	private bool CanPlayTaunt()
	{
		return TimeSinceLastForceTaunt >= ForceTauntCooldown;
	}

	public void OnRoundStarting()
	{
		Log.Info( "Round starting" );
		RoundState = GameState.Starting;
		RoundLength = 30;
		TimeSinceRoundStateChanged = 0;
		AssignEvenTeams();

		Scene.GetAllComponents<MapInstance>().FirstOrDefault()?.UnloadMap();




		foreach ( var player in Scene.GetAllComponents<Player>().Where( x => x.TeamComponent.TeamName == Team.Hunters.ToString() ) )
		{
			player.HunterStart();
		}
		if ( IsFirstRound )
		{
			// Make intermission time 1 second after round 1
			PreRoundTime = 1;
			IsFirstRound = false;
		}
		if ( !IsProxy )
		{
			BroadcastPopup( "Hide or die", "The seekers will be unblinded in 30s", 30f );
		}
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToList();
		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			var randomPoint = Game.Random.FromList( spawnPoints );
			player.EyeAngles = randomPoint.Transform.Rotation.Angles();
			player.Transform.World = randomPoint.Transform.World;
		}



	}
	public Random GetRandom()
	{
		return new Random();
	}
	public void AssignEvenTeams()
	{
		Random rng = GetRandom();
		var randomList = AllPlayers.OrderBy( a => rng.Next() ).ToList();

		for ( int i = 0; i < randomList.Count; i++ )
		{
			if ( i % 2 == 0 )
			{
				randomList[i].TeamComponent.ChangeTeam( Team.Props );
			}
			else
			{
				randomList[i].TeamComponent.ChangeTeam( Team.Hunters );
			}
			randomList[i].Respawn();
		}
	}
	public void ForceRestart()
	{
		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			player.Respawn();
		}
		RoundState = GameState.Preparing;
		TimeSinceRoundStateChanged = 0;
		RoundLength = PreRoundTime;
		ClearListBroadcast();
		Scene.GetAllComponents<MapInstance>().FirstOrDefault()?.UnloadMap();
	}

	[Broadcast]
	public void BroadcastPopup( string text, string title, float duration = 8f )
	{
		PopupSystem.DisplayPopup( text, title, duration );
	}

	[Broadcast]
	public void ClearListBroadcast()
	{
		PopupSystem.ClearPopups();
	}

	public void OnRoundStart()
	{
		foreach ( var player in Scene.GetAllComponents<Player>().Where( x => x.TeamComponent.TeamName == Team.Hunters.ToString() ) )
		{
			player.HunterUnblind();
		}

		BroadcastPropTags();

		Log.Info( "Gave all props 'prop' tag" );
		RoundState = GameState.Started;
		RoundLength = RoundTime;
		TimeSinceRoundStateChanged = 0;
	}
	[Broadcast]
	public void BroadcastPropTags()
	{
		foreach ( var prop in Scene.Directory.FindByName( "prop_physics" ) )
		{
			prop.Tags.Add( "prop" );
		}
	}
	public void OnRoundEnding()
	{
		OnGoingRound = false;
		RoundState = GameState.Ending;
		TimeSinceRoundStateChanged = 0;
		RoundLength = 15;
	}

	[Sync] public int RoundNumber { get; set; } = 0;
	public int GetRandom( int min, int max )
	{
		return Random.Shared.Int( min, max );
	}
	public void OnRoundEnd()
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
		//TEMP
		ResetRound();
	}
	public void ResetRound()
	{
		foreach ( var player in AllPlayers )
		{
			player.Respawn();
		}
		RoundState = GameState.None;
		TimeSinceRoundStateChanged = 0;
	}

	/// <summary>
	/// Logic happening every update
	/// </summary>
	public void RoundTick()
	{


		// Introduce a cooldown for taunts to prevent spamming
		if ( CanPlayTaunt() )
		{
			foreach ( var player in Scene.GetAllComponents<Player>().Where( x => x.TeamComponent.TeamName == Team.Props.ToString()) )
			{
				var tauntComponents = player.Components.Get<TauntComponent>();

				if ( tauntComponents != null )
				{
					tauntComponents.PlayRandomTaunt();
				}
			}

			//Have to make it a method since if I change one, the rest are not going to be changed
			ResetForceTauntValues();
		}

		if ( TimeSinceRoundStateChanged > RoundLength || Scene.GetAllComponents<Player>().Where( x => x.TeamComponent.TeamName == Team.Hunters.ToString() ).All( x => x.Health <= 0 ) )
		{
			ForceWin( Team.Props );
		}
		else if ( Scene.GetAllComponents<Player>().Where( x => x.TeamComponent.TeamName == Team.Props.ToString() ).Count( x => x.Health > 0 ) <= 0 )
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

	[ConCmd( "restart" )]
	public static void Restart()
	{
		if ( !Game.IsEditor ) return;
		Instance.ForceRestart();
	}

	[ConCmd( "props" )]
	public static void Props()
	{
		Player.Local.TeamComponent.ChangeTeam( Team.Props );
	}

	[ConCmd( "hunters" )]
	public static void Hunters()
	{
		Player.Local.TeamComponent.ChangeTeam( Team.Hunters );
	}

	[ConCmd( "SkipPrep" )]
	public static void SkipPreparing()
	{
		if ( !Game.IsEditor ) return;
		Instance.OnRoundStart();
	}
}
