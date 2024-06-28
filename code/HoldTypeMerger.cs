using System.Numerics;
using System.Security.Authentication;
using Sandbox;
using Sandbox.Citizen;

public sealed class HoldTypeMerger : Component
{
	[Property] public CitizenAnimationHelper AnimHelper { get; set; }
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
			HoldBoneObject = new GameObject();
			WeaponRenderer = HoldBoneObject.Components.Create<SkinnedModelRenderer>();
			WeaponRenderer.Model = ModelToMerge;
			HoldBoneObject.NetworkSpawn();
			WeaponRenderer.GameObject.Parent = AnimHelper.Target.GameObject;
		}
	}

	protected override void OnUpdate()
	{

		if ( Player.Local.IsDead || HoldBoneObject is null ) return;
		if ( HoldBoneObject is not null && !IsProxy )
		{
			var boneTransform = new Transform();
			AnimHelper.Target.TryGetBoneTransform( "hold_r", out boneTransform );
			HoldBoneObject.Transform.Position = boneTransform.Position + Offset;
			HoldBoneObject.Transform.Rotation = boneTransform.Rotation;
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
	[Broadcast]
	public void ChangeModel( string modelName, Vector3 offset, string holdType )
	{
		if ( WeaponRenderer is null ) return;
		ModelToMerge = Model.Load( modelName );
		WeaponRenderer.Model = ModelToMerge;
		HoldType = (CitizenAnimationHelper.HoldTypes)Enum.Parse( typeof( CitizenAnimationHelper.HoldTypes ), holdType );
		Offset = offset;
	}

	[Broadcast]
	public void ClearModel()
	{
		if ( WeaponRenderer is null ) return;
		ModelToMerge = null;
		WeaponRenderer.Model = null;
		AnimHelper.HoldType = CitizenAnimationHelper.HoldTypes.None;
		WeaponRenderer.Enabled = false;

	}

}
