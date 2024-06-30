

using PropHunt;
using Sandbox.Utility;

public class PropShiftingMechanic : Component
{
	public TeamComponent TeamComponent { get; set; }
	public delegate void PropShiftingDelegate( PropShiftingMechanic propShiftingMechanic, Model PropModel, Player player, Inventory inventory );
	[Property] public PropShiftingDelegate OnPropShift { get; set; }
	[Property] public BoxCollider Collider { get; set; }
	[Property, Sync] public bool IsProp { get; set; } = false;
	[Sync] public string ModelPath { get; set; }
	[Property] public int PreviousHealth { get; set; }
	public TauntComponent TauntComponent { get; set; }
	protected override void OnStart()
	{
		TeamComponent = Scene.GetAllComponents<TeamComponent>().FirstOrDefault( x => !x.IsProxy );
		TauntComponent = Scene.GetAllComponents<TauntComponent>().FirstOrDefault( x => !x.IsProxy );
		PreviousHealth = 100;
		if ( !IsProxy )
		{
			ModelPath = Player.Local.BodyRenderer?.Model.ResourcePath;
		}
	}
	protected override void OnUpdate()
	{
		if ( IsProxy ) return;


		if ( Input.Pressed( "View" ) )
		{
			ExitProp();
		}
		if ( ModelPath is null ) return;

		if ( Input.Pressed( "Use" ) )
		{
			ShiftIntoProp();
		}
		AdjustPlayerCollider( GameObject.Id );
	}
	public void AdjustPlayerCollider( Guid caller )
	{
		var player = Scene.Directory.FindByGuid( caller );
		var controller = player.Components.Get<Player>();
		var prop = player.Components.Get<PropShiftingMechanic>();
		var collider = prop.Collider;
		if ( player is null || prop is null || collider is null || controller is null ) return;

		var center = controller.BodyRenderer.Bounds.Center;
		var trace = Scene.Trace.Sweep( collider.KeyframeBody, new global::Transform( controller.BodyRenderer.Bounds.Mins ), new Transform( controller.BodyRenderer.Bounds.Maxs ) ).IgnoreGameObject( collider.GameObject ).Run();
		Vector3 minScale = new Vector3( controller.BodyRenderer.Bounds.Size.x / 2, controller.BodyRenderer.Bounds.Size.y / 2, controller.BodyRenderer.Bounds.Size.z ); // Define minimum scale here

		Vector3 traceScale = trace.Hit ? trace.EndPosition - trace.StartPosition : controller.BodyRenderer.Bounds.Size;
		Vector3 newScale = new Vector3(
			trace.Hit && traceScale.x < controller.BodyRenderer.Bounds.Size.x ? Math.Max( traceScale.x, minScale.x ) : controller.BodyRenderer.Bounds.Size.x,
			trace.Hit && traceScale.y < controller.BodyRenderer.Bounds.Size.y ? Math.Max( traceScale.y, minScale.y ) : controller.BodyRenderer.Bounds.Size.y,
			trace.Hit && traceScale.z < controller.BodyRenderer.Bounds.Size.z ? Math.Max( traceScale.z, minScale.z ) : controller.BodyRenderer.Bounds.Size.z
		);

		collider.Scale = newScale;
		controller.characterController.Height = collider.Scale.z;
		controller.characterController.Radius = Math.Min( collider.Scale.x, collider.Scale.y ) / 2;

		Gizmo.Draw.LineBBox( BBox.FromPositionAndSize( center, collider.Scale ) );
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
		Player.Local.Health = PreviousHealth;
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

		var tr = Scene.Trace.Ray( Scene.Camera.Transform.Position, Scene.Camera.Transform.Position + lookDir.Forward * 300 + Player.Local.CameraDistance )
			.IgnoreGameObject( Player.Local.Body )
			.Run();

		//Gizmo.Draw.LineSphere( tr.HitPosition, 16 );

		if ( !tr.Hit ) return;
		var propModel = tr.GameObject.Components.Get<Prop>( FindMode.EverythingInSelfAndDescendants )?.Model ?? tr.GameObject.Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndDescendants )?.Model;
		var propRenderer = tr.GameObject.Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndDescendants );
		if ( propModel is null ) return;
		if ( propModel.ResourcePath == "models/citizen/citizen.vmdl_c" ) return;
		ModelPath = propModel.ResourcePath;
		Player.Local.Body.Transform.Scale = propRenderer.Transform.Scale;
		Player.Local.BodyRenderer.Tint = propRenderer.Tint;
		Player.Local.BodyRenderer.Model = Model.Load( ModelPath );
		IsProp = ModelPath == "models/citizen/citizen.vmdl_c" ? false : true;

		Log.Info( "changed model" );

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
		if ( !IsProxy )
		{
			OnPropShift?.Invoke( this, pc.Body.Components.Get<SkinnedModelRenderer>().Model, pc, pc.Inventory );

			// Handle the health algorithm for props
			/*
			if ( !IsProp )
			{
				PreviousHealth = (int)Player.Local.Health;
			}
			float multiplier = Math.Clamp( Player.Local.Health / Player.Local.MaxHealth, 0, 1 );
			float health = (float)Math.Pow( propModel.PhysicsBounds.Volume, 0.5f ) * 0.5f;

			health = (float)Math.Round( health / 5 ) * 5;
			Player.Local.MaxHealth = health;
			Player.Local.Health = health * multiplier;

			// Fallback to 10 health if prop is 0 health
			if ( Player.Local.Health <= 0 )
			{
				Player.Local.Health = 10f;
			}*/
		}
		Player.Local.Body.Network.Refresh();

	}

}
