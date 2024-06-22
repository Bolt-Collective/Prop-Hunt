

using Sandbox.Utility;

public class PropShiftingMechanic : Component
{
	public TeamComponent TeamComponent { get; set; }
	public delegate void PropShiftingDelegate( PropShiftingMechanic propShiftingMechanic, Model PropModel, Player player, Inventory inventory );
	[Property] public PropShiftingDelegate OnPropShift { get; set; }
	[Property] public CapsuleCollider Collider { get; set; }
	[Property, Sync] public bool IsProp { get; set; } = false;
	protected override void OnStart()
	{
		TeamComponent = Scene.GetAllComponents<TeamComponent>().FirstOrDefault( x => !x.IsProxy );
	}
	protected override void OnUpdate()
	{
		BodyVis();

		if ( IsProxy || TeamComponent.TeamName != Team.Props.ToString() ) return;
		if ( Input.Pressed( "View" ) )
		{
			ExitProp();
		}
		var pc = Components.Get<Player>();

		//Gizmo.Draw.LineBBox( pc.GameObject.GetBounds() );

		if ( Input.Pressed( "Use" ) )
		{
			ShiftIntoProp();
		}
	}
	public void BodyVis()
	{
		var localPlayer = Scene.GetAllComponents<Player>().FirstOrDefault( x => !x.IsProxy );
		var spectateSystem = Scene.GetAllComponents<SpectateSystem>().FirstOrDefault( x => !x.IsProxy );
		if ( localPlayer.BodyRenderer is null ) return;
		if ( IsProp || !spectateSystem.IsSpectating && !IsProxy )
		{
			var bodyRenderer = localPlayer.BodyRenderer;
			var renderType = localPlayer.CameraDistance == 0 ? ModelRenderer.ShadowRenderType.ShadowsOnly : ModelRenderer.ShadowRenderType.On;
			bodyRenderer.RenderType = renderType;
			if ( bodyRenderer.RenderType != renderType )
			{
				bodyRenderer.Network.Refresh();
			}
		}
		else if ( IsProp && !spectateSystem.IsSpectating && IsProxy )
		{
			localPlayer.BodyRenderer.RenderType = ModelRenderer.ShadowRenderType.On;
		}
	}
	public void ExitProp()
	{
		if ( IsProxy || !Player.Local.AbleToMove ) return;
		var pc = Components.Get<Player>();
		var pcModel = pc.Body.Components.Get<SkinnedModelRenderer>();
		var clothes = pc.Body.GetAllObjects( false ).Where( c => c.Tags.Has( "clothing" ) );
		if ( clothes.Any() )
		{
			foreach ( var cloth in clothes )
			{
				cloth.Enabled = true;
			}
		}
		pcModel.Model = Model.Load( "models/citizen/citizen.vmdl_c" );
		pcModel.Tint = Color.White;
		pcModel.GameObject.Transform.Scale = Vector3.One;
		IsProp = false;
		pc.GameObject.Network.Refresh();
	}


	private void ShiftIntoProp()
	{
		if ( IsProxy || !Player.Local.AbleToMove ) return;
		var pc = Components.Get<Player>();
		var lookDir = pc.EyeAngles.ToRotation();
		var eyePos = Transform.Position + Vector3.Up * 64;

		var tr = Scene.Trace
			.WithoutTags( Steam.SteamId.ToString() )
			.Sphere( 16, eyePos, eyePos + lookDir.Forward * 150 )
			.Run();

		//Gizmo.Draw.LineSphere( tr.HitPosition, 16 );

		if ( !tr.Hit ) return;

		if ( tr.GameObject.Tags.Has( "solid" ) )
			return;

		IsProp = TryChangeModel( tr, pc, this );

		Log.Info( "changed model" );
		if ( !IsProxy )
		{
			OnPropShift?.Invoke( this, pc.Body.Components.Get<SkinnedModelRenderer>().Model, pc, pc.Inventory );
		}
		pc.GameObject.Network.Refresh();
	}


	public bool TryChangeModel( SceneTraceResult tr, Player player, PropShiftingMechanic propShiftingMechanic )
	{
		if ( tr.GameObject is null ) return false;
		var pcModel = player.Body.Components.Get<SkinnedModelRenderer>();
		var propModel = tr.GameObject.Components.Get<ModelRenderer>();
		if ( propModel is null || pcModel is null ) return false;
		if ( tr.Body.BodyType == PhysicsBodyType.Static )
		{
			return propShiftingMechanic.IsProp ? true : false;
		}

		var clothes = player.Body.GetAllObjects( true ).Where( c => c.Tags.Has( "clothing" ) );
		if ( clothes.Any() )
		{
			foreach ( var cloth in clothes )
			{
				cloth.Enabled = false;
			}
		}

		if ( pcModel.Model.Name == propModel.Model.Name )
		{
			return propShiftingMechanic.IsProp ? true : false;
		}
		var finalModel = propModel.Model;
		pcModel.Model = finalModel;
		pcModel.Tint = propModel.Tint;
		pcModel.GameObject.Transform.Scale = propModel.GameObject.Transform.Scale;
		pcModel.Network.Refresh();
		return true;
	}
}
