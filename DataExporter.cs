﻿using Eco.Core.Utils;
using Eco.Gameplay.DynamicValues;
using Eco.Gameplay.Items;
using Eco.Gameplay.Items.Recipes;
using Eco.Gameplay.Modules;
using Eco.Gameplay.Skills;
using Eco.Mods.TechTree;
using Eco.Shared.Localization;
using Newtonsoft.Json;

namespace CavRnMods.DataExporter;

public static class DataExporter
{
    public static readonly TalentGroup[] AllTalentGroups = typeof(TalentGroup).InstancesOfCreatableTypesParallel<TalentGroup>().ToArray();
    public static readonly List<Item> CraftingTables = RecipeManager.AllRecipes.Select(r => r.Family.CraftingTable).Distinct().ToList();

    public static void ExportAll()
    {
        var options = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        var data = new ExportedData(
            Skill.AllSkills.Select(skill => new SkillExported(skill)).ToList(),
            Item.AllItemsExceptHidden.Select(item => new ItemExported(item)).ToList(),
            (
                from tag in TagManager.AllTags
                where Item.AllItemsExceptHidden.Where(x => x.Tags().Contains(tag)).Select(x => x.Name).Any()
                select new TagExported(tag)
            ).ToList(),
            RecipeManager.AllRecipeFamilies.SelectMany(recipeFamily => recipeFamily.Recipes.Select(recipe => new RecipeExported(recipeFamily, recipe))).ToList()
        );

        File.WriteAllText("exported_data.json", JsonConvert.SerializeObject(data, options));
    }

    public static Dictionary<string, string> GenerateLocalization(string name)
    {
        var localizedString = new Dictionary<string, string>();

        foreach (var keyValue in SupportedLanguageUtils.DictToCultureLangCode)
        {
            if (localizedString.ContainsKey(keyValue.Value)) continue;

            localizedString.Add(keyValue.Value, Localizer.LocalizeString(name, keyValue.Key));
        }

        return localizedString;
    }
}

[JsonObject(MemberSerialization.OptIn)]
public class ExportedData
{
    [JsonProperty] public List<SkillExported> Skills { get; set; }

    [JsonProperty] public List<ItemExported> Items { get; set; }

    [JsonProperty] public List<TagExported> Tags { get; set; }

    [JsonProperty] public List<RecipeExported> Recipes { get; set; }

    public ExportedData(List<SkillExported> skills, List<ItemExported> items, List<TagExported> tags, List<RecipeExported> recipes)
    {
        this.Skills = skills;
        this.Items = items;
        this.Tags = tags;
        this.Recipes = recipes;
    }
}

[JsonObject(MemberSerialization.OptIn)]
public class RecipeExported
{
    [JsonProperty] public string Name { get; set; }

    [JsonProperty] public Dictionary<string, string> LocalizedName { get; set; }

    [JsonProperty] public string FamilyName { get; set; }

    [JsonProperty] public float CraftMinutes { get; set; }

    [JsonProperty] public string RequiredSkill { get; set; }

    [JsonProperty] public int RequiredSkillLevel { get; set; }

    [JsonProperty] public bool IsBlueprint { get; set; }

    [JsonProperty] public bool IsDefault { get; set; }

    [JsonProperty] public float Labor { get; set; }

    [JsonProperty] public String CraftingTable { get; set; }

    [JsonProperty] public List<IngredientExported> Ingredients { get; set; }

    [JsonProperty] public List<ProductExported> Products { get; set; }

    public RecipeExported(RecipeFamily recipeFamily, Recipe recipe)
    {
        this.Name = recipe.GetType() != typeof(Recipe) ? recipe.GetType().Name : recipeFamily.GetType().Name;
        this.LocalizedName = DataExporter.GenerateLocalization(recipe.DisplayName);
        this.FamilyName = recipeFamily.RecipeName;
        this.CraftMinutes = recipeFamily.CraftMinutes.GetBaseValue;

        var skill = recipeFamily.RequiredSkills.FirstOrDefault();

        this.RequiredSkill = skill != null ? Item.Get(skill.SkillType).Name : "";
        this.RequiredSkillLevel = skill?.Level ?? 0;

        this.IsBlueprint = recipe.RequiresStrangeBlueprint;
        this.IsDefault = recipe == recipeFamily.DefaultRecipe;

        this.Labor = recipeFamily.Labor;

        this.CraftingTable = recipeFamily.CraftingTable.Name;

        this.Ingredients = new List<IngredientExported>();
        foreach (var ingredient in recipe.Ingredients)
        {
            this.Ingredients.Add(new IngredientExported(ingredient));
        }

        this.Products = new List<ProductExported>();
        foreach (var product in recipe.Products)
        {
            this.Products.Add(new ProductExported(product));
        }
    }
}

[JsonObject(MemberSerialization.OptIn)]
public class ProductExported
{
    [JsonProperty] public string ItemOrTag { get; set; }

    [JsonProperty] public float Quantity { get; set; }

    [JsonProperty] public bool IsDynamic { get; set; }

    [JsonProperty] public string Skill { get; set; }

    [JsonProperty] public bool LavishTalent { get; set; }

