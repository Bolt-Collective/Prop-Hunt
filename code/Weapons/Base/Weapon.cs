using PropHunt;

public sealed class Weapon : Component
{
	public Player Player { get; set; }
	public AmmoContainer AmmoContainer { get; set; }
	[Property, Category( "Weapon Properties" )] public float FireLength { get; set; } = 1000;
	[Property, Category( "Weapon Properties" )] public GameObject Decal { get; set; }
	[Property, Category( "Weapon Properties" )] public float Damage { get; set; } = 25;
	[Property, Category( "Weapon Properties" )] public float ReloadTime { get; set; } = 1;
	[Property, Category( "Weapon Properties" )] public int Ammo { get; set; } = 10;
	[Property, Category( "Weapon Properties" )] public int MaxAmmo { get; set; } = 10;
	[Property, Category( "Weapon Properties" )] public float FireRate { get; set; } = 1;
	[Property, Category( "Weapon Properties" )] public float Spread { get; set; } = 0.1f;
	[Property, Category( "Weapon Properties" )] public float Recoil { get; set; } = 0.1f;
	[Property, Category( "Weapon Properties" )] public SoundEvent FireSound { get; set; }
	public delegate void HitDelegate( Player Self, Player Enemy, Vector3 hitPos, Vector3 traceNormal );
	public delegate void FireDelegate( Player Self, Vector3 endPos, Vector3 traceNormal, bool hit );
	public delegate void ReloadDelegate( Player Self );
	public delegate void PickupDelegate( Player Self, Weapon weapon, Inventory inventory );
	[Property, KeyProperty, Category( "Weapon Actions" )] public HitDelegate OnHit { get; set; }
	[Property, KeyProperty, Category( "Weapon Actions" )] public FireDelegate OnFire { get; set; }
	[Property, KeyProperty, Category( "Weapon Actions" )] public ReloadDelegate OnReload { get; set; }
	[Property, KeyProperty, Category( "Weapon Actions" )] public PickupDelegate OnPickup { get; set; }
	[Property, KeyProperty, Category( "Weapon Actions" )] public Player.PlayerDelegate OnPlayerJump { get; set; }
	[Sync] TimeSince TimeSinceFire { get; set; }
	[Sync] TimeSince TimeSinceReload { get; set; }
	[Sync] public int ShotsFired { get; set; } = 0;
	protected override void OnStart()
	{
		Player = Scene.GetAllComponents<Player>().FirstOrDefault( x => !x.IsProxy );
		AmmoContainer = Scene.GetAllComponents<AmmoContainer>().FirstOrDefault( x => !x.IsProxy );
		if ( IsProxy ) return;
		TimeSinceReload = ReloadTime;
		TimeSinceFire = FireRate;
		OnPickup?.Invoke( Player, this, Player.Inventory );
		Player.OnJumpEvent += OnJump;
	}
	public bool GetIsProxy()
	{
		return IsProxy;
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
	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;
		if ( Input.Down( "attack1" ) && !IsProxy && Ammo > 0 && TimeSinceReload > ReloadTime && TimeSinceFire > FireRate )
		{
			Fire();
			TimeSinceFire = 0;
		}
		if ( Input.Pressed( "reload" ) && !IsProxy )
		{
			Reload();
		}

	}
	[Broadcast]
	public void BroadcastShootSound( Vector3 HitPos )
	{
		Sound.Play( FireSound, HitPos );
	}
	public float GetRandomFloat()
	{
		return Random.Shared.Float( -1, 1 );
	}
	public void Fire()
	{
		if ( Player is null || !Player.AbleToMove || Scene.GetAllComponents<BlindPostprocess>().FirstOrDefault().UseBlind ) return;
		var ray = new Ray( Transform.Position, Player.FreeLooking ? Player.Local.oldEyeAngles.Forward : Player.Local.EyeAngles.Forward );
		ray.Forward += Vector3.Random * Spread;

		Player.EyeAngles += new Angles( -Recoil, GetRandomFloat(), 0 );

		var tr = Scene.Trace.Ray( ray, FireLength )
			.IgnoreGameObject( Player.PropShiftingMechanic.PropsCollider.GameObject )
			.WithoutTags( "mapcollider" )
			.Run();
		ShotsFired++;
		Ammo--;

		BroadcastShootSound( tr.StartPosition );
		OnFire?.Invoke( Player, tr.EndPosition, tr.Normal, tr.Hit );


		if ( tr.Hit )
		{
			if ( tr.GameObject.Root.Components.TryGet<Player>( out var enemy, FindMode.EverythingInSelfAndParent ) )
			{
				if ( Player.TeamComponent.TeamName == enemy.TeamComponent.TeamName ) return;
				enemy.TakeDamage( 25 );
				OnHit?.Invoke( Player, enemy, tr.EndPosition, tr.Normal );
				var particle = Particles.Create( "particles/blood_particles/impact.flesh.vpcf", tr.HitPosition, Rotation.Random );
				particle.GameObject.NetworkSpawn();
			}
			else if ( tr.Body is not null && !tr.GameObject.Tags.Has( "player" ) && tr.GameObject.Tags.Has( "prop" ) )
			{
				tr.Body.ApplyImpulseAt( tr.HitPosition, tr.Direction * 200.0f * tr.Body.Mass.Clamp( 0, 200 ) );
				Player.Local.TakeDamage( 5 );
			}

			Sound.Play( tr.Surface.Sounds.Bullet, tr.HitPosition );
			var farticle = Particles.Create( tr.Surface.ImpactEffects.Bullet.FirstOrDefault(), tr.HitPosition, Rotation.Identity );
			farticle.GameObject.NetworkSpawn();

			var decalPath = Game.Random.FromList( tr.Surface.ImpactEffects.BulletDecal, "decals/bullethole.decal" );
			if ( ResourceLibrary.TryGet<DecalDefinition>( decalPath, out var decalResource ) )
			{
				var decal = Game.Random.FromList( decalResource.Decals );

				PropHuntManager.Instance.CreateDecal( decal.Material, tr.HitPosition, tr.Normal, decal.Rotation.GetValue(), decal.Width.GetValue() / 1.5f, decal.Depth.GetValue(), 30f );
			}
			var damage = new DamageInfo( Damage, GameObject, GameObject, tr.Hitbox );
			damage.Position = tr.HitPosition;
			damage.Shape = tr.Shape;

			foreach ( var damageAble in tr.GameObject.Components.GetAll<IDamageable>() )
			{
				damageAble.OnDamage( damage );
			}

		}
	}

	public async void Reload()
	{
		if ( Ammo == MaxAmmo || AmmoContainer.Ammo == 0 ) return;
		if ( MaxAmmo <= AmmoContainer.Ammo && AmmoContainer.Ammo != 0 )
		{
			OnReload?.Invoke( Player );
			TimeSinceReload = 0;
			await Task.DelaySeconds( ReloadTime );
			Ammo = MaxAmmo;
			AmmoContainer.SubtractAmmo( ShotsFired );
			TimeSinceReload = 0;
		}
		else if ( MaxAmmo >= AmmoContainer.Ammo && AmmoContainer.Ammo != 0 )
		{
			OnReload?.Invoke( Player );
			await Task.DelaySeconds( ReloadTime );
			TimeSinceReload = 0;
			Ammo = AmmoContainer.Ammo;
			AmmoContainer.SubtractAmmo( AmmoContainer.Ammo );

		}
		ShotsFired = 0;
	}
}
