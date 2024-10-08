
using Editor;
using Sandbox;
[CustomEditor( typeof( Inventory.WeaponCloneClass ) )]
public class WeaponClassControlWidget : ControlWidget
{
	public WeaponClassControlWidget( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Column();
		PaintBackground = false;
		Layout.Spacing = 2;
		if ( property.IsNull )
		{
			property.SetValue( new Inventory.WeaponCloneClass() );
		}

		var serializedObject = property.GetValue<Inventory.WeaponCloneClass>()?.GetSerialized();
		if ( serializedObject is null ) return;
		var controlSheet = new ControlSheet();

		var column = controlSheet.AddColumn();
		column.Add( new Label( "Weapon" ) );
		column.AddSpacingCell( 5 );
		column.Add( new GameObjectControlWidget( serializedObject.GetProperty( "Weapon" ) ) );
		column.AddSpacingCell( 5 );
		column.Add( new Label( "Offset" ) );
		column.AddSpacingCell( 5 );
		column.Add( new VectorControlWidget( serializedObject.GetProperty( "Offset" ) ) );
		column.AddSpacingCell( 5 );
		column.Add( new Label( "Scale" ) );
		column.AddSpacingCell( 5 );
		column.Add( new VectorControlWidget( serializedObject.GetProperty( "Scale" ) ) );

		Layout.Add( controlSheet );
	}
}
