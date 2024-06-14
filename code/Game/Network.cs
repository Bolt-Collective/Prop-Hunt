using Sandbox;
using Sandbox.Network;

public sealed class Network : Component, Component.INetworkListener
{
	[Property] public bool StartServer { get; set; } = true;
	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public bool CustomSpawnPoints { get; set; }
	[Property, ShowIf("CustomSpawnPoints", true)] public List<GameObject> Spawns { get; set; } = new();
	[Property] public PropHuntManager PropHuntManager { get; set; }
	protected override void OnAwake()
	{
		if (!GameNetworkSystem.IsActive && StartServer)
		{
			GameNetworkSystem.CreateLobby();
		}
	}

	void INetworkListener.OnActive(Sandbox.Connection conn)
	{
		Transform SpawnPoint;
		if (!CustomSpawnPoints)
		{
		var spawn = Game.Random.FromList(Scene.GetAllComponents<SpawnPoint>().ToList());
		SpawnPoint = spawn.Transform.World;
		}
		else
		{
			if (Spawns.Count == 0)
			{
			SpawnPoint = Transform.World;
			}
			else
			{
			SpawnPoint = Game.Random.FromList(Spawns).Transform.World;
			}
		}
		var playerClone = PlayerPrefab.Clone(SpawnPoint);
		playerClone.NetworkSpawn(conn);
		playerClone.Name = conn.DisplayName;
		PropHuntManager.Players.Add(playerClone);
	}

	void INetworkListener.OnDisconnected(Sandbox.Connection conn)
	{
		var player = PropHuntManager.Players.Find(x => x.Network.OwnerConnection == conn);
		if (player != null)
		{
			PropHuntManager.Players.Remove(player);
		}
	}
}
