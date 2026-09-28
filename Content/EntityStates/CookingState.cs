using EntityStates;
using RoR2;
using UnityEngine;
using System.Collections.Generic;

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
            if (itemDef.tier != ItemTier.FoodTier) return NumFromTier(PickupCatalog.FindPickupIndex(itemDef.itemIndex));

            CraftableCatalog.RecipeEntry[] recipeRelation = CraftableCatalog.FindAllRelatedRecipes(PickupCatalog.FindPickupIndex(itemDef.itemIndex));
            float avgFrequency = 0;

            foreach (CraftableCatalog.RecipeEntry recipe in recipeRelation)
            {
                foreach (PickupIndex pickupIndex in recipe.GetAllPickups())
                {
                    avgFrequency = avgFrequency == 0 ? avgFrequency + NumFromTier(pickupIndex) : NumFromTier(pickupIndex);
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
    private PickupIndex RandomPickupIndex(CharacterMaster master = null)
    {
        if (!master) PickupCatalog.FindPickupIndex(WeightedMealItemDefs[Random.Range(0, WeightedMealItemDefs.Count)].itemIndex);

        HG.ListPool<PickupIndex>.RentCollection(out List<PickupIndex> localMealItems);
        UserProfile userProfile = master.playerCharacterMasterController?.networkUser?.localUser?.userProfile;

        foreach (ItemDef itemDef in WeightedMealItemDefs)
        {
            PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(itemDef.itemIndex);
            if (userProfile.HasDiscoveredPickup(pickupIndex)) localMealItems.Add(pickupIndex);
        }

        return localMealItems.Count > 0 ? localMealItems[Random.Range(0, localMealItems.Count)] : PickupCatalog.FindPickupIndex(RoR2Content.Items.ExtraLifeConsumed.itemIndex);
    }

    public override void OnEnter()
    {
        base.OnEnter();

        hasPlayedSound  = false;
        hasPlayedAnim   = false;
    }
    public override void OnExit()
    {
        Inventory inventory = characterBody.inventory;
        int itemCount       = inventory.GetItemCountEffective(MonsterMeatItem.ItemDef);

        if (characterBody.isPlayerControlled)
        {
            Vector3 modelDirection = GetModelTransform().forward;

            for (int i = 0; i < itemCount; i++)
            {
                Vector3 vectorForce = modelDirection * (i + 10);
                vectorForce = new(vectorForce.x, 12 + (i * 2), vectorForce.z);

                UniquePickup randomPickup = new()
                {
                    pickupIndex = RandomPickupIndex(characterBody.master),
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
    public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
}