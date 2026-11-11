using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RoR2;
using UnityEngine.AddressableAssets;
using RoR2BepInExPack.GameAssetPaths.Version_1_35_0;

namespace ChefOvercooked;

public class MealDistributor
{
    public MealDistributor()
    {
        MealDistributorVariant.CreateMaterial();

        IL.RoR2.PickupDistributorBehavior.Start += PickupDistributorBehavior_Start;

        On.RoR2.PickupDistributorBehavior.GenerateNewPickupServer_bool += PickupDistributorBehavior_GenerateNewPickupServer_bool;
        On.RoR2.PickupDistributorBehavior.Drop += PickupDistributorBehavior_Drop;
    }

    private PickupIndex GeneratePickUp(Xoroshiro128Plus rng = null)
    {
        List<ItemIndex> mealItems = [];

        foreach (ItemDef itemDef in ItemCatalog.allItemDefs)
            if (itemDef.tier == ItemTier.FoodTier && itemDef != MonsterMeatItem.ItemDef) mealItems.Add(itemDef.itemIndex);

        return PickupCatalog.FindPickupIndex(mealItems[rng.RangeInt(0, mealItems.Count)]);
    }

    private void PickupDistributorBehavior_Start(ILContext il)
    {
        ILCursor cursor = new(il);

        if (cursor.TryGotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchCall(typeof(PickupDistributorBehavior), nameof(PickupDistributorBehavior.GenerateNewPickupServer))
        ))
        {
            cursor.Emit(OpCodes.Ldarg_0);

            cursor.EmitDelegate<Action<PickupDistributorBehavior>>(self =>
            {
                if (self.rng.RangeFloat(0f, 1f) < 0.5)
                {
                    self.gameObject.AddComponent<MealDistributorVariant>();
                    self.baseDropAmount = 3;
                }
            });
        }
        else Log.Error("MEALDISTRIBUTOR_START failed to ILHook");
    }
    private void PickupDistributorBehavior_GenerateNewPickupServer_bool(On.RoR2.PickupDistributorBehavior.orig_GenerateNewPickupServer_bool orig, PickupDistributorBehavior self, bool newHidden)
    {
        if (self.gameObject.GetComponent<MealDistributorVariant>())
        {
            if (!NetworkServer.active || self.hasBeenPurchased)
                return;

            UniquePickup newPickup = UniquePickup.none;
            newPickup.pickupIndex = GeneratePickUp(self.rng);

            if (self.tempPickups)
                newPickup.decayValue = 1f;

            self.SetPickup(newPickup, newHidden);
        }
        else orig(self, newHidden);
    }
    private void PickupDistributorBehavior_Drop(On.RoR2.PickupDistributorBehavior.orig_Drop orig, PickupDistributorBehavior self)
    {
        if (self.gameObject.GetComponent<MealDistributorVariant>()) // Jank to make the gap between items actually disappear as it plays the anim
        {
            if (self.itemsDropped == 1 || self.itemsDropped == 3)
                self.itemsDropped++;
        }

        orig(self);
    }

    public class MealDistributorVariant : NetworkBehaviour
    {
        private static Material MealMaterial;
        public static void CreateMaterial()
        {
            MealMaterial = new(Addressables.LoadAssetAsync<Material>(RoR2_DLC3_TemporaryItemsDistributor.matTempItemDist_mat).WaitForCompletion());

            MealMaterial.SetTexture("_MainTex", ChefOverCookedPlugin.Bundle.LoadAsset<Texture>("texMealDistributeDiffuse"));
            MealMaterial.SetTexture("_EmTex", ChefOverCookedPlugin.Bundle.LoadAsset<Texture>("texMealDistributeEmissive"));
        }
        public void Awake()
        {
            PurchaseInteraction purchase = gameObject.GetComponent<PurchaseInteraction>();

            if (purchase)
                purchase.Networkcost *= 2;

            Transform baseTransform = gameObject.transform.Find("mdlTempItemDistributer");
            GameObject interactMesh = baseTransform.Find("mshTemporaryItemDistributer").gameObject;

            SkinnedMeshRenderer render = interactMesh ? interactMesh.GetComponent<SkinnedMeshRenderer>() : null;

            if (render)
                render.material = MealMaterial;

            baseTransform.Find("PickupDisplay (2)").gameObject.SetActive(false);
            baseTransform.Find("PickupDisplay (4)").gameObject.SetActive(false);
        }
    }
}
