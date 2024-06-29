using Sandbox;
namespace PropHunt;
public sealed partial class MapChanger : Component
{
	[Property] public MapInstance MapInstance { get; set; }
	[Property] public List<CustomMapActions> customMapActions { get; set; } = new();
	protected override void OnStart()
	{
		if ( Networking.IsHost )
		{
			OnSceneStart();
			MapInstance.UnloadMap();
		}
		MapInstance.OnMapLoaded += HandleMap;
		MapInstance.OnMapUnloaded += HandleMap;
	}

	protected override void OnDisabled()
	{
		MapInstance.OnMapLoaded -= HandleMap;
		MapInstance.OnMapUnloaded -= HandleMap;
	}
	[Broadcast]
	public void LoadMap( string ident )
	{
		MapInstance.MapName = ident;
	}
	public void OnMapLoaded()
	{
		foreach ( var action in customMapActions )
		{
			action.OnMapLoaded?.Invoke( MapInstance, this, action.MapIndent );
		}
		HandleMap();
	}
	public void OnMapUnloaded()
	{
		foreach ( var action in customMapActions )
		{
			action.OnMapUnloaded?.Invoke( MapInstance, this, action.MapIndent );
		}
		HandleMap();
	}
	public void OnSceneStart()
	{
		foreach ( var action in customMapActions )
		{
			action.OnSceneStart?.Invoke( MapInstance, this, action.MapIndent );
		}
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
	[Button( "Unload Map" )]
	private void UnloadMap()
	{
		MapInstance.UnloadMap();
	}



	//Used to give map creators, who ask us to add custom actions to their maps
	public class CustomMapActions
	{
		public delegate void MapActionDel( MapInstance instance, MapChanger mapChanger, string indent );
		[Property] public MapActionDel OnMapUnloaded { get; set; }
		[Property] public MapActionDel OnMapLoaded { get; set; }
		[Property] public MapActionDel OnSceneStart { get; set; }
		[Property] public string MapIndent { get; set; }

		public CustomMapActions()
		{
			OnMapUnloaded = null;
			OnMapLoaded = null;
			OnSceneStart = null;
			MapIndent = "";
		}

		public CustomMapActions( MapActionDel onMapUnloaded, MapActionDel onMapLoaded, MapActionDel onSceneStart, string indent )
		{
			OnMapUnloaded = onMapUnloaded;
			OnMapLoaded = onMapLoaded;
			OnSceneStart = onSceneStart;
			MapIndent = indent;
		}


	}
}
