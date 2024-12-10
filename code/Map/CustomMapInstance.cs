public sealed class CustomMapInstance : MapInstance
{
	public Package CurrentMap { get; set; }

	protected override async void OnStart()
	{
		CurrentMap = await Package.Fetch( MapName, true );
	}

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
