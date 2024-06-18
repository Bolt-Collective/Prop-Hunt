using Sandbox;

public sealed class SpectateSystem : Component
{
	[Property, Sync] public bool IsSpectating { get; set; }
	protected override void OnUpdate()
	{
		var localPlayer = Scene.GetAllComponents<Player>().FirstOrDefault( x => !x.IsProxy );
		if ( IsSpectating && !IsProxy )
		{
			var camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault( x => x.IsMainCamera );
			var player = Scene.GetAllComponents<Player>().FirstOrDefault();
			camera.Transform.Position = player.CameraPosWorld.Position;
			camera.Transform.Rotation = player.CameraPosWorld.Rotation;
			player.BodyRenderer.RenderType = player.CameraDistance == 0 ? ModelRenderer.ShadowRenderType.ShadowsOnly : ModelRenderer.ShadowRenderType.On;
			localPlayer.AbleToMove = false;
			localPlayer.Body.Enabled = false;
		}
		else if ( IsSpectating )
		{

		}
	}

	public void SetIsSpecating( bool value )
	{
		if ( IsProxy ) return;
		IsSpectating = value;
	}
}
