using Sandbox;

public sealed class TauntComponent : Component
{
	public TimeSince TimeSinceTaunted { get; private set; } = 0;

	[Property]
	public SoundEvent PropTaunts { get; private set; }

	[Property]
	public SoundEvent HunterTaunts { get; private set; }

	[Property]
	public int TauntCooldown { get; set; }

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( Input.Pressed( "taunt" ) && TimeSinceTaunted > TauntCooldown && Player.Local.TeamComponent.TeamName == Team.Props.ToString() )
		{
			BroadcastTaunt( PropTaunts );
			TimeSinceTaunted = 0;
		}
		else if ( Input.Pressed( "taunt" ) && TimeSinceTaunted > TauntCooldown && Player.Local.TeamComponent.TeamName == Team.Hunters.ToString() )
		{
			BroadcastTaunt( HunterTaunts );
			TimeSinceTaunted = 0;
		}
	}

	[Broadcast]
	public void BroadcastTaunt( SoundEvent sound )
	{
		Sound.Play( sound, GameObject.Transform.Position );
	}
}
