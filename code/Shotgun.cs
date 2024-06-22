using System.Threading;
using Sandbox;

public sealed class Shotgun : Component
{
	//Gives access to traces and other useful functions
	[Property] public Item Item { get; set; }
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	protected override void OnStart()
	{
		if ( IsProxy ) return;
		Item.UsesAmmo = true;
		Item.AbleToUse = true;
	}

	private Vector3 _hitPos;
	private Vector3 _hitNormal;
	private bool _hit;
	private GameObject _hitObject;
	protected override void OnUpdate()
	{
		Shoot();

		if ( !IsProxy && Input.Pressed( "reload" ) )
		{
			Renderer.Set( "b_reload", true );
			_ = Item.Reload( 3, 12 );
		}
	}
	public TimeSince TimeSinceLastShot { get; set; }
	private void Shoot()
	{
		if ( !IsProxy && Input.Down( "attack1" ) && TimeSinceLastShot > 0.5f && Item.Ammo > 0 )
		{
			for ( int i = 0; i < 2; i++ )
			{
				Item.Trace( 1000, 15, out _hitPos, out _hitNormal, out _hit, out _hitObject, 5, 0.2f );
				var clone = new GameObject();
				clone.Components.Create<ModelRenderer>();
				clone.Transform.Position = _hitPos;
			}
			Renderer.Set( "b_attack", true );

			TimeSinceLastShot = 0;
		}
	}
	protected override void OnEnabled()
	{
		Renderer.Set( "b_attack", false );
	}
}
