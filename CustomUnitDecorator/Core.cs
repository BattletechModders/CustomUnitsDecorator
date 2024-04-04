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
using IRBTModUtils;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CustomUnitDecorator {
  public class SlotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public GifSpriteAnimator animator = null;
    public void OnPointerEnter(PointerEventData eventData) {
      if (animator != null) { animator.OnPointerEnter(eventData); }
    }
    public void OnPointerExit(PointerEventData eventData) {
      if (animator != null) { animator.OnPointerExit(eventData); }
    }
  }
  //public class GifSpriteAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
  //  public UniGif.GifSprites gif { get; set; }
  //  private float t = 0f;
  //  public Sprite originalSprite { get; set; } = null;
  //  private Image f_portrait = null;
  //  public Image portrait { get { return f_portrait; } set { f_portrait = value; originalSprite = f_portrait.sprite; } }
  //  public bool hovered = false;
  //  private int index = 0;
  //  public void Reset() {
  //    index = 0;
  //    t = 0f;
  //  }
  //  public void LateUpdate() {
  //    if (portrait == null) { return; }
  //    if (gif == null) { return; }
  //    if (gif.frames.Count == 0) { return; }
  //    if (hovered == false) { return; }
  //    t += Time.deltaTime;
  //    if (t > gif.frames[index].m_delaySec) {
  //      this.index = (this.index + 1) % gif.frames.Count;
  //      t = 0f;
  //      portrait.sprite = gif.frames[this.index].m_sprite;
  //    }
  //  }
  //  public void OnPointerEnter(PointerEventData eventData) {
  //    Log.TWL(0, "GifSpriteAnimator.OnPointerEnter");
  //    hovered = true;
  //  }

  //  public void OnPointerExit(PointerEventData eventData) {
  //    Log.TWL(0, "GifSpriteAnimator.OnPointerExit");
  //    this.hovered = false;
  //    if (portrait != null) {
  //      portrait.sprite = this.originalSprite;
  //    }
  //  }
  //}
  //public class GifImageAnimator : MonoBehaviour {
  //  public UniGif.GifImage gif { get; set; }
  //  private float t = 0f;
  //  public RawImage portrait { get; set; } = null;
  //  private int index = 0;
  //  public void Reset() {
  //    index = 0;
  //    t = 0f;
  //  }
  //  public void LateUpdate() {
  //    if (portrait == null) { return; }
  //    if (gif == null) { return; }
  //    if (gif.frames.Count == 0) { return; }
  //    t += Time.deltaTime;
  //    if (t > gif.frames[index].m_delaySec) {
  //      this.index = (this.index + 1) % gif.frames.Count;
  //      t = 0f;
  //      portrait.texture = gif.frames[this.index].m_texture2d;
  //    }
  //  }
  //}
  public class CUDSettings {
    public bool debugLog { get; set; } = false;
    public float iconsXOffset { get; set; } = 3f;
    public string HoverTestImage { get; set; } = "readme.info.dat";
  }
  public static class Core {
    public static UniGif.GifSprites HOVER_SPRITE = null;
    public static string ANIMATED_ICON_SUFFIX = "_animated";
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
        string hover_gif = Path.Combine(directory, Core.Settings.HoverTestImage);
        if (File.Exists(hover_gif)) {
          Core.HOVER_SPRITE = new UniGif.GifSprites(UniGif.GetTexturesList(File.ReadAllBytes(Path.Combine(directory, Core.Settings.HoverTestImage))));
        }
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }
}
