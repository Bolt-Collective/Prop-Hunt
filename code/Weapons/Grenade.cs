using System.Runtime.Remoting;
using Sandbox;

public sealed class Grenade : Component
{
	[Property] public float ExplosionRadius { get; set; } = 100;
	[Property] public float TimeToExplode { get; set; } = 3;
	[Sync] public TimeSince TimeSinceThrown { get; set; }
	[Sync] public bool HasExploded { get; set; } = false;
	[Property] public GameObject ExplosionEffect { get; set; }

	protected override void OnStart()
	{
		TimeSinceThrown = 0;
	}
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
			if ( obj.Components.TryGet<IDamageable>( out var damageable ) )
			{
				damageable.OnDamage( new DamageInfo( 50, GameObject, GameObject ) );

			}
		}
		var explosion = ExplosionEffect.Clone( Transform.Position, Rotation.Identity );
		explosion.NetworkSpawn();
		GameObject.Destroy();
		HasExploded = true;
	}
}
