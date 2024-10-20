public sealed class CustomMapInstance : MapInstance
{
	protected override void OnCreateObject( GameObject gameObject, MapLoader.ObjectEntry objectEntry )
	{
		if ( !Networking.IsHost )
		{
			if ( objectEntry.TypeName == "ent_door" )
				gameObject.Destroy();
			return;
		}
		/*
				if ( objectEntry.TypeName == "prop_physics" )
				{
					gameObject.SetParent( null );
				}*/

	}
}
