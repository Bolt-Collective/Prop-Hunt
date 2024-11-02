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
		//
		// TODO: We don't have a menu, but if we did we could put a special component in the menu
		// scene that we'd now be able to detect, and skip doing the stuff below.
		//

		//
		// Spawn the engine scene.
		// This scene is sent to clients when they join.
		//
		var slo = new SceneLoadOptions();
		slo.IsAdditive = true;
		slo.SetScene( "scenes/engine.scene" );
		Scene.Load( slo );

		// If we're not hosting a lobby, start hosting one
		// so that people can join this game.
		Networking.CreateLobby();
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
