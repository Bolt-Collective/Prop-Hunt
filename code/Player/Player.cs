using Sandbox;
using Sandbox.Citizen;

public class Player : Component
{
	public static Player Local
	{
		get
		{
			if ( !_local.IsValid() )
			{
				_local = Game.ActiveScene.GetAllComponents<Player>().FirstOrDefault( x => x.Network.IsOwner );
			}
			return _local;
		}
	}
	private static Player _local;
	public Vector3 WishVelocity { get; private set; }
	[Property] public GameObject Body { get; set; }
	[Property] public GameObject Eye { get; set; }
	[Property] public CitizenAnimationHelper AnimationHelper { get; set; }
	[Property, Sync] public float CameraDistance { get; set; }

	[Sync]
	public Angles EyeAngles { get; set; }

	[Sync]
	public bool IsRunning { get; set; }


	[RequireComponent] public TeamComponent TeamComponent { get; private set; }

	public float MaxHealth = 100;
	public float Health { get; set; }
	[Sync] public bool FreeLooking { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		Health = MaxHealth;
	}
	private Rotation oldRotation;
	private Angles oldEyeAngles;
public void FreeLook()
{
    var cam = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
    if (cam is null) return;

    if (Input.Pressed("attack2"))
    {
        oldRotation = cam.Transform.Rotation;
		oldEyeAngles = EyeAngles;
    }

    if (Input.Down("attack2"))
    {
        FreeLooking = true;
		var camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault(x => x.IsMainCamera);
		camera.FieldOfView = Preferences.FieldOfView;
		var lookDirection = EyeAngles.ToRotation();
		var center = Transform.Position + Vector3.Up * 64;
        if (CameraDistance != 0)
		{
		
		var tr = Scene.Trace.Ray(center, center - (EyeAngles.Forward * CameraDistance)).WithoutTags("player", "barrier").Run();
		if (tr.Hit)
		{
			camera.Transform.Position = tr.EndPosition + tr.Normal * 2 + Vector3.Up * 10;
		}
		else
		{
			camera.Transform.Position = center - (EyeAngles.Forward * CameraDistance) + Vector3.Up * 10;
		}
		}
		else
		{
			var targetPos = Transform.Position + Vector3.Up * 64;
			camera.Transform.Position = camera.Transform.Position.LerpTo(targetPos, Time.Delta * 60);
		}
	
		camera.Transform.Rotation = lookDirection;

    }

    if (Input.Released("attack2"))
    {
		cam.Transform.Rotation = Rotation.Slerp(cam.Transform.Rotation, oldRotation, Time.Delta * 10.0f);
		EyeAngles = oldEyeAngles;
        FreeLooking = false;
    }
}
	protected override void OnEnabled()
	{
		base.OnEnabled();

		if ( IsProxy )
			return;

		var cam = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
		if ( cam is not null )
		{
			var ee = cam.Transform.Rotation.Angles();
			EyeAngles = ee;
		}
	}
	public void EyeInput()
	{
		var ee = EyeAngles;
		ee += Input.AnalogLook * 0.9f;
		ee.roll = 0;
		ee.pitch = ee.pitch.Clamp( -89, 89 );
		EyeAngles = ee;
	}
	public void CameraPosition()
	{
		var camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault( x => x.IsMainCamera );
		camera.FieldOfView = Preferences.FieldOfView;
		var lookDirection = EyeAngles.ToRotation();
		var center = Transform.Position + Vector3.Up * 64;
		//Trace to see if the camera is inside a wall
		if (CameraDistance != 0)
		{
		var tr = Scene.Trace.Ray(center, center - (EyeAngles.Forward * CameraDistance)).WithoutTags("player", "barrier").Run();
		if (tr.Hit)
		{
			camera.Transform.Position = tr.EndPosition + tr.Normal * 2 + Vector3.Up * 10;
		}
		else
		{
			camera.Transform.Position = center - (EyeAngles.Forward * CameraDistance) + Vector3.Up * 10;
		}
		}
		else
		{
			var targetPos = Transform.Position + Vector3.Up * 64;
			camera.Transform.Position = camera.Transform.Position.LerpTo(targetPos, Time.Delta * 60);
		}
	
		camera.Transform.Rotation = lookDirection;
	}
	public void Animations(CharacterController cc)
	{
		if ( AnimationHelper is not null )
		{
			AnimationHelper.WithVelocity( cc.Velocity );
			AnimationHelper.WithWishVelocity( WishVelocity );
			AnimationHelper.IsGrounded = cc.IsOnGround;
			if (!FreeLooking)
			{
				AnimationHelper.WithLook( EyeAngles.Forward, 1, 1, 1.0f );
			}
			AnimationHelper.MoveStyle = IsRunning ? CitizenAnimationHelper.MoveStyles.Run : CitizenAnimationHelper.MoveStyles.Walk;
		}
	}
	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			FreeLook();
			EyeInput();
			CameraPosition();
			ChangeDistance();
			IsRunning = Input.Down( "Run" );
		}

		var cc = GameObject.Components.Get<CharacterController>();
		if ( cc is null ) return;
		Animations( cc );
		if (!FreeLooking)
		{
			Body.Transform.Rotation = Rotation.Slerp(Body.Transform.Rotation, new Angles(0, EyeAngles.yaw, 0).ToRotation(), Time.Delta * 10.0f);
		}

		
	}
	public void ChangeDistance()
	{
		if (Input.MouseWheel != 0)
		{
			CameraDistance -= Input.MouseWheel.y * 10;
		}
		CameraDistance = CameraDistance.Clamp(0, 1000);
	}
	private void UpdateBodyVisibility()
	{
		if ( AnimationHelper is null )
			return;

		var renderType = (!IsProxy && CameraDistance == 0) ? ModelRenderer.ShadowRenderType.ShadowsOnly : ModelRenderer.ShadowRenderType.On;
		AnimationHelper.Target.RenderType = renderType;
		foreach ( var clothing in AnimationHelper.Target.Components.GetAll<ModelRenderer>( FindMode.InChildren ) )
		{
			if ( !clothing.Tags.Has( "clothing" ) )
				continue;

			clothing.RenderType = renderType;
		}
	}


	[Broadcast]
	public void OnJump()
	{
		AnimationHelper?.TriggerJump();
	}

	protected override void OnPreRender()
	{
		UpdateBodyVisibility();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		BuildWishVelocity();

		var cc = GameObject.Components.Get<CharacterController>();

		if ( cc.IsOnGround && Input.Down( "Jump" ) )
		{
			float flGroundFactor = 1.0f;
			float flMul = 268.3281572999747f * 1.2f;

			cc.Punch( Vector3.Up * flMul * flGroundFactor );

			OnJump();
		}

		if ( cc.IsOnGround )
		{
			cc.Velocity = cc.Velocity.WithZ( 0 );
			cc.Accelerate( WishVelocity );
			cc.ApplyFriction( 4.0f );
		}
		else
		{
			cc.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta * 0.5f;
			cc.Accelerate( WishVelocity.ClampLength( 50 ) );
			cc.ApplyFriction( 0.1f );
		}

		cc.Move();

		if ( !cc.IsOnGround )
		{
			cc.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta * 0.5f;
		}
		else
		{
			cc.Velocity = cc.Velocity.WithZ( 0 );
		}
	}

	public void BuildWishVelocity()
	{
		var rot = EyeAngles.ToRotation();

		WishVelocity = rot * Input.AnalogMove;
		WishVelocity = WishVelocity.WithZ( 0 );

		if ( !WishVelocity.IsNearZeroLength ) WishVelocity = WishVelocity.Normal;

		if ( Input.Down( "Run" ) ) WishVelocity *= 320.0f;
		else WishVelocity *= 110.0f;
	}
}
