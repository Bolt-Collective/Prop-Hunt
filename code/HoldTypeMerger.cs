using System.Numerics;
using System.Security.Authentication;
using Sandbox;
using Sandbox.Citizen;

public sealed class HoldTypeMerger : Component
{
	public CitizenAnimationHelper AnimHelper { get; set; }
	[Property] public Model ModelToMerge { get; set; }
	public GameObject HoldBoneObject { get; set; }
	[Property, Sync] public Vector3 Offset { get; set; }
	[Property] public CitizenAnimationHelper.HoldTypes HoldType { get; set; }
	[Property] public SkinnedModelRenderer WeaponRenderer;
	[Property, Sync] public bool shouldRender { get; set; }
	protected override void OnStart()
	{
		if ( !IsProxy )
		{
			AnimHelper = Player.Local.AnimationHelper;
			HoldBoneObject = new GameObject();
			WeaponRenderer = HoldBoneObject.Components.Create<SkinnedModelRenderer>();
			WeaponRenderer.Model = ModelToMerge;
			HoldBoneObject.NetworkSpawn();
			WeaponRenderer.GameObject.Parent = AnimHelper.Target.GameObject;
		}
	}

	protected override void OnPreRender()
	{

		if ( Player.Local.IsDead || HoldBoneObject is null ) return;
		if ( IsProxy ) return;
		if ( HoldBoneObject is not null )
		{
			var boneTransform = new Transform();
			AnimHelper.Target.TryGetBoneTransformLocal( "hold_r", out boneTransform );
			HoldBoneObject.Transform.LocalPosition = boneTransform.Position + Offset;
			HoldBoneObject.Transform.LocalRotation = boneTransform.Rotation;
			BroadcastHoldType();
		}
		shouldRender = ModelToMerge is not null && WeaponRenderer is not null;
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
	public void BroadcastHoldType()
	{
		if ( AnimHelper is null ) return;
		AnimHelper.HoldType = HoldType;
	}
}
