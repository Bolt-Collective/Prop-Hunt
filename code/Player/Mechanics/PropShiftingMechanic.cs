

public class PropShiftingMechanic : Component
{
	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		var pc = Components.Get<Player>();

		Gizmo.Draw.LineBBox( pc.Body.GetBounds() );


		if ( Input.Pressed("Use"))
		{
			ShiftIntoProp();
		}

	}

	[Broadcast]
	private void ShiftIntoProp()
	{
		var pc = Components.Get<Player>();
		var lookDir = pc.EyeAngles.ToRotation();
		var eyePos = Transform.Position + Vector3.Up * 60;

		var tr = Scene.Trace
			.WithoutTags( "player" )
			.Sphere( 16, eyePos, eyePos + lookDir.Forward * 150 )
			.Run();

		//Gizmo.Draw.LineSphere( tr.HitPosition, 16 );

		if ( !tr.Hit ) return;

		if ( tr.Body.GetGameObject() is not GameObject go )
			return;

		if ( go.Tags.Has( "solid" ) )
			return;
		
		TryChangeModel( tr, pc );

		Log.Info( "changed model" );
	}


	private static async void TryChangeModel(SceneTraceResult tr, Player player)
	{
		var pcModel = player.Body.Components.Get<SkinnedModelRenderer>();
		var propModel = tr.GameObject.Components.Get<ModelRenderer>();

		var clothes = player.Body.GetAllObjects( true ).Where( c => c.Tags.Has( "clothing" ) );
		if ( clothes.Any() )
		{
			foreach ( var cloth in clothes )
			{
				cloth.Destroy();
			}
		}

		if ( pcModel.Model.Name == propModel.Model.Name )
			return;

		var finalModel = await Model.LoadAsync( propModel.Model.ResourcePath );
		pcModel.Model = finalModel;
		pcModel.Tint = propModel.Tint;
	}
}
