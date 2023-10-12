using BattleTech;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CustomUnitDecorator {
  public class CUDSettings {
    public bool debugLog { get; set; } = false;
    public float iconsXOffset { get; set; } = 3f;
  }
  public static class Core {
    public static readonly float Epsilon = 0.001f;
    public static string BaseDir { get; private set; }
    public static CUDSettings Settings { get; set; } = new CUDSettings();
    public static void FinishedLoading(List<string> loadOrder) {
      Log.TWL(0, "FinishedLoading", true);
      try {
        TooltipPrefab_Mech_SetData.PrepareAPI();
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
    public static T FindObject<T>(this GameObject go, string name) where T : Component {
      T[] arr = go.GetComponentsInChildren<T>(true);
      foreach (T component in arr) { if (component.gameObject.transform.name == name) { return component; } }
      return null;
    }

    public static void Init(string directory, string settingsJson) {
      Log.BaseDirectory = directory;
      Log.InitLog();
      Core.BaseDir = directory;
      Core.Settings = JsonConvert.DeserializeObject<CustomUnitDecorator.CUDSettings>(settingsJson);
      Log.TWL(0, "Initing... " + directory + " version: " + Assembly.GetExecutingAssembly().GetName().Version, true);
      try {
        CustomComponents.Registry.RegisterSimpleCustomComponents(Assembly.GetExecutingAssembly());
        var harmony = new Harmony("io.kmission.customunitdecorator");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }
}
