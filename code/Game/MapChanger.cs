using Sandbox;
namespace PropHunt;
public sealed class MapChanger : Component
{
	[Property] public MapInstance MapInstance { get; set; }
	protected override void OnEnabled()
	{
		if ( !Networking.IsHost ) return;
		MapInstance.OnMapLoaded += HandleMap;
	}

	protected override void OnDisabled()
	{
		if ( !Networking.IsHost ) return;
		MapInstance.OnMapLoaded -= HandleMap;
	}
	[Broadcast]
	public void LoadMap( string ident )
	{
		MapInstance.MapName = ident;
	}
	public void HandleMap()
	{
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();

		foreach ( var player in Scene.GetAllComponents<Player>().ToArray() )
		{
			if ( player.IsProxy )
				continue;

			var randomSpawnPoint = Random.Shared.FromArray( spawnPoints );
			if ( randomSpawnPoint is null ) continue;

			player.Transform.Position = randomSpawnPoint.Transform.Position;

			if ( player.Components.TryGet<Player>( out var pc ) )
			{
				pc.EyeAngles = randomSpawnPoint.Transform.Rotation.Angles();
			}

		}

	}


	[ConCmd( "map" )]
	public static void LoadMapCmd( string ident )
	{
		if ( !Networking.IsHost ) return;
		var mapChanger = Game.ActiveScene.GetAllComponents<MapChanger>().FirstOrDefault();
		if ( mapChanger == null )
		{
			Log.Error( "No MapChanger component found in the scene" );
			return;
		}
		else
		{
			mapChanger.LoadMap( ident );
		}
	}
}
