public sealed class DeathTrigger : Component, Component.ITriggerListener
{
	protected override void OnUpdate()
	{

	}

	void ITriggerListener.OnTriggerEnter( Sandbox.Collider other )
	{
		if ( other.GameObject.Components.TryGet<Player>( out var player, FindMode.EverythingInSelfAndParent ) )
		{
			//player.TakeDamage( 1000 );
		}
	}

}
