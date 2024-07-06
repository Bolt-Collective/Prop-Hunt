using Sandbox;

public sealed class FlashlightComponent : Component
{
	[Property] public SpotLight Flashlight { get; set; }

	[Sync] public bool Enabled { get; set; } = false;

	protected override void OnAwake()
	{
		if ( IsProxy ) return;

	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

	}


	[Broadcast]
	private void BroadcastFlashlight()
	{
		Flashlight.Transform.Rotation = Player.Local.Eye.Transform.Rotation;
		Flashlight.Transform.Position = Player.Local.Eye.Transform.Position + Player.Local.Eye.Transform.Rotation.Forward * 40;
	}


	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		BroadcastFlashlight();


		if ( Input.Pressed( "Menu" ) ) {
			Enabled = !Enabled;
			Flashlight.Enabled = Enabled;
		}
	}
}
