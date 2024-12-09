﻿using Eco.Core.Plugins;
using Eco.Shared.Utils;

namespace EcoGnomeMod;

using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;

public class EcoGnomeConfig: Singleton<EcoGnomeConfig>
{
    public string EcoGnomeUrl { get; set; } = "https://eco-gnome.com";
}

public class EcoGnomePlugin: Singleton<EcoGnomePlugin>, IModKitPlugin, IInitializablePlugin, IConfigurablePlugin
{
    public static ThreadSafeAction OnSettingsChanged = new();
    public IPluginConfig PluginConfig => this.config;
    private readonly PluginConfig<EcoGnomeConfig> config;
    public EcoGnomeConfig Config => this.config.Config;
    public ThreadSafeAction<object, string> ParamChanged { get; set; } = new();

    public EcoGnomePlugin()
    {
        this.config = new PluginConfig<EcoGnomeConfig>("EcoGnome");
        this.SaveConfig();
    }

    public string GetStatus()
    {
        return "OK";
    }

    public string GetCategory()
    {
        return "Mods";
    }

    public void Initialize(TimedTask timer)
    {
        DataExporter.ExportAll();
    }

    public object GetEditObject() => this.config.Config;
    public void OnEditObjectChanged(object o, string param) { this.SaveConfig(); }
}
