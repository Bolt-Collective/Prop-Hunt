
public static class GameObjectExtensions
{
	public static void DestroyAsync( this GameObject self, float time )
	{
		var timedDestroy = self.Components.Create<TimeDestroy>();
		timedDestroy.Time = time;
	}
}
