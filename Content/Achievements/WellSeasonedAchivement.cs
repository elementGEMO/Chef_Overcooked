using RoR2;
using RoR2.Achievements;

namespace ChefOvercooked.Achievements;

[RegisterAchievement(Identifier, "Skills.Chef.Cook", "ActivateChef", 3U, typeof(WellSeasonedServerServerAchievement))]
public class WellSeasonedAchievement : BaseAchievement
{
    public const string Identifier = "ChefOvercookedWellSeasonedAchievement";
    private class WellSeasonedServerServerAchievement : BaseServerAchievement
    {
        public override void OnInstall()
        {
            base.OnInstall();
            On.RoR2.CharacterBody.OnInventoryChanged += CharacterBody_OnInventoryChanged;
            //CharacterBody.onBodyInventoryChangedGlobal += CharacterBody_onBodyInventoryChangedGlobal;
            //Inventory.onInventoryChangedGlobal += Inventory_onInventoryChangedGlobal;
            //CharacterBody.
        }

        private void CharacterBody_OnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);

            int itemCount = self.inventory.GetTotalItemCountOfTier(ItemTier.FoodTier);
            if (itemCount >= 5) Grant();
        }



        /*
        private void CharacterBody_onBodyInventoryChangedGlobal(CharacterBody obj)
        {
            int itemCount = obj.inventory.GetTotalItemCountOfTier(ItemTier.FoodTier);
            if (itemCount > 5)
                Grant();
        }
        */

        public override void OnUninstall()
        {
            base.OnUninstall();
            On.RoR2.CharacterBody.OnInventoryChanged -= CharacterBody_OnInventoryChanged;
            //CharacterBody.onBodyInventoryChangedGlobal -= CharacterBody_onBodyInventoryChangedGlobal;
        }
    }

    public override BodyIndex LookUpRequiredBodyIndex()
    {
        return BodyCatalog.FindBodyIndex("ChefBody");
    }

    public override void OnBodyRequirementMet()
    {
        base.OnBodyRequirementMet();
        SetServerTracked(shouldTrack: true);
    }

    public override void OnBodyRequirementBroken()
    {
        SetServerTracked(shouldTrack: false);
        base.OnBodyRequirementBroken();
    }

}
