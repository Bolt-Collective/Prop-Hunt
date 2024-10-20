public sealed class PropShifting : Component
{

	// Better to do it this way than broadcasting it
	[Sync]
	public string ModelPath { get; set; }

	public SkinnedModelRenderer BodyRenderer => Player.Local?.Body?.GetComponent<SkinnedModelRenderer>();

	protected override void OnStart()
	{
		if ( !IsProxy )
		{
			ModelPath = BodyRenderer?.Model.ResourcePath;
		}
	}

	protected override void OnUpdate()
	{
		if ( !Player.Local.IsValid )
			return;

		if ( IsProxy )
			return;

		if ( Input.Pressed( "Use" ) )
		{
			ShiftIntoProp();
		}

	}

	public void ShiftIntoProp()
	{
		var trace = Scene.Trace.Ray( Player.Local.AimRay, 500 )
			.IgnoreGameObject( Player.Local.Body )
			.WithoutTags( "preventprops", "player" )
			.Run();

		if ( !trace.Hit )
			return;

		if ( trace.GameObject.Components.TryGet( out Prop prop ) )
		{
			Log.Info( prop.Model.Name );

			ModelPath = prop.Model.ResourcePath;
			BodyRenderer.Model = Model.Load( ModelPath );
		}

		ToggleClothing( false );


		Gizmo.Draw.Line( trace.StartPosition, trace.EndPosition );
		Gizmo.Draw.LineSphere( trace.HitPosition, 16 );

		BodyRenderer.GameObject.Network.Refresh();
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

	}

	internal void ToggleClothing( bool enabled )
	{
		var clothes = BodyRenderer.GameObject
			.GetAllObjects( false )
			.Where( c => c.Tags.Has( "clothing" ) );

		if ( clothes.Any() )
		{
			foreach ( var cloth in clothes )
			{
				cloth.Enabled = enabled;
				cloth.Network.Refresh();
			}
		}
	}
}
