using System.Drawing;
using Sandbox;

public class CustomMapLoader : MapInstance
{
	protected override void OnCreateObject( GameObject gameObject, MapLoader.ObjectEntry objectEntry )
	{
		if ( objectEntry.TypeName == "ent_door" )
		{
			//Create ModelRenderer
			var renderer = gameObject.Components.Create<ModelRenderer>();
			renderer.Model = objectEntry.GetResource<Model>( "model" );
			//Create Rigidbody and st the locking and flags
			var rb = gameObject.Components.Create<Rigidbody>();
			var locking = new PhysicsLock();
			locking.Pitch = true;
			locking.Roll = true;
			rb.Locking = locking;
			rb.RigidbodyFlags = RigidbodyFlags.DisableCollisionSounds;
			//Create ModelCollider and set the model
			var modelCollider = gameObject.Components.Create<ModelCollider>();
			modelCollider.Model = renderer.Model;
			//Create HingeJoint, set the min and max angle, motor mode, and body.
			var joint = gameObject.Components.Create<HingeJoint>();
			joint.MinAngle = -80;
			joint.MaxAngle = 80;
			joint.Motor = HingeJoint.MotorMode.TargetAngle;
		}

	}
}
