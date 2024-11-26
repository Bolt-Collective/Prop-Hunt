using Sandbox.Utility;

namespace PropHunt;

public sealed class Network : Component, Component.INetworkListener
{
    [Property] [Group( "Miscellaneous" )] private readonly List<ulong> PlayerWhitelist = new()
    {
        76561198043979097, // trende
        76561199001645276, // kicks
        76561199407136830 // Paths
    };

    [Property] public bool StartServer { get; set; } = true;
    [Property] public GameObject PlayerPrefab { get; set; }
    [Property] public bool CustomSpawnPoints { get; set; }

    [Property]
    [ShowIf( "CustomSpawnPoints", true )]
    public List<GameObject> Spawns { get; set; } = new();

    [Property] public SceneFile MenuScene { get; set; }

    [Property] [Group( "Miscellaneous" )] public bool DevMode { get; set; }


    void INetworkListener.OnActive( Connection conn )
    {
        Transform SpawnPoint;

        if ( !CustomSpawnPoints )
        {
            var spawn = Game.Random.FromList( Scene.GetAllComponents<SpawnPoint>().ToList() );
            if ( spawn is null )
            {
                SpawnPoint = new Transform( Vector3.Zero, Rotation.Identity );
            }
            else
            {
                SpawnPoint = spawn.Transform.World;
            }
        }
        else
        {
            if ( Spawns.Count == 0 )
            {
                SpawnPoint = Transform.World;
            }
            else
            {
                SpawnPoint = Game.Random.FromList( Spawns ).Transform.World;
            }
        }

        var playerClone = PlayerPrefab.Clone( SpawnPoint );
        playerClone.NetworkSpawn( conn );
        conn.CanRefreshObjects = true;
        playerClone.Name = conn.DisplayName;
    }

    protected override async Task OnLoad()
    {
        if ( StartServer && !Networking.IsActive && !Scene.IsEditor )
        {
            LoadingScreen.Title = "Creating Lobby";
            await Task.DelayRealtimeSeconds( 0.1f );
            Networking.CreateLobby();
        }

        if ( DevMode && !PlayerWhitelist.Contains( Steam.SteamId ) )
        {
            LoadingScreen.Title = "Access Denied: Developer Lobby";
            Log.Error( "You've tried to join a developer lobby on Prop Hunt, please join other lobbies" );
            Game.Disconnect();
        }
    }
}
