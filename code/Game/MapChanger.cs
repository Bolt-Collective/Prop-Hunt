using Sandbox;

public sealed class MapChanger : Component
{
	[Property] public MapInstance MapInstance { get; set; }
	protected override void OnEnabled()
	{
		MapInstance.OnMapLoaded += HandleMap;
	}

	protected override void OnDisabled()
	{
		MapInstance.OnMapLoaded -= HandleMap;
	}
	[Broadcast]
	public void LoadMap( string ident )
	{
		MapInstance.MapName = ident;
	}

	[Broadcast]
	public void HandleMap()
	{
		var playerList = Scene.GetAllComponents<Player>().ToList();
		var spawns = Scene.GetAllComponents<SpawnPoint>().ToList();
		foreach ( var player in playerList )
		{
			var randomSpawnPoint = Game.Random.FromList( spawns );
			player.Transform.Position = randomSpawnPoint.Transform.Position;
		}
	}

	[ConCmd( "map" )]
	public static void LoadMapCmd( string ident )
	{
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
