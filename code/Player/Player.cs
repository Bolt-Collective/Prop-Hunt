using Sandbox.Citizen;
public class Player : Component
{
	[RequireComponent]
	public ShrimpleCharacterController Controller { get; set; }

	[RequireComponent]
	public CitizenAnimationHelper AnimationHelper { get; set; }

	[Property]
	public GameObject Body { get; set; }
	public CameraComponent Camera => Scene.GetAll<CameraComponent>().Where( x => x.IsMainCamera ).FirstOrDefault();

	[Property]
	[Range( 50f, 200f, 10f )]
	public float WalkSpeed { get; set; } = 300f;

	[Property]
	[Range( 25f, 100f, 5f )]
	public float DuckSpeed { get; set; } = 50f;

	[Property]
	[Range( 200f, 500f, 20f )]
	public float JumpStrength { get; set; } = 350f;

	[Sync]
	public bool IsCrouching { get; set; } = false;

	[Property, Category( "Stats" )] public float MaxHealth { get; set; } = 100;
	[Property, Sync, Category( "Stats" )] public float Health { get; set; }
	[Property, Sync, Category( "Stats" )] public float CameraDistance { get; set; }

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

	public Client Client { get; set; }

	[Sync]
	public Angles EyeAngles { get; set; }

	protected override void OnStart()
	{
		if ( IsProxy )
			return;

		Camera.ZFar = 32768f;
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		var wishDirection = Input.AnalogMove.Normal * Rotation.FromYaw( EyeAngles.yaw );
		var isDucking = Input.Down( "Duck" );
		var wishSpeed = isDucking ? DuckSpeed : WalkSpeed;

		Controller.WishVelocity = wishDirection * wishSpeed;
		Controller.Move();

		if ( Input.Pressed( "Jump" ) && Controller.IsOnGround )
		{
			Controller.Punch( Vector3.Up * JumpStrength );
			AnimationHelper?.TriggerJump();
		}
	}

	private void UpdateAnimation()
	{
		if ( !AnimationHelper.IsValid() ) return;

		IsCrouching = Input.Down( "Duck" );


		AnimationHelper.WithWishVelocity( Controller.WishVelocity );
		AnimationHelper.WithVelocity( Controller.Velocity );
		AnimationHelper.DuckLevel = IsCrouching ? 1f : 0f;
		AnimationHelper.IsGrounded = Controller.IsOnGround;
		var lookDir = EyeAngles.ToRotation().Forward * 1024;
		AnimationHelper.WithLook( lookDir, 1, 0.5f, 0.25f );
	}

	private void MouseInput()
	{
		var e = EyeAngles;
		e += Input.AnalogLook;
		e.pitch = e.pitch.Clamp( -90, 90 );
		e.roll = 0.0f;
		EyeAngles = e;
	}

	private void UpdateCamera()
	{
		Camera.Transform.Rotation = EyeAngles.ToRotation();
		Camera.Transform.LocalPosition = Transform.Position + new Vector3( 0, 0, 64 );
	}
	private void UpdateBodyVisibility()
	{
		if ( !AnimationHelper.IsValid() )
			return;

		var renderType = (!IsProxy) ? ModelRenderer.ShadowRenderType.ShadowsOnly : ModelRenderer.ShadowRenderType.On;
		AnimationHelper.Target.RenderType = renderType;

		foreach ( var clothing in AnimationHelper.Target.Components.GetAll<ModelRenderer>( FindMode.InChildren ) )
		{
			if ( !clothing.Tags.Has( "clothing" ) )
				continue;

			clothing.RenderType = renderType;
		}
	}

	protected override void OnPreRender()
	{
		UpdateBodyVisibility();
	}

	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			MouseInput();
			UpdateCamera();
		}

		UpdateAnimation();

		Body.Transform.Rotation = Rotation.Slerp( Body.Transform.Rotation, Rotation.FromYaw( EyeAngles.yaw ), Time.Delta * 5f );
	}


}
