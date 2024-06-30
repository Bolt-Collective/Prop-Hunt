using Facepunch;
using PropHunt;
using Sandbox;
using Sandbox.Citizen;
using Sandbox.Network;
using Sandbox.UI.Panels.Chat;
using Sandbox.Utility;

public class Player : Component
{
	public Inventory Inventory { get; set; }
	public delegate void PlayerDelegate( Player player, Inventory inventory );
	[Property] public PlayerDelegate OnDeath { get; set; }
	[Property] public PlayerDelegate OnJumpEvent { get; set; }
	[Sync] public bool IsGrabbing { get; set; }
	[Property] public Hc1CharacterController characterController { get; set; }
	[Sync] public bool IsDead { get; set; } = false;
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
	[Property, Sync] public bool AbleToMove { get; set; } = true;
	[Property] public CitizenAnimationHelper AnimationHelper { get; set; }
	[Property, Sync] public float CameraDistance { get; set; }
	[Sync] public bool IsCrouching { get; set; }

	[Sync]
	public Angles EyeAngles { get; set; }

	[Sync]
	public bool IsRunning { get; set; }


	[RequireComponent] public TeamComponent TeamComponent { get; private set; }
	public PropShiftingMechanic PropShiftingMechanic { get; set; }
	public float MaxHealth = 100;
	[Sync, Property] public float Health { get; set; }
	[Sync] public bool FreeLooking { get; set; }
	//Going to be used for spectating for unassigned players
	[Sync] public Transform CameraPosWorld { get; set; }
	[Property] public AmmoContainer AmmoContainer { get; set; }
	[Property] public SkinnedModelRenderer BodyRenderer { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		Health = MaxHealth;
	}
	private Rotation oldRotation;
	private Angles oldEyeAngles;

