﻿using Facepunch;
using PropHunt;
using Sandbox.Citizen;

public class Player : Component
{
	//Required components
	[RequireComponent] public Inventory Inventory { get; set; }
	[RequireComponent] public TeamComponent TeamComponent { get; private set; }
	[RequireComponent] public PropShiftingMechanic PropShiftingMechanic { get; set; }
	//Actions
	public delegate void PlayerDelegate( Player player, Inventory inventory );
	[Property, KeyProperty, Category( "Player Actions" )] public PlayerDelegate OnDeath { get; set; }
	[Property, KeyProperty, Category( "Player Actions" )] public PlayerDelegate OnJumpEvent { get; set; }
	//Syncs
	[Sync] public string CurrentMapVote { get; set; }
	[Sync] public bool AbleToMove { get; set; } = true;
	[Sync] public Angles EyeAngles { get; set; }
	[Sync] public bool IsDead { get; set; } = false;
	[Sync] public bool IsCrouching { get; set; }
	[Sync] public bool IsRunning { get; set; }
	[Sync] public bool FreeLooking { get; set; }
	[Sync] public Transform CameraPosWorld { get; set; }
	[Sync] public bool IsGrabbing { get; set; }
	[Sync] public Rotation oldRotation { get; set; }
	[Sync] public Angles oldEyeAngles { get; set; }
	[Sync] public bool AbleToVote { get; set; } = true;
	[Sync] public bool ShouldFreeLook { get; set; } = false;
	[Sync] public int SpectateIndex { get; set; } = 0;
	[Sync] public bool SnappingToPlayer { get; set; } = false;
	//Stats
	[Property, Category( "Stats" )] public float MaxHealth { get; set; } = 100;
	[Sync, Property, Category( "Stats" )] public float Health { get; set; }
	[Property, Sync, Category( "Stats" )] public float CameraDistance { get; set; }
	//Refrences
	[Property, Category( "Refrences" )] public CitizenAnimationHelper AnimationHelper { get; set; }
	[Property, Category( "Refrences" )] public AmmoContainer AmmoContainer { get; set; }
	[Property, Category( "Refrences" )] public SkinnedModelRenderer BodyRenderer { get; set; }
	[Property, Category( "Refrences" )] public GameObject Body { get; set; }
	[Property, Category( "Refrences" )] public GameObject Eye { get; set; }
	[Property, Category( "Refrences" )] public Hc1CharacterController characterController { get; set; }
	public Vector3 WishVelocity { get; private set; }
	public Player CurrentlySpectatedPlayer { get; set; }
	public bool IsSpectator { get; set; } = false;
	[Property, Category( "Other" )] public SceneFile Menu { get; set; }

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

	[Button( "Kick", "close" ), Category( "Utility" )]
	public void Kick()
	{
		if ( IsProxy ) return;
		Game.ActiveScene.Load( Menu );
	}

	[Button( "Network Refresh", "refresh" ), Category( "Utility" )]
	public void Refresh()
	{
		GameObject.Network.Refresh();
	}

	protected override void OnAwake()
	{
		Health = MaxHealth;
	}

	protected override void OnStart()
	{
		Inventory = Components.Get<Inventory>();
		TeamComponent = Components.Get<TeamComponent>();
		PropShiftingMechanic = Components.Get<PropShiftingMechanic>();
		AmmoContainer = Components.Get<AmmoContainer>();
		if ( IsProxy ) return;
		if ( PropHuntManager.Instance?.OnGoingRound == true && PropHuntManager.Instance is not null )
		{
			TakeDamage( 1000, false );
		}
	}

