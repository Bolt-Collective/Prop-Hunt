﻿

using PropHunt;
using Sandbox.Utility;

public class PropShiftingMechanic : Component
{
	public TeamComponent TeamComponent { get; set; }
	public delegate void PropShiftingDelegate( PropShiftingMechanic propShiftingMechanic, Model PropModel, Player player, Inventory inventory );
	[Property, KeyProperty] public PropShiftingDelegate OnPropShift { get; set; }
	[Property] public ModelCollider PropsCollider { get; set; }
	[Property, Sync] public bool IsProp { get; set; } = false;
	[Sync] public string ModelPath { get; set; }
	[Property] public int PreviousHealth { get; set; }
	[Sync] public ulong BodyGroups { get; set; }
	public TauntComponent TauntComponent { get; set; }
	protected override void OnStart()
	{
		TeamComponent = Scene.GetAllComponents<TeamComponent>().FirstOrDefault( x => !x.IsProxy );
		TauntComponent = Scene.GetAllComponents<TauntComponent>().FirstOrDefault( x => !x.IsProxy );
		PreviousHealth = 100;
		if ( !IsProxy )
		{
			ModelPath = Player.Local.BodyRenderer?.Model.ResourcePath;
			BodyGroups = Player.Local.BodyRenderer.BodyGroups;
		}
	}
	protected override void OnUpdate()
	{
		if ( Input.Pressed( "View" ) )
		{
			ExitProp();
		}
		if ( ModelPath is null ) return;

		if ( Input.Pressed( "Use" ) )
		{
			ShiftIntoProp();
		}
	}

	public void ExitProp()
	{
		if ( Player.Local is null || !Player.Local.AbleToMove || IsProxy || GameObject is null || Components?.Get<Player>() is null || this is null ) return;
		var pc = Components?.Get<Player>();
		if ( !pc.IsValid() ) return;
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
		PropsCollider.Model = Model.Load( ModelPath );
		pcModel.Tint = Color.White;
		pcModel.GameObject.Transform.Scale = Vector3.One;
		pcModel.GameObject.Transform.Scale = Vector3.One;
		//Player.Local.Health = PreviousHealth;
		pcModel.BodyGroups = BodyGroups;
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
		if ( Player.Local is null || !Player.Local.AbleToMove || IsProxy || Player.Local.TeamComponent.TeamName != Team.Props.ToString() || GameObject is null || Components?.Get<Player>() is null || this is null ) return;
		var pc = Components?.Get<Player>();
		if ( !pc.IsValid() ) return;
		var lookDir = pc.EyeAngles.ToRotation();
		var eyePos = Transform.Position + Vector3.Up * 64;

		var tr = Scene.Trace.Ray( Scene.Camera.Transform.Position, Scene.Camera.Transform.Position + lookDir.Forward * (300 + Player.Local.CameraDistance) )
			.IgnoreGameObject( Player.Local.PropShiftingMechanic.PropsCollider.GameObject )
			.WithoutTags( "preventprops" )
			.Run();

		//Gizmo.Draw.LineSphere( tr.HitPosition, 16 );

		if ( !tr.Hit ) return;
		var propModel = tr.GameObject.Components.Get<Prop>( FindMode.EverythingInSelfAndDescendants )?.Model ?? tr.GameObject.Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndDescendants )?.Model;
		var propRenderer = tr.GameObject.Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndDescendants );
		if ( propModel is null ) return;
		Log.Info( propModel.Name );
		if ( propModel.Name == "models/citizen/citizen.vmdl" ) return;
		ModelPath = propModel.ResourcePath;
		Player.Local.Body.Transform.Scale = propRenderer.Transform.Scale;
		Player.Local.BodyRenderer.Tint = propRenderer.Tint;
		Player.Local.BodyRenderer.Model = Model.Load( ModelPath );
		IsProp = ModelPath == "models/citizen/citizen.vmdl_c" ? false : true;
		PropsCollider.Model = Model.Load( ModelPath );
		Log.Info( "changed model" );

		var body = Player.Local.BodyRenderer;
		var clothes = Player.Local.Body.GetAllObjects( true ).Where( c => c.Tags.Has( "clothing" ) );
		if ( clothes.Any() && IsProp )
		{
			foreach ( var cloth in clothes )
			{
				if ( cloth is null ) continue;
				cloth.Enabled = false;
				cloth.Network.Refresh();
			}
		}
		if ( !IsProxy )
		{
			OnPropShift?.Invoke( this, pc.Body.Components.Get<SkinnedModelRenderer>().Model, pc, pc.Inventory );

			// Handle the health algorithm for props

			if ( !IsProp )
			{
				PreviousHealth = (int)pc.Health;
			}
			float multiplier = Math.Clamp( pc.Health / pc.MaxHealth, 0, 1 );
			float health = (float)Math.Pow( propModel.Bounds.Volume, 0.5f ) * 0.5f;

			health = (float)Math.Round( health / 6 ) * 5;
			pc.MaxHealth = health;
			pc.Health = health * multiplier;

			// Fallback to 10 health if prop is 0 health
			if ( pc.Health <= 0 )
			{
				pc.Health = 10f;
			}

			if ( IsProp )
			{
				pc.CameraDistance = pc.CameraDistance != 0 ? pc.CameraDistance : 150;
			}
		}
		pc.Body.Network.Refresh();

	}

}
