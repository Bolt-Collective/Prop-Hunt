using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using PropHunt;
using Sandbox;

public sealed class Inventory : Component
{
	[Property] public int Size { get; set; } = 9;
	[Property] public List<GameObject> Items { get; set; }
	[Property] public List<WeaponCloneClass> StartingItems { get; set; } = new();
	[Property, Sync] public int ActiveIndex { get; set; }
	[Property, Sync] public bool AbleToSwitch { get; set; } = true;
	public Player Player { get; set; }
	public GameObject ActiveItem => Items[ActiveIndex];
	public int ActiveAmmo { get; set; }
	public TeamComponent TeamComponent { get; set; }
	[Property] public List<CloneConstraint> cloneConstraints { get; set; } = new();
	[Property] public List<GameObject> ActiveConstarints { get; set; } = new();
	protected override void OnStart()
	{
		Player = Scene.GetAllComponents<Player>().FirstOrDefault( x => !x.IsProxy );
		TeamComponent = Scene.GetAllComponents<TeamComponent>().FirstOrDefault( x => !x.IsProxy );
		if ( IsProxy ) return;
		Items = new List<GameObject>( new GameObject[Size] );
	}
	public void SpawnStartingItems()
	{
		if ( StartingItems.Count != 0 && TeamComponent.TeamName == Team.Hunters.ToString() )
		{
			for ( int i = 0; i < StartingItems.Count; i++ )
			{
				AddItem( StartingItems[i].Weapon, i, StartingItems[i].Offset );
			}
		}
		if ( TeamComponent.TeamName == Team.Hunters.ToString() && cloneConstraints.Count != 0 )
		{
			for ( int i = 0; i < cloneConstraints.Count; i++ )
			{
				AddConstraint( cloneConstraints[i] );
			}
		}
	}
	protected override void OnUpdate()
	{
		if ( !IsProxy && TeamComponent.TeamName == Team.Hunters.ToString() )
		{
			ActiveAmmo = ActiveItem is not null ? Player.Local.Inventory.ActiveItem.Components.TryGet<Weapon>( out var weapon ) ? weapon.Ammo :
					Player.Local.Inventory.ActiveItem.Components.TryGet<Item>( out var item ) && item.UsesAmmo ? item.Ammo :
					Player.Local.Inventory.ActiveItem.Components.TryGet<Shotgun>( out var shotgun ) ? shotgun.Item.Ammo : 0 : 0;
			ItemInputs();
			for ( int i = 0; i < Items.Count; i++ )
			{
				if ( i == ActiveIndex && Items[i] is not null )
				{
					Items[i].Enabled = true;
				}
				else if ( Items[i] is not null && Items[i] is not null )
				{
					Items[i].Enabled = false;
				}
			}
		}
		if ( !IsProxy && TeamComponent.TeamName != Team.Hunters.ToString() )
		{
			Clear();
			foreach ( var weapon in Scene.GetAllComponents<Weapon>().Where( x => !x.IsProxy ) )
			{
				weapon.Destroy();
			}
			foreach ( var item in Scene.GetAllComponents<Item>().Where( x => !x.IsProxy ) )
			{
				item.Destroy();
			}
		}
		if ( !IsProxy && (GameObject.Components.GetAll<Weapon>( FindMode.EverythingInSelfAndAncestors ).Count() + GameObject.Components.GetAll<Item>( FindMode.EverythingInSelfAndAncestors ).Count() + GameObject.Components.GetAll<ThrowableWeapon>().Count()) == 0 && TeamComponent.TeamName == Team.Hunters.ToString() )
		{
			SpawnStartingItems();
		}
	}
	public void AddConstraint( CloneConstraint constraint )
	{
		if ( IsProxy || cloneConstraints.Count <= ActiveConstarints.Count ) return;
		var clone = constraint.Clone.Clone();
		clone.Parent = constraint.Parent;
		ActiveConstarints.Add( clone );
		clone.NetworkSpawn();
	}
	public void AddItem( GameObject item, int slot, Vector3 offset )
	{
		if ( IsProxy ) return;
		if ( slot < 0 || slot >= Items.Count )
		{
			return;
		}
		if ( Items[slot] is null )
		{
			var clone = item.Clone();
			clone.Transform.LocalPosition = offset;
			clone.Parent = GameObject;
			clone.NetworkSpawn();
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
		for ( int i = 0; i < Items.Count; i++ )
		{
			RemoveItem( i );
		}
		for ( int i = 0; i < ActiveConstarints.Count; i++ )
		{
			ActiveConstarints[i].Destroy();
			ActiveConstarints.RemoveAt( i );
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
		/*
		if ( Input.Pressed( "slot5" ) )
		{
			ActiveIndex = 4;
		}*/
		if ( Input.UsingController )
		{
			if ( Input.Pressed( "nextweapon" ) && ActiveIndex < Items.Count - 1 )
			{
				ActiveIndex++;
			}
			if ( Input.Pressed( "prevweapon" ) && ActiveIndex > 0 )
			{
				ActiveIndex--;
			}
		}
	}

	public class CloneConstraint
	{
		public GameObject Parent { get; set; }
		public GameObject Clone { get; set; }

		public CloneConstraint()
		{
			Parent = null;
			Clone = null;
		}

		public CloneConstraint( GameObject parent, GameObject clone )
		{
			Parent = parent;
			Clone = clone;
		}
	}
	//Used for a control widget to allow the setting of a weapon and an offset
	public class WeaponCloneClass
	{
		public GameObject Weapon { get; set; }
		public Vector3 Offset { get; set; }

		public WeaponCloneClass()
		{
			Weapon = null;
			Offset = Vector3.Zero;
		}
		public WeaponCloneClass( GameObject weapon, Vector3 offset )
		{
			Weapon = weapon;
			Offset = offset;
		}
	}
}

