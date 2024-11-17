using Sandbox.Citizen;
public class Player : Component, Component.IDamageable
{
	[RequireComponent]
	public ShrimpleCharacterController Controller { get; set; }

	[RequireComponent]
	public CitizenAnimationHelper AnimationHelper { get; set; }

	[Property]
	public GameObject Body { get; set; }
	public CameraComponent Camera => Scene.GetAll<CameraComponent>().FirstOrDefault( x => x.IsMainCamera );

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

	[Property] public float CameraDistance { get; set; } = 0f;
	public bool IsFirstPerson => CameraDistance == 0f;

	/// <summary>
	/// Constructs a ray using the camera's GameObject
	/// </summary>
	public Ray AimRay
	{
		get
		{
			if ( Camera.IsValid() )
			{
				return new( Camera.WorldPosition + Camera.WorldRotation.Forward, Camera.WorldRotation.Forward );
			}

			return new( WorldPosition + Vector3.Up * 64f, EyeAngles.ToRotation().Forward );
		}
	}

	[Broadcast]
	public void Respawn()
	{
		if ( IsProxy )
			return;

		Health = MaxHealth;
	}

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

		if ( !Input.Down( "Menu" ) )
		{
			CameraDistance = Math.Clamp( CameraDistance - Input.MouseWheel.y * 32f, 0f, 256f );
		}
	}

	private void UpdateCamera()
	{
		if (!IsValid)
			return;
		
		/*		Camera.WorldRotation = EyeAngles.ToRotation();
				Camera.LocalPosition = WorldPosition + new Vector3( 0, 0, 64 );*/

		var targetCameraPos = WorldPosition + new Vector3( 0, 0, 64 );

		if ( CameraDistance > 0 )
		{
			var tr = Scene.Trace.Ray( targetCameraPos, targetCameraPos + (EyeAngles.Forward * -CameraDistance) )
				.WithoutTags( "player", "trigger" )
				.Run();

			if ( tr.Hit )
			{
				targetCameraPos = tr.HitPosition.LerpTo( targetCameraPos, RealTime.Delta * 5 );
			}
			else
			{
				targetCameraPos = tr.EndPosition;
			}
		}

		Camera.LocalPosition = targetCameraPos;
		Camera.WorldRotation = EyeAngles.ToRotation();
		Camera.FieldOfView = Preferences.FieldOfView;
	}
	private void UpdateBodyVisibility()
	{
		if ( !AnimationHelper.IsValid() )
			return;

		var renderType = (!IsProxy && IsFirstPerson) ? ModelRenderer.ShadowRenderType.ShadowsOnly : ModelRenderer.ShadowRenderType.On;
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

		Body.WorldRotation = Rotation.Slerp( Body.WorldRotation, Rotation.FromYaw( EyeAngles.yaw ), Time.Delta * 5f );
	}


	public void OnDamage( in DamageInfo damage )
	{
		if ( IsProxy ) return;
		if ( Health <= 0 ) return;

		Health -= damage.Damage;

		if ( Health <= 0 )
		{
			Health = 0;
			Death();
		}
	}
	
	void Death()
	{
		//CreateRagdoll();
		//CreateRagdollAndGhost();

		GameObject.Destroy();
	}
}
