using Sandbox;

public sealed class FlashlightComponent : Component
{
	[Property] public SpotLight Flashlight { get; set; }


	[Broadcast]
	private void BroadcastFlashlight( GameObject caller )
	{
		var player = caller.Components.Get<Player>();
		if ( player == null ) return;
		Flashlight.Transform.Rotation = player.Eye.Transform.Rotation;
		Flashlight.Transform.Position = player.Eye.Transform.Position + player.Eye.Transform.Rotation.Forward * 40;
	}
	[Broadcast]
	private void EnableFlashlight( GameObject caller )
	{
		var flashLight = caller.Components.Get<FlashlightComponent>();
		if ( flashLight == null ) return;

		flashLight.Flashlight.Enabled = !flashLight.Flashlight.Enabled;
	}
	protected override void OnFixedUpdate()
	{
		if ( Player.Local is null ) return;
		if ( Player.Local.TeamComponent.TeamName == Team.Unassigned.ToString() ) return;

		if ( Player.Local.TeamComponent.TeamName == Team.Props.ToString() ) return;

		if ( IsProxy ) return;

		BroadcastFlashlight( GameObject.Root );
		if ( Input.Pressed( "menu" ) )
		{
			EnableFlashlight( GameObject );
		}

	}
}
