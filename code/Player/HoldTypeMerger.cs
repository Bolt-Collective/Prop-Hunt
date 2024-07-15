using System.Numerics;
using System.Security.Authentication;
using Sandbox;
using Sandbox.Citizen;

public sealed class HoldTypeMerger : Component
{
	public CitizenAnimationHelper AnimHelper { get; set; }
	[Property, Sync] public Vector3 Offset { get; set; }
	[Property] public CitizenAnimationHelper.HoldTypes HoldType { get; set; }
	[Property] public SkinnedModelRenderer WeaponRenderer { get; set; }
	[Sync] public bool shouldRender { get; set; }
	protected override void OnStart()
	{
		if ( !IsProxy )
		{
			AnimHelper = Player.Local.AnimationHelper;
		}
	}

	protected override void OnPreRender()
	{
		if ( Player.Local is null || Player.Local.IsDead || Player.Local.TeamComponent?.TeamName == Team.Unassigned.ToString() || Player.Local.Body is null || Player.Local.BodyRenderer is null )
			return;

		if ( GameObject != null && AnimHelper != null )
		{
			if ( AnimHelper.Target.TryGetBoneTransform( "hold_r", out var boneTransform ) )
			{
				GameObject.Parent.Transform.Position = boneTransform.Position + Offset;
				GameObject.Parent.Transform.Rotation = boneTransform.Rotation;
				if ( !IsProxy )
					BroadcastHoldType( AnimHelper.GameObject );
			}
		}

		shouldRender = WeaponRenderer != null;
		if ( shouldRender )
		{
			WeaponRenderer.Enabled = true;
			var proxyRenderType = IsProxy || Player.Local.CameraDistance != 0 ? ModelRenderer.ShadowRenderType.On : ModelRenderer.ShadowRenderType.ShadowsOnly;
			WeaponRenderer.RenderType = WeaponRenderer.Model == null ? ModelRenderer.ShadowRenderType.Off : proxyRenderType;
		}
	}
	[Broadcast]
	public void BroadcastHoldType( GameObject Caller )
	{
		var helper = Caller.Components.Get<CitizenAnimationHelper>();
		if ( helper is null ) return;
		helper.HoldType = HoldType;
	}
}
