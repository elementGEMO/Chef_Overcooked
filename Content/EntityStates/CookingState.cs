using EntityStates;
using RoR2;
using UnityEngine;
using System.Collections.Generic;

namespace ChefOvercooked
{
    public class CookingState : GenericCharacterMain
    {

        private static List<ItemDef> FoodItems;
        private static List<ItemDef> FoodItemsNoDLC;

        private bool hasPlayedSound;
        private bool hasPlayedAnim;

        [SystemInitializer(typeof(ItemCatalog))]
        private static void CompileFood()
        {
            FoodItems = [];

            foreach (ItemDef item in ItemCatalog.allItemDefs)
            {
                if (item != null && item.tier == ItemTier.FoodTier)
                {
                    if (item == MonsterMeatItem.ItemDef) continue;
                    FoodItems.Add(item);
                }
            }
        }
        private UniquePickup GetRandomFood()
        {
            int randomIndex = UnityEngine.Random.Range(0, FoodItems.Count);

            UniquePickup tempPickup = new()
            {
                pickupIndex = PickupCatalog.FindPickupIndex(FoodItems[randomIndex].itemIndex),
                decayValue = 1f / 4f
            };

            return tempPickup;
        }
        public override void OnEnter()
        {
            base.OnEnter();

            hasPlayedSound = false;
            hasPlayedAnim = false;
        }
        public override void OnExit()
        {
            Inventory inventory = characterBody.inventory;
            int itemCount       = inventory.GetItemCountEffective(MonsterMeatItem.ItemDef);
            Vector3 modelDirection = GetModelTransform().forward;
            

            for (int i = 0; i < itemCount; i++)
            {
                Vector3 vectorForce = modelDirection * ((i) + 10);
                vectorForce = new(vectorForce.x, 12 + (i * 2), vectorForce.z);

                PickupDropletController.CreatePickupDroplet(GetRandomFood(), characterBody.corePosition, vectorForce, false, false);
            }

            /*
             * if (base.characterBody.isPlayerControlled)
				{
					PickupDropletController.CreatePickupDroplet(uniquePickup, vector2, vector, false, false);
				}
				else
				{
					PickupDef pickupDef = PickupCatalog.GetPickupDef(uniquePickup.pickupIndex);
					ItemIndex itemIndex = pickupDef.itemIndex;
					ScrapperController.CreateItemTakenOrb(vector2, base.gameObject, itemIndex);
					base.characterBody.inventory.GiveItemTemp(itemIndex, 1f);
					string baseToken = (base.teamComponent.teamIndex == TeamIndex.Player) ? "PLAYER_PICKUP" : "MONSTER_PICKUP";
					Chat.SendBroadcastChat(new Chat.PlayerPickupChatMessage
					{
						subjectAsCharacterBody = base.characterBody,
						baseToken = baseToken,
						pickupToken = Language.GetStringFormatted("ITEM_MODIFIER_TEMP", new object[]
						{
							Language.GetStringFormatted(pickupDef.nameToken, Array.Empty<object>())
						}),
						pickupColor = pickupDef.baseColor,
						pickupQuantity = 1U
					});
				}
            */

            inventory.RemoveItemPermanent(MonsterMeatItem.ItemDef, itemCount);

            base.OnExit();
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!hasPlayedAnim)
            {
                PlayAnimation("Gesture, Override", "FireYesChef", "FireYesChef.playbackRate", 1f, 0f);
                hasPlayedAnim = true;
            }

            if (fixedAge >= 0.25 && !hasPlayedSound)
            {
                Util.PlaySound("Play_chef_skill4_boost_activate", gameObject);
                hasPlayedSound = true;
            }

            if (fixedAge >= 1.05 && isAuthority)
            {
                outer.SetNextStateToMain();
            }
        }
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
    }
}