using System.Threading.Tasks;

public sealed class GameManager : GameObjectSystem<GameManager>, Component.INetworkListener, ISceneStartup
{
	public GameManager( Scene scene ) : base( scene )
	{
	}

	void ISceneStartup.OnHostPreInitialize( SceneFile scene )
	{
		Log.Info( $"PROPHUNT: Loading scene {scene.ResourceName}" );
	}

	void ISceneStartup.OnHostInitialize()
	{

	}

	void Component.INetworkListener.OnActive( Sandbox.Connection channel )
	{
		Log.Info( $"Player '{channel.DisplayName}' has joined the game" );

		var startLocation = FindSpawnLocation().WithScale( 1 );

		// Spawn this object and make the client the owner
		var playerGo = GameObject.Clone( "/prefabs/ph_player.prefab", new CloneConfig { Name = $"Player - {channel.DisplayName}", StartEnabled = true, Transform = startLocation } );
		var playerClient = GameObject.Clone( "/prefabs/ph_player_client.prefab", new CloneConfig { Name = $"Client - {channel.DisplayName}", StartEnabled = true, Transform = startLocation } );

		channel.CanRefreshObjects = true;
		channel.CanSpawnObjects = true;

		var player = playerGo.GetComponent<Player>();
		player.Client = playerClient.GetComponent<Client>();
		player.Respawn();
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
		return Transform.Zero;
	}
}
