using Editor;
using Editor.ActionGraphs;
using PropHunt;
using Sandbox;
[CustomEditor( typeof( MapChanger.CustomMapActions ) )]
public sealed class CustomMapAction : ControlWidget
{
	public CustomMapAction( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Column();

		Layout.Spacing = 2;

		if ( property.IsNull )
		{
			property.SetValue( new MapChanger.CustomMapActions() );
		}

		var serializedObject = property.GetValue<MapChanger.CustomMapActions>()?.GetSerialized();

		if ( serializedObject is null ) return;

		serializedObject.TryGetProperty( nameof( MapChanger.CustomMapActions.MapAction ), out var mapAction );
		serializedObject.TryGetProperty( nameof( MapChanger.CustomMapActions.MapIndent ), out var mapIndent );

		Layout.Add( new StringControlWidget( mapIndent ) { } );

		Layout.AddSpacingCell( 25 );

		Layout.Add( new ActionControlWidget( mapAction ) { } );
	}
}
