using Sandbox;

public sealed class ViewModel : Component
{
	public Player Player { get; set; }
	[Property] public SkinnedModelRenderer Gun { get; set; }
	[Property] public GameObject Main { get; set; }
	float lastPitch;
	float lastYaw;
	float yawInertia;
	float pitchInertia;
	bool UseInteria = false;
	protected override void OnStart()
	{
		Player = Scene.GetAllComponents<Player>().FirstOrDefault( x => !x.IsProxy );
	}

	void ApplyInertia()
	{
		var camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault( x => !x.IsProxy );
		var cameraRot = camera.Transform.Rotation;

		if ( !UseInteria )
		{
			lastPitch = cameraRot.Pitch();
			lastYaw = cameraRot.Yaw();
			yawInertia = 0;
			pitchInertia = 0;
			UseInteria = true;
		}
		var pitch = cameraRot.Pitch();
		var yaw = cameraRot.Yaw();
		pitchInertia = Angles.NormalizeAngle( pitch - lastPitch );
		yawInertia = Angles.NormalizeAngle( yaw - lastYaw );
		lastPitch = pitch;
		lastYaw = yaw;
		Gun.Set( "aim_yaw_inertia", yawInertia * 2 );
		Gun.Set( "aim_pitch_inertia", pitchInertia * 2 );
	}

	void GroundSpeed()
	{
		Gun.Set( "move_groundspeed", Player.GameObject.Components.Get<CharacterController>().Velocity.Length );
	}
	Vector3 LocalPos = Vector3.Zero;
	protected override void OnUpdate()
	{
		var spectateSystem = Scene.GetAllComponents<SpectateSystem>().FirstOrDefault( x => !x.IsProxy );
		GameObject.Enabled = !IsProxy;
		if ( IsProxy ) return;
		if ( Player.CameraDistance != 0 )
		{
			foreach ( var model in GameObject.Components.GetAll<SkinnedModelRenderer>( FindMode.InDescendants ) )
			{
				model.Enabled = false;
			}
		}
		else
		{
			foreach ( var model in GameObject.Components.GetAll<SkinnedModelRenderer>( FindMode.InDescendants ) )
			{
				model.Enabled = true;
			}
		}
		Main.Parent = Player.Eye;
		LocalPos = LocalPos.LerpTo( LocalPos, Time.Delta * 10f );
		ApplyInertia();
		GroundSpeed();
	}

	protected override void OnEnabled()
	{
		if ( IsProxy ) return;
		Gun.Set( "b_deploy_dry", true );
	}
}