    public ProductExported(CraftingElement craftingElement)
    {
        this.ItemOrTag = craftingElement.Item.Name;

        if (craftingElement.Quantity is ModuleModifiedValue moduleModifiedValue)
        {
            this.Quantity = craftingElement.Quantity.GetBaseValue;
            this.IsDynamic = true;

            var skillType = moduleModifiedValue.SkillType;
            this.Skill = skillType != null ? Item.Get(skillType).Name : "";
        }
        else if (craftingElement.Quantity is MultiDynamicValue multiDynamicValue)
        {
            this.Quantity = craftingElement.Quantity.GetBaseValue;
            this.IsDynamic = true;

            var skillType = ((ModuleModifiedValue)multiDynamicValue.Values[0]).SkillType;
            this.Skill = skillType != null ? Item.Get(skillType).Name : "";

            this.LavishTalent = true;
        }
        else
        {
            this.Quantity = craftingElement.Quantity.GetBaseValue;
            this.IsDynamic = false;
            this.Skill = "";
            this.LavishTalent = false;
        }
    }
}

[JsonObject(MemberSerialization.OptIn)]
public class IngredientExported
{
    [JsonProperty] public string ItemOrTag { get; set; }

    [JsonProperty] public float Quantity { get; set; }

    [JsonProperty] public bool IsDynamic { get; set; }

    [JsonProperty] public string Skill { get; set; }

    [JsonProperty] public bool LavishTalent { get; set; }

    public IngredientExported(IngredientElement ingredientElement)
    {
        this.ItemOrTag = ingredientElement.Tag?.Name ?? ingredientElement.Item.Name;

        if (ingredientElement.Quantity is ModuleModifiedValue moduleModifiedQuantity)
        {
            this.Quantity = ingredientElement.Quantity.GetBaseValue;
            this.IsDynamic = true;

            var skillType = moduleModifiedQuantity.SkillType;
            this.Skill = skillType != null && skillType != typeof(Skill) ? Item.Get(skillType).Name : "";
        }
        else if (ingredientElement.Quantity is MultiDynamicValue multiDynamicQuantity)
        {
            this.Quantity = ingredientElement.Quantity.GetBaseValue;
            this.IsDynamic = true;

            var skillType = ((ModuleModifiedValue)multiDynamicQuantity.Values[0]).SkillType;
            this.Skill = skillType != null ? Item.Get(skillType).Name : "";

            this.LavishTalent = true;
        }
        else
        {
            this.Quantity = ingredientElement.Quantity.GetBaseValue;
            this.IsDynamic = false;
            this.Skill = "";
            this.LavishTalent = false;
        }
    }
}

[JsonObject(MemberSerialization.OptIn)]
public class ItemExported
{
    [JsonProperty] public string Name { get; set; }

    [JsonProperty] public Dictionary<string, string> LocalizedName { get; set; }

    [JsonProperty] public bool? IsPluginModule { get; set; }

    [JsonProperty] public float? PluginModulePercent { get; set; }

    [JsonProperty] public bool? IsCraftingTable { get; set; }

    [JsonProperty] public string[]? CraftingTablePluginModules { get; set; }

    public ItemExported(Item item)
    {
        this.Name = item.Name;
        this.LocalizedName = DataExporter.GenerateLocalization(item.DisplayName);

        if (item is EfficiencyModule efficiencyModule)
        {
            this.IsPluginModule = true;
            this.PluginModulePercent = efficiencyModule.SkillType != null ? efficiencyModule.SkillMultiplier : efficiencyModule.GenericMultiplier;
        }

        if (DataExporter.CraftingTables.Contains(item))
        {
            this.IsCraftingTable = true;

            var stackables = ItemAttribute.Get<AllowPluginModulesAttribute>(item.Type)?.GetStackables();

            if (stackables != null)
            {
                List<string> modules = new List<string>();

                foreach (var stackable in stackables)
                {
                    if (!(stackable is Tag tag))
                    {
                        modules.Add(Item.Get(stackable.GetType()).Name);
                        continue;
                    }

                    if (!TagManager.TagToTypes.TryGetValue(tag, out var moduleTypes))
                        continue;

                    foreach (var moduleType in moduleTypes)
                        modules.Add(Item.Get(moduleType).Name);
                }

                this.CraftingTablePluginModules = modules.ToArray();
            }
        }
    }
}

[JsonObject(MemberSerialization.OptIn)]
public class TagExported
{
    [JsonProperty] public string Name { get; set; }

    [JsonProperty] public Dictionary<string, string> LocalizedName { get; set; }

    [JsonProperty] public string[] AssociatedItems { get; set; }

    public TagExported(Tag tag)
    {
        this.Name = tag.Name;
        this.LocalizedName = DataExporter.GenerateLocalization(tag.DisplayName);
        this.AssociatedItems = Item.AllItemsExceptHidden.Where(x => x.Tags().Contains(tag)).Select(x => x.Name).ToArray();
    }
}

[JsonObject(MemberSerialization.OptIn)]
public class SkillExported
{
    [JsonProperty] public string Name { get; set; }

    [JsonProperty] public Dictionary<string, string> LocalizedName { get; set; }

    [JsonProperty] public string? Profession { get; set; }

    [JsonProperty] public float[] LaborReducePercent { get; set; }

    [JsonProperty] public float? LavishTalentValue { get; set; }

    public SkillExported(Skill skill)
    {
        this.Name = skill.Name;
        this.LocalizedName = DataExporter.GenerateLocalization(skill.DisplayName);
        this.Profession = skill.Prerequisites.FirstOrDefault()?.SkillType.Name;
        this.LaborReducePercent = skill.MultiStrategy.Factors;

        var lavishTalentGroup = DataExporter.AllTalentGroups.FirstOrDefault(tg => tg.OwningSkill == skill.Type && tg.Type.ToString().Contains("LavishWorkspace"));

        if (lavishTalentGroup is not null)
        {
            this.LavishTalentValue = TalentManager.AllTalents.FirstOrDefault(t => t.GetType() == lavishTalentGroup.Talents[0])?.Value;
        }
    }
}
