using System.Net.Http.Headers;
using Editor;
using Sandbox;
[CustomEditor( typeof( Inventory.CloneConstraint ) )]
public class InventoryCloneConstraintWidget : ControlWidget
{
	public InventoryCloneConstraintWidget( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Column();
		Layout.Spacing = 2;
		if ( property.IsNull )
		{
			property.SetValue( new Inventory.CloneConstraint() );
		}

		var serializedObject = property.GetValue<Inventory.CloneConstraint>()?.GetSerialized();
		if ( serializedObject is null ) return;

		serializedObject.TryGetProperty( nameof( Inventory.CloneConstraint.Clone ), out var clone );
		serializedObject.TryGetProperty( nameof( Inventory.CloneConstraint.Parent ), out var parent );

		Layout.Add( new GameObjectControlWidget( clone ) { } );
		Layout.AddSpacingCell( 25 );
		Layout.Add( new GameObjectControlWidget( parent ) { } );
	}
}
