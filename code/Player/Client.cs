[Icon( "people" )]
public class Client : Component
{
	[Property, HostSync, Group( "Setup" )]
	public Team Team { get; set; }

	protected override void OnUpdate()
	{

	}
}
