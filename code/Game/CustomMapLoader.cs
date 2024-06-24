using Sandbox;

public class CustomMapLoader : MapInstance
{
	protected override void OnCreateObject( GameObject gameObject, MapLoader.ObjectEntry objectEntry )
	{
		if ( objectEntry.TypeName == "ent_door" )
		{
			var model = gameObject.Components.Create<ModelRenderer>();
			model.Model = objectEntry.GetResource<Model>( "model" );
			var collider = gameObject.Components.Create<ModelCollider>();
			collider.Model = model.Model;
			gameObject.Components.Create<Door>();
			gameObject.NetworkSpawn( null );
		}
	}
}