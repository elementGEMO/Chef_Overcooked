using R2API;
using RoR2;
using System.ComponentModel;
using System;
using UnityEngine;

namespace ChefOvercooked;
public abstract class ItemBase : GenericBase<ItemDef>
{
    protected virtual bool IsConsumed   => false;
    protected virtual bool IsRemovable  => false;
    protected virtual bool IsHidden     => false;

    protected virtual GameObject PickupModelPrefab  => null;
    protected virtual Sprite PickupIconSprite       => null;

    protected virtual ItemTag[] Tags        => [];
    protected virtual CombinedItemTier Tier => ItemTier.NoTier;
    
    protected virtual string DisplayName => null;
    protected virtual string Description => null;
    protected virtual string PickupText => null;
    protected virtual string Lore => null;

    protected virtual ItemDisplayRuleDict ItemDisplay() => null;
    
    protected override void Create()
    {
        Value = ScriptableObject.CreateInstance<ItemDef>();
        Value.name = Name;

        Value.isConsumed = IsConsumed;
        Value.canRemove = IsRemovable;
        Value.hidden = IsHidden;

        Value.pickupModelPrefab = PickupModelPrefab;
        Value.pickupIconSprite = PickupIconSprite;

        Value.tags = Tags;
        Value.tier = Tier;
        Value._itemTierDef = Tier;
        Value.deprecatedTier = Tier;

        if (Value)
        {
            Value.AutoPopulateTokens();

            if (!string.IsNullOrWhiteSpace(DisplayName)) Value.nameToken = HelperLanguage.LanguageAdd(Value.nameToken, DisplayName);
            if (!string.IsNullOrWhiteSpace(Description)) Value.descriptionToken = HelperLanguage.LanguageAdd(Value.descriptionToken, Description);
            if (!string.IsNullOrWhiteSpace(PickupText)) Value.pickupToken = HelperLanguage.LanguageAdd(Value.pickupToken, PickupText);
            if (!string.IsNullOrWhiteSpace(Lore)) Value.loreToken = HelperLanguage.LanguageAdd(Value.loreToken, Lore);

            LogDisplay();
        }

        ItemAPI.Add(new CustomItem(Value, ItemDisplay()));
    }
    protected virtual void LogDisplay() { }
}