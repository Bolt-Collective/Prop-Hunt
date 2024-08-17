using System.Drawing;
using Sandbox;

public class CustomMapLoader : MapInstance
{
	protected override void OnCreateObject( GameObject gameObject, MapLoader.ObjectEntry objectEntry )
	{
		if ( objectEntry.TypeName == "ent_door" )
		{
			Model resource = objectEntry.GetResource<Model>( "model" );

			var skMdl = gameObject.Components.Create<SkinnedModelRenderer>();
			skMdl.Model = resource;
			var mdlCollider = gameObject.Components.Create<ModelCollider>();
			mdlCollider.Model = resource;

			var door = gameObject.Components.Create<Door>();

			Angles movedir = objectEntry.GetValue<Angles>( "movedir" );
			float distance = objectEntry.GetValue<float>( "distance" );
			Vector3 origin = objectEntry.GetValue<Vector3>( "pivot" );

			bool startslocked = objectEntry.GetValue<bool>( "startslocked" );
			bool locked = objectEntry.GetValue<bool>( "locked" );

			Door.DoorMoveType movedir_type = objectEntry.GetValue<Door.DoorMoveType>( "movedir_type" );
			door.Axis = movedir;
			door.Distance = distance;
			door.MoveDirType = movedir_type;
			door.Locked = locked || startslocked;
			door.PivotPosition = gameObject.Transform.World.PointToWorld( origin );
			door.Collider = mdlCollider;

			gameObject.SetParent( GameObject );
			gameObject.NetworkSpawn( null );
		}

	}
}
