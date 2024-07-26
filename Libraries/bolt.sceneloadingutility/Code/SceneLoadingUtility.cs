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
using Microsoft.CSharp.RuntimeBinder;
using Sandbox;
using Sandbox.ActionGraphs;
namespace SceneLoading
{
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
				bool gameObjectSpawned = false;

				foreach ( var componentType in clone.ComponentTypes )
				{

					if ( clone.Flags == LoadingFlags.CheckForComponents && Game.ActiveScene.Components.GetAll( componentType, FindMode.EverythingInSelfAndDescendants ).Count() > 0 )
					{
						Log.Info( "Component found, skipping" );
						continue;
					}
					else if ( clone.Flags == LoadingFlags.DestroyFirst && Game.ActiveScene.Components.GetAll( componentType, FindMode.EverythingInSelfAndDescendants ).Count() > 0 )
					{
						Log.Info( "Component found, replacing" );
						Game.ActiveScene.Components.GetAll( componentType, FindMode.EverythingInSelfAndDescendants ).FirstOrDefault()?.GameObject.Destroy();
						if ( gameObjectSpawned ) return;
						var gb = clone.Prefab.Clone();
						gb.BreakFromPrefab();
						if ( clone.NetworkSpawn ) gb.NetworkSpawn( null );
						gameObjectSpawned = true;
						continue;
					}
					else if ( clone.Flags == LoadingFlags.DestroyAll && Game.ActiveScene.Components.GetAll( componentType, FindMode.EverythingInSelfAndDescendants ).Count() > 0 )
					{
						Log.Info( "Component found, replacing all" );
						foreach ( var component in Game.ActiveScene.Components.GetAll( componentType, FindMode.EverythingInSelfAndDescendants ) )
						{
							component.GameObject.Destroy();
						}
						if ( gameObjectSpawned ) return;
						var gb = clone.Prefab.Clone();
						gb.BreakFromPrefab();
						if ( clone.NetworkSpawn ) gb.NetworkSpawn( null );
						gameObjectSpawned = true;
						continue;
					}

					if ( !gameObjectSpawned )
					{
						var obj = clone.Prefab.Clone();
						obj.BreakFromPrefab();
						if ( clone.NetworkSpawn ) obj.NetworkSpawn( null );
						gameObjectSpawned = true;
					}
				}
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
		public List<Type> ComponentTypes { get; set; }
		public bool NetworkSpawn { get; set; } = false;


		public SceneLoadingClass()
		{
			Prefab = null;
			Flags = LoadingFlags.None;
			ComponentTypes = null;
		}

