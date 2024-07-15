using Sandbox;

public sealed class BlindPostprocess : PostProcess
{
	public IDisposable RenderHook { get; set; }
	[Property] public Color color { get; set; } = Color.Black;
	[Property] public bool UseBlind { get; set; } = false;
	protected override void OnEnabled()
	{
		RenderHook = Camera.AddHookBeforeOverlay( "Blind Postprocess", 1000, RenderEffect );
	}

	protected override void OnDisabled()
	{
		RenderHook?.Dispose();
		RenderHook = null;
	}

	RenderAttributes Attributes = new RenderAttributes();

	public void RenderEffect( SceneCamera cam )
	{
		if ( !cam.EnablePostProcessing || !UseBlind ) return;
		Attributes.Set( "color", color );
		Graphics.GrabFrameTexture( "ColorBuffer", Attributes );
		Graphics.Blit( Material.FromShader( "shaders/blind" ), Attributes );
	}
}
