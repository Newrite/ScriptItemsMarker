﻿namespace ScriptItemsMarker;

#pragma warning disable CA1416

using System;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Noggog;
using static Mutagen.Bethesda.FormKeys.SkyrimSE.Skyrim.Keyword;
using System.Threading.Tasks;

[JsonObject(ItemRequired = Required.Always)]
public partial class Config
{
  [JsonProperty("keywordSetting")] public KeywordSetting KeywordSetting { get; set; }

  [JsonConstructor]
  private Config(KeywordSetting keywordSetting)
  {
    KeywordSetting = keywordSetting;
  }
}

public class KeywordSetting
{
  [JsonProperty("modname")] public string ModName { get; set; } = "SomeEsp.esp";
  [JsonProperty("formid")] public string FormId { get; set; } = "0x800";
}

public partial class Settings
{
  [JsonProperty("keywordSetting")] public KeywordSetting KeywordSetting { get; set; } = new();
}

public static class Program
{
  static Lazy<Settings> LazySettings = new();
  private static Settings Settings { get; set; } = null!;

  public static async Task<int> Main(string[] args)
  {
    return await SynthesisPipeline.Instance.AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
      .SetAutogeneratedSettings(nickname: "Settings", path: "config.json", out LazySettings)
      .SetTypicalOpen(GameRelease.SkyrimSE, new ModKey("ScriptItemsMarker.esp", ModType.Plugin))
      .Run(args);
  }

  private static void SynthesisLog(string message, bool special = false)
  {
    if (special)
    {
      Console.WriteLine();
      Console.Write(">>> ");
    }

    Console.WriteLine(message);
    if (special) Console.WriteLine();
  }

  private static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
  {
    var configFilePath = Path.Combine(state.ExtraSettingsDataPath, "config.json");
    string errorMessage;

    if (!File.Exists(configFilePath))
    {
      errorMessage = "Cannot find config.json for Custom Weights.";
      SynthesisLog(errorMessage);
      throw new FileNotFoundException(errorMessage, configFilePath);
    }

    try
    {
      Settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(configFilePath),
        new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore})!;
    }
    catch (JsonSerializationException jsonException)
    {
      errorMessage = "Failed to Parse config.json, please review the format.";
      SynthesisLog(errorMessage);
      throw new JsonSerializationException(errorMessage, jsonException);
    }


    var weights = Settings.KeywordSetting;
    var modName = Settings.KeywordSetting.ModName;
    var formId = Settings.KeywordSetting.FormId;
    
    SynthesisLog($"Item Weight Configuration: {modName} {formId}", true);


    SynthesisLog("Done patching script items marker!", true);
  }
}