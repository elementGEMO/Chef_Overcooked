using RoR2;
using UnityEngine;

namespace ChefOvercooked;
public class ItemReRender
{
    public ItemReRender()
    {
        ItemCatalog.availability.CallWhenAvailable(() =>
        {
            DLC3Content.Items.WyrmOnHit.pickupIconSprite = ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("texIconPickupWyrmOnHit");
        });
    }
}