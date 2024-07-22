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
	[Property, KeyProperty, Category( "Item Actions" )] public Player.PlayerDelegate OnPlayerJump { get; set; }
	[Property, KeyProperty, Category( "Item Actions" )] public PickupDelegate OnPickup { get; set; }
	[Property, KeyProperty, Category( "Item Actions" )] public UseDelegate OnUse { get; set; }
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
	public void Trace( float TraceDistance, int damage, out Vector3 startPos, out Vector3 hitPos, out Vector3 traceNormal, out bool hit, out GameObject TraceObject, float Delay = 0.0f, float Spread = 0.0f )
	{
		if ( Scene.GetAllComponents<BlindPostprocess>().FirstOrDefault().UseBlind )
		{
			hitPos = default;
			startPos = default;
			traceNormal = default;
			hit = false;
			TraceObject = null;
			return;
		}

		var ray = new Ray( Transform.Position, Player.FreeLooking ? Player.Local.oldEyeAngles.Forward : Player.Local.EyeAngles.Forward );
		ray.Forward += ray.Forward * Vector3.Random * Spread;

		var tr = Scene.Trace.Ray( ray, TraceDistance )
			.IgnoreGameObject( Player.PropShiftingMechanic.PropsCollider.GameObject )
			.WithoutTags( "mapcollider" )
			.Run();
		if ( Player is null || !Player.AbleToMove )
		{
			hitPos = tr.EndPosition;
			traceNormal = tr.Normal;
			hit = tr.Hit;
			TraceObject = tr.GameObject;
			startPos = tr.StartPosition;
			return;
		}
		Ammo--;
		ShotsFired++;
		hitPos = tr.EndPosition;
		startPos = tr.StartPosition;
		traceNormal = tr.Normal;
		hit = tr.Hit;
		if ( tr.Hit )
		{
			TraceObject = tr.GameObject;
			if ( tr.GameObject.Root.Components.TryGet<Player>( out var enemy, FindMode.EverythingInSelfAndParent ) )
			{
				if ( Player.TeamComponent.TeamName == enemy.TeamComponent.TeamName ) return;
				enemy.TakeDamage( damage );
				Particles.Create( "particles/blood_particles/impact.flesh.vpcf", tr.HitPosition, Rotation.Random );
			}
			else if ( tr.Body is not null && tr.GameObject.Tags.Has( "prop" ) )
			{
				tr.Body.ApplyImpulseAt( tr.HitPosition, tr.Direction * 200.0f * tr.Body.Mass.Clamp( 0, 200 ) );
				Player.Local.TakeDamage( 5 );
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
