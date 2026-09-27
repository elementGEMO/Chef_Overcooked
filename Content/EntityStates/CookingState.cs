using EntityStates;
using RoR2;
using UnityEngine;
using System.Collections.Generic;

namespace ChefOvercooked;
public class CookingState : GenericCharacterMain
{
    private static List<ItemDef> RunMealItemDefs;

    private bool hasPlayedSound;
    private bool hasPlayedAnim;

    public static void CreateEffects()
    {
        Run.onRunStartGlobal += Run_onRunStartGlobal;
    }

    private static void Run_onRunStartGlobal(Run instance)
    {
        PluginConfig.ItemOptions itemPool   = PluginConfig.FoodDrops.Value;
        bool isAlloyedEnabled               = instance.IsExpansionEnabled(ChefOverCookedPlugin.AlloyedCollective);
        RunMealItemDefs                     = [];

        if (itemPool == PluginConfig.ItemOptions.Ingredients || !isAlloyedEnabled)
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
                        RunMealItemDefs.Add(itemDef);
                        break;
                }
            }
        } 
        else
        {
            if (itemPool == PluginConfig.ItemOptions.FoodTier)
            {
                foreach (ItemDef itemDef in ItemCatalog.allItemDefs)
                {
                    if (itemDef == null) continue;
                    if (!instance.IsItemAvailable(itemDef.itemIndex)) continue;
                    if (itemDef.tier != ItemTier.FoodTier) continue;
                    if (itemDef == MonsterMeatItem.ItemDef) continue;

                    RunMealItemDefs.Add(itemDef);
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
                        RunMealItemDefs.Add(itemDef);
                    }
                    else if (itemDef.ContainsTag(ItemTag.FoodRelated))
                    {
                        switch (itemDef.tier)
                        {
                            case ItemTier.Tier1:
                            case ItemTier.Tier2:
                            case ItemTier.Tier3:
                            case ItemTier.Boss:
                                RunMealItemDefs.Add(itemDef);
                                break;
                        }
                    }
                }
            }
        }
    }
    private PickupIndex RandomPickupIndex(CharacterMaster master = null)
    {
        if (master)
        {
            using var _ = HG.ListPool<PickupIndex>.RentCollection(out List<PickupIndex> localMealItems);
            UserProfile userProfile = master.playerCharacterMasterController?.networkUser?.localUser?.userProfile;

            foreach (ItemDef itemDef in RunMealItemDefs)
            {
                PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(itemDef.itemIndex);
                if (userProfile.HasDiscoveredPickup(pickupIndex)) localMealItems.Add(pickupIndex);
            }

            if (localMealItems.Count > 0)
            {
                return localMealItems[UnityEngine.Random.Range(0, localMealItems.Count)];
            }
            else
            {
                return PickupCatalog.FindPickupIndex(ItemCatalog.GetItemDef(RoR2Content.Items.ExtraLifeConsumed.itemIndex).itemIndex);
            }
        }
        else
        {
            return PickupCatalog.FindPickupIndex(RunMealItemDefs[UnityEngine.Random.Range(0, RunMealItemDefs.Count)].itemIndex);
        }
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