	public void FreeLook()
	{
		var cam = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
		if ( cam is null || Health <= 0 ) return;
		if ( Player.Local.CameraDistance == 0 )
		{
			ShouldFreeLook = false;
			FreeLooking = false;
			return;
		}

		if ( Input.Pressed( "attack2" ) )
		{
			if ( !ShouldFreeLook )
			{
				oldRotation = cam.Transform.Rotation;
				oldEyeAngles = EyeAngles;
				FreeLooking = true;
			}
			else
			{
				cam.Transform.Rotation = Rotation.Slerp( cam.Transform.Rotation, oldRotation, Time.Delta * 10.0f );
				EyeAngles = oldEyeAngles;
				FreeLooking = false;
			}
			ShouldFreeLook = !ShouldFreeLook;
		}

		if ( ShouldFreeLook )
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
		else
		{
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

	public void SnapToPlayer()
	{
		if ( TeamComponent.TeamName != Team.Unassigned.ToString() || !PropHuntManager.Instance.OnGoingRound || PropHuntManager.Instance.RoundState == GameState.WaitingForPlayers )
		{
			SnappingToPlayer = false;
			CurrentlySpectatedPlayer = null;
			return;
		}
		var listOfPlayers = Scene.GetAllComponents<Player>().Where( x => x.TeamComponent.TeamName != Team.Unassigned.ToString() && !x.IsDead ).ToList();
		if ( listOfPlayers.Count == 0 || listOfPlayers is null )
		{
			SnappingToPlayer = false;
			CurrentlySpectatedPlayer = null;
			return;
		}
		if ( Input.Down( "forward" ) || Input.Down( "backward" ) || Input.Down( "right" ) || Input.Down( "left" ) )
		{
			SnappingToPlayer = false;
		}
		if ( Input.Pressed( "attack1" ) )
		{
			SpectateIndex++;
			if ( SpectateIndex >= listOfPlayers.Count )
			{
				SpectateIndex = 0;
			}
			SnappingToPlayer = true;
		}
		if ( Input.Pressed( "attack2" ) )
		{
			SpectateIndex--;
			if ( SpectateIndex < 0 )
			{
				SpectateIndex = listOfPlayers.Count - 1;
			}
			SnappingToPlayer = true;
		}
		if ( SpectateIndex >= 0 && SpectateIndex < listOfPlayers.Count )
		{
			var player = listOfPlayers[SpectateIndex];
			CurrentlySpectatedPlayer = player;
		}
		else
		{
			CurrentlySpectatedPlayer = null;
			return;
		}

		if ( CurrentlySpectatedPlayer is null )
		{
			SnappingToPlayer = false;
			CurrentlySpectatedPlayer = null;
			return;
		}

		if ( SnappingToPlayer && CurrentlySpectatedPlayer is not null )
		{
			var target = CurrentlySpectatedPlayer.Body.Transform.Position + CurrentlySpectatedPlayer.Body.Transform.Rotation.Up * 32 + CurrentlySpectatedPlayer.Body.Transform.Rotation.Backward * 100;
			Transform.Position = target;
			var lookAtRot = Rotation.LookAt( CurrentlySpectatedPlayer.Eye.Transform.Position - Scene.Camera.Transform.Position );
			var lerpYaw = Rotation.Lerp( Scene.Camera.Transform.Rotation, lookAtRot, Time.Delta * 50 );
			var lerpPitch = Rotation.Lerp( Scene.Camera.Transform.Rotation, lookAtRot, Time.Delta * 10 );
			Scene.Camera.Transform.Rotation = new Angles( lerpPitch.Pitch(), lerpYaw.Yaw(), 0 ).ToRotation();
		}
		else if ( CurrentlySpectatedPlayer is null )
		{
			SnappingToPlayer = false;
		}
	}

	protected override void OnEnabled()
	{
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
			SnapToPlayer();

			var blind = Scene.GetAllComponents<BlindPostprocess>()?.FirstOrDefault();

			if ( TeamComponent.TeamName == Team.Hunters.ToString() && PropHuntManager.Instance.RoundState == GameState.Starting && blind is not null )
			{
				blind.UseBlind = true;
			}
			else if ( blind is not null )
			{
				blind.UseBlind = false;
			}

			if ( Health > 0 && TeamComponent.TeamName != Team.Unassigned.ToString() )
			{
				AbleToMove = true;
				IsSpectator = false;
			}

			if ( AbleToMove && TeamComponent.TeamName != Team.Unassigned.ToString() )
			{
				//Input related methods
				UpdateCrouch();
				ToggleFirstPerson();
				FreeLook();
				ChangeDistance();
				if ( Input.Pressed( "use" ) )
				{
					UseItems();
				}
				var eyePos = Eye.Transform.Position;
				eyePos = Body.Transform.Position + Vector3.Up * (IsCrouching ? 32 : 64);
				Eye.Transform.Position = eyePos;
				Eye.Transform.Rotation = EyeAngles.ToRotation();
			}

			CameraPosition();
			UpdatePlayerControllerRadius( GameObject );

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

			IsSpectator = true;
		}

		var cc = GameObject.Components.Get<Hc1CharacterController>();
		if ( cc is null ) return;

		Animations( cc );

		if ( !FreeLooking )
		{
			Body.Transform.Rotation = Rotation.Slerp( Body.Transform.Rotation, new Angles( 0, EyeAngles.yaw, 0 ).ToRotation(), Time.Delta * 10.0f );
		}
	}

	public void ToggleFirstPerson()
	{
		if ( Input.Pressed( "toggle3rdperson" ) )
		{
			CameraDistance = CameraDistance == 0 ? 150 : 0;
		}
	}

	[Broadcast]
	public void UpdatePlayerControllerRadius( GameObject caller )
	{
		var cc = caller.Components.Get<Hc1CharacterController>();
		var player = caller.Components.Get<Player>();
		if ( characterController is null || player is null ) return;
		var radius = cc.Radius;
		radius = Math.Min( player.BodyRenderer.Bounds.Size.x, player.BodyRenderer.Bounds.Size.y ) / 2;
		radius = Math.Clamp( radius, 2, 16 );
		cc.Radius = radius;

		if ( player.TeamComponent.TeamName == Team.Props.ToString() )
		{
			cc.Height = player.BodyRenderer.Bounds.Size.z;
		}
		else
		{
			cc.Height = player.IsCrouching ? 32 : 64;
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

	[Broadcast]
	public void UpdateColliders( GameObject caller )
	{
		var playerGb = caller;
		if ( playerGb is null ) return;

		var player = playerGb.Components.Get<Player>();
		if ( player is null ) return;

		var colliders = player.Body.Components.GetAll<Collider>( FindMode.EverythingInDescendants ).ToList();

		if ( AbleToMove && TeamComponent.TeamName != Team.Unassigned.ToString() )
		{
			foreach ( Collider collider in colliders )
			{
				collider.Enabled = true;
			}
		}
		else
		{
			foreach ( Collider collider in colliders )
			{
				collider.Enabled = false;
			}
		}
	}

	public void CheckForKillBounds()
	{
		if ( !Scene.GetAllComponents<MapChanger>().FirstOrDefault().IsMapLoaded || !PropHuntManager.Instance.OnGoingRound || TeamComponent.TeamName == Team.Unassigned.ToString() || PropHuntManager.Instance.RoundState == GameState.WaitingForPlayers || PropHuntManager.Instance.RoundState == GameState.Starting || PropHuntManager.Instance.RoundState == GameState.Preparing ) return;
		var mapInstance = Scene.GetAll<MapInstance>()?.FirstOrDefault();

		var bounds = mapInstance.Bounds;

		if ( !mapInstance.IsValid )
			return;

		if ( Transform.Position.z < bounds.Mins.z - 1000 )
		{
			TakeDamage( 100 );
		}

		if ( Transform.Position.z > bounds.Maxs.z + 1000 )
		{
			TakeDamage( 100 );
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		CheckForKillBounds();
		UpdateColliders( GameObject );

		if ( TeamComponent.TeamName != Team.Hunters.ToString() )
		{
			ClearHoldType( GameObject );
		}

		if ( AbleToMove && TeamComponent.TeamName != Team.Unassigned.ToString() )
		{
			BuildWishVelocity();

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
			if ( !SnappingToPlayer )
			{
				Scene.Camera.Transform.Rotation = EyeAngles.ToRotation();
			}

			Scene.Camera.Transform.Position = GameObject.Transform.Position + Vector3.Up * 64;
			var runSpeed = Input.Down( "run" ) ? 2000 : 500;
			GameObject.Transform.Position += new Angles( EyeAngles.pitch, EyeAngles.yaw, 0 ).ToRotation() * Input.AnalogMove * runSpeed * Time.Delta;
		}
	}

	public bool CanUncrouch()
	{
		var tr = characterController.TraceDirection( Vector3.Up * 16 );
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
	public void TakeDamage( float damage, bool deathMessage = true )
	{
		if ( IsDead ) return;

		Health -= damage;

		if ( Health <= 0 && !IsDead )
		{
			Health = 0;
			IsDead = true;
			Death( GameObject, deathMessage );
		}
	}

	[Broadcast]
	void Death( GameObject caller, bool deathMessage )
	{
		if ( IsSpectator ) return;

		var player = caller.Components.Get<Player>();
		if ( player is null ) return;

		player.Health = 0;

		player.DisableBody();

		player.TeamComponent.ChangeTeam( Team.Unassigned );
		player.OnDeath?.Invoke( this, GameObject.Components.Get<Inventory>() );

		ClearHoldType( caller );

		player.Inventory.Clear();
		player.AbleToMove = false;
		player.GameObject.Components.Get<FlashlightComponent>().Flashlight.Enabled = false;

		if ( deathMessage )
		{
			KillFeed.BroadcastKillFeedEvent( player.Network.Owner.DisplayName, Color.Black );
		}
	}

	[Broadcast]
	public void ClearHoldType( GameObject caller )
	{
		var player = caller.Components.Get<Player>();
		if ( player is null ) return;
		player.AnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.None;
	}


	[Broadcast]
	public void Respawn( GameObject caller )
	{
		var playerGb = caller;
		if ( playerGb is null ) return;

		var player = playerGb.Components.Get<Player>();
		if ( player is null ) return;

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
	}

	[Broadcast]
	public void ResetStats( GameObject caller )
	{
		var playerGb = caller;
		if ( playerGb is null ) return;

		var player = playerGb.Components.Get<Player>();
		if ( player is null ) return;

		player?.AmmoContainer?.ResetAmmo();

		player.IsCrouching = false;
		player.IsRunning = false;
		player.FreeLooking = false;
		player.CameraDistance = 0;
		player.IsGrabbing = false;
		player.Body.Enabled = true;
		player.AbleToMove = true;
		player?.PropShiftingMechanic?.ExitProp();

		player.AnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.None;

		if ( player.TeamComponent.TeamName == Team.Hunters.ToString() )
		{
			player.Health = (float)(PropHuntManager.Instance?.LobbySettings?.HunterHealth);
		}
		else if ( player.TeamComponent.TeamName == Team.Props.ToString() )
		{
			player.Health = (float)(PropHuntManager.Instance?.LobbySettings?.PropHealth);
		}
		else
		{
			player.Health = 100;
		}
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

		Inventory?.SpawnStartingItems();
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
