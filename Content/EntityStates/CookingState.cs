using EntityStates;
using RoR2;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;
using RoR2.Stats;

namespace ChefOvercooked;
public class CookingState : GenericCharacterMain
{
    private static List<MealWeight> RunMealItemDefs;
    private static List<ItemDef> WeightedMealItemDefs;

    private class MealWeight(ItemDef itemDef)
    {
        public ItemDef itemDef = itemDef;

        // Dumb function to average the tiers numerically to add a weighted randomness from the recipe
        public int GetFrequency()
        {
            if (itemDef.tier != ItemTier.FoodTier) return (int) Mathf.Pow(NumFromTier(PickupCatalog.FindPickupIndex(itemDef.itemIndex)), 1f / Mathf.Log10(4));

            CraftableCatalog.RecipeEntry[] recipeRelation = CraftableCatalog.FindAllRelatedRecipes(PickupCatalog.FindPickupIndex(itemDef.itemIndex));
            float avgFrequency = 0;

            foreach (CraftableCatalog.RecipeEntry recipe in recipeRelation)
            {
                foreach (PickupIndex pickupIndex in recipe.GetAllPickups())
                {
                    avgFrequency = avgFrequency == 0 ? NumFromTier(pickupIndex) : (avgFrequency + NumFromTier(pickupIndex)) / 2;
                }
            }

            return (int) Mathf.Pow(avgFrequency, 1f / Mathf.Log10(4));
        }
        private int NumFromTier(PickupIndex pickupIndex)
        {
            ItemDef itemDef = ItemCatalog.GetItemDef(pickupIndex.itemIndex);

            if (itemDef)
            {
                return itemDef.tier switch
                {
                    ItemTier.Tier2 => 3,
                    ItemTier.Tier3 => 2,
                    ItemTier.Boss => 1,
                    _ => 4,
                };
            }

            return 4;
        }
    }

    private bool hasPlayedSound;
    private bool hasPlayedAnim;

    public static void CreateEffects()
    {
        Run.onRunStartGlobal += Run_onRunStartGlobal;
    }
    private static void Run_onRunStartGlobal(Run instance)
    {
        PluginConfig.ItemOptions itemOptions    = PluginConfig.FoodDrops.Value;
        bool isDLC3Enabled                      = instance.IsExpansionEnabled(ChefOverCookedPlugin.AlloyedCollective);
        RunMealItemDefs                         = [];
        WeightedMealItemDefs                    = [];

        if (itemOptions == PluginConfig.ItemOptions.Ingredients || !isDLC3Enabled)
        {
            foreach (ItemDef itemDef in ItemCatalog.allItemDefs)
            {
                if (itemDef == null) continue;
                if (!instance.IsItemAvailable(itemDef.itemIndex)) continue;
                if (itemDef.DoesNotContainTag(ItemTag.FoodRelated)) continue;

                switch (itemDef.tier)
                {
                    case ItemTier.Tier1:
                    case ItemTier.Tier2:
                    case ItemTier.Tier3:
                    case ItemTier.Boss:
                        RunMealItemDefs.Add(new MealWeight(itemDef));
                        break;
                }
            }
        }
        else
        {
            if (itemOptions == PluginConfig.ItemOptions.FoodTier)
            {
                foreach (ItemDef itemDef in ItemCatalog.allItemDefs)
                {
                    if (itemDef == null) continue;
                    if (!instance.IsItemAvailable(itemDef.itemIndex)) continue;
                    if (itemDef.tier != ItemTier.FoodTier) continue;
                    if (itemDef == MonsterMeatItem.ItemDef) continue;

                    RunMealItemDefs.Add(new MealWeight(itemDef));
                }
            }
            else
            {
                foreach (ItemDef itemDef in ItemCatalog.allItemDefs)
                {
                    if (itemDef == null) continue;
                    if (!instance.IsItemAvailable(itemDef.itemIndex)) continue;
                    if (itemDef == MonsterMeatItem.ItemDef) continue;

                    if (itemDef.tier == ItemTier.FoodTier)
                    {
                        RunMealItemDefs.Add(new MealWeight(itemDef));
                    }
                    else if (itemDef.ContainsTag(ItemTag.FoodRelated))
                    {
                        switch (itemDef.tier)
                        {
                            case ItemTier.Tier1:
                            case ItemTier.Tier2:
                            case ItemTier.Tier3:
                            case ItemTier.Boss:
                                RunMealItemDefs.Add(new MealWeight(itemDef));
                                break;
                        }
                    }
                }
            }
        }

        foreach (MealWeight item in RunMealItemDefs)
        {
            Log.Info(string.Format("Cook Item Pool - {0} ... {1}x", item.itemDef.name, item.GetFrequency()));
            for (int i = 0; i < item.GetFrequency(); i++) WeightedMealItemDefs.Add(item.itemDef);
        }
    }
    private PickupIndex RandomPickupIndex()
    {
        return WeightedMealItemDefs.Count > 0 ? PickupCatalog.FindPickupIndex(WeightedMealItemDefs[Random.Range(0, WeightedMealItemDefs.Count)].itemIndex) : PickupCatalog.FindPickupIndex(RoR2Content.Items.ExtraLifeConsumed.itemIndex);

        /*
        // Set paramter as CharacterBody characterBody = null
        
        PlayerStatsComponent playerStats    = characterBody ? PlayerStatsComponent.FindBodyStatsComponent(characterBody) : null;
        NetworkUser playerUser              = playerStats.playerCharacterMasterController ? playerStats.playerCharacterMasterController.networkUser : null;
        UserProfile playerProfile           = playerUser ? playerUser.localUser.userProfile : null;

        if (characterBody == null || playerProfile == null)
            return PickupCatalog.FindPickupIndex(WeightedMealItemDefs[Random.Range(0, WeightedMealItemDefs.Count)].itemIndex);

        HG.ListPool<PickupIndex>.RentCollection(out List<PickupIndex> localMealItems);

        foreach (ItemDef itemDef in WeightedMealItemDefs)
        {
            PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(itemDef.itemIndex);

            if (playerProfile.HasDiscoveredPickup(pickupIndex))
                localMealItems.Add(pickupIndex);
        }
        */

        //return localMealItems.Count > 0 ? localMealItems[Random.Range(0, localMealItems.Count)] : PickupCatalog.FindPickupIndex(RoR2Content.Items.ExtraLifeConsumed.itemIndex);
    }

