

using Sandbox.Utility;

public class PropShiftingMechanic : Component
{
	public TeamComponent Team { get; set; }
	public delegate void PropShiftingDelegate( PropShiftingMechanic propShiftingMechanic, Model PropModel, Player player, Inventory inventory );
	[Property] public PropShiftingDelegate OnPropShift { get; set; }
	[Property] public CapsuleCollider CapsuleCollider { get; set; }
	[Property] public BoxCollider Collider { get; set; }
	[Property, Sync] public bool IsProp { get; set; } = false;
	public GameObject ActiveCollider { get; set; }
	protected override void OnStart()
	{
		Team = Scene.GetAllComponents<TeamComponent>().FirstOrDefault( x => !x.IsProxy );
	}
	protected override void OnUpdate()
	{
		if ( IsProp )
		{
			CapsuleCollider.Enabled = false;
			Collider.Enabled = true;
			Collider.Scale = Player.Local.AnimationHelper.Target.Bounds.Size;
			ActiveCollider = Collider.GameObject;
		}
		else
		{
			if ( CapsuleCollider is not null )
			{
				CapsuleCollider.Enabled = true;
				ActiveCollider = CapsuleCollider.GameObject;
			}
			if ( Collider is not null )
			{
				Collider.Enabled = false;
			}
		}
		if ( IsProxy || Team.Team == global::Team.Hunters || Team.Team == global::Team.Unassigned || !Player.Local.AbleToMove ) return;
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
	[Broadcast]
	public void ExitProp()
	{
		if ( IsProxy ) return;
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
		pcModel.Network.Refresh();
	}


	[Broadcast]
	private async void ShiftIntoProp()
	{
		if ( IsProxy ) return;
		var pc = Components.Get<Player>();
		var lookDir = pc.EyeAngles.ToRotation();
		var eyePos = Transform.Position + Vector3.Up * 64;

		var tr = Scene.Trace
			.WithoutTags( Steam.SteamId.ToString() )
			.Sphere( 16, eyePos, eyePos + lookDir.Forward * 150 )
			.Run();

		//Gizmo.Draw.LineSphere( tr.HitPosition, 16 );

		if ( !tr.Hit ) return;

		if ( tr.Body.GetGameObject() is not GameObject go )
			return;

		if ( go.Tags.Has( "solid" ) )
			return;

		IsProp = await TryChangeModel( tr, pc, this ).ContinueWith( x => x.Result );

		Log.Info( "changed model" );
		if ( !IsProxy )
		{
			OnPropShift?.Invoke( this, pc.Body.Components.Get<SkinnedModelRenderer>().Model, pc, pc.Inventory );
		}
	}


	private static async Task<bool> TryChangeModel( SceneTraceResult tr, Player player, PropShiftingMechanic propShiftingMechanic )
	{
		var pcModel = player.Body.Components.Get<SkinnedModelRenderer>();
		var propModel = tr.GameObject.Components.Get<ModelRenderer>();
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
		var finalModel = await Model.LoadAsync( propModel.Model.ResourcePath );
		pcModel.Model = finalModel;
		pcModel.Tint = propModel.Tint;
		pcModel.GameObject.Transform.Scale = propModel.GameObject.Transform.Scale;
		pcModel.Network.Refresh();
		return true;
	}
}
