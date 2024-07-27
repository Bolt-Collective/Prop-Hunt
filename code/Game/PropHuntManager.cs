using System.Data;
using System.IO;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Transactions;
using Sandbox.Utility;
namespace PropHunt;

[Title( "Game Manager" )]
[Description( "The brains of Prop Hunt. Controls rounds, teams, etc." )]
public partial class PropHuntManager : Component, Component.INetworkListener
{
	[HostSync, Property] public GameState RoundState { get; set; } = GameState.None;
	[HostSync] public string RoundStateText { get; set; }

	[HostSync, Property] public TimeSince TimeSinceRoundStateChanged { get; set; } = 0;
	[HostSync, Property] public int RoundLength { get; set; } = 6 * 60;

	public static int PreRoundTime { get; set; } = 5;

	/// <summary>
	/// How many rounds to play before map voting
	/// </summary>


	[Group( "Game Sounds" ), Property]
	public SoundEvent NotificationSound { get; set; }

	[Group( "Game Sounds" ), Property]
	public SoundEvent PropsWinSound { get; set; }

	[Group( "Game Sounds" ), Property]
	public SoundEvent HuntersWinSound { get; set; }

	[Group( "Game Sounds" ), Property]
	public SoundEvent OnePropLeftSound { get; set; }

	[Group( "Game Sounds" ), Property]
	public SoundEvent TimeRunningOutSound { get; set; }

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
	[Property] public Dictionary<string, int> Votes { get; set; } = new();
	[Property, Sync] public bool OnGoingRound { get; set; } = false;
	[Sync] public TimeSince TimeSinceLastForceTaunt { get; set; }
	[Property] public LobbySettings LobbySettings { get; set; } = new();
	[Property, Sync] public bool PauseRoundState { get; set; } = false;
	protected override void OnStart()
	{
		Instance = this;
		if ( Networking.IsHost )
		{
			var json = FileSystem.Data.ReadJson<string>( "lobbysettings.json" );
			if ( json is not null )
			{
				LobbySettings = JsonSerializer.Deserialize<LobbySettings>( json );
			}
			else
			{
				LobbySettings = new LobbySettings();
			}
			if ( LobbySettings.RoundCount < 0 )
			{
				LobbySettings.RoundCount = 0;
			}
		}
	}
	[Broadcast]
	public void AddVote( Player player, string map )
	{
		Log.Info( "Vote added for " + map );
		if ( player.AbleToVote )
		{
			if ( Votes.ContainsKey( map ) )
			{
				Votes[map]++;
			}
			else
			{
				Votes.Add( map, 1 );
			}
			player.CurrentMapVote = map;
			player.AbleToVote = false;
		}
		else if ( !player.AbleToVote && player.CurrentMapVote != map )
		{
			if ( Votes.ContainsKey( player.CurrentMapVote ) )
			{
				Votes[player.CurrentMapVote]--;
				if ( Votes[player.CurrentMapVote] == 0 )
				{
					Votes.Remove( player.CurrentMapVote );
				}
			}

			if ( Votes.ContainsKey( map ) )
			{
				Votes[map]++;
			}
			else
			{
				Votes.Add( map, 1 );
			}
			player.CurrentMapVote = map;
		}

	}

	protected override void OnUpdate()
	{
		if ( !IsProxy && AllPlayers.Count() > 2 )
		{
			MaxPlayersToStart = Connection.All.Count;
		}
		else
		{
			MaxPlayersToStart = LobbySettings.PlayersNeededToStart;
		}
		if ( !Networking.IsHost ) return;

		if ( RoundState != GameState.Started )
		{
			ResetForceTauntValues();
		}
		GameStateManager();
	}
	void INetworkListener.OnBecameHost( Sandbox.Connection previousHost )
	{
		Log.Info( "I am the host now" );
	}
	[Broadcast]
	void GameStateManager()
	{
		if ( PauseRoundState || !Scene.GetAllComponents<MapInstance>().FirstOrDefault().IsLoaded )
		{
			TimeSinceRoundStateChanged = RoundLength;
			return;
		}
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
				Restart();
				break;
			case GameState.Voting:
				RoundStateText = "Voting";
				if ( TimeSinceRoundStateChanged > RoundLength )
					DoMapVote();
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
		return TimeSinceLastForceTaunt >= LobbySettings.ForcedTauntTime;
	}
	[Broadcast]
	public void ReloadMapRPC()
	{
		Scene.GetAllComponents<MapInstance>().FirstOrDefault().UnloadMap();
	}
	public void OnRoundStarting()
	{
		Log.Info( "Round starting" );
		RoundState = GameState.Starting;
		RoundLength = LobbySettings.PreRoundTime;
		TimeSinceRoundStateChanged = 0;
		AssignEvenTeams();

		ReloadMapRPC();
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToList();
		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			var randomPoint = Game.Random.FromList( spawnPoints );
			if ( randomPoint is not null )
			{
				player.EyeAngles = randomPoint.Transform.Rotation.Angles();
				player.Transform.World = randomPoint.Transform.World;
			}
		}

