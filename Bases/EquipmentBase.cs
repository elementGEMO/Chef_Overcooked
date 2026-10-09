using RoR2;
using R2API;
using UnityEngine;

namespace ChefOvercooked;
public abstract class EquipmentBase : GenericBase<EquipmentDef>
{
    protected virtual bool IsConsumed => false;
    protected virtual bool CanDrop => true;
    protected virtual float Cooldown => 0f;

    protected virtual GameObject PickupModelPrefab => null;
    protected virtual Sprite PickupIconSprite => null;
    
    protected virtual string DisplayName => null;
    protected virtual string Description => null;
    protected virtual string PickupText => null;
    protected virtual string Lore => null;

    protected virtual ItemDisplayRuleDict ItemDisplay() => null;
    
    protected override void Create()
    {
        Value = ScriptableObject.CreateInstance<EquipmentDef>();
        Value.name = Name;

        Value.isConsumed = IsConsumed;
        Value.canDrop = CanDrop;

        Value.cooldown = Cooldown;

        Value.pickupModelPrefab = PickupModelPrefab;
        Value.pickupIconSprite = PickupIconSprite;

        if (Value)
        {
            Value.AutoPopulateTokens();

            if (!string.IsNullOrWhiteSpace(DisplayName)) Value.nameToken = HelperLanguage.LanguageAdd(Value.nameToken, DisplayName);
            if (!string.IsNullOrWhiteSpace(Description)) Value.descriptionToken = HelperLanguage.LanguageAdd(Value.descriptionToken, Description);
            if (!string.IsNullOrWhiteSpace(PickupText)) Value.pickupToken = HelperLanguage.LanguageAdd(Value.pickupToken, PickupText);
            if (!string.IsNullOrWhiteSpace(Lore)) Value.loreToken = HelperLanguage.LanguageAdd(Value.loreToken, Lore);

            LogDisplay();
        }

        ItemAPI.Add(new CustomEquipment(Value, ItemDisplay()));

        On.RoR2.EquipmentSlot.PerformEquipmentAction += EquipmentSlot_PerformEquipmentAction;
    }

    internal bool EquipmentSlot_PerformEquipmentAction(On.RoR2.EquipmentSlot.orig_PerformEquipmentAction orig, EquipmentSlot self, EquipmentDef equipmentDef)
    {
        if (equipmentDef == Value)
            return ActivateEquip(self);
        else
            return orig(self, equipmentDef);
    }
    protected virtual bool ActivateEquip(EquipmentSlot slot) => false;
    protected virtual void LogDisplay() { }
}