	protected override void OnStart()
	{
		Inventory = Components.Get<Inventory>();
		TeamComponent = Components.Get<TeamComponent>();
		PropShiftingMechanic = Components.Get<PropShiftingMechanic>();
		AmmoContainer = Components.Get<AmmoContainer>();
		if ( IsProxy ) return;
		if ( PropHuntManager.Instance.OnGoingRound == true )
		{
			TakeDamage( 1000 );
		}
	}
	public void FreeLook()
	{
		var cam = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
		if ( cam is null || Health <= 0 ) return;

		if ( Input.Pressed( "attack2" ) )
		{
			oldRotation = cam.Transform.Rotation;
			oldEyeAngles = EyeAngles;
		}

		if ( Input.Down( "attack2" ) )
		{
			FreeLooking = true;
			var camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault( x => x.IsMainCamera );
			var bodyRenderer = Body.Components.Get<SkinnedModelRenderer>();
			camera.FieldOfView = Preferences.FieldOfView;
			var lookDirection = EyeAngles.ToRotation();
			var center = PropShiftingMechanic.IsProp ? Body.GetBounds().Center : Transform.Position + Vector3.Up * 64;
			if ( CameraDistance != 0 )
			{

				var tr = Scene.Trace.Ray( center, center - (EyeAngles.Forward * CameraDistance) ).WithoutTags( "player", "barrier" ).Run();
				if ( tr.Hit )
				{
					if ( PropShiftingMechanic.IsProp )
					{
						camera.Transform.Position = Vector3.Lerp( camera.Transform.Position, tr.EndPosition + tr.Normal * 2 + Vector3.Up * 10, Time.Delta * 50 );
					}
					else
					{
						if ( PropShiftingMechanic.IsProp )
						{
							camera.Transform.Position = Vector3.Lerp( camera.Transform.Position, center - (EyeAngles.Forward * CameraDistance) + Vector3.Up * 10, Time.Delta * 50 );
						}
						else
						{
							camera.Transform.Position = center - (EyeAngles.Forward * CameraDistance) + Vector3.Up * 10;
						}
					}
				}
				else
				{
					camera.Transform.Position = center - (EyeAngles.Forward * CameraDistance) + Vector3.Up * 10;
				}
			}
			else
			{
				var targetPos = PropShiftingMechanic.IsProp ? center : Transform.Position + Vector3.Up * (IsCrouching ? 32 : 64);
				if ( PropShiftingMechanic.IsProp )
				{
					camera.Transform.Position = Vector3.Lerp( camera.Transform.Position, targetPos, Time.Delta * 50 );
				}
				else
				{
					camera.Transform.Position = targetPos;
				}
			}

			if ( PropShiftingMechanic.IsProp && CameraDistance != 0 )
			{
				camera.Transform.Rotation = Rotation.Slerp( camera.Transform.Rotation, lookDirection, Time.Delta * 50 );
			}
			else
			{
				camera.Transform.Rotation = lookDirection;
			}

		}

		if ( Input.Released( "attack2" ) )
		{
			cam.Transform.Rotation = Rotation.Slerp( cam.Transform.Rotation, oldRotation, Time.Delta * 10.0f );
			EyeAngles = oldEyeAngles;
			FreeLooking = false;
		}
	}
	//Used with the IUse interface
	public void UseItems()
	{
		Log.Info( "Using items" );
		var tr = Scene.Trace.Ray( Scene.Camera.ScreenNormalToRay( 0.5f ), 500 )
			.IgnoreGameObject( Body )
			.Run();
		if ( tr.Hit )
		{
			if ( tr.GameObject.Components.TryGet<IUse>( out var use, FindMode.EverythingInSelfAndParent ) )
			{
				use.OnUse( this );
			}
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
		if ( IsProxy ) return;
		var ee = EyeAngles;
		ee += Input.AnalogLook;
		ee.roll = 0;
		ee.pitch = ee.pitch.Clamp( -89, 89 );
		EyeAngles = ee;
	}
	public void CameraPosition()
	{
		var camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault( x => x.IsMainCamera );
		var bodyRenderer = Body.Components.Get<SkinnedModelRenderer>();
		if ( bodyRenderer is null ) return;
		camera.FieldOfView = Preferences.FieldOfView;
		var lookDirection = EyeAngles.ToRotation();
		var center = PropShiftingMechanic.IsProp ? bodyRenderer.Bounds.Center : Transform.Position + Vector3.Up * 64;
		var localCameraPos = Transform.World.PointToLocal( camera.Transform.Position );

		//Trace to see if the camera is inside a wall
		if ( CameraDistance != 0 )
		{
			var tr = Scene.Trace.Ray( center, center - (EyeAngles.Forward * CameraDistance) ).WithoutTags( "player", "barrier" ).Run();
			if ( tr.Hit )
			{
				if ( PropShiftingMechanic.IsProp )
				{
					camera.Transform.Position = Vector3.Lerp( camera.Transform.Position, tr.EndPosition + tr.Normal * 2 + Vector3.Up * 10, Time.Delta * 50 );
				}
				else
				{
					camera.Transform.Position = tr.EndPosition + tr.Normal * 2 + Vector3.Up * 10;
				}
			}
			else
			{
				if ( PropShiftingMechanic.IsProp )
				{
					camera.Transform.Position = Vector3.Lerp( camera.Transform.Position, center - (EyeAngles.Forward * CameraDistance) + Vector3.Up * 10, Time.Delta * 50 );
				}
				else
				{
					camera.Transform.Position = center - (EyeAngles.Forward * CameraDistance) + Vector3.Up * 10;
				}
			}
		}
		else
		{
			var targetPos = PropShiftingMechanic.IsProp ? center : Transform.Position + Vector3.Up * (IsCrouching ? 32 : 64);
			if ( PropShiftingMechanic.IsProp )
			{
				camera.Transform.Position = Vector3.Lerp( camera.Transform.Position, targetPos, Time.Delta * 50 );
			}
			else
			{
				camera.Transform.Position = targetPos;
			}
		}

		if ( PropShiftingMechanic.IsProp && CameraDistance != 0 )
		{
			camera.Transform.Rotation = Rotation.Slerp( camera.Transform.Rotation, lookDirection, Time.Delta * 50 );
		}
		else
		{
			camera.Transform.Rotation = lookDirection;
		}
	}
	public void Animations( Hc1CharacterController cc )
	{
		if ( AnimationHelper is not null && AbleToMove && !PropShiftingMechanic.IsProp )
		{
			AnimationHelper.WithVelocity( cc.Velocity );
			AnimationHelper.WithWishVelocity( WishVelocity );
			AnimationHelper.IsGrounded = cc.IsOnGround;
			if ( !FreeLooking )
			{
				AnimationHelper.WithLook( EyeAngles.Forward, 1, 1, 1.0f );
			}
			AnimationHelper.MoveStyle = IsRunning ? CitizenAnimationHelper.MoveStyles.Run : CitizenAnimationHelper.MoveStyles.Walk;
			AnimationHelper.DuckLevel = IsCrouching ? 1 : 0;
		}
	}
	protected override void OnUpdate()
	{
		if ( PropHuntManager.Instance.RoundState == GameState.Preparing && TeamComponent.TeamName == Team.Hunters.ToString() )
		{
			Scene.GetAllComponents<BlindPostprocess>().FirstOrDefault().UseBlind = true;
		}
		if ( !IsProxy )
		{
			EyeInput();
			var blind = Scene.GetAllComponents<BlindPostprocess>().FirstOrDefault();
			if ( TeamComponent.TeamName == Team.Hunters.ToString() && PropHuntManager.Instance.RoundState == GameState.Starting )
			{
				blind.UseBlind = true;
			}
			else
			{
				blind.UseBlind = false;
			}
			if ( Health > 0 && TeamComponent.TeamName != Team.Unassigned.ToString() )
			{
				AbleToMove = true;
			}
			if ( AbleToMove && TeamComponent.TeamName != Team.Unassigned.ToString() )
			{
				UpdateCrouch();
				if ( Input.Pressed( "use" ) )
				{
					UseItems();
				}
				FreeLook();

				ChangeDistance();
				var eyePos = Eye.Transform.Position;
				eyePos = Body.Transform.Position + Vector3.Up * (IsCrouching ? 32 : 64);
				Eye.Transform.Position = eyePos;
				Eye.Transform.Rotation = EyeAngles.ToRotation();
			}

			CameraPosition();
			characterController.Height = 64;
			characterController.Radius = 16;
			CameraPosWorld = Scene.GetAllComponents<CameraComponent>().FirstOrDefault( x => x.IsMainCamera ).Transform.World;
			IsRunning = Input.Down( "Run" );
		}
		else
		{
			//Have to do it again here, because if I do it outside of the proxy, the interp is weird
			var eyePos = Eye.Transform.Position;
			eyePos = Body.Transform.Position + Vector3.Up * (IsCrouching ? 32 : 64);
			Eye.Transform.Position = eyePos;
			Eye.Transform.Rotation = EyeAngles.ToRotation();
		}

		var cc = GameObject.Components.Get<Hc1CharacterController>();
		if ( cc is null ) return;
		Animations( cc );
		if ( !FreeLooking )
		{
			Body.Transform.Rotation = Rotation.Slerp( Body.Transform.Rotation, new Angles( 0, EyeAngles.yaw, 0 ).ToRotation(), Time.Delta * 10.0f );
		}


	}
	public void ChangeDistance()
	{
		if ( Input.MouseWheel != 0 )
		{
			CameraDistance -= Input.MouseWheel.y * 10;
		}
		CameraDistance = CameraDistance.Clamp( 0, 1000 );
	}
	private void UpdateBodyVisibility()
	{
		if ( AnimationHelper is null || BodyRenderer is null || Health < 0 || AnimationHelper.Target is null )
			return;

		var renderType = (!IsProxy) && CameraDistance == 0 ? ModelRenderer.ShadowRenderType.ShadowsOnly : ModelRenderer.ShadowRenderType.On;
		AnimationHelper.Target.RenderType = renderType;
		foreach ( var clothing in AnimationHelper?.Target?.Components?.GetAll<ModelRenderer>( FindMode.InChildren ) )
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
		if ( !IsProxy )
		{
			OnJumpEvent?.Invoke( this, Inventory );
		}
	}

	protected override void OnPreRender()
	{
		UpdateBodyVisibility();
	}
	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		if ( AbleToMove && TeamComponent.TeamName != Team.Unassigned.ToString() )
		{
			BuildWishVelocity();

			var modelCollider = Body.Components.Get<ModelCollider>();

			if ( modelCollider is not null )
				modelCollider.Enabled = true;

			var cc = characterController;

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
		else
		{
			var modelCollider = Body.Components.Get<ModelCollider>();

			if ( modelCollider is not null )
				modelCollider.Enabled = false;

			Scene.Camera.Transform.Rotation = EyeAngles.ToRotation();
			Scene.Camera.Transform.Position = GameObject.Transform.Position + Vector3.Up * 64;
			GameObject.Transform.Position += new Angles( EyeAngles.pitch, EyeAngles.yaw, 0 ).ToRotation() * Input.AnalogMove * 1000 * Time.Delta;
		}
	}
	[Button( "Network Refresh" )]
	public void Refresh()
	{
		GameObject.Network.Refresh();
	}
	public bool CanUncrouch()
	{
		var tr = characterController.TraceDirection( Vector3.Up * 32 );
		return !tr.Hit;
	}

	public void UpdateCrouch()
	{
		if ( PropShiftingMechanic.IsProp ) return;
		if ( !Input.Down( "duck" ) )
		{
			if ( !CanUncrouch() ) return;
			characterController.Height = 64;
			IsCrouching = false;
		}
		else
		{
			characterController.Height = 32;
			IsCrouching = true;
		}
	}
	public void BuildWishVelocity()
	{
		var rot = EyeAngles.ToRotation();

		WishVelocity = rot * Input.AnalogMove;
		WishVelocity = WishVelocity.WithZ( 0 );

		if ( !WishVelocity.IsNearZeroLength ) WishVelocity = WishVelocity.Normal;

		if ( Input.Down( "Run" ) ) WishVelocity *= PropShiftingMechanic.IsProp ? 220 : 320.0f;
		else WishVelocity *= 110.0f;
	}

	[Broadcast]
	public void TakeDamage( float damage )
	{
		if ( IsDead ) return;

		Health -= damage;
		if ( Health <= 0 )
		{
			Health = 0;
			IsDead = true;
			Death( GameObject.Id );
		}
	}
	[Broadcast]
	void Death( Guid caller )
	{
		var player = Scene.Directory.FindByGuid( caller ).Components.Get<Player>();
		player.Health = 0;
		player.DisableBody();
		player.TeamComponent.ChangeTeam( Team.Unassigned );
		ChatBox.Instance.AddMessage( "", $"{Network.OwnerConnection.DisplayName} fucking died 💀" );
		player.OnDeath?.Invoke( this, GameObject.Components.Get<Inventory>() );
		player.AnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.None;
		player.Inventory.Clear();
		player.AbleToMove = false;
	}

	[Broadcast]
	public void Respawn( Guid caller )
	{
		var player = Scene.Directory.FindByGuid( caller ).Components.Get<Player>();
		player.ResetStats( caller );
		player.AbleToMove = true;
		player.IsDead = false;
	}
	[Broadcast]
	public void DisableBody()
	{
		foreach ( var cloth in Body.GetAllObjects( true ).Where( c => c.Tags.Has( "clothing" ) ) )
		{
			cloth.Enabled = false;
		}
		if ( Body is not null )
		{
			Body.Enabled = false;
		}

		if ( PropShiftingMechanic.MapCollider is not null )
		{
			PropShiftingMechanic.MapCollider.Enabled = false;
		}
		if ( PropShiftingMechanic.PropsCollider is not null )
		{
			PropShiftingMechanic.MapCollider.Enabled = false;
		}
	}
	[Broadcast]
	public void ResetStats( Guid caller )
	{
		var player = Scene.Directory.FindByGuid( caller ).Components.Get<Player>();
		player.AmmoContainer?.ResetAmmo();
		player.Health = 100;
		player.IsCrouching = false;
		player.IsRunning = false;
		player.FreeLooking = false;
		player.CameraDistance = 0;
		player.IsGrabbing = false;
		player.Body.Enabled = true;
		player.AbleToMove = true;
		if ( player.PropShiftingMechanic.MapCollider is not null )
		{
			player.PropShiftingMechanic.MapCollider.Enabled = true;
		}
		if ( player.PropShiftingMechanic.PropsCollider is not null )
		{
			player.PropShiftingMechanic.MapCollider.Enabled = true;
		}
		player.PropShiftingMechanic.ExitProp();
		player.AnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.None;
	}

	[ConCmd( "kill" )]
	public static void Kill()
	{
		if ( Local is null || PropHuntManager.Instance.RoundState == GameState.WaitingForPlayers ) return;
		Local.TakeDamage( 100 );
		Local.Network.Refresh();
	}

	[ConCmd( "reset" )]
	public static void ResetPlayers()
	{
		foreach ( var player in Game.ActiveScene.GetAllComponents<Player>() )
		{
			player.TakeDamage( 10000 );
		}
	}

	[Broadcast]
	public void HunterStart()
	{
		if ( IsProxy ) return;
		Inventory.SpawnStartingItems();
		AbleToMove = false;
		if ( Scene.GetAllComponents<CameraComponent>().FirstOrDefault( x => x.IsMainCamera ).Components.TryGet<BlindPostprocess>( out var blind ) )
		{
			Log.Info( "Blinding hunters" );
			blind.UseBlind = true;
		}
	}

	[Broadcast]
	public void HunterUnblind()
	{
		if ( IsProxy ) return;
		AbleToMove = true;
		if ( Scene.GetAllComponents<CameraComponent>().FirstOrDefault( x => x.IsMainCamera ).Components.TryGet<BlindPostprocess>( out var blind ) )
		{
			blind.UseBlind = false;
		}
	}
	[ActionGraphNode( "Get Proxy Component" )]
	public static void GetProxyComponent<T>( GameObject CurrentGameObject, out T Component, out bool Result ) where T : Component
	{
		Component = CurrentGameObject.Components.Get<T>();
		Component = Game.ActiveScene.GetAllComponents<T>().FirstOrDefault( x => !CurrentGameObject.Network.IsProxy );
		Result = Component is not null;
	}
}
