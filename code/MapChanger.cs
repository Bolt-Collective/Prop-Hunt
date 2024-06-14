using System.Formats.Tar;
using Sandbox;

public sealed class MapChanger : Component
{
	[Property] public MapInstance MapInstance { get; set; }
	protected override void OnEnabled()
	{
		MapInstance.OnMapLoaded += HandelMap;
	}

	protected override void OnDisabled()
	{
		MapInstance.OnMapLoaded -= HandelMap;
	}
	[Broadcast]
	public void LoadMap(string Indent)
	{
		MapInstance.MapName = Indent;
	}

	[Broadcast]
	public void HandelMap()
	{
		var playerList = Scene.GetAllComponents<Player>().ToList();
		var spawns = Scene.GetAllComponents<SpawnPoint>().ToList();
		foreach (var player in playerList)
		{
			var randomSpawnPoint = Game.Random.FromList(spawns);
			player.Transform.Position = randomSpawnPoint.Transform.Position;
		}
	}

	[ConCmd("change_map")]
	public static void LoadMapCmd(string Indent)
	{
		var mapChanger = Game.ActiveScene.GetAllComponents<MapChanger>().FirstOrDefault();
		if (mapChanger == null)
		{
			Log.Error("No MapChanger component found in the scene");
			return;
		}
		else
		{
			mapChanger.LoadMap(Indent);
		}
	}
}
