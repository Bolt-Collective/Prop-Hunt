using System;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Citizen;
using Sandbox.Utility;

public sealed class Item : Component
{
	public Player Player { get; set; }
	public Inventory Inventory { get; set; }
	public AmmoContainer AmmoContainer { get; set; }
	public delegate void PickupDelegate( Player Self, Item item, Inventory inventory );
	public delegate void UseDelegate( Player Self, Item item, Inventory inventory, bool AbleToUse );
	[Property, Category( "Item Actions" )] public Player.PlayerDelegate OnPlayerJump { get; set; }
	[Property, Category( "Item Actions" )] public PickupDelegate OnPickup { get; set; }
	[Property, Category( "Item Actions" )] public UseDelegate OnUse { get; set; }
	[Property, Category( "Item Properties" ), Sync] public bool UsesAmmo { get; set; }
	[Property, Category( "Item Properties" ), ShowIf( "UsesAmmo", true ), Sync] public int Ammo { get; set; }
	[Property, Sync] public bool AbleToUse { get; set; } = true;
	public int ShotsFired { get; set; } = 0;
	protected override void OnStart()
	{
		Player = Scene.GetAllComponents<Player>().FirstOrDefault( x => !x.IsProxy );
		AmmoContainer = Scene.GetAllComponents<AmmoContainer>().FirstOrDefault( x => !x.IsProxy );
		if ( IsProxy ) return;
		Player.OnJumpEvent += OnJump;
	}
	protected override void OnEnabled()
	{
		if ( Player is null ) return;
		Player.OnJumpEvent += OnJump;
	}
	protected override void OnDisabled()
	{
		if ( Player is null ) return;
		Player.OnJumpEvent -= OnJump;
	}
	public void OnJump( Player Player, Inventory Inventory )
	{
		OnPlayerJump?.Invoke( Player, Inventory );
	}
	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			OnUse?.Invoke( Player, this, Inventory, AbleToUse );
			if ( UsesAmmo )
			{
				if ( Ammo <= 0 )
				{
					AbleToUse = false;
				}
				else
				{
					AbleToUse = true;
				}
			}

		}
	}
	[Impure]
	public void Trace( float TraceDistance, int damage, out Vector3 hitPos, out Vector3 traceNormal, out bool hit, out GameObject TraceObject, float Delay = 0.0f, float Spread = 0.0f )
	{
		var ray = Scene.Camera.ScreenNormalToRay( 0.5f );
		ray.Forward += ray.Forward * Vector3.Random * Spread;
		var tr = Scene.Trace.Ray( ray, TraceDistance )
			.IgnoreGameObject( Player.GameObject )
			.Run();
		if ( Player is null || !Player.AbleToMove )
		{
			hitPos = tr.EndPosition;
			traceNormal = tr.Normal;
			hit = tr.Hit;
			TraceObject = tr.GameObject;
			return;
		}
		Ammo--;
		ShotsFired++;
		hitPos = tr.EndPosition;
		traceNormal = tr.Normal;
		hit = tr.Hit;
		if ( tr.Hit )
		{
			TraceObject = tr.GameObject;
			if ( tr.GameObject.Components.TryGet<Player>( out var enemy, FindMode.EverythingInSelfAndParent ) )
			{
				if ( Player.IsFriendly( enemy ) ) return;
				enemy.TakeDamage( damage );
				Particles.Create( "particles/blood_particles/impact.flesh.vpcf", tr.HitPosition.Normal, Rotation.Random );
			}
			if ( tr.Body is not null )
			{
				tr.Body.ApplyImpulseAt( tr.HitPosition, tr.Direction * 200.0f * tr.Body.Mass.Clamp( 0, 200 ) );
			}
			var trDamage = new DamageInfo( damage, GameObject, GameObject, tr.Hitbox );
			trDamage.Position = tr.HitPosition;
			trDamage.Shape = tr.Shape;
			foreach ( var damageAble in tr.GameObject.Components.GetAll<IDamageable>() )
			{
				damageAble.OnDamage( trDamage );
			}
		}
		else
		{
			TraceObject = null;
		}
	}

	[ActionGraphNode( "Broadcast Anim" )]
	public static void BroadcastAnim( Guid RendererId, string AnimName, bool True )
	{
		if ( Game.ActiveScene.IsProxy ) return;
		var renderer = Game.ActiveScene.Directory.FindByGuid( RendererId );
		if ( renderer is not null && renderer.Components.TryGet<SkinnedModelRenderer>( out var rendererComponent ) )
		{
			rendererComponent.Set( AnimName, True );
		}
		Log.Info( "Broadcast Holdtype" );
	}

	[ActionGraphNode( "Broadcast Holdtype" ), Broadcast]
	public static void BroadcastHoldtype( Guid RendererId, CitizenAnimationHelper.HoldTypes holdType )
	{
		if ( Game.ActiveScene.IsProxy ) return;
		var renderer = Game.ActiveScene.Directory.FindByGuid( RendererId );
		if ( renderer is not null && renderer.Components.TryGet<CitizenAnimationHelper>( out var rendererComponent ) )
		{
			rendererComponent.HoldType = holdType;
		}
	}

	[ActionGraphNode( "Get GameObject Id" ), Pure]
	public static Guid GetGameObjectId( GameObject gameObject )
	{
		return gameObject.Id;
	}


	public async Task Reload( float reloadTime, int MaxAmmo )
	{
		await Task.DelaySeconds( reloadTime );
		if ( MaxAmmo <= AmmoContainer.Ammo && AmmoContainer.Ammo > 0 )
		{
			Ammo = MaxAmmo;
			AmmoContainer.SubtractAmmo( ShotsFired );
		}
		else if ( MaxAmmo >= AmmoContainer.Ammo && AmmoContainer.Ammo > 0 )
		{
			Ammo = AmmoContainer.Ammo;
		}
		ShotsFired = 0;
	}

	public async Task FireDelay( float delay )
	{
		AbleToUse = false;
		await GameTask.DelaySeconds( delay );
		AbleToUse = true;
	}

	public bool GetIsProxy()
	{
		return IsProxy;
	}
}
