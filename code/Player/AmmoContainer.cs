using Sandbox;

public sealed class AmmoContainer : Component
{
	[Property, Sync] public int Ammo { get; set; } = 10;
	[Property, Sync] public int MaxAmmo { get; set; } = 10;

	private int startingAmmo;
	private int startingMaxAmmo;

	protected override void OnStart()
	{
		startingAmmo = Ammo;
		startingMaxAmmo = MaxAmmo;
	}

	public void SubtractAmmo( int amount )
	{
		Ammo -= amount;
	}
	public void AddAmmo( int amount )
	{
		Ammo += amount;
	}

	public void ResetAmmo()
	{
		Ammo = startingAmmo;
		MaxAmmo = startingMaxAmmo;
	}
}
