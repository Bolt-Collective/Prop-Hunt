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
		if ( Player.Local.IsDead || GameObject is null || AnimHelper is null || Player.Local is null ) return;
		if ( GameObject is not null )
		{
			var boneTransform = new Transform();
			AnimHelper.Target.TryGetBoneTransform( "hold_r", out boneTransform );
			GameObject.Parent.Transform.Position = boneTransform.Position + Offset;
			GameObject.Parent.Transform.Rotation = boneTransform.Rotation;
			if ( !IsProxy )
				BroadcastHoldType( AnimHelper.GameObject.Id );
		}
		shouldRender = WeaponRenderer is not null;
		if ( WeaponRenderer is not null )
		{
			WeaponRenderer.Enabled = shouldRender;
			if ( shouldRender )
			{
				var proxyRenderType = IsProxy || Player.Local.CameraDistance != 0 ? ModelRenderer.ShadowRenderType.On : ModelRenderer.ShadowRenderType.ShadowsOnly;
				WeaponRenderer.RenderType = WeaponRenderer.Model is null ? ModelRenderer.ShadowRenderType.Off : proxyRenderType;
			}
		}
	}
	[Broadcast]
	public void BroadcastHoldType( Guid Caller )
	{
		var helper = Scene.Directory.FindByGuid( Caller ).Components.Get<CitizenAnimationHelper>();
		if ( helper is null ) return;
		helper.HoldType = HoldType;
	}
}
