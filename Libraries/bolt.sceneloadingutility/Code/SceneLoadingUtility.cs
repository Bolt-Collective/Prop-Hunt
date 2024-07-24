using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Formats.Tar;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Sandbox;
using Sandbox.ActionGraphs;

public class SceneLoadingUtility
{
	public static void LoadScene( SceneFile sceneFile, SceneLoadingResource sceneLoadingResource )
	{
		var objects = sceneFile.GameObjects;
		Game.ActiveScene.Load( new SceneFile() );

		foreach ( var obj in objects )
		{
			var gameObject = Game.ActiveScene.CreateObject();
			gameObject.Deserialize( obj );
		}

		foreach ( var clone in sceneLoadingResource.SceneLoadingClasses )
		{
			if ( clone.Flags == LoadingFlags.CheckForComponents && Game.ActiveScene.Components.GetAll( clone.ComponentType, FindMode.EverythingInSelfAndDescendants ).Count() > 0 )
			{
				Log.Info( "Component found, skipping" );
				continue;
			}
			else if ( clone.Flags == LoadingFlags.DestroyFirst && Game.ActiveScene.Components.GetAll( clone.ComponentType, FindMode.EverythingInSelfAndDescendants ).Count() > 0 )
			{
				Log.Info( "Component found, replacing" );
				Game.ActiveScene.Components.GetAll( clone.ComponentType, FindMode.EverythingInSelfAndDescendants ).FirstOrDefault().GameObject.Destroy();
				var gb = clone.Prefab.Clone();
				gb.BreakFromPrefab();
				continue;
			}
			else if ( clone.Flags == LoadingFlags.DestroyAll && Game.ActiveScene.Components.GetAll( clone.ComponentType, FindMode.EverythingInSelfAndDescendants ).Count() > 0 )
			{
				Log.Info( "Component found, replacing all" );
				foreach ( var component in Game.ActiveScene.Components.GetAll( clone.ComponentType, FindMode.EverythingInSelfAndDescendants ) )
				{
					component.GameObject.Destroy();
				}
				var gb = clone.Prefab.Clone();
				gb.BreakFromPrefab();
				continue;
			}

			var obj = clone.Prefab.Clone();
			obj.BreakFromPrefab();
		}
	}
}
public enum LoadingFlags
{
	None,
	[Description( "Checks if a component is in the scene, if it is, the prefab will not be spawned" )]
	CheckForComponents,
	[Description( "Checks if a component is in the scene, if it is, the first one will be destroyed, and the prefab will be spawned" )]
	DestroyFirst,
	[Description( "Checks if a component is in the scene, if it is, all of them will be destroyed and the prefab will be spawned" )]
	DestroyAll,

}


[GameResource( "SceneLoadingResource", "loading", "A resource that allows spawning of prefabs on scene start", Icon = "public" )]
public class SceneLoadingResource : GameResource
{
	public List<SceneLoadingClass> SceneLoadingClasses { get; set; } = new();

}
public class SceneLoadingClass
{
	public GameObject Prefab { get; set; }
	public LoadingFlags Flags { get; set; } = LoadingFlags.None;
	public Type ComponentType { get; set; }


	public SceneLoadingClass()
	{
		Prefab = null;
		Flags = LoadingFlags.None;
		ComponentType = null;
	}

	public SceneLoadingClass( GameObject prefab, LoadingFlags flags, Type componentType )
	{
		Prefab = prefab;
		Flags = flags;
		ComponentType = componentType;
	}
}

public class CustomScene
{
	public SceneFile sceneFileDupe { get; set; }
	public string RawScene { get; private set; }
	public Scene newScene { get; private set; } = new();
	public CustomScene( SceneFile sceneFile )
	{
		sceneFileDupe = new SceneFile();
		sceneFileDupe.GameObjects = sceneFile.GameObjects
			.Select( obj => JsonSerializer.Deserialize<JsonObject>( JsonSerializer.Serialize( obj ) ) )
			.ToArray();
	}

	public void CreateObject( GameObject gameObject )
	{
		var objects = sceneFileDupe.GameObjects.ToList();
		objects.Add( gameObject.Serialize() );
		sceneFileDupe.GameObjects = objects.ToArray();
	}
	public void RemoveObject( JsonNode obj )
	{
		List<JsonObject> gameObjects = sceneFileDupe.GameObjects.ToList();
		Log.Info( obj );
		var selectedObject = gameObjects.Find( x => x == obj );
		Log.Info( sceneFileDupe.GameObjects.Length );
		gameObjects.Remove( selectedObject );

		sceneFileDupe.GameObjects = gameObjects.ToArray();
		Log.Info( sceneFileDupe.GameObjects.Length );

	}

	public void LoadScene()
	{
		Game.ActiveScene.Load( sceneFileDupe );
	}

	public void RemoveComponent( JsonObject jsonObject, Type componentType )
	{
		List<JsonObject> gameObjects = sceneFileDupe.GameObjects.ToList();
		jsonObject.TryGetPropertyValue( "Components", out var jsonnode );
		if ( jsonnode is not null )
		{
			var jsonString = jsonnode.ToString();
			if ( !string.IsNullOrWhiteSpace( jsonString ) )
			{
				Log.Info( jsonString );
				var components = JsonSerializer.Deserialize<List<JsonNode>>( jsonString );
				components.RemoveAll( x => x["__type"]?.ToString() == componentType.ToString() );
				jsonString = JsonSerializer.Serialize( components );
				jsonObject["Components"] = jsonString;
			}

		}
		sceneFileDupe.GameObjects = gameObjects.ToArray();
	}
	public IEnumerable<JsonObject> GetAllObjectsByType( Type type )
	{
		return sceneFileDupe.GameObjects
	  .Where( obj =>
	  {
		  if ( obj.TryGetPropertyValue( "Components", out var jsonnode ) && jsonnode != null )
		  {
			  var jsonString = jsonnode.ToString();
			  if ( !string.IsNullOrWhiteSpace( jsonString ) )
			  {
				  var components = JsonSerializer.Deserialize<List<JsonNode>>( jsonString );
				  return components.Any( component => component["__type"]?.ToString() == type.ToString() );
			  }
		  }
		  return false;
	  } );
	}
	public IEnumerable<JsonNode> GetAllObjectsByName( string name )
	{
		var objects = sceneFileDupe.GameObjects;
		foreach ( var obj in objects )
		{
			obj.TryGetPropertyValue( "Name", out var objName );
			if ( objName is not null && objName.ToString() == name )
			{
				yield return obj;
			}
		}
	}
}
