using RoR2;
using RoR2.Achievements;

namespace ChefOvercooked.Achievements;

[RegisterAchievement("ChefOvercookedWellSeasonedAchievement", "Skills.Chef.Cook", "ActivateChef", 5U, typeof(WellSeasonedServerServerAchievement))]
public class WellSeasonedAchievement : BaseAchievement
{
    private class WellSeasonedServerServerAchievement : BaseServerAchievement
    {
        public override void OnInstall()
        {
            base.OnInstall();
            On.RoR2.CharacterBody.OnInventoryChanged += CharacterBody_OnInventoryChanged;
        }

        private void CharacterBody_OnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);

            int itemCount = self.inventory.GetTotalItemCountOfTier(ItemTier.FoodTier);
            if (itemCount > 5)
                Grant();
        }

        public override void OnUninstall()
        {
            base.OnUninstall();
            On.RoR2.CharacterBody.OnInventoryChanged -= CharacterBody_OnInventoryChanged;
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
