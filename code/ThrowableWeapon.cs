using Sandbox;
using Sandbox.Utility;

public sealed class ThrowableWeapon : Component
{
	[Property] public GameObject ThrowableObject { get; set; }
	[Property] public float ThrowPower { get; set; } = 1000f;
	[Property] public Action OnThrow { get; set; }
	[Property] public bool HasThrown { get; set; } = false;
	[Sync] TimeSince TimeSinceThrown { get; set; }
	protected override void OnUpdate()
	{
		if ( Input.Pressed( "attack1" ) && !IsProxy && !HasThrown )
		{
			Throw();
			OnThrow?.Invoke();
			HasThrown = true;
		}
		if ( TimeSinceThrown > 5f && HasThrown )
		{
			GameObject.Destroy();
		}
	}

	[Broadcast]
	public void Throw()
	{
		if ( !IsProxy )
		{
			var tr = Scene.Trace.Ray( new( Player.Local.Eye.Transform.Position, Player.Local.EyeAngles.Forward ), 10f )
			.IgnoreGameObjectHierarchy( GameObject.Root )
			.WithoutTags( Steam.SteamId.ToString() )
			.Run();

			var pos = tr.Hit ? tr.HitPosition + tr.Normal : Player.Local.Eye.Transform.Position + Player.Local.EyeAngles.Forward * 32;
			var rot = Rotation.From( 0, Player.Local.EyeAngles.yaw + 90, 90 );
			var playerVelo = Player.Local.characterController.Velocity;
			var throwable = ThrowableObject.Clone( pos, rot );
			throwable.Tags.Add( "ignore" );

			var rb = throwable.Components.Get<Rigidbody>();
			rb.Velocity = playerVelo + Player.Local.EyeAngles.Forward * ThrowPower + Vector3.Up * 100f;
			throwable.NetworkSpawn();
			TimeSinceThrown = 0;
		}
	}
}
