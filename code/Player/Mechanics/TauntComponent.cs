using Sandbox;

public sealed class TauntComponent : Component
{
	public TimeSince TimeSinceTaunted { get; private set; } = 0;

	[Property] public List<SoundEvent> PropTaunts { get; set; } = new();

	[Property] public List<SoundEvent> HunterTaunts { get; set; } = new();

	[Property]
	public int TauntCooldown { get; set; }

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( Input.Pressed( "taunt" ) && TimeSinceTaunted > TauntCooldown && Player.Local.TeamComponent.TeamName == Team.Props.ToString() )
		{
			BroadcastTaunt( Game.Random.FromList( PropTaunts ).ResourceName );
			TimeSinceTaunted = 0;
		}
		else if ( Input.Pressed( "taunt" ) && TimeSinceTaunted > TauntCooldown && Player.Local.TeamComponent.TeamName == Team.Hunters.ToString() )
		{
			BroadcastTaunt( Game.Random.FromList( HunterTaunts ).ResourceName );
			TimeSinceTaunted = 0;
		}
	}

	[Broadcast]
	public void BroadcastTaunt( string sound )
	{
		Sound.Play( sound, GameObject.Transform.Position );
	}
}
