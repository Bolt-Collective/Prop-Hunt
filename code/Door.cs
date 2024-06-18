using Sandbox;

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
			GameObject.Transform.Rotation = Rotation.From( 0, 90, 0 );
		}
		else
		{
			GameObject.Transform.Rotation = Rotation.From( 0, 0, 0 );
		}
	}

	bool IUse.CanUse( Player player )
	{
		return true;
	}
}
