namespace PropHunt;
public sealed partial class MapChanger : Component
{
	[Property] public MapInstance MapInstance { get; set; }
	[Property] public List<CustomMapActions> customMapActions { get; set; } = new();
	[Sync] public bool IsMapLoaded { get; set; } = false;
	protected override void OnStart()
	{
		if ( !MapInstance.IsValid )
			return;

		if ( Networking.IsHost )
		{
			OnSceneStart();
			MapInstance.UseMapFromLaunch = false;

			return;
		}
		MapInstance.OnMapLoaded += OnMapLoaded;
		MapInstance.OnMapUnloaded += OnMapUnloaded;
	}

	protected override void OnDisabled()
	{
		if ( !Networking.IsHost )
			return;

		if ( !MapInstance.IsValid )
			return;

		MapInstance.OnMapLoaded -= OnMapLoaded;
		MapInstance.OnMapUnloaded -= OnMapUnloaded;
	}
	[Broadcast]
	public void LoadMap( string ident )
	{
		MapInstance.MapName = ident;
	}
	public void OnMapLoaded()
	{
		IsMapLoaded = true;

		if ( customMapActions != null )
		{
			foreach ( var action in customMapActions )
			{
				if ( action.MapIndent == MapInstance.MapName )
				{
					action.OnMapLoaded?.Invoke( MapInstance, this, action.MapIndent );
				}
			}
		}

		HandleMap();
	}
	public void OnMapUnloaded()
	{
		IsMapLoaded = false;
		foreach ( var action in customMapActions )
		{
			if ( action.MapIndent == MapInstance.MapName )
			{
				action.OnMapUnloaded?.Invoke( MapInstance, this, action.MapIndent );
			}
		}
		HandleMap();
	}
	public void OnSceneStart()
	{
		foreach ( var action in customMapActions )
		{
			if ( action.MapIndent == MapInstance.MapName )
			{
				action.OnSceneStart?.Invoke( MapInstance, this, action.MapIndent );
			}
		}
	}
	public void HandleMap()
	{
		if ( !Networking.IsHost )
			return;

		Scene.GetAllComponents<MapCollider>().FirstOrDefault().Tags.Add( "map" );
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		BroadcastPropTags();
		foreach ( var player in Scene.GetAllComponents<Player>().ToArray() )
		{
			var randomSpawnPoint = Random.Shared.FromArray( spawnPoints );
			if ( randomSpawnPoint is null ) continue;

			player.Transform.Position = randomSpawnPoint.Transform.Position;

			if ( player.Components.TryGet<Player>( out var pc ) )
			{
				pc.EyeAngles = randomSpawnPoint.Transform.Rotation.Angles();
			}

		}

	}
	[Broadcast]
	public void BroadcastPropTags()
	{
		foreach ( var prop in Scene.Directory.FindByName( "prop_physics" ) )
		{
			prop.Tags.Add( "prop" );
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
