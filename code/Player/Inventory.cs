using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using Sandbox;

public sealed class Inventory : Component
{
	[Property] public int Size { get; set; } = 9;
	[Property] public List<GameObject> Items { get; set; }
	[Property] public List<GameObject> StartingItems { get; set; } = new();
	[Property] public GameObject ActiveItem { get; set; }
	[Property] public int ActiveIndex { get; set; }
	[Property, Sync] public bool AbleToSwitch { get; set; } = true; 
	public Player Player { get; set; }
	public TeamComponent Team { get; set; }
	protected override void OnStart()
	{
		Player = Scene.GetAllComponents<Player>().FirstOrDefault(x => !x.IsProxy);
		Team = Scene.GetAllComponents<TeamComponent>().FirstOrDefault(x => !x.IsProxy);
		if (IsProxy) return;
		Items = new List<GameObject>(new GameObject[Size]);
		if (StartingItems.Count != 0)
		{
			for (int i = 0; i < StartingItems.Count; i++)
			{
				AddItem(StartingItems[i], i);
			}
		}
	}
	protected override void OnUpdate()
	{
		if (!IsProxy && Team.Team == global::Team.Hunters)
		{
			ItemInputs();
			for (int i = 0; i < Items.Count; i++)
			{
				if (Items[i] is not null && i == ActiveIndex)
				{
					ActiveItem = Items[i];
				}
			}
			if (ActiveItem is not null)
			{
				ActiveItem.Enabled = true;
			}
		}
	}

	public void AddItem(GameObject item, int slot)
	{
		if (IsProxy) return;
		if (Items[slot] is null)
		{
			var clone = item.Clone();
			clone.Parent = GameObject;
			if (clone.Components.TryGet<Weapon>(out var weapon))
			{
				weapon.OnPickup?.Invoke(Player, weapon, this);
			}
			if (clone.Components.TryGet<Item>(out var itemComponent))
			{
				itemComponent.OnPickup?.Invoke(Player, itemComponent, this);
			}
			clone.NetworkSpawn();
			Items[slot] = clone;
		}
	}

	public void RemoveItem(int slot)
	{
		if (Items[slot] is not null)
		{
			Items[slot].Destroy();
			Items[slot] = null;
		}
	}

	void ItemInputs()
	{
		if (!AbleToSwitch) return;
		if (Input.Pressed("slot1"))
		{
			ActiveIndex = 0;
		}
		if (Input.Pressed("slot2"))
		{
			ActiveIndex = 1;
		}
		if (Input.Pressed("slot3"))
		{
			ActiveIndex = 2;
		}
		if (Input.Pressed("slot4"))
		{
			ActiveIndex = 3;
		}
		if (Input.Pressed("slot5"))
		{
			ActiveIndex = 4;
		}
		if (Input.Pressed("slot6"))
		{
			ActiveIndex = 5;
		}
		if (Input.Pressed("slot7"))
		{
			ActiveIndex = 6;
		}
		if (Input.Pressed("slot8"))
		{
			ActiveIndex = 7;
		}
		if (Input.Pressed("slot9"))
		{
			ActiveIndex = 8;
		}
		if (Input.Pressed("slot0"))
		{
			ActiveIndex = 9;
		}
	}
}
