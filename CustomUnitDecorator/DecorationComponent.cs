using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
using CustomComponents;
using HarmonyLib;
using Localize;
using SVGImporter;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CustomUnitDecorator {
  [CustomComponent("DecoratorComponent", true)]
  public class DecoratorComponent : SimpleCustomComponent {
    public string Icon { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public float Importance { get; set; } = 0f;
    public bool DependenciesLoaded(DataManager dataManager, uint loadWeight) {
      if (dataManager == null) { return false; }
      if (string.IsNullOrEmpty(this.Icon)) { return true; }
      return dataManager.Exists(BattleTechResourceType.SVGAsset, this.Icon);
    }
    public void GatherDependencies(DataManager dataManager, DataManager.DependencyLoadRequest dependencyLoad, uint activeRequestWeight) {
      if (dataManager == null) { return; }
      if (string.IsNullOrEmpty(this.Icon) == false) {
        dependencyLoad.RequestResource(BattleTechResourceType.SVGAsset, this.Icon);
      }
    }
  }
  public class DecorationIcons: MonoBehaviour {
    public readonly int MAX_ICONS_ROWS = 3;
    public readonly int MAX_ICONS_COLS = 3;
    public List<List<DecorationIcon>> icons { get; set; } = new List<List<DecorationIcon>>();
    public MechBayMechUnitElement parent { get; set; } = null;
    public RectTransform alertRect { get; set; } = null;
    public void Instantine() {
      parent = this.gameObject.GetComponent<MechBayMechUnitElement>();
      alertRect = parent.alertIconObj.GetComponent<RectTransform>();
      for (int row=0; row < MAX_ICONS_ROWS; ++row) {
        List<DecorationIcon> icons_line = new List<DecorationIcon>();
        icons.Add(icons_line);
        for (int col = 0; col < MAX_ICONS_COLS; ++col) {
          GameObject icon = GameObject.Instantiate(parent.alertIconObj);
          icon.transform.localScale = Vector3.one * 0.7f;
          icon.transform.SetParent(parent.alertIconObj.transform.parent);
          DecorationIcon deco = icon.AddComponent<DecorationIcon>();
          deco.Init();
          icons_line.Add(deco);
          icon.SetActive(true);
        }
      }
    }
    public void Update() {
      if (alertRect == null) { return; }
      Vector2 pos = alertRect.anchoredPosition;
      if (pos.x != 0) {
        pos.x = Core.Settings.iconsXOffset;
      }
      pos.y -= alertRect.sizeDelta.y;
      float starting_x = pos.x;
      float starting_y = pos.y;
      for (int row = 0; row < icons.Count; ++row) {
        pos.x = starting_x;
        for (int col = 0; col < icons[row].Count; ++col) {
          icons[row][col].rect.anchoredPosition = pos;
          pos.x += (icons[row][col].rect.sizeDelta.x * icons[row][col].rect.localScale.x);
        }
        pos.y -= (icons[row][0].rect.sizeDelta.y * icons[row][0].rect.localScale.y);
      }
    }
    public void SetData(MechDef mechDef) {
      try {
        foreach (var icons_line in icons) {
          foreach (var icon in icons_line) {
            icon.gameObject.SetActive(false);
          }
        }
        if (mechDef == null) { return; }
        Log.TWL(0,$"DecorationIcons.SetData:{mechDef.ChassisID}");
        List<List<DecoratorComponent>> components = new List<List<DecoratorComponent>>();
        foreach (var compRef in mechDef.Inventory) {
          if (compRef == null) { continue; }
          if (compRef.Def == null) { continue; }
          List<DecoratorComponent> decorators = new List<DecoratorComponent>(compRef.Def.GetComponents<DecoratorComponent>());
          if (decorators == null) { continue; }
          Log.WL(1, $"{compRef.ComponentDefID}:{decorators.Count}");
          List<DecoratorComponent> deco_line = null;
          foreach (var decorator in decorators) {
            if (decorator == null) { continue; }
            if (deco_line == null) { deco_line = new List<DecoratorComponent>(); };
            deco_line.Add(decorator);
          }
          if ((deco_line != null)&&(deco_line.Count > 0)) {
            deco_line.Sort((a, b) => { return b.Importance.CompareTo(a.Importance); });
            components.Add(deco_line);
          }
        }
        components.Sort((a, b) => { return b[0].Importance.CompareTo(a[0].Importance); });
        int icon_row = 0;
        foreach (var deco_line in components) {
          if (icon_row >= icons.Count) { break; }
          int icon_col = 0;
          foreach (var deco in deco_line) {
            Log.W(1, $"{deco.Text}");
            if (icon_col >= icons[icon_row].Count) { break; }
            this.icons[icon_row][icon_col].SetComponent(deco);
            this.icons[icon_row][icon_col].gameObject.SetActive(true);
            ++icon_col;
          }
          Log.WL(1, "");
          ++icon_row;
        }
      } catch(Exception e) {
        Log.TWL(0,e.ToString(),true);
      }
    }
  }
  public class DecorationIcon: MonoBehaviour {
    public SVGImage icon { get; set; } = null;
    public SVGImage border { get; set; } = null;
    public HBSTooltip tooltip { get; set; } = null;
    public LocalizableText text { get; set; } = null;
    public RectTransform rect { get; set; } = null;
    public Color color { get; set; } = Color.white;
    public DecoratorComponent component { get; set; } = null;
    private bool inited = false;
    public void Awake() {
      this.Init();
    }
    public void Init() {
      try {
        if (inited) { return; }
        inited = true;
        rect = this.gameObject.GetComponent<RectTransform>();
        if (icon == null) { icon = this.gameObject.FindObject<SVGImage>("alert_BG"); }
        if (border == null) { border = this.gameObject.FindObject<SVGImage>("alert_border"); }
        if (tooltip == null) { tooltip = this.gameObject.FindObject<HBSTooltip>("alert-layout"); }
        if (text == null) { text = this.gameObject.FindObject<LocalizableText>("alert_bang"); }
      } catch (Exception e) {
        Log.TWL(0,e.ToString(), true);
      }
    }
    public void SetComponent(DecoratorComponent decoration) {
      this.component = decoration;
      if (decoration == null) { return; }
      if (decoration.Def.dataManager == null) { return; }
      if (this.icon != null) {
        string iconname = string.Empty;
        if (string.IsNullOrEmpty(decoration.Icon) == false) {
          iconname = decoration.Icon;
        } else {
          iconname = decoration.Def.Description.Icon;
        }
        if (string.IsNullOrEmpty(iconname) == false) {
          icon.vectorGraphics = component.Def.dataManager.GetObjectOfType<SVGAsset>(iconname, BattleTechResourceType.SVGAsset);
        }
      }
      if(border != null)border.vectorGraphics = null;
      if ((text != null) && (string.IsNullOrEmpty(component.Text) == false)) {
        text.fontSize = component.Text.Length <= 1?20:12;
        if (component.Text.Length > 2) {
          text.SetText(component.Text.Substring(0, 2));
        } else {
          text.SetText(component.Text);
        }
      }
      Color color = Color.white;
      if (icon != null) {
        UIColorRefTracker uiColorRef = this.icon.gameObject.GetComponent<UIColorRefTracker>();
        if (uiColorRef != null) {
          if (string.IsNullOrEmpty(decoration.Color) == false) {
            if (ColorUtility.TryParseHtmlString(decoration.Color, out color) == false) {
              uiColorRef.SetUIColor(MechComponentDef.GetUIColor(decoration.Def));
            } else {
              uiColorRef.SetCustomColor(UIColor.Custom, color);
            }
          } else {
            uiColorRef.SetUIColor(MechComponentDef.GetUIColor(decoration.Def));
          }
          Log.WL(2, $"color:{uiColorRef.colorRef.UIColor} {uiColorRef.colorRef.color}");
        }
      }
      if (tooltip != null) {
        tooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(TooltipUtilities.MechComponentDefHandlerForTooltip(this.component.Def)));
      }
    }
  }
  [HarmonyPatch(typeof(MechBayMechUnitElement))]
  [HarmonyPatch("SetAlertIcon")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(bool), typeof(UIColor) })]
  public static class MechBayMechUnitElement_SetAlertIcon {
    public static void Postfix(MechBayMechUnitElement __instance, bool shouldShow, UIColor color) {
      try {
        Log.TWL(0, $"MechBayMechUnitElement.SetAlertIcon {(__instance.mechDef == null?"null": __instance.mechDef.ChassisID)}");
        if (__instance.mechDef == null) { return; }
        DecorationIcons deco = __instance.gameObject.GetComponent<DecorationIcons>();
        if(deco == null) {
          deco = __instance.gameObject.AddComponent<DecorationIcons>();
          deco.Instantine();
        }
        deco.SetData(__instance.mechDef);
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(MechComponentDef))]
  [HarmonyPatch("DependenciesLoaded")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(uint) })]
  public static class PilotDef_DependenciesLoaded {
    public static void Postfix(MechComponentDef __instance, uint loadWeight, ref bool __result) {
      try {
        if (__result == false) { return; }
        DecoratorComponent decoration = __instance.GetComponent<DecoratorComponent>();
        if (decoration == null) { return; }
        Log.TWL(0, "MechComponentDef.DependenciesLoaded " + __instance.Description.Id);
        if (decoration.DependenciesLoaded(__instance.dataManager, loadWeight) == false) { __result = false; }
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(MechComponentDef))]
  [HarmonyPatch("GatherDependencies")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(DataManager), typeof(DataManager.DependencyLoadRequest), typeof(uint) })]
  public static class PilotDef_GatherDependencies {
    public static void Postfix(MechComponentDef __instance, DataManager dataManager, DataManager.DependencyLoadRequest dependencyLoad, uint activeRequestWeight) {
      try {
        DecoratorComponent decoration = __instance.GetComponent<DecoratorComponent>();
        if (decoration == null) { return; }
        Log.TWL(0, "MechComponentDef.GatherDependencies " + __instance.Description.Id);
        decoration.GatherDependencies(dataManager, dependencyLoad, activeRequestWeight);
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(TooltipPrefab_Mech))]
  [HarmonyPatch("SetData")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(object) })]
  public static class TooltipPrefab_Mech_SetData {
    private static MethodInfo DropCostsEnhanced_DropCostManager_Instance = null;
    private static MethodInfo DropCostsEnhanced_DropCostManager_CalculateMechCost = null;
    public static int CalculateMechCost(this MechDef mechDef) {
      float currentValue = 0.0f;
      float maxValue = 0.0f;
      try {
        if (DropCostsEnhanced_DropCostManager_Instance == null) { MechStatisticsRules.CalculateCBillValue(mechDef, ref currentValue, ref maxValue); } else
        if (DropCostsEnhanced_DropCostManager_CalculateMechCost == null) { MechStatisticsRules.CalculateCBillValue(mechDef, ref currentValue, ref maxValue); } else {
          object Instance = DropCostsEnhanced_DropCostManager_Instance.Invoke(null, new object[] { });
          System.Int32 result = (System.Int32)DropCostsEnhanced_DropCostManager_CalculateMechCost.Invoke(Instance, new object[] { mechDef });
          currentValue = result;
          //Log.TWL(0,$"{mechDef.chassisID} CalculateMechCost:{result}:{result.GetType()}");

          //currentValue = (float)
        }
      } catch (Exception e) {
        Log.TWL(0,e.ToString());
      }
      return Mathf.RoundToInt(currentValue / 1000f);
    }
    public static void PrepareAPI() {
      Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
      Log.TWL(0, "DropCostsEnhanced Detecting");
      Assembly DropCostsEnhanced_assembly = null;
      foreach (Assembly assembly in assemblies) {
        if (assembly.FullName.StartsWith("DropCostsEnhanced, Version=")) {
          DropCostsEnhanced_assembly = assembly;
          break;
        }
      }
      if (DropCostsEnhanced_assembly == null) { return; }
      Log.WL(1, "DropCostsEnhanced assembly found");
      Type DropCostsEnhanced_DropCostManager = DropCostsEnhanced_assembly.GetType("DropCostsEnhanced.DropCostManager");
      if (DropCostsEnhanced_DropCostManager == null) { return; }
      Log.WL(1, "DropCostsEnhanced.DropCostManager found");
      DropCostsEnhanced_DropCostManager_Instance = AccessTools.PropertyGetter(DropCostsEnhanced_DropCostManager, "Instance");
      if (DropCostsEnhanced_DropCostManager_Instance == null) { return; }
      Log.WL(1, "DropCostsEnhanced.DropCostManager.Instance found");
      DropCostsEnhanced_DropCostManager_CalculateMechCost = AccessTools.Method(DropCostsEnhanced_DropCostManager, "CalculateMechCost");
      if (DropCostsEnhanced_DropCostManager_CalculateMechCost == null) { return; }
      Log.WL(1, "DropCostsEnhanced.DropCostManager.CalculateMechCost found");
    }
    public static void Postfix(TooltipPrefab_Mech __instance, object data) {
      try {
        if (data is MechDef mechDef) {
          __instance.WeightField.SetText("(Class:{0}) <b>({1}:{2}K)</b>", mechDef.Chassis.weightClass,Strings.CurrentCulture == Strings.Culture.CULTURE_RU_RU?"Цен.":"Cost", mechDef.CalculateMechCost().ToString("N0"));
        }
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }
}