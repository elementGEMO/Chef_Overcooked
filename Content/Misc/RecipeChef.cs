using R2API;
using R2API.ContentManagement;
using RoR2;
using RoR2.ContentManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ChefOvercooked;

public class RecipeCatalogChef
{
    public class RecipeString
    {
        public CraftableDef craftableDef;
        public string inputOne;
        public string inputTwo;
        public string output;
        public int amount;
    }
    private static readonly List<RecipeString> RecipeBook = [];
    public static void AddRecipe(string inputOne, string inputTwo, string output, int amount = 1)
    {
        RecipeString recipe = new()
        {
            craftableDef = new(),
            inputOne = inputOne,
            inputTwo = inputTwo,
            output = output,
            amount = amount
        };

        RecipeBook.Add(recipe);
        RegisterRecipeChef.craftableDefs.Add(recipe.craftableDef);
    }
    public static void Init()
    {
        new RegisterRecipeChef().Initialize();
        PickupCatalog.availability.CallWhenAvailable(CreateRecipes);
    }
    private static void CreateRecipes()
    {
        foreach (RecipeString recipeString in RecipeBook)
        {
            CraftableDef craftableDef   = recipeString.craftableDef;
            string errorReason          = "";

            ItemDef inputOneItem        = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(recipeString.inputOne));
            EquipmentDef inputOneEquip  = EquipmentCatalog.GetEquipmentDef(EquipmentCatalog.FindEquipmentIndex(recipeString.inputOne));

            ItemDef inputTwoItem        = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(recipeString.inputTwo));
            EquipmentDef inputTwoEquip  = EquipmentCatalog.GetEquipmentDef(EquipmentCatalog.FindEquipmentIndex(recipeString.inputTwo));

            ItemDef outputItem = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(recipeString.output));

            if (!inputOneItem && !inputOneEquip) errorReason += recipeString.inputOne + ", ";
            if (!inputTwoItem && !inputTwoEquip) errorReason += recipeString.inputTwo + ", ";
            if (!outputItem) errorReason += recipeString.output;

            if (!errorReason.Equals(""))
            {
                Log.Error("Cannot parse recipe of " + recipeString.output + " due to not finding " + errorReason);
                continue;
            }

            Recipe recipe = new()
            {
                amountToDrop = recipeString.amount,
                ingredients =
                [
                    new() { pickup = inputOneItem ? inputOneItem : inputOneEquip },
                    new() { pickup = inputTwoItem ? inputTwoItem : inputTwoEquip }
                ]
            };

            craftableDef.pickup = outputItem;
            craftableDef.recipes = [recipe];
        }

        RecipeBook.Clear();
    }
}

// Uses Swuff's Configurable Crafting internals + PsuedoPulse's
public class RegisterRecipeChef : IContentPackProvider
{
    internal ContentPack recipeContent = new();
    public static List<CraftableDef> craftableDefs = [];
    public string identifier => ChefOverCookedPlugin.PluginName + "_Recipes";
    public void Initialize()
    {
        ContentManager.collectContentPackProviders += new ContentManager.CollectContentPackProvidersDelegate(ContentManager_collectContentpackProviders);
    }
    private void ContentManager_collectContentpackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
    {
        addContentPackProvider.Invoke((IContentPackProvider)(object)this);
    }
    public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
    {
        recipeContent.identifier = identifier;
        recipeContent.craftableDefs.Add([.. craftableDefs]);
        args.ReportProgress(1f);
        yield break;
    }
    public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
    {
        ContentPack.Copy(recipeContent, args.output);
        args.ReportProgress(1f);
        yield break;
    }

    public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
    {
        args.ReportProgress(1f);
        yield break;
    }
}