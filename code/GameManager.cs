using System.Threading.Tasks;

public sealed class GameManager : Component, Component.INetworkListener
{
	public Vector3[] ScanPoints { get; set; }

	[Property] public bool StartServer { get; set; } = true;

	[Property]
	public GameObject PlayerPrefab { get; set; }

	[Property]
	public GameObject PlayerClientPrefab { get; set; }

	protected override void OnUpdate()
	{

	}
	protected override async Task OnLoad()
	{
		if ( Scene.IsEditor )
			return;

		if ( StartServer && !Networking.IsActive )
		{
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds( 0.1f );
			Networking.CreateLobby();
		}
	}

	void INetworkListener.OnActive( Sandbox.Connection channel )
	{
		Log.Info( $"Player '{channel.DisplayName}' has joined the game" );

		var startLocation = FindSpawnLocation().WithScale( 1 );

		// Spawn this object and make the client the owner
		var playerGo = PlayerPrefab.Clone( new CloneConfig { Name = $"Player - {channel.DisplayName}", StartEnabled = true, Transform = startLocation } );
		var playerClient = PlayerClientPrefab.Clone( new CloneConfig { Name = $"Client - {channel.DisplayName}", StartEnabled = true, Transform = startLocation } );

		var player = playerGo.GetComponent<Player>();
		player.Client = playerClient.GetComponent<Client>();
		playerClient.NetworkSpawn( channel );
		playerGo.NetworkSpawn( channel );
	}

	/// <summary>
	/// Find the most appropriate place to respawn
	/// </summary>
	Transform FindSpawnLocation()
	{
		//
		// If we have any SpawnPoint components in the scene, then use those
		//
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		if ( spawnPoints.Length > 0 )
		{
			return Random.Shared.FromArray( spawnPoints ).Transform.World;
		}

		//
		// Failing that, spawn where we are
		//
		return Transform.World;
	}
}
