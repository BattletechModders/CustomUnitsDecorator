using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
using CustomComponents;
using HarmonyLib;
using IRBTModUtils;
using Localize;
using SVGImporter;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

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
  public static class GifAnimatorHelper {
    public static GifSpriteAnimator AddGitSprite(this Image img, string id, bool alwaysAnimate) {
      GifSpriteAnimator gifSpriteAnimator = null;
      try {
        if (img == null) { return null; }
        if (string.IsNullOrEmpty(id)) { return null; }
        gifSpriteAnimator = img.gameObject.GetComponent<GifSpriteAnimator>();
        if (gifSpriteAnimator == null) {
          gifSpriteAnimator = img.gameObject.AddComponent<GifSpriteAnimator>();
          gifSpriteAnimator.portrait = img;
        }
        gifSpriteAnimator.gif = null;
        gifSpriteAnimator.alwaysAnimate = false;
        gifSpriteAnimator.hovered = false;
        string animate_id = id + Core.ANIMATED_ICON_SUFFIX;
        gifSpriteAnimator.gif = GifStorageHelper.GetSprites(animate_id);
        Log.WL(0, $"AddGitSprite:{animate_id} {(gifSpriteAnimator.gif == null ? "not exists" : "exists")}");
        if ((gifSpriteAnimator.gif == null) && (Core.HOVER_SPRITE != null)) { gifSpriteAnimator.gif = Core.HOVER_SPRITE; }
        if (gifSpriteAnimator.gif != null) {
          gifSpriteAnimator.origSprite = img.sprite;
        } else {
          gifSpriteAnimator.origSprite = null;
          gifSpriteAnimator.gif = GifStorageHelper.GetSprites(id);
        }
        gifSpriteAnimator.Reset();
        gifSpriteAnimator.OnUnhover();
        gifSpriteAnimator.alwaysAnimate = alwaysAnimate;
      }catch(Exception e) {
        Log.TWL(0, e.ToString());
        UIManager.logger.LogException(e);
      }
      Log.WL(1, $" success");
      return gifSpriteAnimator;
    }
    public static void AddGitSpriteHover(this Image img, GameObject hoverCarrier, string id, bool alwaysAnimate) {
      if (img == null) { return; }
      var gifSpriteAnimator = img.AddGitSprite(id, alwaysAnimate);
      if (gifSpriteAnimator == null) { return; }
      if (hoverCarrier == null) { return; }
      GifSpriteAnimatorHover hover = hoverCarrier.GetComponent<GifSpriteAnimatorHover>();
      if (hover == null) { hover = hoverCarrier.AddComponent<GifSpriteAnimatorHover>(); }
      hover.imageAnimator = gifSpriteAnimator;
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
  //[HarmonyPatch(typeof(MechBayMechUnitElement))]
  //[HarmonyPatch("SetLabel")]
  //[HarmonyPatch(MethodType.Normal)]
  //[HarmonyPatch(new Type[] { typeof(bool) })]
  //public static class MechBayMechUnitElement_SetLabel {
  //  public static void Postfix(MechBayMechUnitElement __instance) {
  //    try {
  //      Log.TWL(0, $"MechBayMechUnitElement.SetLabel {(__instance.mechDef == null ? "null" : __instance.mechDef.ChassisID)}");
  //      if (__instance.mechDef == null) { return; }
  //      if (Core.HOVER_SPRITE == null) { return; }
  //      if (__instance.mechImage != null) {
  //        GifSpriteAnimator gifSpriteAnimator = __instance.mechImage.gameObject.GetComponent<GifSpriteAnimator>();
  //        if (gifSpriteAnimator == null) {
  //          gifSpriteAnimator = __instance.mechImage.gameObject.AddComponent<GifSpriteAnimator>();
  //        }
  //        gifSpriteAnimator.portrait = __instance.mechImage;
  //        //gifSpriteAnimator.gif = null;
  //        //if (__instance.mechDef != null) {
  //          gifSpriteAnimator.gif = Core.HOVER_SPRITE;
  //        //}
  //        gifSpriteAnimator.Reset();
  //        Log.WL(1, "gifSpriteAnimator.gif: " + (gifSpriteAnimator.gif == null ? "null" : "not null"));
  //        SlotHover hover = __instance.MechTooltip.gameObject.GetComponent<SlotHover>();
  //        if (hover == null) { hover = __instance.MechTooltip.gameObject.AddComponent<SlotHover>(); }
  //        hover.animator = gifSpriteAnimator;
  //      }
  //    } catch (Exception e) {
  //      Log.TWL(0, e.ToString(), true);
  //    }
  //  }
  //}
  [HarmonyPatch(typeof(MechComponentDef))]
  [HarmonyPatch("DependenciesLoaded")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(uint) })]
  public static class MechComponentDef_DependenciesLoaded {
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
  public static class MechComponentDef_GatherDependencies {
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
          __instance.PNGImage.AddGitSprite(mechDef.Chassis.Description.Icon, true);
        }
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }
  //[HarmonyPatch(typeof(InventoryDataObject_SalvageFullMech))]
  //[HarmonyPatch("RefreshInfoOnWidget")]
  //[HarmonyPatch(MethodType.Normal)]
  //public static class InventoryDataObject_SalvageFullMech_RefreshInfoOnWidget {
  //  public static void Postfix(InventoryDataObject_SalvageFullMech __instance, InventoryItemElement theWidget) {
  //    try {
  //      Log.TWL(0, "InventoryDataObject_SalvageFullMech.RefreshInfoOnWidget");
  //      if (__instance.mechDef != null) {
  //        theWidget.iconMech.AddGitSprite(__instance.mechDef.Description.Icon, false);
  //      }
  //    } catch (Exception e) {
  //      Log.TWL(0, e.ToString(), true);
  //    }
  //  }
  //}
  //[HarmonyPatch(typeof(InventoryDataObject_ShopFullMech))]
  //[HarmonyPatch("RefreshInfoOnWidget")]
  //[HarmonyPatch(MethodType.Normal)]
  //public static class InventoryDataObject_ShopFullMech_RefreshInfoOnWidget {
  //  public static void Postfix(InventoryDataObject_ShopFullMech __instance, InventoryItemElement theWidget) {
  //    try {
  //      Log.TWL(0, "InventoryDataObject_ShopFullMech.RefreshInfoOnWidget");
  //      if (__instance.mechDef != null) {
  //        theWidget.iconMech.AddGitSprite(__instance.mechDef.Description.Icon, false);
  //      }
  //    } catch (Exception e) {
  //      Log.TWL(0, e.ToString(), true);
  //    }
  //  }
  //}
  [HarmonyPatch(typeof(LanceMechSlot))]
  [HarmonyPatch("Init")]
  [HarmonyPatch(MethodType.Normal)]
  public static class LanceMechSlot_Init {
    public static void Postfix(LanceMechSlot __instance, MechDef mechDef, LanceConfigurator LC, int availableCBills, bool inSelectionList, bool isFavorite, OnMechSlotSelected mechCB = null) {
      try {
        Log.TWL(0, "LanceMechSlot.Init");
        if (__instance.curMech != null) {
          __instance.mechImage.AddGitSprite(__instance.curMech.Chassis.Description.Icon, false);
        }
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(LanceMechSlot))]
  [HarmonyPatch("SetRandomOverlay")]
  [HarmonyPatch(MethodType.Normal)]
  public static class LanceMechSlot_SetRandomOverlay {
    public static void Postfix(LanceMechSlot __instance, bool isRandom) {
      try {
        Log.TWL(0, "LanceMechSlot.SetRandomOverlay");
        if (isRandom == false) { return; }
        if (__instance.mechImage != null) {
          GifSpriteAnimator gifSpriteAnimator = __instance.mechImage.gameObject.GetComponent<GifSpriteAnimator>();
          if (gifSpriteAnimator == null) {
            gifSpriteAnimator = __instance.mechImage.gameObject.AddComponent<GifSpriteAnimator>();
            gifSpriteAnimator.portrait = __instance.mechImage;
          }
          gifSpriteAnimator.gif = null;
          gifSpriteAnimator.Reset();
          Log.WL(1, "gifSpriteAnimator.gif: " + (gifSpriteAnimator.gif == null ? "null" : "not null"));
        }
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }
  //[HarmonyPatch(typeof(ListElementController_SalvageFullMech_NotListView))]
  //[HarmonyPatch("RefreshInfoOnWidget")]
  //[HarmonyPatch(MethodType.Normal)]
  //public static class ListElementController_SalvageFullMech_NotListView_RefreshInfoOnWidget {
  //  public static void Postfix(ListElementController_SalvageFullMech_NotListView __instance, InventoryItemElement theWidget) {
  //    try {
  //      Log.TWL(0, "ListElementController_SalvageFullMech_NotListView.RefreshInfoOnWidget");
  //      if (__instance.mechDef != null) {
  //        theWidget.iconMech.AddGitSprite(__instance.mechDef.Description.Icon, false);
  //      }
  //    } catch (Exception e) {
  //      Log.TWL(0, e.ToString(), true);
  //    }
  //  }
  //}
  [HarmonyPatch(typeof(MechBayMechUnitElement))]
  [HarmonyPatch("SetData")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(IMechLabDropTarget), typeof(DataManager), typeof(int), typeof(MechDef), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
  public static class MechBayMechUnitElement_SetData_Gif {
    public static void Postfix(MechBayMechUnitElement __instance, IMechLabDropTarget dropParent, DataManager dataManager, int baySlot, MechDef mechDef, bool inMaintenance, bool isFieldable, bool hasFieldableWarnings, bool allowInteraction, bool blockRaycast, bool buttonEnabled) {
      try {
        Log.TWL(0, "MechBayMechUnitElement.SetData");
        if(__instance.chassisDef != null) {
          __instance.mechImage.AddGitSpriteHover(__instance.MechTooltip.gameObject, __instance.chassisDef.Description.Icon, false);
        }
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(MechBayMechUnitElement))]
  [HarmonyPatch("SetData")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(IMechLabDropTarget), typeof(DataManager), typeof(int), typeof(ChassisDef), typeof(bool), typeof(bool) })]
  public static class MechBayMechUnitElement_SetData_Gif2 {
    public static void Postfix(MechBayMechUnitElement __instance, IMechLabDropTarget dropParent, DataManager dataManager, int baySlot, ChassisDef chassisDef, bool inMaintenance, bool allowDrag) {
      try {
        Log.TWL(0, "MechBayMechUnitElement.SetData");
        if (__instance.chassisDef != null) {
          __instance.mechImage.AddGitSpriteHover(__instance.MechTooltip.gameObject, __instance.chassisDef.Description.Icon, false);
        }
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(MechUnitElementWidget))]
  [HarmonyPatch("SetIcon")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(string) })]
  public static class MechUnitElementWidget_SetIcon {
    public static void Postfix(MechUnitElementWidget __instance, string icon) {
      try {
        Log.TWL(0, "MechUnitElementWidget.SetIcon");
        __instance.mechIcon.AddGitSpriteHover(__instance.MechTooltip?.gameObject, icon, false);
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(LanceLoadoutMechItem))]
  [HarmonyPatch("SetData")]
  [HarmonyPatch(MethodType.Normal)]
  public static class LanceLoadoutMechItem_SetData {
    public static void Postfix(LanceLoadoutMechItem __instance) {
      try {
        Log.TWL(0, "LanceLoadoutMechItem.SetData");
        GifSpriteAnimator gifSpriteAnimator = __instance.mechElement.gameObject.GetComponentInChildren<GifSpriteAnimator>(true);
        if((gifSpriteAnimator != null) && (__instance.MechTooltip != null)) {
          GifSpriteAnimatorHover hover = __instance.MechTooltip.gameObject.GetComponent<GifSpriteAnimatorHover>();
          if (hover == null) { hover = __instance.MechTooltip.gameObject.AddComponent<GifSpriteAnimatorHover>(); }
          hover.imageAnimator = gifSpriteAnimator;
        }
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(TooltipPrefab_Chassis))]
  [HarmonyPatch("SetData")]
  [HarmonyPatch(MethodType.Normal)]
  public static class TooltipPrefab_Chassis_SetData {
    public static void Postfix(TooltipPrefab_Chassis __instance, object data) {
      try {
        Log.TWL(0, "TooltipPrefab_Chassis.SetData");
        if (data is ChassisDef chassisDef) {
          __instance.PNGImage.AddGitSprite(chassisDef.Description.Icon, true);
        }
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(MechDef))]
  [HarmonyPatch("DependenciesLoaded")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(uint) })]
  public static class MechDef_DependenciesLoaded {
    public static void Postfix(MechDef __instance, uint loadWeight, ref bool __result) {
      try {
        Log.TWL(0, "MechDef.DependenciesLoaded " + __instance.Description.Id);
        if (__result == false) { return; }
        if (__instance.dataManager == null) { return; }
        if (string.IsNullOrEmpty(__instance.Description.Icon)) { return; }
        string animated_icon = __instance.Description.Icon + Core.ANIMATED_ICON_SUFFIX;
        if (__instance.dataManager.ResourceLocator.EntryByID(animated_icon, BattleTechResourceType.Sprite) == null) { return; }
        if (GifStorageHelper.GetSprites(animated_icon) != null) { return; }
        __result = false;
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(MechDef))]
  [HarmonyPatch("GatherDependencies")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(DataManager), typeof(DataManager.DependencyLoadRequest), typeof(uint) })]
  public static class MechDef_GatherDependencies {
    public static void Postfix(MechDef __instance, DataManager dataManager, DataManager.DependencyLoadRequest dependencyLoad, uint activeRequestWeight) {
      try {
        Log.TWL(0, "MechDef.GatherDependencies " + __instance.Description.Id);
        if (string.IsNullOrEmpty(__instance.Description.Icon)) { return; }
        string animated_icon = __instance.Description.Icon + Core.ANIMATED_ICON_SUFFIX;
        if (dataManager.ResourceLocator.EntryByID(animated_icon, BattleTechResourceType.Sprite) == null) { return; }
        if (GifStorageHelper.GetSprites(animated_icon) != null) { return; }
        dependencyLoad.RequestResource(BattleTechResourceType.Sprite, animated_icon);
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(ChassisDef))]
  [HarmonyPatch("DependenciesLoaded")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(uint) })]
  public static class ChassisDef_DependenciesLoaded {
    public static void Postfix(ChassisDef __instance, uint loadWeight, ref bool __result) {
      try {
        Log.TWL(0, "MechDef.DependenciesLoaded " + __instance.Description.Id);
        if (__result == false) { return; }
        if (__instance.dataManager == null) { return; }
        if (string.IsNullOrEmpty(__instance.Description.Icon)) { return; }
        string animated_icon = __instance.Description.Icon + Core.ANIMATED_ICON_SUFFIX;
        if (__instance.dataManager.ResourceLocator.EntryByID(animated_icon, BattleTechResourceType.Sprite) == null) { return; }
        if (GifStorageHelper.GetSprites(animated_icon) != null) { return; }
        __result = false;
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(ChassisDef))]
  [HarmonyPatch("GatherDependencies")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(DataManager), typeof(DataManager.DependencyLoadRequest), typeof(uint) })]
  public static class ChassisDef_GatherDependencies {
    public static void Postfix(ChassisDef __instance, DataManager dataManager, DataManager.DependencyLoadRequest dependencyLoad, uint activeRequestWeight) {
      try {
        Log.TWL(0, "MechDef.GatherDependencies " + __instance.Description.Id);
        if (string.IsNullOrEmpty(__instance.Description.Icon)) { return; }
        string animated_icon = __instance.Description.Icon + Core.ANIMATED_ICON_SUFFIX;
        if (dataManager.ResourceLocator.EntryByID(animated_icon, BattleTechResourceType.Sprite) == null) { return; }
        if (GifStorageHelper.GetSprites(animated_icon) != null) { return; }
        dependencyLoad.RequestResource(BattleTechResourceType.Sprite, animated_icon);
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }
}