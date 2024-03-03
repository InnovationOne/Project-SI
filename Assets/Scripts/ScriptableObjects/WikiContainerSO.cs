using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Container/Wiki Container")]

public class WikiContainerSO : ScriptableObject
{
    public List<ItemSO> Items;

    public void AddItem(ItemSO itemSO) { 
        Items.Add(itemSO); 
    }
    
    public void SortItems() {
        Items = Items.
            OrderBy(x => x.ItemType)
            .ThenBy(x => x.ItemName)
            .ToList();
    }
}
