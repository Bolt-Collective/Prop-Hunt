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

		if ( Input.Pressed( "Flashlight" ) && TimeSinceTaunted > TauntCooldown )
		{
			Sound.Play( PropTaunts, GameObject.Transform.Position );
			TimeSinceTaunted = 0;
		}
	}
}
