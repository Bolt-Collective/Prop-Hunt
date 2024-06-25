

using Sandbox.Utility;

public class PropShiftingMechanic : Component
{
	public TeamComponent TeamComponent { get; set; }
	public delegate void PropShiftingDelegate( PropShiftingMechanic propShiftingMechanic, Model PropModel, Player player, Inventory inventory );
	[Property] public PropShiftingDelegate OnPropShift { get; set; }
	[Property] public CapsuleCollider Collider { get; set; }
	[Property, Sync] public bool IsProp { get; set; } = false;
	[Sync] public string ModelPath { get; set; }
	protected override void OnStart()
	{
		TeamComponent = Scene.GetAllComponents<TeamComponent>().FirstOrDefault( x => !x.IsProxy );
		ModelPath = Player.Local.BodyRenderer.Model.ResourcePath;
	}
	protected override void OnUpdate()
	{

		if ( IsProxy || TeamComponent.TeamName != Team.Props.ToString() ) return;
		if ( Input.Pressed( "View" ) )
		{
			ExitProp();
		}
		var pc = Components.Get<Player>();
		if ( ModelPath is null ) return;
		//Gizmo.Draw.LineBBox( pc.GameObject.GetBounds() );

		if ( Input.Pressed( "Use" ) )
		{
			ShiftIntoProp();
		}
	}
	public void ExitProp()
	{
		if ( !Player.Local.AbleToMove || IsProxy ) return;
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
		ModelPath = "models/citizen/citizen.vmdl_c";
		pcModel.Model = Model.Load( ModelPath );
		pcModel.Tint = Color.White;
		pcModel.GameObject.Transform.Scale = Vector3.One;
		pcModel.GameObject.Transform.Scale = Vector3.One;
		if ( clothes.Any() )
		{
			foreach ( var cloth in clothes )
			{
				cloth.Enabled = true;
				cloth.Network.Refresh();
			}
		}
		Player.Local.Body.Network.Refresh();
		IsProp = false;

	}
	private void ShiftIntoProp()
	{
		if ( !Player.Local.AbleToMove || IsProxy ) return;
		var pc = Components.Get<Player>();
		var lookDir = pc.EyeAngles.ToRotation();
		var eyePos = Transform.Position + Vector3.Up * 64;

		var tr = Scene.Trace
			.IgnoreGameObject( Player.Local.Body )
			.Sphere( 16, eyePos, eyePos + lookDir.Forward * 150 )
			.Run();

		//Gizmo.Draw.LineSphere( tr.HitPosition, 16 );

		if ( !tr.Hit ) return;
		var propModel = tr.GameObject.Components.Get<Prop>( FindMode.EverythingInSelfAndDescendants )?.Model ?? tr.GameObject.Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndDescendants )?.Model;
		var propRenderer = tr.GameObject.Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndDescendants );
		if ( propModel is null ) return;
		ModelPath = propModel.ResourcePath;
		Player.Local.Body.Transform.Scale = propRenderer.Transform.Scale;
		Player.Local.BodyRenderer.Tint = propRenderer.Tint;
		Player.Local.BodyRenderer.Model = Model.Load( ModelPath );
		IsProp = ModelPath == "models/citizen/citizen.vmdl_c" ? false : true;

		Log.Info( "changed model" );
		if ( !IsProxy )
		{
			OnPropShift?.Invoke( this, pc.Body.Components.Get<SkinnedModelRenderer>().Model, pc, pc.Inventory );
		}
		var body = Player.Local.BodyRenderer;
		var clothes = Player.Local.Body.GetAllObjects( true ).Where( c => c.Tags.Has( "clothing" ) );
		if ( clothes.Any() && IsProp )
		{
			foreach ( var cloth in clothes )
			{
				cloth.Enabled = false;
				cloth.Network.Refresh();
			}
		}
		Player.Local.Body.Network.Refresh();
	}

}
