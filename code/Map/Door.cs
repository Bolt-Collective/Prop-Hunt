using Sandbox;
using Sandbox.Utility;

public sealed class Door : Component, IUse
{
	[Sync] public bool IsOpened { get; set; } = false;
	protected override void OnUpdate()
	{

	}
	void IUse.OnUse( Player player )
	{
		IsOpened = !IsOpened;
		if ( IsOpened )
		{
			_ = LerpDoor( 1, new Angles( 0, 90, 0 ), Easing.Linear );
		}
		else
		{
			_ = LerpDoor( 1, new Angles( 0, 0, 0 ), Easing.Linear );
		}
	}

	bool IUse.CanUse( Player player )
	{
		return true;
	}
	public async Task LerpDoor( float seconds, Angles to, Easing.Function easer )
	{
		TimeSince timeSince = 0;
		Rotation from = Transform.Rotation;

		while ( timeSince < seconds )
		{
			var size = Rotation.Lerp( from, to, easer( timeSince / seconds ) );
			Transform.Rotation = size;
			await Task.Frame();
		}
	}
}
