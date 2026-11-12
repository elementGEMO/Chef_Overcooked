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
    private static List<CookingState.MealWeight> RunMealItemDefs;
    private static List<ItemDef> WeightedMealItemDefs;
    public MealDistributor()
    {
        MealDistributorVariant.CreateMaterial();

        Run.onRunStartGlobal += Run_onRunStartGlobal;

        IL.RoR2.PickupDistributorBehavior.Start += PickupDistributorBehavior_Start;

        On.RoR2.PickupDistributorBehavior.GenerateNewPickupServer_bool += PickupDistributorBehavior_GenerateNewPickupServer_bool;
        On.RoR2.PickupDistributorBehavior.Drop += PickupDistributorBehavior_Drop;
        On.RoR2.PickupDistributorBehavior.RerollPickup += PickupDistributorBehavior_RerollPickup;
    }

    private void PickupDistributorBehavior_RerollPickup(On.RoR2.PickupDistributorBehavior.orig_RerollPickup orig, PickupDistributorBehavior self)
    {
        if (self.gameObject.GetComponent<MealDistributorVariant>())
        {
            UniquePickup newPickup = UniquePickup.none;
            newPickup.pickupIndex = GeneratePickUp(self.rng);

            self.SetPickup(newPickup);
            self.UpdatePickupDisplays();
        }
        else orig(self);
    }

    private void Run_onRunStartGlobal(Run instance)
    {
        RunMealItemDefs         = [];
        WeightedMealItemDefs    = [];

        foreach (ItemDef itemDef in ItemCatalog.allItemDefs)
        {
            if (itemDef == null) continue;
            if (!instance.IsItemAvailable(itemDef.itemIndex)) continue;
            if (itemDef.tier != ItemTier.FoodTier) continue;
            if (itemDef == MonsterMeatItem.ItemDef) continue;

            RunMealItemDefs.Add(new CookingState.MealWeight(itemDef));
        }

        if (PluginConfig.Do_Meal_Weight.Value && RunMealItemDefs.Count > 0)
            WeightedMealItemDefs = CookingState.MealWeight.PopulateWeights(RunMealItemDefs);
    }

    private PickupIndex GeneratePickUp(Xoroshiro128Plus rng)
    {
        if (RunMealItemDefs.Count > 0 && !PluginConfig.Do_Meal_Weight.Value)
            return PickupCatalog.FindPickupIndex(RunMealItemDefs[rng.RangeInt(0, RunMealItemDefs.Count)].itemDef.itemIndex);

        if (WeightedMealItemDefs.Count > 0)
            return PickupCatalog.FindPickupIndex(WeightedMealItemDefs[rng.RangeInt(0, WeightedMealItemDefs.Count)].itemIndex);

        Log.Error("Could not pick a Meal item for Meal Distributor");
        return PickupIndex.none;
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
                if (self.rng.RangeFloat(0f, 1f) < PluginConfig.Distributor_Chance.Value / 100)
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
                newPickup.decayValue = PluginConfig.Distribute_Temp_Duration.Value;

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

            if (PluginConfig.Distributor_Color.Value == PluginConfig.DistributorColor.Blue)
                return;

            if (PluginConfig.Distributor_Color.Value == PluginConfig.DistributorColor.Orange)
                MealMaterial.SetTexture("_MainTex", ChefOverCookedPlugin.Bundle.LoadAsset<Texture2D>("texMealDistributeDiffuse"));

            if (PluginConfig.Distributor_Color.Value == PluginConfig.DistributorColor.White)
                MealMaterial.SetTexture("_MainTex", ChefOverCookedPlugin.Bundle.LoadAsset<Texture2D>("texMealDistributeDiffuseAlt"));

            MealMaterial.SetTexture("_EmTex", ChefOverCookedPlugin.Bundle.LoadAsset<Texture2D>("texMealDistributeEmissive"));
            MealMaterial.SetTexture("_SplatmapTex", Addressables.LoadAssetAsync<Texture2D>(RoR2_DLC1_RegeneratingScrap.texRegeneratingScrapSplatMap_png).WaitForCompletion());
            MealMaterial.SetTexture("_GreenChannelTex", Addressables.LoadAssetAsync<Texture2D>(RoR2_Base_Common_TiledTextures.texTileRustyMetal_tif).WaitForCompletion());

            MealMaterial.EnableKeyword("SPLATMAP");
            MealMaterial.EnableKeyword("FORCE_SPEC");

            MealMaterial.SetFloat("_SpecularStrength", 0.118f);
            MealMaterial.SetFloat("_SpecularExponent", 1.25f);
            MealMaterial.SetFloat("_SplatmapTileScale", 1.25f);
            MealMaterial.SetFloat("_GreenChannelBias", -0.48f);
            MealMaterial.SetFloat("_BlueChannelBias", -2f);
            MealMaterial.SetFloat("_Depth", 0f);

            MealMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        public void Awake()
        {
            PurchaseInteraction purchase = gameObject.GetComponent<PurchaseInteraction>();

            if (purchase)
                purchase.Networkcost = Mathf.CeilToInt(purchase.Networkcost * PluginConfig.Cost_Multiplier.Value);

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
