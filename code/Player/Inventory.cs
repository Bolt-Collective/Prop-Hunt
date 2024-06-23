using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using PropHunt;
using Sandbox;

public sealed class Inventory : Component
{
	[Property] public int Size { get; set; } = 9;
	[Property] public List<GameObject> Items { get; set; }
	[Property] public List<GameObject> StartingItems { get; set; } = new();
	[Property] public GameObject ActiveItem { get; set; }
	[Property] public int ActiveIndex { get; set; }
	[Property] public bool AbleToSwitch { get; set; } = true;
	public Player Player { get; set; }
	public TeamComponent TeamComponent { get; set; }
	protected override void OnStart()
	{
		Player = Scene.GetAllComponents<Player>().FirstOrDefault( x => !x.IsProxy );
		TeamComponent = Scene.GetAllComponents<TeamComponent>().FirstOrDefault( x => !x.IsProxy );
		if ( IsProxy ) return;
		Items = new List<GameObject>( new GameObject[Size] );
	}
	public void SpawnStartingItems()
	{
		if ( IsProxy ) return;
		if ( StartingItems.Count != 0 && TeamComponent.TeamName == Team.Hunters.ToString() )
		{
			for ( int i = 0; i < StartingItems.Count; i++ )
			{
				AddItem( StartingItems[i], i );
			}
		}
	}
	protected override void OnUpdate()
	{
		if ( IsProxy ) return;
		if ( TeamComponent.TeamName == Team.Hunters.ToString() && !IsProxy )
		{
			ItemInputs();
			for ( int i = 0; i < Items.Count; i++ )
			{
				if ( i == ActiveIndex )
				{
					ActiveItem = Items[i];
				}
				else if ( Items[i] is not null )
				{
					Items[i].Enabled = false;
				}
			}
			if ( ActiveItem is not null )
			{
				ActiveItem.Enabled = true;
			}
			if ( Items.All( x => x is null ) )
			{
				SpawnStartingItems();
			}
		}
		if ( TeamComponent.TeamName != Team.Hunters.ToString() )
		{
			Clear();
		}
	}

	public void AddItem( GameObject item, int slot )
	{
		if ( IsProxy ) return;
		if ( Items[slot] is null )
		{
			var clone = item.Clone();
			clone.Transform.LocalPosition = new Vector3( 0, 0, 0 );
			clone.Parent = GameObject;
			//clone.NetworkSpawn();
			if ( clone.Components.TryGet<Weapon>( out var weapon ) )
			{
				weapon.OnPickup?.Invoke( Player, weapon, this );
			}
			if ( clone.Components.TryGet<Item>( out var itemComponent ) )
			{
				itemComponent.OnPickup?.Invoke( Player, itemComponent, this );
			}

			Items[slot] = clone;
		}
	}

	public void Clear()
	{
		if ( IsProxy ) return;
		ActiveItem = null;
		for ( int i = 0; i < Items.Count; i++ )
		{
			RemoveItem( i );
		}
		foreach ( var weapon in GameObject.Components.GetAll<Weapon>( FindMode.EverythingInSelfAndChildren ) )
		{
			weapon.GameObject.Destroy();
		}
		foreach ( var item in GameObject.Components.GetAll<Item>( FindMode.EverythingInSelfAndChildren ) )
		{
			item.GameObject.Destroy();
		}
		foreach ( var throwAble in GameObject.Components.GetAll<ThrowableWeapon>( FindMode.EverythingInSelfAndChildren ) )
		{
			throwAble.GameObject.Destroy();
		}
	}

	public void RemoveItem( int slot )
	{
		if ( IsProxy ) return;
		if ( Items[slot] is not null )
		{
			Items[slot].Destroy();
			Items[slot] = null;
		}
	}

	void ItemInputs()
	{
		if ( !AbleToSwitch ) return;
		if ( Input.Pressed( "slot1" ) )
		{
			ActiveIndex = 0;
		}
		if ( Input.Pressed( "slot2" ) )
		{
			ActiveIndex = 1;
		}
		if ( Input.Pressed( "slot3" ) )
		{
			ActiveIndex = 2;
		}
		if ( Input.Pressed( "slot4" ) )
		{
			ActiveIndex = 3;
		}
		if ( Input.Pressed( "slot5" ) )
		{
			ActiveIndex = 4;
		}
	}
}
