using Sandbox;
namespace PropHunt;
public sealed class MapChanger : Component
{
	[Property] public MapInstance MapInstance { get; set; }
	[Property] public List<CustomMapActions> customMapActions { get; set; } = new();
	protected override void OnStart()
	{
		//If we are the host, we want to unload the map so the actions get triggerd
		MapInstance.UnloadMap();
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
	public void HandleMap()
	{
		foreach ( var action in customMapActions )
		{
			if ( action.MapIndent == MapInstance.MapName )
			{
				action.MapAction?.Invoke( MapInstance, MapInstance.MapName );
			}
		}

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


	//Custom mapper methods

	//Adds vertex snapping to all opium assets
	public static void GnomeFigTagsAdd()
	{
		foreach ( var modelRenderer in Game.ActiveScene.GetAllComponents<ModelRenderer>() )
		{
			modelRenderer.SceneObject.Attributes.SetCombo( "D_VERTEX_SNAP", true );
		}
	}

	//Used to give map creators, who ask us to add custom actions to their maps
	public class CustomMapActions
	{
		public delegate void MapActionDel( MapInstance instance, string indent );
		[Property] public MapActionDel MapAction { get; set; }
		[Property] public string MapIndent { get; set; }

		public CustomMapActions()
		{
			MapAction = null;
			MapIndent = "";
		}

		public CustomMapActions( MapActionDel action, string indent )
		{
			MapAction = action;
			MapIndent = indent;
		}


	}
}
