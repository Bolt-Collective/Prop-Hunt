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
		PaintBackground = false;
		Layout.Spacing = 2;

		if ( property.IsNull )
		{
			property.SetValue( new MapChanger.CustomMapActions() );
		}

		var serializedObject = property.GetValue<MapChanger.CustomMapActions>()?.GetSerialized();

		if ( serializedObject is null ) return;


		var controlSheet = new ControlSheet();
		controlSheet.AddObject( serializedObject );
		Layout.Add( controlSheet );

	}
}