    public override void OnEnter()
    {
        base.OnEnter();

        hasPlayedSound  = false;
        hasPlayedAnim   = false;
    }
    public override void OnExit()
    {
        if (!NetworkServer.active) return;

        Inventory inventory = characterBody.inventory;
        int itemCount       = inventory.GetItemCountEffective(MonsterMeatItem.ItemDef);

        if (characterBody.isPlayerControlled)
        {
            for (int i = 0; i < itemCount; i++)
            {
                Vector3 vectorForce = GetModelTransform().forward * (i + 10);
                vectorForce = new(vectorForce.x, 12 + (i * 2), vectorForce.z);

                UniquePickup randomPickup = new()
                {
                    pickupIndex = RandomPickupIndex(),
                    decayValue = PluginConfig.Temp_Duration.Value
                };

                PickupDropletController.CreatePickupDroplet(randomPickup, characterBody.corePosition, vectorForce, false, false);
            }
        }
        else
        {
            string pickupToken  = (teamComponent.teamIndex == TeamIndex.Player) ? "PLAYER_PICKUP" : "MONSTER_PICKUP";
            
            for (int i = 0; i < itemCount; i++)
            {
                PickupDef pickupDef = PickupCatalog.GetPickupDef(RandomPickupIndex());
                ItemIndex itemIndex = pickupDef.itemIndex;

                characterBody.inventory.GiveItemTemp(itemIndex, PluginConfig.Temp_Duration.Value);

                Chat.SendBroadcastChat(new Chat.PlayerPickupChatMessage
                {
                    subjectAsCharacterBody = characterBody,
                    baseToken = pickupToken,
                    pickupQuantity = 1U,
                    pickupColor = pickupDef.baseColor,
                    pickupToken = Language.GetStringFormatted("ITEM_MODIFIER_TEMP", [Language.GetStringFormatted(pickupDef.nameToken, [])])
                });
            }
        }

        inventory.RemoveItemPermanent(MonsterMeatItem.ItemDef, itemCount);

        base.OnExit();
    }
    public override void FixedUpdate()
    {
        base.FixedUpdate();

        // Aborts State early if there is no Monster Meat
        if (characterBody.inventory && characterBody.inventory.GetItemCountEffective(MonsterMeatItem.ItemDef) <= 0) outer.SetNextStateToMain();

        if (!hasPlayedAnim)
        {
            hasPlayedAnim = true;
            PlayAnimation("Gesture, Override", "FireYesChef", "FireYesChef.playbackRate", 1f, 0f);
        }

        if (fixedAge >= 0.25 && !hasPlayedSound)
        {
            hasPlayedSound = true;
            Util.PlaySound("Play_chef_skill4_boost_activate", gameObject);
        }

        if (fixedAge >= 1.05 && isAuthority)
        {
            outer.SetNextStateToMain();
        }
    }
    public override void OnSerialize(NetworkWriter writer)
    {
        base.OnSerialize(writer);
    }
    public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
}