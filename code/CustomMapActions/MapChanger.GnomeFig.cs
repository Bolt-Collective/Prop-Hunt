namespace PropHunt;
public partial class MapChanger
{
	[Category( "Gnomefig" )]
	public void CreateGnomeFigPostprocess()
	{
		Scene.GetAllComponents<CameraComponent>().FirstOrDefault( x => x.IsMainCamera ).Components.Create<GnomefigPostprocess>();
	}
	public class GnomefigPostprocess : PostProcess
	{
		IDisposable renderHook;

		protected override void OnEnabled()
		{
			renderHook = Camera.AddHookBeforeOverlay( "My Post Processing", 1000, RenderEffect );
		}
		protected override void OnDisabled()
		{
			renderHook?.Dispose();
			renderHook = null;
		}
		RenderAttributes attributes = new RenderAttributes();
		public void RenderEffect( SceneCamera camera )
		{
			if ( !camera.EnablePostProcessing ) return;
			//camera.Attributes.SetCombo( "D_VERTEX_SNAP", 1 );
			Graphics.GrabFrameTexture( "ColorBuffer", attributes );
			Graphics.GrabDepthTexture( "DepthBuffer", attributes );
			Graphics.Blit( Material.FromShader( Cloud.Shader( "facepunch.goback_postprocess" ) ), attributes );

		}
	}
	[Pure]
	public static bool GetIsProxy( GameObject gameObject )
	{
		return gameObject.Network.IsProxy;
	}
}
