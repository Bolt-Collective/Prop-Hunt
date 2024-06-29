
public static class GameObjectExtensions
{
	public static void DestroyAsync( this GameObject self, float time )
	{
		var timedDestroy = self.Components.Create<TimeDestroy>();
		timedDestroy.Time = time;
	}

	//Needed because tags are broken in actiongraph
	//Without this GameObject self the node will not showup
	public static void AddTags( this GameObject self, GameObject target, string tag )
	{
		target.Tags.Add( tag );
	}
}
