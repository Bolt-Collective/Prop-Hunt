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
	public delegate void HitDelegate(Player Self, Player Enemy, Vector3 hitPos, Vector3 traceNormal);
	public delegate void FireDelegate(Player Self, Vector3 endPos, Vector3 traceNormal, bool hit);
	public delegate void ReloadDelegate(Player Self);
	public delegate void PickupDelegate(Player Self, Weapon weapon, Inventory inventory);
	[Property, Category("Weapon Actions")] public HitDelegate OnHit { get; set; }
	[Property, Category("Weapon Actions")] public FireDelegate OnFire { get; set; }
	[Property, Category("Weapon Actions")] public ReloadDelegate OnReload { get; set; }
	[Property, Category("Weapon Actions")] public PickupDelegate OnPickup { get; set; }
	TimeSince TimeSinceReload;
	[Sync] public int ShotsFired { get; set; } = 0;
	protected override void OnStart()
	{
		Player = Scene.GetAllComponents<Player>().FirstOrDefault(x => !x.IsProxy);
		AmmoContainer = Scene.GetAllComponents<AmmoContainer>().FirstOrDefault(x => !x.IsProxy);
		if (IsProxy) return;
		TimeSinceReload = ReloadTime;
	}

	protected override void OnFixedUpdate()
	{
		if (Input.Pressed("attack1") && !IsProxy && Ammo > 0 && TimeSinceReload > ReloadTime)
		{
			Fire();
		}
		if (Input.Pressed("reload") && !IsProxy)
		{
			Reload();
			OnReload?.Invoke(Player);
		}
	}

	public void Fire()
	{
		var tr = Scene.Trace.Ray(Player.Transform.Position + Vector3.Up * 64, Player.Transform.Position + Player.EyeAngles.Forward * FireLength)
		.WithoutTags(Steam.SteamId.ToString())
		.Run();
		ShotsFired++;
		Ammo--;
		OnFire?.Invoke(Player, tr.EndPosition, tr.Normal, tr.Hit);
		if (tr.Hit && tr.GameObject.Parent.Components.TryGet<Player>(out var enemy, FindMode.EnabledInSelfAndChildren))
		{
			enemy.TakeDamage(25);
			OnHit?.Invoke(Player, enemy, tr.EndPosition, tr.Normal);
		}
	}
	
	public void Reload()
	{
		if (MaxAmmo <= AmmoContainer.Ammo && AmmoContainer.Ammo > 0)
		{
			Ammo = MaxAmmo;
			AmmoContainer.SubtractAmmo(ShotsFired);
			TimeSinceReload = 0;
		}
		else if (MaxAmmo >= AmmoContainer.Ammo && AmmoContainer.Ammo > 0)
		{
			Ammo = AmmoContainer.Ammo;
			AmmoContainer.SubtractAmmo(AmmoContainer.Ammo);
			TimeSinceReload = 0;
		}
		ShotsFired = 0;
	}
}
