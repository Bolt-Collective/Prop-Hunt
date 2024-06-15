using System.Runtime.CompilerServices;
using Sandbox;
using Sandbox.Utility;

public sealed class Weapon : Component
{
	public Player Player { get; set; }
	public AmmoContainer AmmoContainer { get; set; }
	[Property, Category("Weapon Properties")] public float FireLength { get; set; } = 1000;
	[Property, Category("Weapon Properties")] public float Damage { get; set; } = 25;
	[Property, Category("Weapon Properties")] public float ReloadTime { get; set; } = 1;
	[Property, Category("Weapon Properties")] public int Ammo { get; set; } = 10;
	[Property, Category("Weapon Properties")] public int MaxAmmo { get; set; } = 10;
	[Property, Category("Weapon Properties")] public float FireRate { get; set; } = 1;
	[Property, Category("Weapon Properties")] public float Spread { get; set; } = 0.1f;
	[Property, Category("Weapon Properties")] public float Recoil { get; set; } = 0.1f;
	[Property, Category("Weapon Properties")] public SoundEvent FireSound { get; set; }
	public delegate void HitDelegate(Player Self, Player Enemy, Vector3 hitPos, Vector3 traceNormal);
	public delegate void FireDelegate(Player Self, Vector3 endPos, Vector3 traceNormal, bool hit);
	public delegate void ReloadDelegate(Player Self);
	public delegate void PickupDelegate(Player Self, Weapon weapon, Inventory inventory);
	[Property, Category("Weapon Actions")] public HitDelegate OnHit { get; set; }
	[Property, Category("Weapon Actions")] public FireDelegate OnFire { get; set; }
	[Property, Category("Weapon Actions")] public ReloadDelegate OnReload { get; set; }
	[Property, Category("Weapon Actions")] public PickupDelegate OnPickup { get; set; }
	[Property, Category("Weapon Actions")] public Player.PlayerDelegate OnPlayerJump { get; set; }
	TimeSince TimeSinceFire;
	TimeSince TimeSinceReload;
	[Sync] public int ShotsFired { get; set; } = 0;
	protected override void OnStart()
	{
		Player = Scene.GetAllComponents<Player>().FirstOrDefault(x => !x.IsProxy);
		AmmoContainer = Scene.GetAllComponents<AmmoContainer>().FirstOrDefault(x => !x.IsProxy);
		if (IsProxy) return;
		TimeSinceReload = ReloadTime;
		TimeSinceFire = FireRate;
		OnPickup?.Invoke(Player, this, Player.Inventory);
		Player.OnJumpEvent += OnJump;
	}
	public bool GetIsProxy()
	{
		return IsProxy;
	}
	protected override void OnEnabled()
	{
		if (Player is null) return;
		Player.OnJumpEvent += OnJump;
	}
	protected override void OnDisabled()
	{
		if (Player is null) return;
		Player.OnJumpEvent -= OnJump;
	}
	public void OnJump(Player Player, Inventory Inventory)
	{
		OnPlayerJump?.Invoke(Player, Inventory);
	}
	protected override void OnFixedUpdate()
	{
		if (IsProxy) return;
		if (Input.Down("attack1") && !IsProxy && Ammo > 0 && TimeSinceReload > ReloadTime && TimeSinceFire > FireRate)
		{
			Fire();
			TimeSinceFire = 0;
		}
		if (Input.Pressed("reload") && !IsProxy)
		{
			Reload();
		}
		
	}
	[Broadcast]
	public void BroadcastShootSound(Vector3 HitPos)
	{
		Sound.Play(FireSound);
	}
	public float GetRandomFloat()
	{
		return Random.Shared.Float(-1, 1);
	}
	public void Fire()
	{
		var ray = new Ray(Player.Transform.Position + Vector3.Up * 64, Player.Transform.Position + Player.EyeAngles.Forward * FireLength);
		ray.Forward += Vector3.Random * Spread;
		Player.EyeAngles += new Angles(-Recoil, GetRandomFloat(), 0);
		var tr = Scene.Trace.Ray(ray, FireLength)
		.WithoutTags("player")
		.Run();
		ShotsFired++;
		Ammo--;
		BroadcastShootSound(tr.EndPosition);
		OnFire?.Invoke(Player, tr.EndPosition, tr.Normal, tr.Hit);
		if (tr.Hit && tr.GameObject.Components.TryGet<Player>(out var enemy, FindMode.EnabledInSelfAndChildren))
		{
			if (Player.IsFriendly(enemy)) return;
			enemy.TakeDamage(25);
			OnHit?.Invoke(Player, enemy, tr.EndPosition, tr.Normal);
		}
		if (tr.Hit)
		{
			
			if ( tr.Body is not null )
		{
			tr.Body.ApplyImpulseAt( tr.HitPosition, tr.Direction * 200.0f * tr.Body.Mass.Clamp( 0, 200 ) );
		}
		var damage = new DamageInfo(Damage, GameObject, GameObject, tr.Hitbox);
		damage.Position = tr.HitPosition;
		damage.Shape = tr.Shape;
		foreach (var damageAble in tr.GameObject.Components.GetAll<IDamageable>())
		{
			damageAble.OnDamage(damage);
		}
		}
	}
	
	public async void Reload()
	{
		if (MaxAmmo <= AmmoContainer.Ammo && AmmoContainer.Ammo != 0)
		{
			OnReload?.Invoke(Player);
			TimeSinceReload = 0;
			await Task.DelaySeconds(ReloadTime);
			Ammo = MaxAmmo;
			AmmoContainer.SubtractAmmo(ShotsFired);
			TimeSinceReload = 0;
		}
		else if (MaxAmmo >= AmmoContainer.Ammo && AmmoContainer.Ammo != 0)
		{
			OnReload?.Invoke(Player);
			await Task.DelaySeconds(ReloadTime);
			TimeSinceReload = 0;
			Ammo = AmmoContainer.Ammo;
			AmmoContainer.SubtractAmmo(AmmoContainer.Ammo);
			
		}
		ShotsFired = 0;
	}
}