		public SceneLoadingClass( GameObject prefab, LoadingFlags flags, List<Type> componentType )
		{
			Prefab = prefab;
			Flags = flags;
			ComponentTypes = componentType;
		}
	}

	public class CustomScene
	{
		[Description( "The scenefile you are manipulating, override to change it" )] public virtual SceneFile sceneFileDupe { get; set; }
		public string RawScene { get; private set; }
		public Scene newScene { get; private set; } = new();
		[Description( "Called when a scene object is created, return the object you want to spawn, or null to use the default object" )]
		public Action<JsonObject> OnSceneObjectCreated { get; set; }
		public Action<JsonObject[]> BeforeSceneLoaded { get; set; }
		public CustomScene( SceneFile sceneFile )
		{
			sceneFileDupe = new SceneFile();
			sceneFileDupe.GameObjects = sceneFile.GameObjects
				.Select( obj => JsonSerializer.Deserialize<JsonObject>( JsonSerializer.Serialize( obj ) ) )
				.ToArray();
		}

		internal void LoadSceneInternal()
		{
			var finalScene = new SceneFile();
			var finalList = new List<JsonObject>();
			foreach ( var obj in sceneFileDupe.GameObjects )
			{
				OnSceneObjectCreated?.Invoke( obj );
				finalList.Add( obj );
			}

			finalScene.GameObjects = finalList.ToArray();
			BeforeSceneLoaded?.Invoke( finalScene.GameObjects );
			Game.ActiveScene.Load( finalScene );
		}
		[Description( "Load the custom scene" )]
		public void LoadScene()
		{
			LoadSceneInternal();
		}

		[Description( "Create a GameObject within the custom scene" )]
		public void CreateObject( GameObject gameObject )
		{
			var clone = gameObject.Clone();
			var objects = sceneFileDupe.GameObjects.ToList();
			objects.Add( clone.Serialize() );
			Log.Info( gameObject.Serialize().ToString() );
			sceneFileDupe.GameObjects = objects.ToArray();
		}

		[Description( "Remove a GameObject from the custom scene" )]
		public void RemoveObject( JsonNode obj )
		{
			List<JsonObject> gameObjects = sceneFileDupe.GameObjects.ToList();
			var selectedObject = gameObjects.Find( x => x == obj );
			gameObjects.Remove( selectedObject );
			sceneFileDupe.GameObjects = gameObjects.ToArray();

		}



		public void RemoveComponentByType( JsonObject obj, Type type )
		{
			var preObj = JsonSerializer.Deserialize<JsonObject>( JsonSerializer.Serialize( obj ) );
			if ( obj.TryGetPropertyValue( "Components", out var jsonnode ) && jsonnode is not null )
			{
				var jsonString = jsonnode.ToString();
				if ( !string.IsNullOrWhiteSpace( jsonString ) )
				{
					var components = JsonSerializer.Deserialize<List<JsonNode>>( jsonString );
					var component = components.Find( x => x["__type"]?.ToString() == type.ToString() );
					if ( component is not null )
					{
						components.Remove( component );
						obj["Components"] = JsonSerializer.Deserialize<JsonNode>( JsonSerializer.Serialize( components ) );
					}
				}

				var gbList = sceneFileDupe.GameObjects.ToList();
				if ( gbList.Find( x => x == obj ) is null )
				{
					var parentNode = FindParent( preObj );
					var parent = gbList.Find( x => x == parentNode );
					if ( parent != null && parent.TryGetPropertyValue( "Children", out var childrenJsonNode ) && childrenJsonNode != null )
					{
						if ( childrenJsonNode is JsonArray childrenArray )
						{
							UpdateChildComponentsRecursively( childrenArray, obj );
						}
						else
						{
							Log.Error( "The 'Children' node is not of type 'JsonArray'." );
						}
					}
				}
				else
				{
					gbList[gbList.FindIndex( x => x == obj )] = obj;
				}
				sceneFileDupe.GameObjects = gbList.ToArray();
			}
		}

		private void UpdateChildComponentsRecursively( JsonArray childrenArray, JsonObject obj )
		{
			foreach ( var child in childrenArray )
			{
				if ( child is JsonObject childObject )
				{
					if ( childObject["__guid"].ToString() == obj["__guid"].ToString() )
					{
						childObject["Components"] = JsonSerializer.Deserialize<JsonNode>( JsonSerializer.Serialize( obj["Components"] ) );
					}
					else if ( childObject.TryGetPropertyValue( "Children", out var nestedChildrenJsonNode ) && nestedChildrenJsonNode is JsonArray nestedChildrenArray )
					{
						UpdateChildComponentsRecursively( nestedChildrenArray, obj );
					}
				}
			}
		}

		public void AddComponent( JsonObject obj, JsonObject newComponent )
		{
			var preObj = JsonSerializer.Deserialize<JsonObject>( JsonSerializer.Serialize( obj ) );
			if ( obj.TryGetPropertyValue( "Components", out var jsonnode ) && jsonnode is not null )
			{
				var jsonString = jsonnode.ToString();
				if ( !string.IsNullOrWhiteSpace( jsonString ) )
				{
					var components = JsonSerializer.Deserialize<List<JsonNode>>( jsonString );
					components.Add( newComponent );
					obj["Components"] = JsonSerializer.Deserialize<JsonNode>( JsonSerializer.Serialize( components ) );
				}
				else
				{
					var components = new List<JsonNode> { newComponent };
					obj["Components"] = JsonSerializer.Deserialize<JsonNode>( JsonSerializer.Serialize( components ) );
				}

				var gbList = sceneFileDupe.GameObjects.ToList();
				if ( gbList.Find( x => x == obj ) is null )
				{
					var parentNode = FindParent( preObj );
					var parent = gbList.Find( x => x == parentNode );
					if ( parent != null && parent.TryGetPropertyValue( "Children", out var childrenJsonNode ) && childrenJsonNode != null )
					{
						if ( childrenJsonNode is JsonArray childrenArray )
						{
							UpdateChildComponentsRecursively( childrenArray, obj );
						}
						else
						{
							Log.Error( "The 'Children' node is not of type 'JsonArray'." );
						}
					}
				}
				else
				{
					gbList[gbList.FindIndex( x => x == obj )] = obj;
				}
				sceneFileDupe.GameObjects = gbList.ToArray();
			}
		}

		public void AddComponentByType( JsonObject obj, Type componentType )
		{
			var preObj = JsonSerializer.Deserialize<JsonObject>( JsonSerializer.Serialize( obj ) );
			if ( obj.TryGetPropertyValue( "Components", out var jsonnode ) && jsonnode is not null )
			{
				var jsonString = jsonnode.ToString();
				var components = !string.IsNullOrWhiteSpace( jsonString )
					? JsonSerializer.Deserialize<List<JsonNode>>( jsonString )
					: new List<JsonNode>();

				var newComponent = new JsonObject
				{
					["__type"] = componentType.ToString()
				};

				components.Add( newComponent );
				obj["Components"] = JsonSerializer.Deserialize<JsonNode>( JsonSerializer.Serialize( components ) );

				var gbList = sceneFileDupe.GameObjects.ToList();
				if ( gbList.Find( x => x == obj ) is null )
				{
					var parentNode = FindParent( preObj );
					var parent = gbList.Find( x => x == parentNode );
					if ( parent != null && parent.TryGetPropertyValue( "Children", out var childrenJsonNode ) && childrenJsonNode != null )
					{
						if ( childrenJsonNode is JsonArray childrenArray )
						{
							UpdateChildComponentsRecursively( childrenArray, obj );
						}
						else
						{
							Log.Error( "The 'Children' node is not of type 'JsonArray'." );
						}
					}
				}
				else
				{
					gbList[gbList.FindIndex( x => x == obj )] = obj;
				}
				sceneFileDupe.GameObjects = gbList.ToArray();
			}
		}

		public JsonObject FindParent( JsonNode children )
		{
			foreach ( var obj in sceneFileDupe.GameObjects )
			{
				var parent = FindParentRecursive( obj, children );
				if ( parent != null )
				{
					return parent;
				}
			}
			return null;
		}

		private JsonObject FindParentRecursive( JsonObject parent, JsonNode children )
		{
			if ( parent.TryGetPropertyValue( "Children", out var childrenJsonNode ) )
			{
				var childrenList = JsonSerializer.Deserialize<List<JsonObject>>( childrenJsonNode.ToString() );
				if ( childrenList != null )
				{
					foreach ( var child in childrenList )
					{
						if ( child != null && child["__guid"].ToString() == children["__guid"].ToString() )
						{
							return parent;
						}

						var foundParent = FindParentRecursive( child, children );
						if ( foundParent != null )
						{
							return parent;
						}
					}
				}
			}
			return null;
		}

		public IEnumerable<JsonObject> GetAllObjectsByType( Type type )
		{
			return GetAllObjectsByTypeRecursive( sceneFileDupe.GameObjects, type );
		}

		public IEnumerable<JsonObject> GetAllObjectsByGuid( string guid )
		{
			return GetAllObjectsByGuidRecursive( sceneFileDupe.GameObjects, guid );
		}

		private IEnumerable<JsonObject> GetAllObjectsByTypeRecursive( IEnumerable<JsonObject> gameObjects, Type type )
		{
			foreach ( var obj in gameObjects )
			{
				if ( obj.TryGetPropertyValue( "Components", out var jsonnode ) && jsonnode is not null )
				{
					var jsonString = jsonnode.ToString();
					if ( !string.IsNullOrWhiteSpace( jsonString ) )
					{
						var components = JsonSerializer.Deserialize<List<JsonNode>>( jsonString );
						if ( components.Any( component => component["__type"]?.ToString() == type.ToString() ) )
						{
							yield return obj;
						}
					}
				}

				if ( obj.TryGetPropertyValue( "Children", out var childrenJsonNode ) && childrenJsonNode is not null )
				{
					var childrenString = childrenJsonNode.ToString();
					if ( !string.IsNullOrWhiteSpace( childrenString ) )
					{
						var children = JsonSerializer.Deserialize<List<JsonObject>>( childrenString );
						foreach ( var child in GetAllObjectsByTypeRecursive( children, type ) )
						{
							yield return child;
						}
					}
				}
			}
		}

		private IEnumerable<JsonObject> GetAllObjectsByGuidRecursive( IEnumerable<JsonObject> gameObjects, string guid )
		{
			foreach ( var obj in gameObjects )
			{
				if ( obj.TryGetPropertyValue( "__guid", out var jsonNode ) && jsonNode is not null )
				{
					var objectGuid = jsonNode.ToString();
					if ( !string.IsNullOrWhiteSpace( objectGuid ) && objectGuid == guid )
					{
						yield return obj;
					}
				}

				if ( obj.TryGetPropertyValue( "Children", out var childrenJsonNode ) && childrenJsonNode is not null )
				{
					var childrenString = childrenJsonNode.ToString();
					if ( !string.IsNullOrWhiteSpace( childrenString ) )
					{
						var children = JsonSerializer.Deserialize<List<JsonObject>>( childrenString );
						foreach ( var child in GetAllObjectsByGuidRecursive( children, guid ) )
						{
							yield return child;
						}
					}
				}
			}
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

}
