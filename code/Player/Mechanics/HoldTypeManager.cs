using Sandbox;
using Sandbox.Citizen;

public sealed class HoldTypeManager : Component
{
	[Property] public SkinnedModelRenderer HoldBoneRenderer { get; set; }
	[Property] public CitizenAnimationHelper citizenAnimationHelper { get; set; }
	protected override void OnUpdate()
	{
		if ( HoldBoneRenderer.Model == null )
		{
			HoldBoneRenderer.Enabled = false;
		}
		else
		{
			HoldBoneRenderer.Enabled = true;
		}
		if ( !IsProxy )
		{
			HoldBoneRenderer.RenderType = Player.Local.CameraDistance == 0 ? ModelRenderer.ShadowRenderType.ShadowsOnly : ModelRenderer.ShadowRenderType.On;
		}
	}
	public void SetHoldTypeRenderer( CitizenAnimationHelper.HoldTypes hold, Model model, Vector3 Offset )
	{
		citizenAnimationHelper.HoldType = hold;
		HoldBoneRenderer.Model = model;
		HoldBoneRenderer.Transform.LocalPosition = Offset;
		HoldBoneRenderer.Network.Refresh();
	}
}
