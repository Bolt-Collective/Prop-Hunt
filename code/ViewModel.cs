using Facepunch;
using Sandbox;

public sealed class ViewModel : Component
{
	public Player Player { get; set; }
	[Property] public SkinnedModelRenderer Gun { get; set; }
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
		if ( Player.GameObject.Components.Get<Hc1CharacterController>() is null ) return;
		Gun.Set( "move_groundspeed", Player.GameObject.Components.Get<Hc1CharacterController>().Velocity.Length );
	}
	Vector3 LocalPos = Vector3.Zero;
	protected override void OnUpdate()
	{
		GameObject.Enabled = !GameObject.Parent.IsProxy;
		if ( GameObject.Parent.IsProxy ) return;
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
		if ( Player is null || GameObject.Parent is null || Player.Eye is null ) return;
		GameObject.Parent.Parent = Player.Eye;
		LocalPos = LocalPos.LerpTo( LocalPos, Time.Delta * 10f );
		if ( Player.AbleToMove && Gun is not null )
		{
			ApplyInertia();
			GroundSpeed();
		}
	}

	protected override void OnEnabled()
	{
		if ( GameObject.Parent.IsProxy || Gun is null ) return;
		Gun.Set( "b_deploy_dry", true );
	}
}
