using Editor;
using Sandbox;
using SceneLoading;

//[CustomEditor(typeof(SceneLoadingClass))]
public sealed class SceneLoadingResourceCustomEditor : ControlWidget
{
	public SceneLoadingResourceCustomEditor( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Column();

		if ( property.IsNull )
		{
			property.SetValue( new SceneLoadingClass() );
		}

		var so = property.GetValue<SceneLoadingClass>()?.GetSerialized();
		if ( so is null ) return;
		var controlSheet = new ControlSheet();
		controlSheet.AddObject( so );
		Layout.Add( controlSheet );
	}
}