		if ( Scene.GetAllComponents<Player>().All( x => x.TeamComponent.TeamName == Team.Unassigned.ToString() ) )
		{
			ForceRestart();
			return;
		}

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
			BroadcastPopup( "Hide or die", "The hunters will be unblinded in 30s", NotificationSound.ResourceName, 5f );
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
			randomList[i].Respawn( randomList[i].GameObject );
		}
	}
	public void ForceRestart()
	{
		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			player.Respawn( player.GameObject );
		}
		RoundState = GameState.WaitingForPlayers;
		TimeSinceRoundStateChanged = 0;
		RoundLength = PreRoundTime;
		ClearListBroadcast();
		Scene.GetAllComponents<MapInstance>().FirstOrDefault()?.UnloadMap();
	}

	[Broadcast]
	public void BroadcastPopup( string text, string title, string sound, float duration = 8f )
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


		RoundState = GameState.Started;
		RoundLength = LobbySettings.RoundTime;
		TimeSinceRoundStateChanged = 0;
	}

	public void OnRoundEnding()
	{
		OnGoingRound = false;
		RoundState = GameState.Ending;


		TimeSinceRoundStateChanged = 0;
		RoundLength = 15;
	}

	[Sync, Property] public int RoundNumber { get; set; } = 0;
	public int GetRandom( int min, int max )
	{
		return Random.Shared.Int( min, max );
	}
	public void OnRoundEnd()
	{
		TimeSinceRoundStateChanged = 0;

		GetPlayers( Team.Props ).ToList().Clear();
		GetPlayers( Team.Hunters ).ToList().Clear();
		// TODO: implement RTV and map votes

		if ( RoundNumber >= LobbySettings.RoundCount && LobbySettings.AllowMapVoting )
		{
			RoundLength = 25;
			RoundState = GameState.Voting;
			RoundNumber = 0;
		}
		else
		{
			RoundNumber++;
			ResetRound();
		}


		var spawns = Scene.GetAllComponents<SpawnPoint>().ToList();
		if ( spawns.Count == 0 ) return;
		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			var randomSpawn = Game.Random.FromList( spawns );
			player.Transform.Position = randomSpawn.Transform.Position;
			player.EyeAngles = randomSpawn.Transform.Rotation.Angles();
		}
	}
	public async void DoMapVote()
	{
		Log.Info( "Map vote" );

		if ( Votes is not null && Votes.Count > 0 )
		{
			var map = Votes.OrderByDescending( x => x.Value ).First().Key;
			NextMap = map;
			Log.Info( "Next map: " + map );
			Scene.GetAllComponents<MapChanger>()?.FirstOrDefault()?.LoadMap( map );
		}
		else
		{
			Log.Info( "No votes" );
		}

		ClearVotes();
		while ( !Scene.GetAllComponents<MapInstance>().FirstOrDefault().IsLoaded )
		{
			PauseRoundState = true;
			await Task.Frame();
		}
		PauseRoundState = false;
		ResetRound();
		Scene.GetAllComponents<MapInstance>().FirstOrDefault().UnloadMap();
	}
	[Broadcast]
	public void ClearVotes()
	{
		Votes.Clear();
		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			player.AbleToVote = true;
		}
	}
	public void ResetRound()
	{
		foreach ( var player in AllPlayers )
		{
			player.Respawn( player.GameObject );
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
			foreach ( var player in Scene.GetAllComponents<Player>().Where( x => x.TeamComponent.TeamName == Team.Props.ToString() ) )
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

		ForceWins();

	}

	void ForceWins()
	{

		if ( TimeSinceRoundStateChanged > LobbySettings.RoundTime || Scene.GetAllComponents<Player>().Where( x => x.TeamComponent.TeamName == Team.Hunters.ToString() ).All( x => x.Health <= 0 ) )
		{
			ForceWin( Team.Props );
			BroadcastPopup( "In the end...", $"Props win!", PropsWinSound.ResourceName );
		}
		else if ( Scene.GetAllComponents<Player>().Where( x => x.TeamComponent.TeamName == Team.Props.ToString() ).Count( x => x.Health > 0 ) <= 0 )
		{
			ForceWin( Team.Hunters );
			BroadcastPopup( "Better luck next time, props...", $"Hunters win!", HuntersWinSound.ResourceName );
		}
	}

	private Team WinningTeam { get; set; }
	[HostSync] public string WinningTeamName { get; set; }

	public void ForceWin( Team team )
	{
		WinningTeam = team;
		WinningTeamName = team.GetName();

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
