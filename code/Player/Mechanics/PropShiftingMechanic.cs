

using PropHunt;
using Sandbox.Utility;

public class PropShiftingMechanic : Component
{
	public TeamComponent TeamComponent { get; set; }
	public delegate void PropShiftingDelegate( PropShiftingMechanic propShiftingMechanic, Model PropModel, Player player, Inventory inventory );
	[Property] public PropShiftingDelegate OnPropShift { get; set; }
	[Property] public ModelCollider Collider { get; set; }
	[Property, Sync] public bool IsProp { get; set; } = false;
	[Sync] public string ModelPath { get; set; }
	[Property] public int PreviousHealth { get; set; }
	public TauntComponent TauntComponent { get; set; }
	protected override void OnStart()
	{
		TeamComponent = Scene.GetAllComponents<TeamComponent>().FirstOrDefault( x => !x.IsProxy );
		TauntComponent = Scene.GetAllComponents<TauntComponent>().FirstOrDefault( x => !x.IsProxy );
		PreviousHealth = 100;
		if ( !IsProxy )
		{
			ModelPath = Player.Local.BodyRenderer?.Model.ResourcePath;
		}
	}
	[Property, Sync] TimeSince timeSinceLastMove { get; set; }
	[Property, Sync] TimeSince timeSinceLastForceTaunt { get; set; }
	[Property, Sync] Vector3 LastPosition { get; set; }
	[Property, Sync] float distanceMoved { get; set; }
	protected override void OnUpdate()
	{
		if ( IsProxy ) return;
		if ( PropHuntManager.Instance.RoundState != GameState.Started )
		{
			ResetForceTauntValues();
		}
		if ( TeamComponent.TeamName != Team.Props.ToString() ) return;
		distanceMoved = Vector3.DistanceBetween( LastPosition, Player.Local.Transform.Position );
		//Set time last moved based on the wish velocity of the player
		if ( Player.Local.WishVelocity.Length != 0 )
		{
			timeSinceLastMove = 0;
		}
		if ( distanceMoved > 5 )
		{
			LastPosition = Player.Local.Transform.Position;
		}
		Log.Info( distanceMoved + " " + timeSinceLastMove );
		//If the player has not moved for 30 seconds or moved a bit, play a random taunt
		// Introduce a cooldown for taunts to prevent spamming
		if ( timeSinceLastMove > 60 && distanceMoved < 5 && CanPlayTaunt() )
		{
			Log.Info( "Playing taunt" + timeSinceLastForceTaunt + " " + timeSinceLastMove );
			TauntComponent?.PlayRandomTaunt();
			//Have to make it a method since if I change one, the rest are not going to be changed
			ResetForceTauntValues();
		}

		if ( Input.Pressed( "View" ) )
		{
			ExitProp();
		}
		var pc = Components.Get<Player>();
		if ( ModelPath is null ) return;

		if ( Input.Pressed( "Use" ) )
		{
			ShiftIntoProp();
		}
	}
	private void ResetForceTauntValues()
	{
		LastPosition = Player.Local.Transform.Position;
		timeSinceLastMove = 0;
		timeSinceLastForceTaunt = 0;
	}


	// Method to check if a taunt can be played based on a cooldown
	private bool CanPlayTaunt()
	{
		return timeSinceLastForceTaunt > TauntComponent.TauntCooldown;
	}
	public void ExitProp()
	{
		if ( !Player.Local.AbleToMove || IsProxy ) return;
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
		ModelPath = "models/citizen/citizen.vmdl_c";
		pcModel.Model = Model.Load( ModelPath );
		pcModel.Tint = Color.White;
		Collider.Model = Model.Load( ModelPath );
		pcModel.GameObject.Transform.Scale = Vector3.One;
		pcModel.GameObject.Transform.Scale = Vector3.One;
		Player.Local.Health = PreviousHealth;
		if ( clothes.Any() )
		{
			foreach ( var cloth in clothes )
			{
				cloth.Enabled = true;
				cloth.Network.Refresh();
			}
		}
		Player.Local.Body.Network.Refresh();
		IsProp = false;

	}
	private void ShiftIntoProp()
	{
		if ( !Player.Local.AbleToMove || IsProxy ) return;
		var pc = Components.Get<Player>();
		var lookDir = pc.EyeAngles.ToRotation();
		var eyePos = Transform.Position + Vector3.Up * 64;

		var tr = Scene.Trace.Ray( Scene.Camera.Transform.Position, Scene.Camera.Transform.Position + lookDir.Forward * 300 + Player.Local.CameraDistance )
			.IgnoreGameObject( Player.Local.Body )
			.Run();

		//Gizmo.Draw.LineSphere( tr.HitPosition, 16 );

		if ( !tr.Hit ) return;
		var propModel = tr.GameObject.Components.Get<Prop>( FindMode.EverythingInSelfAndDescendants )?.Model ?? tr.GameObject.Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndDescendants )?.Model;
		var propRenderer = tr.GameObject.Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndDescendants );
		if ( propModel is null ) return;
		if ( propModel.ResourcePath == "models/citizen/citizen.vmdl_c" ) return;
		ModelPath = propModel.ResourcePath;
		Player.Local.Body.Transform.Scale = propRenderer.Transform.Scale;
		Player.Local.BodyRenderer.Tint = propRenderer.Tint;
		Player.Local.BodyRenderer.Model = Model.Load( ModelPath );
		Collider.Model = Model.Load( ModelPath );
		IsProp = ModelPath == "models/citizen/citizen.vmdl_c" ? false : true;

		Log.Info( "changed model" );

		var body = Player.Local.BodyRenderer;
		var clothes = Player.Local.Body.GetAllObjects( true ).Where( c => c.Tags.Has( "clothing" ) );
		if ( clothes.Any() && IsProp )
		{
			foreach ( var cloth in clothes )
			{
				cloth.Enabled = false;
				cloth.Network.Refresh();
			}
		}
		if ( !IsProxy )
		{
			OnPropShift?.Invoke( this, pc.Body.Components.Get<SkinnedModelRenderer>().Model, pc, pc.Inventory );

			// Handle the health algorithm for props
			/*
			if ( !IsProp )
			{
				PreviousHealth = (int)Player.Local.Health;
			}
			float multiplier = Math.Clamp( Player.Local.Health / Player.Local.MaxHealth, 0, 1 );
			float health = (float)Math.Pow( propModel.PhysicsBounds.Volume, 0.5f ) * 0.5f;

			health = (float)Math.Round( health / 5 ) * 5;
			Player.Local.MaxHealth = health;
			Player.Local.Health = health * multiplier;

			// Fallback to 10 health if prop is 0 health
			if ( Player.Local.Health <= 0 )
			{
				Player.Local.Health = 10f;
			}*/
		}
		Player.Local.Body.Network.Refresh();

	}

}
