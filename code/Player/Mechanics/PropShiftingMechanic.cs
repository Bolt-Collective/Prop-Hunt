

public class PropShiftingMechanic : Component
{
	public TeamComponent Team { get; set; }
	public delegate void PropShiftingDelegate(PropShiftingMechanic propShiftingMechanic, Model PropModel, Player player, Inventory inventory);
	[Property] public PropShiftingDelegate OnPropShift { get; set; }
	protected override void OnStart()
	{
		Team = Scene.GetAllComponents<TeamComponent>().FirstOrDefault(x => !x.IsProxy);
	}
	protected override void OnUpdate()
	{
		if ( IsProxy || Team.Team == global::Team.Hunters || Team.Team == global::Team.Unassigned  ) return;
		if (Input.Pressed("View"))
		{
			ExitProp();
		}
		var pc = Components.Get<Player>();

		//Gizmo.Draw.LineBBox( pc.GameObject.GetBounds() );


		if ( Input.Pressed("Use"))
		{
			ShiftIntoProp();
		}

	}
	[Broadcast]
	public void ExitProp()
	{
		var pc = Components.Get<Player>();
		var pcModel = pc.Body.Components.Get<SkinnedModelRenderer>();
		var clothes = pc.Body.GetAllObjects(false).Where( c => c.Tags.Has( "clothing" ) );
		if ( clothes.Any() )
		{
			foreach ( var cloth in clothes )
			{
				cloth.Enabled = true;
			}
		}
		pcModel.Model = Model.Load("models/citizen/citizen.vmdl_c");
		pcModel.Tint = Color.White;
	}


	[Broadcast]
	private void ShiftIntoProp()
	{
		var pc = Components.Get<Player>();
		var lookDir = pc.EyeAngles.ToRotation();
		var eyePos = Transform.Position + Vector3.Up * 64;

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
		if (!IsProxy)
		{
			OnPropShift?.Invoke(this, pc.Body.Components.Get<SkinnedModelRenderer>().Model, pc, pc.Inventory);
		}
	}


	private static async void TryChangeModel(SceneTraceResult tr, Player player)
	{
		var pcModel = player.Body.Components.Get<SkinnedModelRenderer>();
		var propModel = tr.GameObject.Components.Get<ModelRenderer>();
		if (tr.Body.BodyType == PhysicsBodyType.Static) return;

		var clothes = player.Body.GetAllObjects( true ).Where( c => c.Tags.Has( "clothing" ) );
		if ( clothes.Any() )
		{
			foreach ( var cloth in clothes )
			{
				cloth.Enabled = false;
			}
		}

		if ( pcModel.Model.Name == propModel.Model.Name )
			return;

		var finalModel = await Model.LoadAsync( propModel.Model.ResourcePath );
		pcModel.Model = finalModel;
		pcModel.Tint = propModel.Tint;
	}
}
