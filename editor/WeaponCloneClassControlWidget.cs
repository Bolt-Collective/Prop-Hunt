
using Editor;
using Sandbox;
[CustomEditor( typeof( Inventory.WeaponCloneClass ) )]
public class WeaponClassControlWidget : ControlWidget
{
	public WeaponClassControlWidget( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Column();
		Layout.Spacing = 2;
		if ( property.IsNull )
		{
			property.SetValue( new Inventory.WeaponCloneClass() );
		}

		var serializedObject = property.GetValue<Inventory.WeaponCloneClass>()?.GetSerialized();
		if ( serializedObject is null ) return;
		var controlSheet = new ControlSheet();
		controlSheet.AddObject( serializedObject );
		Layout.Add( controlSheet );
	}
}
