using System.Data;
using System.Net.Http.Headers;
using Sandbox.Utility;
namespace PropHunt;

[Title( "Game Manager" )]
[Description( "The brains of Prop Hunt. Controls rounds, teams, etc." )]
public partial class PropHuntManager : Component, Component.INetworkListener
{
	[HostSync] public GameState RoundState { get; set; } = GameState.None;
	[Property] public bool AllowPeopleButUsToJoin { get; set; } = false;
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
	[Property] public SceneFile FallBackScene { get; set; }

	protected override void OnStart()
	{
		Instance = this;
	}
	void INetworkListener.OnActive( Sandbox.Connection conn )
	{
		//The last one is my dad's steam id, I don't mind him joining
		if ( conn.SteamId == 76561199001645276 || conn.SteamId == 76561198043979097 || conn.SteamId == 76561199101178253 || AllowPeopleButUsToJoin ) return;
		if ( Steam.SteamId == conn.SteamId )
		{
			Game.ActiveScene.Load( FallBackScene );
		}
	}
	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			//MaxPlayersToStart = FileSystem.Data.ReadAllText( "MinPlayers.txt" ).ToInt();
		}
		if ( !IsProxy && AllPlayers.Count() > 2 )
		{
			//MaxPlayersToStart = Connection.All.Count;
		}
		else
		{
			//MaxPlayersToStart = 2;
		}
		if ( !IsProxy )
		{
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
		}
		if ( !Networking.IsHost ) return;
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
		foreach ( var prop in Scene.GetAllComponents<PropShiftingMechanic>() )
		{
			if ( prop is null ) return;
			prop.ExitProp();
		}
		foreach ( var inv in Scene.GetAllComponents<Inventory>() )
		{
			inv.Clear();
		}
	}
	public void OnRoundStarting()
	{
		Log.Info( "Round starting" );
		RoundState = GameState.Starting;
		RoundLength = 30;
		TimeSinceRoundStateChanged = 0;

		foreach ( var player in AllPlayers )
		{
			player.TeamComponent.GetRandomTeam();
			player.AbleToMove = true;
			player.Health = 100;
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
			BroadcastPopup( "Hide or die", "The seekers will be unblinded in 30s", 30f );
		}
	}

	[Broadcast]
	public void BroadcastPopup( string text, string title, float duration = 8f )
	{
		PopupSystem.DisplayPopup( text, title, duration );
	}

	public void OnRoundStart()
	{
		foreach ( var player in Scene.GetAllComponents<Player>().Where( x => x.TeamComponent.TeamName == Team.Hunters.ToString() ) )
		{
			player.HunterUnblind();
		}
		RoundState = GameState.Started;
		RoundLength = RoundTime;
		TimeSinceRoundStateChanged = 0;
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
