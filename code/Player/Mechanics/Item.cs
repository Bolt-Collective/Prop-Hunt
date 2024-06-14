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
	public delegate void PickupDelegate(Player Self, Item item, Inventory inventory);
	public delegate void UseDelegate(Player Self, Item item, Inventory inventory, bool AbleToUse);
	[Property, Category("Item Actions")] public PickupDelegate OnPickup { get; set; }
	[Property, Category("Item Actions")] public UseDelegate OnUse { get; set; }
	[Property, Category("Item Properties"), Sync] public bool UsesAmmo { get; set; }
	[Property, Category("Item Properties"), ShowIf("UsesAmmo", true), Sync] public int Ammo { get; set; }
	[Sync] public bool AbleToUse { get; set; } = true;
	public int ShotsFired { get; set; } = 0;
	protected override void OnStart()
	{
		Player = Scene.GetAllComponents<Player>().FirstOrDefault(x => !x.IsProxy);
		AmmoContainer = Scene.GetAllComponents<AmmoContainer>().FirstOrDefault(x => !x.IsProxy);
	}
	protected override void OnUpdate()
	{
		if (!IsProxy)
		{
			OnUse?.Invoke(Player, this, Inventory, AbleToUse);
			if (UsesAmmo)
			{
			if (Ammo < 0)
			{
				AbleToUse = true;
			}
			else
			{
				AbleToUse = false;
			}
			}

		}
	}
	[Impure]
	public void Trace(float TraceDistance, int damage, out Vector3 hitPos, out Vector3 traceNormal, out bool hit, out GameObject TraceObject)
	{
		var tr = Scene.Trace.Ray(Player.Transform.Position + Vector3.Up * 64, Player.Transform.Position + Player.EyeAngles.Forward * TraceDistance)
		.WithoutTags(Steam.SteamId.ToString())
		.Run();
		Ammo--;
		ShotsFired++;
		hitPos = tr.EndPosition;
		traceNormal = tr.Normal;
		hit = tr.Hit;
		if (tr.Hit)
		{
			TraceObject = tr.GameObject;
			if (tr.GameObject.Parent.Components.TryGet<Player>(out var enemy, FindMode.EnabledInSelfAndChildren))
			{
				enemy.TakeDamage(damage);
			}
		}
		else
		{
			TraceObject = null;
		}
	}

	[ActionGraphNode("Broadcast Anim"), Broadcast]
	public static void BroadcastAnim(Guid RendererId, string AnimName, bool True)
	{
		if (Game.ActiveScene.IsProxy) return;
		var renderer = Game.ActiveScene.Directory.FindByGuid(RendererId);
		if (renderer is not null && renderer.Components.TryGet<SkinnedModelRenderer>(out var rendererComponent))
		{
			rendererComponent.Set(AnimName, True);
		}
	}

	[ActionGraphNode("Broadcast Holdtype"), Broadcast]
	public static void BroadcastHoldtype(Guid RendererId, CitizenAnimationHelper.HoldTypes holdType)
	{
		if (Game.ActiveScene.IsProxy) return;
		var renderer = Game.ActiveScene.Directory.FindByGuid(RendererId);
		if (renderer is not null && renderer.Components.TryGet<CitizenAnimationHelper>(out var rendererComponent))
		{
			rendererComponent.HoldType = holdType;
		}
	}

	[ActionGraphNode("Get GameObject Id"), Pure]
	public static Guid GetGameObjectId(GameObject gameObject)
	{
		return gameObject.Id;
	}


	public async Task Reload(float reloadTime, int MaxAmmo)
	{
		await Task.DelaySeconds(reloadTime);
		if (MaxAmmo <= AmmoContainer.Ammo && AmmoContainer.Ammo > 0)
		{
			Ammo = MaxAmmo;
			AmmoContainer.SubtractAmmo(ShotsFired);
		}
		else if (MaxAmmo >= AmmoContainer.Ammo && AmmoContainer.Ammo > 0)
		{
			Ammo = AmmoContainer.Ammo;
		}
		ShotsFired = 0;
	}

	public async Task FireDelay(float delay)
	{
		AbleToUse = false;
		await Task.DelaySeconds(delay);
		AbleToUse = true;
	}
}
