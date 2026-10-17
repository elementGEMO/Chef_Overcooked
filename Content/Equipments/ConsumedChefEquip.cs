using RoR2;
using RoR2.ExpansionManagement;
using RoR2BepInExPack.GameAssetPaths.Version_1_35_0;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ChefOvercooked;
using static StringHelper;
using static HelperFontColor;

public class ConsumedChefEquip : EquipmentBase
{
    protected override ExpansionDef RequiredExpansion => Addressables.LoadAssetAsync<ExpansionDef>(RoR2_DLC3.DLC3_asset).WaitForCompletion();
    protected override string Name => "ConsumedChefEquip";
    public static EquipmentDef EquipmentDef;
    protected override float Cooldown => 70f;
    protected override bool CanDrop => false;

    protected override GameObject PickupModelPrefab => ChefOverCookedPlugin.Bundle.LoadAsset<GameObject>("summonChefModel");
    protected override Sprite PickupIconSprite => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("texConsumedChefIcon");
    protected override string PickupText => "Seems like Wandering CHEF is done serving orders.";
    protected override string Description => "Seems like Wandering CHEF is done serving orders.";
    protected override string DisplayName => "À La Carte (Consumed)";

    protected override void Initialize()
    {
        EquipmentDef = Value;
    }
    protected override bool ActivateEquip(EquipmentSlot slot)
    {
        if (slot.characterBody && slot.characterBody.gameObject)
        {
            Chat.SendBroadcastChat(new Chat.BodyChatMessage
            {
                bodyObject = slot.characterBody.gameObject,
                token = "Yes, CHEF!"
            });
        }
        return true;
    }
    protected override void LogDisplay()
    {
        ModelPanelParameters modelParam = PickupModelPrefab.AddComponent<ModelPanelParameters>();
        var foundMesh = PickupModelPrefab.transform.GetChild(0);

        if (!foundMesh) return;

        modelParam.focusPointTransform = foundMesh;
        modelParam.cameraPositionTransform = foundMesh;
        modelParam.minDistance = 3f;
        modelParam.maxDistance = 12f;
    }
}