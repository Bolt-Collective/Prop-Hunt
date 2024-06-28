using Sandbox;
[Description( "This component enables the GameObject based on the slot in the inventory" )]
public sealed class InventoryEnableSyncer : Component
{
	[Property, Sync] public int Slot { get; set; }
	private Inventory Inventory;
	protected override void OnStart()
	{
		Inventory = Scene.GetAllComponents<Inventory>().FirstOrDefault( x => !x.IsProxy );
	}

	protected override void OnUpdate()
	{
		if ( GameObject is not null )
		{
			GameObject.Enabled = Inventory.ActiveIndex == Slot;
		}

	}
}
