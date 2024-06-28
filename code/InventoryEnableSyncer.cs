using Sandbox;
[Description( "This component enables the GameObject based on the slot in the inventory" )]
public sealed class InventoryEnableSyncer : Component
{
	[Property, Sync, Description( "Bind this slot to the slot from the inventory you want to mirror" )] public int Slot { get; set; }
	protected override void OnStart()
	{

	}

	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			UpdateEnabled( Player.Local.GameObject.Id );
		}
	}

	[Broadcast]
	public void UpdateEnabled( Guid caller )
	{
		var inv = Scene.Directory.FindByGuid( caller ).Components.Get<Inventory>();
		foreach ( var child in GameObject.Children )
		{
			if ( inv.ActiveIndex == Slot )
			{
				child.Enabled = true;
			}
			else
			{
				child.Enabled = false;
			}
		}
	}

}
