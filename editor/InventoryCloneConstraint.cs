using System.Net.Http.Headers;
using Editor;
using Sandbox;
[CustomEditor( typeof( Inventory.CloneConstraint ) )]
public sealed class InventoryCloneConstraintWidget : ControlWidget
{
	public InventoryCloneConstraintWidget( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Column();
		PaintBackground = false;
		Layout.Spacing = 2;
		if ( property.IsNull )
		{
			property.SetValue( new Inventory.CloneConstraint() );
		}

		var serializedObject = property.GetValue<Inventory.CloneConstraint>()?.GetSerialized();
		if ( serializedObject is null ) return;

		var controlSheet = new ControlSheet();
		controlSheet.AddObject( serializedObject );
		Layout.Add( controlSheet );
	}
}
