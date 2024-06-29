

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
		if ( ModelPath is null || TeamComponent.TeamName != Team.Props.ToString() ) return;

		if ( Input.Pressed( "Use" ) )
		{
			ShiftIntoProp();
		}
		AdjustPlayerCollider( GameObject.Id );

	}
	[Broadcast]
	public void AdjustPlayerCollider( Guid caller )
	{
		var player = Scene.Directory.FindByGuid( caller )?.Components.Get<Player>();
		if ( player == null ) return;

		var prop = player.GameObject?.Components.Get<PropShiftingMechanic>();
		if ( prop == null || prop.Collider == null ) return;

		var collider = prop.Collider;
		Collider.Center = player.BodyRenderer.Bounds.Center;
		if ( prop.IsProp )
		{
			Vector3 initialAdjustmentScale = Vector3.Max( collider.Scale * 0.8f, new Vector3( 0.5f, 0.5f, 0.5f ) );
			collider.Scale = initialAdjustmentScale;

			// Perform a sweep to check for clipping after initial adjustment
			bool isClipping;
			do
			{
				var sweepResult = Scene.Trace.Sweep( collider.KeyframeBody, player.Body.Transform.World, new global::Transform( player.Body.Transform.Position + Vector3.Up * 0.1f, player.Transform.Rotation ) )
					.IgnoreGameObject( player.Body )
					.Size( collider.Scale / 0.9f ) // Use the current collider scale for the sweep
					.Run();

				isClipping = sweepResult.Hit;
				if ( isClipping )
				{
					// Further reduce the collider scale if clipping is detected
					collider.Scale *= 0.3f;
				}

			} while ( isClipping && collider.Scale.LengthSquared > 0.1f );

			if ( !isClipping )
			{
				collider.Scale = player.BodyRenderer?.Bounds.Size ?? Vector3.One;
			}
		}
		else
		{
			collider.Scale = player.BodyRenderer?.Bounds.Size ?? Vector3.One;
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
		Collider.Scale = pcModel.Bounds.Size;
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
		}
		Player.Local.Body.Network.Refresh();
	}

}
