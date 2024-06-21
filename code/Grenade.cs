using System.Runtime.Remoting;
using Sandbox;

public sealed class Grenade : Component
{
	[Property] public float ExplosionRadius { get; set; } = 100;
	[Property] public float TimeToExplode { get; set; } = 3;
	[Sync] public TimeSince TimeSinceThrown { get; set; }
	[Sync] public bool HasExploded { get; set; } = false;

	protected override void OnUpdate()
	{
		Explode();
	}

	[Broadcast]
	public void Explode()
	{
		if ( IsProxy || TimeSinceThrown < TimeToExplode || HasExploded ) return;
		var objs = Scene.FindInPhysics( new Sphere( Transform.Position, ExplosionRadius ) );
		foreach ( var obj in objs )
		{
			if ( obj.Root.Components.TryGet<Player>( out var player, FindMode.EverythingInSelfAndAncestors ) )
			{
				player.TakeDamage( 50 );
			}
		}
		GameObject.Destroy();
		HasExploded = true;
	}
}

public static class Explosion
{
	public static void AtPoint( Vector3 point, float radius, float damage )
	{
		var scene = Game.ActiveScene;
		if ( !scene.IsValid ) return;
		var objs = scene.FindInPhysics( new Sphere( point, radius ) );
		var trace = scene.Trace;
		foreach ( var obj in objs )
		{
			if ( obj.Root.Components.Get<Player>( FindMode.EverythingInSelfAndAncestors ) is not null )
				continue;
			var tr = trace.Ray( point, obj.Transform.Position ).Run();
			if ( tr.Hit && tr.GameObject.IsValid() )
			{
				if ( tr.GameObject.Components.Get<Player>( FindMode.EverythingInSelfAndAncestors ) is not null )
				{
					var player = tr.GameObject.Components.Get<Player>( FindMode.EverythingInSelfAndAncestors );
					player.TakeDamage( damage );
				}
			}
		}
	}
}
