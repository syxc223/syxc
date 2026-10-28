using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// British-detective (Victorian) UI theme.
/// 1) Processes generated art into sliced sprites under Assets/Art/UI.
/// 2) Creates SpecialElite + NotoSerifSC TMP font assets.
/// 3) Restyles MainMenuCanvas / GameHUDCanvas / RoomItemPrefab.
/// </summary>
public static class DetectiveThemeTool
{
    const string ArtDir = "Assets/Art/UI";
    const string FontDir = "Assets/Art/Fonts";

    const string SrcMenuBg = "Assets/Art/UI/Detective_MenuBG_src.png";
    const string SrcPanel = "Assets/TJGenerators/History/Image_20260903_121457.png";
    const string SrcButton = "Assets/TJGenerators/History/Image_20260903_121457 1.png";
    const string SrcStrip = "Assets/TJGenerators/History/Image_20260903_121458.png";
    const string SrcSeal = "Assets/TJGenerators/History/Image_20260903_121458 1.png";

    const string DstMenuBg = ArtDir + "/Detective_MenuBG.png";
    const string DstPanel = ArtDir + "/Detective_Panel.png";
    const string DstButton = ArtDir + "/Detective_Button.png";
    const string DstStrip = ArtDir + "/Detective_PaperStrip.png";
    const string DstSeal = ArtDir + "/Detective_WaxSeal.png";

    const string SpecialEliteSdf = FontDir + "/SpecialElite SDF.asset";
    const string NotoSerifSdf = FontDir + "/NotoSerifSC SDF.asset";
    const string NotoSansSdf = "Assets/Codely/Fonts/NotoSansSC-Regular SDF.asset";

    // ---- palette ----
    static readonly Color Ink = new Color(0.23f, 0.16f, 0.09f);            // dark ink on parchment
    static readonly Color Parchment = new Color(0.93f, 0.86f, 0.71f);      // parchment text on dark
    static readonly Color ParchmentGold = new Color(0.95f, 0.85f, 0.65f); // title gold
    static readonly Color Sepia = new Color(0.62f, 0.52f, 0.35f);         // secondary
    static readonly Color Brass = new Color(0.69f, 0.55f, 0.24f);         // slider fill / accents
    static readonly Color BrassLight = new Color(0.88f, 0.75f, 0.47f);    // slider handle
    static readonly Color Walnut = new Color(0.16f, 0.125f, 0.08f);       // inset fields
    static readonly Color WalnutDeep = new Color(0.13f, 0.10f, 0.075f);
    static readonly Color Scrim = new Color(0.07f, 0.055f, 0.04f, 0.55f);
    static readonly Color ErrorRed = new Color(0.80f, 0.33f, 0.22f);

    static Sprite _panel;
    static Sprite _button;
    static Sprite _strip;
    static Sprite _seal;
    static Sprite _menuBg;
    static TMP_FontAsset _display;
    static TMP_FontAsset _body;

    [MenuItem("Tools/\u6BD5\u8BBE/\u82F1\u4F26\u4FA6\u63A2UI/\u4E00\u952E\u5E94\u7528")]
    public static void RunAll()
    {
        BackupScene();
        ProcessArt();
        EnsureFonts();
        ApplyToScene();
        ApplyToRoomItemPrefab();
        AssetDatabase.SaveAssets();
        Debug.Log("[DetectiveTheme] Full theme applied.");
    }

    [MenuItem("Tools/\u6BD5\u8BBE/\u82F1\u4F26\u4FA6\u63A2UI/\u4EC5\u91CD\u5BFC\u7F8E\u672F\u7D20\u6750")]
    public static void MenuProcessArt() { ProcessArt(); }

    [MenuItem("Tools/\u6BD5\u8BBE/\u82F1\u4F26\u4FA6\u63A2UI/\u4EC5\u521B\u5EFA\u5B57\u4F53")]
    public static void MenuEnsureFonts() { EnsureFonts(); }

    [MenuItem("Tools/\u6BD5\u8BBE/\u82F1\u4F26\u4FA6\u63A2UI/\u4EC5\u5E94\u7528\u573A\u666F\u6837\u5F0F")]
    public static void MenuApplyScene()
    {
        LoadAssets();
        ApplyToScene();
        ApplyToRoomItemPrefab();
        AssetDatabase.SaveAssets();
    }

    static void BackupScene()
    {
        try
        {
            var src = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Assets/Scenes/SampleScene.unity");
            var dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Backups");
            Directory.CreateDirectory(dir);
            var dst = Path.Combine(dir, "SampleScene_before_detective_ui.unity");
            if (!File.Exists(dst)) File.Copy(src, dst);
            Debug.Log($"[DetectiveTheme] Scene backup: {dst}");
        }
        catch (Exception e) { Debug.LogWarning($"[DetectiveTheme] Backup failed: {e.Message}"); }
    }

    // ============================================================
    // 1) ART PROCESSING
    // ============================================================

    enum ArtKind { Background, Panel, Button, Strip, Seal }

    static void ProcessArt()
    {
        Directory.CreateDirectory(ArtDir);
        _written.Clear();
        WriteArt(SrcMenuBg, DstMenuBg, ArtKind.Background);
        WriteArt(SrcPanel, DstPanel, ArtKind.Panel);
        WriteArt(SrcButton, DstButton, ArtKind.Button);
        WriteArt(SrcStrip, DstStrip, ArtKind.Strip);
        WriteArt(SrcSeal, DstSeal, ArtKind.Seal);
        AssetDatabase.Refresh();
        foreach (var (rel, kind) in _written)
            ConfigureArt(rel, kind);
        Debug.Log("[DetectiveTheme] Art processed into " + ArtDir);
    }

    static readonly List<(string rel, ArtKind kind)> _written = new List<(string, ArtKind)>();

    /// <summary>The generated art uses a flat chroma-green backdrop (71,112,76).</summary>
    static bool IsChroma(Color32 c)
    {
        int dr = Mathf.Abs(c.r - 71), dg = Mathf.Abs(c.g - 112), db = Mathf.Abs(c.b - 76);
        return (dr + dg + db) < 36;
    }

    /// <summary>Key chroma out: defringe to dark brown, optionally set alpha 0.</summary>
    static Color32[] KeyChroma(Color32[] px, bool transparent)
    {
        var res = new Color32[px.Length];
        for (int i = 0; i < px.Length; i++)
        {
            if (IsChroma(px[i]))
                res[i] = new Color32(43, 28, 15, transparent ? (byte)0 : (byte)255);
            else
                res[i] = px[i];
        }
        return res;
    }

    /// <summary>Bounding box of non-chroma pixels, inset by pad.</summary>
    static RectInt ContentBounds(Color32[] px, int w, int h, int pad)
    {
        int minX = w, minY = h, maxX = -1, maxY = -1;
        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                if (IsChroma(px[row + x])) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }
        if (maxX < minX) return new RectInt(0, 0, w, h);
        minX = Mathf.Max(0, minX + pad); minY = Mathf.Max(0, minY + pad);
        maxX = Mathf.Min(w - 1, maxX - pad); maxY = Mathf.Min(h - 1, maxY - pad);
        if (maxX <= minX || maxY <= minY) return new RectInt(0, 0, w, h);
        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    static void WriteArt(string src, string dst, ArtKind kind)
    {
        string absSrc = Path.GetFullPath(src);
        if (!File.Exists(absSrc)) { Debug.LogError($"[DetectiveTheme] Missing source: {src}"); return; }

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        if (!tex.LoadImage(File.ReadAllBytes(absSrc))) { Debug.LogError($"[DetectiveTheme] Failed to load {src}"); return; }

        string absDst = Path.GetFullPath(dst);
        int w = tex.width, h = tex.height;
        var px = tex.GetPixels32();
        UnityEngine.Object.DestroyImmediate(tex);

        switch (kind)
        {
            case ArtKind.Background:
            {
                // Replace chroma with dark fog color, then crop to artwork bounds.
                var keyed = KeyChroma(px, false);
                var r = ContentBounds(px, w, h, 14);
                WriteRegionRaw(keyed, w, h, r, r.width, r.height, absDst);
                break;
            }
            case ArtKind.Panel:
            {
                var keyed = KeyChroma(px, true);
                var r = ContentBounds(px, w, h, 2);
                WriteRegionRaw(keyed, w, h, r, r.width, r.height, absDst);
                break;
            }
            case ArtKind.Button:
            {
                var keyed = KeyChroma(px, true);
                var r = ContentBounds(px, w, h, 2);
                WriteRegionRaw(keyed, w, h, r, 384, 96, absDst);
                break;
            }
            case ArtKind.Strip:
            {
                var keyed = KeyChroma(px, true);
                var r = ContentBounds(px, w, h, 2);
                WriteRegionRaw(keyed, w, h, r, 1024, 200, absDst);
                break;
            }
            case ArtKind.Seal:
            {
                var keyed = KeyChroma(px, true);
                var r = ContentBounds(px, w, h, 2);
                WriteRegionRaw(keyed, w, h, r, r.width, r.height, absDst);
                break;
            }
        }

        string rel = "Assets" + absDst.Substring(Application.dataPath.Length).Replace('\\', '/');
        _written.Add((rel, kind));
        Debug.Log($"[DetectiveTheme] wrote {kind} -> {rel}");
    }

    /// <summary>Bilinear resample of a region (with premade pixel array) into dstW x dstH PNG.</summary>
    static void WriteRegionRaw(Color32[] spx, int sw, int sh, RectInt region, int dstW, int dstH, string absDst)
    {
        var dst = new Texture2D(dstW, dstH, TextureFormat.RGBA32, false, true);
        var dpx = new Color32[dstW * dstH];

        for (int dy = 0; dy < dstH; dy++)
        {
            for (int dx = 0; dx < dstW; dx++)
            {
                float u = (dx + 0.5f) / dstW;
                float v = (dy + 0.5f) / dstH;
                float sxf = region.x + u * region.width - 0.5f;
                float syf = region.y + v * region.height - 0.5f;
                int x0 = Mathf.Clamp((int)sxf, region.xMin, region.xMax - 1);
                int y0 = Mathf.Clamp((int)syf, region.yMin, region.yMax - 1);
                int x1 = Mathf.Min(x0 + 1, region.xMax - 1);
                int y1 = Mathf.Min(y0 + 1, region.yMax - 1);
                float fx = Mathf.Clamp01(sxf - x0);
                float fy = Mathf.Clamp01(syf - y0);

                var c00 = spx[y0 * sw + x0]; var c10 = spx[y0 * sw + x1];
                var c01 = spx[y1 * sw + x0]; var c11 = spx[y1 * sw + x1];

                float r = Lerp(Lerp(c00.r, c10.r, fx), Lerp(c01.r, c11.r, fx), fy);
                float g = Lerp(Lerp(c00.g, c10.g, fx), Lerp(c01.g, c11.g, fx), fy);
                float b = Lerp(Lerp(c00.b, c10.b, fx), Lerp(c01.b, c11.b, fx), fy);
                float a = Lerp(Lerp(c00.a, c10.a, fx), Lerp(c01.a, c11.a, fx), fy);
                dpx[dy * dstW + dx] = new Color32((byte)r, (byte)g, (byte)b, (byte)a);
            }
        }
        dst.SetPixels32(dpx);
        File.WriteAllBytes(absDst, dst.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(dst);
    }

    static float Lerp(float a, float b, float t) => a + (b - a) * t;

    static void ConfigureArt(string rel, ArtKind kind)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(rel);
        if (imp == null) { Debug.LogError($"[DetectiveTheme] Importer missing: {rel}"); return; }
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.mipmapEnabled = false;
        imp.filterMode = FilterMode.Bilinear;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.alphaIsTransparency = true;

        if (kind == ArtKind.Background)
        {
            imp.textureType = TextureImporterType.Sprite; // rendered via Image (RawImage proved unreliable here)
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.wrapMode = TextureWrapMode.Clamp;
        }
        else
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            Vector4 border = Vector4.zero;
            if (kind == ArtKind.Panel) border = new Vector4(100, 100, 100, 100);
            else if (kind == ArtKind.Button) border = new Vector4(26, 12, 26, 12);
            else if (kind == ArtKind.Strip) border = new Vector4(90, 26, 90, 26);
            imp.spriteBorder = border;
        }
        imp.SaveAndReimport();
        Debug.Log($"[DetectiveTheme] configured {kind}: {rel}");
    }

    // ============================================================
    // 2) FONTS
    // ============================================================

    static void EnsureFonts()
    {
        Directory.CreateDirectory(FontDir);
        var display = CreateFontAsset(FontDir + "/SpecialElite-Regular.ttf", SpecialEliteSdf, "SpecialElite SDF");
        var body = CreateFontAsset(FontDir + "/NotoSerifSC-Regular.otf", NotoSerifSdf, "NotoSerifSC SDF");

        if (display != null && body != null && (display.fallbackFontAssetTable == null || !display.fallbackFontAssetTable.Contains(body)))
        {
            display.fallbackFontAssetTable = new List<TMP_FontAsset> { body };
            EditorUtility.SetDirty(display);
        }

        var notoSans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NotoSansSdf);
        if (body != null && notoSans != null && (body.fallbackFontAssetTable == null || !body.fallbackFontAssetTable.Contains(notoSans)))
        {
            body.fallbackFontAssetTable = new List<TMP_FontAsset> { notoSans };
            EditorUtility.SetDirty(body);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[DetectiveTheme] Fonts ready (SpecialElite SDF + NotoSerifSC SDF).");
    }

    static TMP_FontAsset CreateFontAsset(string fontPath, string assetPath, string name)
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) return existing;

        AssetDatabase.ImportAsset(fontPath, ImportAssetOptions.ForceSynchronousImport);
        var source = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (source == null) { Debug.LogError($"[DetectiveTheme] Font file missing: {fontPath}"); return null; }

        var fa = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        if (fa == null) { Debug.LogError($"[DetectiveTheme] CreateFontAsset failed for {fontPath}"); return null; }

        fa.name = name;
        fa.isMultiAtlasTexturesEnabled = true;
        AssetDatabase.CreateAsset(fa, assetPath);
        if (fa.material != null && !EditorUtility.IsPersistent(fa.material))
        {
            fa.material.name = name + " Atlas Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
        }
        foreach (var atlas in fa.atlasTextures ?? Array.Empty<Texture2D>())
        {
            if (atlas == null || EditorUtility.IsPersistent(atlas)) continue;
            atlas.name = name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlas, fa);
        }
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    // ============================================================
    // 3) SCENE APPLICATION
    // ============================================================

    static void LoadAssets()
    {
        _panel = AssetDatabase.LoadAssetAtPath<Sprite>(DstPanel);
        _button = AssetDatabase.LoadAssetAtPath<Sprite>(DstButton);
        _strip = AssetDatabase.LoadAssetAtPath<Sprite>(DstStrip);
        _seal = AssetDatabase.LoadAssetAtPath<Sprite>(DstSeal);
        _menuBg = AssetDatabase.LoadAssetAtPath<Sprite>(DstMenuBg);
        _display = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SpecialEliteSdf);
        _body = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NotoSerifSdf);
    }

    static void ApplyToScene()
    {
        LoadAssets();
        GameObject menu = null, hud = null;
        foreach (var root in UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == "MainMenuCanvas") menu = root;
            if (root.name == "GameHUDCanvas") hud = root;
        }
        if (menu == null || hud == null) { Debug.LogError("[DetectiveTheme] Canvases not found"); return; }

        StyleMenuCanvas(menu.transform);
        StyleHudCanvas(hud.transform);

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[DetectiveTheme] Scene styled & saved.");
    }

    // ---------- helpers ----------

    static Transform FT(Transform root, string path)
    {
        var t = root.Find(path);
        if (t == null) Debug.LogWarning($"[DetectiveTheme] Missing: {path}");
        return t;
    }

    static Image EnsureImage(GameObject go)
    {
        var img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        return img;
    }

    static void SetRect(Transform t, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = (RectTransform)t;
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    static void SetButton(Transform t, float? width = null, float? height = null)
    {
        var btn = t.GetComponent<Button>();
        if (btn == null) { Debug.LogWarning($"[DetectiveTheme] No Button on {t.name}"); return; }
        var img = t.GetComponent<Image>();
        if (img == null) img = t.gameObject.AddComponent<Image>();
        img.sprite = _button; img.type = Image.Type.Sliced; img.color = Color.white;
        if (btn.targetGraphic == null) btn.targetGraphic = img;

        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.12f, 1.05f, 0.86f);
        cb.pressedColor = new Color(0.82f, 0.72f, 0.55f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.55f, 0.50f, 0.42f, 0.6f);
        cb.fadeDuration = 0.12f;
        btn.colors = cb;

        var label = t.GetComponentInChildren<TMP_Text>(true);
        if (label != null && _display != null)
        {
            label.font = _display;
            label.color = Ink;
            label.fontSize = Mathf.Max(label.fontSize, 20f);
        }
        if (width.HasValue || height.HasValue)
        {
            var rt = (RectTransform)t;
            rt.sizeDelta = new Vector2(width ?? rt.sizeDelta.x, height ?? rt.sizeDelta.y);
        }
    }

    static void SetInput(Transform t, float? width = null, float? height = null)
    {
        var f = t.GetComponent<TMP_InputField>();
        if (f == null) { Debug.LogWarning($"[DetectiveTheme] No InputField on {t.name}"); return; }
        var img = t.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = null; img.type = Image.Type.Simple;
            img.color = new Color(Walnut.r, Walnut.g, Walnut.b, 0.92f);
        }
        if (f.textComponent != null && _body != null) { f.textComponent.font = _body; f.textComponent.color = Parchment; }
        if (f.placeholder is TMP_Text ph && _body != null) { ph.font = _body; ph.color = new Color(0.68f, 0.58f, 0.44f, 0.75f); }
        f.caretColor = new Color(0.85f, 0.70f, 0.35f);
        f.selectionColor = new Color(0.69f, 0.55f, 0.24f, 0.35f);
        if (width.HasValue || height.HasValue)
        {
            var rt = (RectTransform)t;
            rt.sizeDelta = new Vector2(width ?? rt.sizeDelta.x, height ?? rt.sizeDelta.y);
        }
    }

    static void SetText(Transform t, TMP_FontAsset font, Color color, float size, TextAlignmentOptions? align = null)
    {
        var tmp = t.GetComponent<TMP_Text>();
        if (tmp == null) { Debug.LogWarning($"[DetectiveTheme] No TMP on {t.name}"); return; }
        // Remove stale fallback submeshes; TMP recreates them on next rebuild.
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            var ch = t.GetChild(i);
            if (ch.name.StartsWith("TMP SubMesh")) UnityEngine.Object.DestroyImmediate(ch.gameObject);
        }
        if (font != null) tmp.font = font;
        tmp.color = color; tmp.fontSize = size;
        tmp.margin = Vector4.zero;
        if (align.HasValue) tmp.alignment = align.Value;
        tmp.SetAllDirty();
    }

    /// <summary>Theme a standard uGUI slider subtree (Background / Fill / Handle).</summary>
    static void StyleSliderTrack(Transform slider, Color trackColor)
    {
        if (slider == null) return;
        var rootImg = slider.GetComponent<Image>();
        if (rootImg != null) { rootImg.sprite = null; rootImg.color = trackColor; }
        var bg = slider.Find("Background");
        if (bg != null)
        {
            var bImg = bg.GetComponent<Image>();
            if (bImg != null) { bImg.sprite = null; bImg.color = new Color(trackColor.r, trackColor.g, trackColor.b, trackColor.a * 0.6f); }
        }
        var fill = slider.Find("Fill Area/Fill");
        if (fill != null)
        {
            var fImg = fill.GetComponent<Image>();
            if (fImg != null) { fImg.sprite = null; fImg.color = Brass; }
        }
        var handle = slider.Find("Handle Slide Area/Handle");
        if (handle != null)
        {
            var hImg = handle.GetComponent<Image>();
            if (hImg != null) { hImg.sprite = null; hImg.color = BrassLight; }
        }
    }

    /// <summary>Wire a TMP_InputField (on t) with TextArea + Placeholder + Text children.</summary>
    static void WireSimpleInputField(TMP_InputField field, string placeholderText)
    {
        var go = field.gameObject;
        var ta = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        ta.transform.SetParent(go.transform, false);
        var taRt = (RectTransform)ta.transform;
        taRt.anchorMin = Vector2.zero; taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(6, 0); taRt.offsetMax = new Vector2(-6, 0);

        var ph = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        ph.transform.SetParent(ta.transform, false);
        var phRt = (RectTransform)ph.transform;
        phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
        phRt.offsetMin = Vector2.zero; phRt.offsetMax = Vector2.zero;
        var phTmp = ph.GetComponent<TextMeshProUGUI>();
        phTmp.text = placeholderText;
        phTmp.fontSize = 16;
        phTmp.fontStyle = FontStyles.Italic;
        phTmp.color = new Color(0.68f, 0.58f, 0.44f, 0.75f);
        phTmp.alignment = TextAlignmentOptions.MidlineLeft;
        phTmp.raycastTarget = false;

        var txt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txt.transform.SetParent(ta.transform, false);
        var tRt = (RectTransform)txt.transform;
        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
        var tTmp = txt.GetComponent<TextMeshProUGUI>();
        tTmp.fontSize = 16;
        tTmp.color = Parchment;
        tTmp.alignment = TextAlignmentOptions.MidlineLeft;
        tTmp.raycastTarget = false;

        field.textViewport = taRt;
        field.textComponent = tTmp;
        field.placeholder = phTmp;
        field.caretColor = new Color(0.85f, 0.70f, 0.35f);
        field.selectionColor = new Color(0.69f, 0.55f, 0.24f, 0.35f);
    }

    /// <summary>Build a minimal single-line TMP_InputField (bg + TextArea + Placeholder + Text).</summary>
    static TMP_InputField BuildSimpleInputField(Transform parent, string placeholderText)
    {
        var go = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0.15f, 0.115f, 0.075f, 0.92f);

        var field = go.GetComponent<TMP_InputField>();
        field.lineType = TMP_InputField.LineType.SingleLine;
        WireSimpleInputField(field, placeholderText);
        return field;
    }

    static Transform AddChildPanel(Transform parent, string name, Vector2 size, Vector2 pos)
    {
        var existing = parent.Find(name);
        if (existing != null)
        {
            SetRect(existing, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            return existing;
        }
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        SetRect(go.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        var img = go.GetComponent<Image>();
        img.sprite = _panel; img.type = Image.Type.Sliced; img.color = Color.white;
        go.transform.SetAsFirstSibling();
        return go.transform;
    }

    // ---------- main menu ----------

    static void StyleMenuCanvas(Transform menu)
    {
        // Background (Image + sprite; RawImage proved unreliable in this canvas)
        var bg = menu.Find("Background");
        if (bg != null && _menuBg != null)
        {
            var raw = bg.GetComponent<RawImage>();
            if (raw != null) UnityEngine.Object.DestroyImmediate(raw);
            var img = bg.GetComponent<Image>();
            if (img == null) img = bg.gameObject.AddComponent<Image>();
            img.sprite = _menuBg;
            img.color = Color.white;
            img.raycastTarget = false;
        }

        // Title & subtitle (kept above panels via sibling order)
        var title = FT(menu, "Title");
        if (title != null)
        {
            title.GetComponent<TMP_Text>().text = "\u96FE\u90FD\u7591\u6848"; // 雾都疑案
            SetText(title, _display, ParchmentGold, 110, TextAlignmentOptions.Center);
            SetRect(title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 385), new Vector2(1300, 160));
            title.SetAsLastSibling();
        }
        var subtitle = FT(menu, "Subtitle");
        if (subtitle != null)
        {
            SetText(subtitle, _display, new Color(0.72f, 0.61f, 0.44f, 0.95f), 26, TextAlignmentOptions.Center);
            subtitle.GetComponent<TMP_Text>().text = "A VICTORIAN CO-OP DETECTIVE TALE  \u00B7  DEMO";
            SetRect(subtitle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 285), new Vector2(1000, 36));
            subtitle.SetAsLastSibling();
        }

        // ----- Login panel -----
        var login = FT(menu, "LoginPanel");
        if (login != null)
        {
            var rootImg = EnsureImage(login.gameObject);
            rootImg.sprite = null; rootImg.color = Scrim;

            AddChildPanel(login, "PanelBG", new Vector2(640, 660), new Vector2(0, -90));

            var loginTitle = FT(login, "LoginTitle");
            if (loginTitle != null)
            {
                loginTitle.GetComponent<TMP_Text>().text = "\u4FA6\u63A2\u7B7E\u5230"; // 侦探签到
                SetText(loginTitle, _display, Ink, 42, TextAlignmentOptions.Center);
                SetRect(loginTitle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 95), new Vector2(420, 56));
            }

            var userInput = FT(login, "UsernameInput");
            if (userInput != null) { SetRect(userInput, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 35), new Vector2(360, 46)); SetInput(userInput); }
            var passInput = FT(login, "PasswordInput");
            if (passInput != null) { SetRect(passInput, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -25), new Vector2(360, 46)); SetInput(passInput); }

            var msg = FT(login, "MessageText");
            if (msg != null) { SetText(msg, _body, ErrorRed, 18, TextAlignmentOptions.Center); SetRect(msg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(440, 26)); }

            var row = FT(login, "ButtonRow");
            if (row != null)
            {
                SetRect(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -125), new Vector2(400, 56));
                var lb = row.Find("LoginButton");
                if (lb != null) { SetRect(lb, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-105, 0), new Vector2(190, 48)); SetButton(lb); }
                var rb = row.Find("RegisterButton");
                if (rb != null) { SetRect(rb, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(105, 0), new Vector2(190, 48)); SetButton(rb); }
            }

            var quit = FT(login, "QuitButton");
            if (quit != null) { SetRect(quit, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -195), new Vector2(180, 44)); SetButton(quit); }

            var ip = FT(login, "IPDisplay");
            if (ip != null) SetText(ip, _body, new Color(0.75f, 0.66f, 0.50f, 0.95f), 22, TextAlignmentOptions.Center);

            // Register panel: own scrim + own parchment (hides login content behind it)
            var reg = FT(login, "RegisterPanel");
            if (reg != null)
            {
                var regImg = reg.GetComponent<Image>();
                if (regImg == null) regImg = reg.gameObject.AddComponent<Image>();
                regImg.enabled = true;
                regImg.sprite = null;
                regImg.color = new Color(0.07f, 0.055f, 0.04f, 0.98f);

                AddChildPanel(reg, "RegPanelBG", new Vector2(640, 660), new Vector2(0, -90));

                var regTitle = FT(reg, "RegTitle");
                if (regTitle != null)
                {
                    regTitle.GetComponent<TMP_Text>().text = "\u65B0\u4FA6\u63A2\u767B\u8BB0"; // 新侦探登记
                    SetText(regTitle, _display, Ink, 42, TextAlignmentOptions.Center);
                    SetRect(regTitle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 95), new Vector2(440, 56));
                }
                foreach (var (path, y) in new[] { ("RegUsernameInput", 30f), ("RegPasswordInput", -30f), ("RegConfirmInput", -90f) })
                {
                    var inp = FT(reg, path);
                    if (inp != null) { SetRect(inp, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(360, 46)); SetInput(inp); }
                }
                var regMsg = FT(reg, "RegMessageText");
                if (regMsg != null) { SetText(regMsg, _body, ErrorRed, 18, TextAlignmentOptions.Center); SetRect(regMsg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -135), new Vector2(440, 26)); }
                var confirmBtn = FT(reg, "ConfirmRegisterButton");
                if (confirmBtn != null) { SetRect(confirmBtn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -195), new Vector2(180, 48)); SetButton(confirmBtn); }
                var backBtn = FT(reg, "BackButton");
                if (backBtn != null) { SetRect(backBtn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -255), new Vector2(180, 48)); SetButton(backBtn); }
            }
        }

        // ----- Room panel -----
        var room = FT(menu, "RoomPanel");
        if (room != null)
        {
            var rootImg = EnsureImage(room.gameObject);
            rootImg.sprite = null; rootImg.color = Scrim;

            AddChildPanel(room, "PanelBG", new Vector2(1320, 760), new Vector2(0, -60));

            var welcome = FT(room, "WelcomeText");
            if (welcome != null)
            {
                SetText(welcome, _display, Ink, 26, TextAlignmentOptions.Left);
                var wTmp = welcome.GetComponent<TMP_Text>();
                wTmp.enableWordWrapping = false;
                wTmp.overflowMode = TextOverflowModes.Overflow;
                SetRect(welcome, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-190, 185), new Vector2(700, 40));
            }

            var logout = FT(room, "LogoutButton");
            if (logout != null) { SetRect(logout, new Vector2(1, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-700, -355), new Vector2(140, 40)); SetButton(logout); }
            var quitRoom = FT(room, "QuitButton");
            if (quitRoom != null) { SetRect(quitRoom, new Vector2(1, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-545, -355), new Vector2(140, 40)); SetButton(quitRoom); }

            var sv = FT(room, "RoomScrollView");
            if (sv != null)
            {
                SetRect(sv, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -95), new Vector2(1120, 470));
                var svImg = sv.GetComponent<Image>();
                if (svImg != null) { svImg.sprite = null; svImg.color = new Color(WalnutDeep.r, WalnutDeep.g, WalnutDeep.b, 0.85f); }
            }

            var bottomRow = FT(room, "BottomButtons");
            if (bottomRow != null) ((RectTransform)bottomRow).anchoredPosition = new Vector2(0, 190);
            var createBtn = FT(room, "BottomButtons/CreateRoomButton");
            if (createBtn != null) { SetRect(createBtn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 48.4f), new Vector2(210, 48)); SetButton(createBtn); }
            var refreshBtn = FT(room, "BottomButtons/RefreshButton");
            if (refreshBtn != null) { SetRect(refreshBtn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 116.8f), new Vector2(210, 48)); SetButton(refreshBtn); }

            // Create room dialog
            var dialog = FT(room, "CreateRoomDialog");
            if (dialog != null)
            {
                var dImg = EnsureImage(dialog.gameObject);
                dImg.sprite = null; dImg.color = Scrim;
                var panel = FT(dialog, "DialogPanel");
                if (panel != null)
                {
                    var pImg = EnsureImage(panel.gameObject);
                    pImg.sprite = _panel; pImg.type = Image.Type.Sliced; pImg.color = Color.white;
                    ((RectTransform)panel).sizeDelta = new Vector2(420, 280);
                    var dTitle = FT(panel, "DialogTitle");
                    if (dTitle != null) SetText(dTitle, _display, Ink, 38, TextAlignmentOptions.Center);
                    var nameInput = FT(panel, "RoomNameInput");
                    if (nameInput != null) SetInput(nameInput, 320, 44);
                    var okBtn = FT(panel, "ConfirmCreateButton");
                    if (okBtn != null) SetButton(okBtn, 140, 44);
                    var cancelBtn = FT(panel, "CancelCreateButton");
                    if (cancelBtn != null) SetButton(cancelBtn, 140, 44);
                }
            }

            // Direct connect bar (recreated if removed — RoomPanel.cs still references it)
            var dcb = room.Find("DirectConnectBar");
            if (dcb == null)
            {
                var go = new GameObject("DirectConnectBar", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(room, false);
                dcb = go.transform;
            }
            {
                var barImg = EnsureImage(dcb.gameObject);
                barImg.sprite = _strip; barImg.type = Image.Type.Sliced; barImg.color = new Color(0.96f, 0.94f, 0.90f);
                SetRect(dcb, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 35), new Vector2(400, 56));

                var dcInput = dcb.Find("DirectConnectInput");
                if (dcInput == null)
                {
                    var igo = new GameObject("DirectConnectInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
                    igo.transform.SetParent(dcb, false);
                    dcInput = igo.transform;
                    var newField = igo.GetComponent<TMP_InputField>();
                    newField.lineType = TMP_InputField.LineType.SingleLine;
                    WireSimpleInputField(newField, "\u8F93\u5165\u4E3B\u673AIP\u76F4\u8FDE..."); // 输入主机IP直连...
                }
                SetRect(dcInput, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10, 0), new Vector2(-90, 30));
                var dcInputImg = dcInput.GetComponent<Image>();
                if (dcInputImg != null) { dcInputImg.sprite = null; dcInputImg.color = new Color(0.15f, 0.115f, 0.075f, 0.92f); }
                var dcField = dcInput.GetComponent<TMP_InputField>();
                if (dcField != null)
                {
                    if (dcField.textComponent != null && _body != null) { dcField.textComponent.font = _body; dcField.textComponent.color = Parchment; }
                    if (dcField.placeholder is TMP_Text dcPh && _body != null) { dcPh.font = _body; dcPh.color = new Color(0.68f, 0.58f, 0.44f, 0.75f); }
                }

                var dcBtn = dcb.Find("DirectConnectButton");
                if (dcBtn == null)
                {
                    var bgo = new GameObject("DirectConnectButton", typeof(RectTransform), typeof(Image), typeof(Button));
                    bgo.transform.SetParent(dcb, false);
                    dcBtn = bgo.transform;
                    var lbl = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                    lbl.transform.SetParent(dcBtn, false);
                    var lrt = (RectTransform)lbl.transform;
                    lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                    lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
                    lbl.GetComponent<TMP_Text>().text = "\u76F4\u8FDE"; // 直连
                }
                SetRect(dcBtn, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-10, 0), new Vector2(80, 36));
                SetButton(dcBtn);
            }

            // Rewire RoomPanel serialized refs (they were nulled when the bar was deleted)
            var rpComp = room.GetComponent("RoomPanel");
            if (rpComp != null)
            {
                var so = new UnityEditor.SerializedObject(rpComp);
                var inProp = so.FindProperty("_directConnectInput");
                var btnProp = so.FindProperty("_directConnectButton");
                if (inProp != null && (inProp.objectReferenceValue == null || true))
                    inProp.objectReferenceValue = dcb.Find("DirectConnectInput")?.GetComponent<TMP_InputField>();
                if (btnProp != null)
                    btnProp.objectReferenceValue = dcb.Find("DirectConnectButton")?.GetComponent<Button>();
                so.ApplyModifiedProperties();
                UnityEditor.EditorUtility.SetDirty(rpComp);
            }
        }
    }

    // ---------- HUD ----------

    static void StyleHudCanvas(Transform hud)
    {
        // Top-left case file strip
        var hudPanel = hud.Find("HUDPanel");
        if (hudPanel == null)
        {
            var go = new GameObject("HUDPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(hud, false);
            go.transform.SetAsFirstSibling();
            hudPanel = go.transform;
        }
        {
            var img = EnsureImage(hudPanel.gameObject);
            img.sprite = _strip; img.type = Image.Type.Sliced; img.color = Color.white;
            SetRect(hudPanel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(390, -72), new Vector2(700, 124));
        }
        var puzzleName = FT(hud, "PuzzleName");
        if (puzzleName != null)
        {
            SetText(puzzleName, _display, Ink, 38, TextAlignmentOptions.MidlineLeft);
            SetRect(puzzleName, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(435, -44), new Vector2(560, 44));
            puzzleName.GetComponent<TMP_Text>().text = "Chamber 01";
        }
        var hint = FT(hud, "Hint");
        if (hint != null)
        {
            SetText(hint, _body, new Color(0.32f, 0.24f, 0.15f, 1f), 24, TextAlignmentOptions.MidlineLeft);
            hint.GetComponent<TMP_Text>().fontStyle = FontStyles.Italic;
            SetRect(hint, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(435, -92), new Vector2(560, 36));
            hint.GetComponent<TMP_Text>().text = "Cooperate to reach the exit!";
        }

        // Player badge (parchment strip behind the label)
        var playerLabel = FT(hud, "PlayerLabel");
        if (playerLabel != null)
        {
            var badge = hud.Find("PlayerBadge");
            if (badge == null)
            {
                var go = new GameObject("PlayerBadge", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(hud, false);
                badge = go.transform;
                badge.SetSiblingIndex(playerLabel.GetSiblingIndex());
            }
            var bImg = badge.GetComponent<Image>();
            bImg.sprite = _strip; bImg.type = Image.Type.Sliced; bImg.color = Color.white;
            SetRect(badge, new Vector2(1, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-150, -130), new Vector2(260, 96));

            SetText(playerLabel, _display, Ink, 30, TextAlignmentOptions.Center);
            SetRect(playerLabel, new Vector2(1, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-150, -130), new Vector2(240, 84));
            playerLabel.GetComponent<TMP_Text>().text = "P1";
        }

        var fps = hud.Find("FPSText") ?? hud.Find("FpsText");
        if (fps != null) SetText(fps, _body, new Color(0.62f, 0.53f, 0.40f, 0.8f), 18, TextAlignmentOptions.TopRight);

        // In-game volume panel (added by a later pass) — theme it to walnut & brass
        var vol = hud.Find("VolumePanel");
        if (vol != null)
        {
            var vImg = vol.GetComponent<Image>();
            if (vImg == null) vImg = vol.gameObject.AddComponent<Image>();
            vImg.sprite = null; vImg.color = new Color(WalnutDeep.r, WalnutDeep.g, WalnutDeep.b, 0.92f);
            var volRows = new[] { "MasterSlider", "SfxSlider", "BgmSlider" };
            var volYs = new[] { 48f, 0f, -48f };
            for (int i = 0; i < volRows.Length; i++)
            {
                var row = vol.Find(volRows[i]);
                if (row == null) continue;
                SetRect(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, volYs[i]), new Vector2(230, 44));
                var volLabel = row.Find("Label");
                if (volLabel != null)
                {
                    SetText(volLabel, _body, Parchment, 15, TextAlignmentOptions.MidlineRight);
                    SetRect(volLabel, new Vector2(0, 0), new Vector2(0.34f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                }
                var sliderT = row.Find("Slider");
                if (sliderT != null)
                {
                    SetRect(sliderT, new Vector2(0.36f, 0.15f), new Vector2(1, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                    StyleSliderTrack(sliderT, new Color(0.22f, 0.18f, 0.12f, 0.9f));
                }
            }
        }

        // Complete overlay
        var overlay = FT(hud, "CompleteOverlay");
        if (overlay != null)
        {
            var oImg = EnsureImage(overlay.gameObject);
            oImg.sprite = null; oImg.color = new Color(0.08f, 0.055f, 0.04f, 0.92f);

            var seal = overlay.Find("WaxSeal");
            if (seal == null)
            {
                var go = new GameObject("WaxSeal", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(overlay, false);
                seal = go.transform;
            }
            var sImg = seal.GetComponent<Image>();
            sImg.sprite = _seal; sImg.color = Color.white; sImg.raycastTarget = false;
            SetRect(seal, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(230, 230));
            seal.localEulerAngles = new Vector3(0, 0, 10);

            var ct = FT(overlay, "CompleteText");
            if (ct != null)
            {
                SetText(ct, _display, Parchment, 64, TextAlignmentOptions.Center);
                SetRect(ct, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(900, 320));
                ct.GetComponent<TMP_Text>().text = "Complete!";
            }
        }

        // Pause panel
        var pause = FT(hud, "PausePanel");
        if (pause != null)
        {
            var pImg = pause.GetComponent<Image>();
            if (pImg == null) pImg = pause.gameObject.AddComponent<Image>();
            pImg.sprite = null; pImg.color = new Color(0.05f, 0.04f, 0.03f, 0.72f);

            var inner = FT(pause, "InnerPanel");
            if (inner == null)
            {
                var go = new GameObject("InnerPanel", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(pause, false);
                inner = go.transform;
            }
            var iImg = EnsureImage(inner.gameObject);
            iImg.sprite = _panel; iImg.type = Image.Type.Sliced; iImg.color = Color.white;
            SetRect(inner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440, 460));

            var pTitle = FT(inner, "Title");
            if (pTitle != null) { SetText(pTitle, _display, Ink, 46, TextAlignmentOptions.Center); SetRect(pTitle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 145), new Vector2(300, 56)); }
            var resume = FT(inner, "ResumeButton");
            if (resume != null) { SetRect(resume, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 95), new Vector2(260, 48)); SetButton(resume); }
            var setBtn = FT(inner, "SettingsButton");
            if (setBtn != null) { SetRect(setBtn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 15), new Vector2(260, 48)); SetButton(setBtn); }
            var exitBtn = FT(inner, "ExitRoomButton");
            if (exitBtn != null) { SetRect(exitBtn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -65), new Vector2(260, 48)); SetButton(exitBtn); }

            var pHint = inner.Find("Hint");
            if (pHint == null)
            {
                var go = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(inner, false);
                pHint = go.transform;
            }
            SetText(pHint, _body, new Color(0.35f, 0.27f, 0.18f), 18, TextAlignmentOptions.Center);
            pHint.GetComponent<TMP_Text>().text = "\u6309 ESC \u7EE7\u7EED"; // 按 ESC 继续
            SetRect(pHint, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -140), new Vector2(320, 26));
        }

        // Echo test panel
        var echo = FT(hud, "EchoTestPanel");
        if (echo != null)
        {
            var eImg = EnsureImage(echo.gameObject);
            eImg.sprite = _panel; eImg.type = Image.Type.Sliced; eImg.color = Color.white;

            var eTitle = FT(echo, "Title");
            if (eTitle != null) SetText(eTitle, _display, Ink, 26, TextAlignmentOptions.Center);
            var ipLabel = FT(echo, "IpLabel");
            if (ipLabel != null) SetText(ipLabel, _body, Ink, 18, TextAlignmentOptions.Center);
            var ipInput = FT(echo, "IpInput");
            if (ipInput != null) SetInput(ipInput);
            var startBtn = FT(echo, "ButtonRow/StartButton");
            if (startBtn != null) SetButton(startBtn);
            var clearBtn = FT(echo, "ButtonRow/ClearButton");
            if (clearBtn != null) SetButton(clearBtn);
            var outBg = FT(echo, "OutputBG");
            if (outBg != null)
            {
                var obImg = outBg.GetComponent<Image>();
                if (obImg != null) { obImg.sprite = null; obImg.color = new Color(WalnutDeep.r, WalnutDeep.g, WalnutDeep.b, 0.92f); }
                var outText = FT(outBg, "Viewport/OutputText");
                if (outText != null) SetText(outText, _body, new Color(0.90f, 0.84f, 0.70f), 15, TextAlignmentOptions.TopLeft);
            }
            var closeBtn = FT(echo, "CloseButton");
            if (closeBtn != null) SetButton(closeBtn);
        }

        // Settings panel (structure was rebuilt in a later pass — restyle to theme)
        var settings = FT(hud, "SettingsPanel");
        if (settings != null)
        {
            var sRootImg = EnsureImage(settings.gameObject);
            sRootImg.sprite = null; sRootImg.color = new Color(0.05f, 0.04f, 0.03f, 0.72f);

            var inner = FT(settings, "InnerPanel");
            if (inner == null)
            {
                var go = new GameObject("InnerPanel", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(settings, false);
                inner = go.transform;
            }
            var iImg = EnsureImage(inner.gameObject);
            iImg.sprite = _panel; iImg.type = Image.Type.Sliced; iImg.color = Color.white;
            SetRect(inner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 820));

            var sTitle = FT(inner, "Title");
            if (sTitle != null)
            {
                SetText(sTitle, _display, Ink, 40, TextAlignmentOptions.Center);
                SetRect(sTitle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 265), new Vector2(400, 40));
            }

            // Resolution caption + dropdown
            var resLabel = inner.Find("ResLabel");
            if (resLabel == null)
            {
                var go = new GameObject("ResLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(inner, false);
                resLabel = go.transform;
            }
            SetText(resLabel, _body, Ink, 24, TextAlignmentOptions.MidlineRight);
            resLabel.GetComponent<TMP_Text>().text = "\u5206\u8FA8\u7387:"; // 分辨率:
            SetRect(resLabel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-310, 195), new Vector2(280, 48));

            var dropdown = FT(inner, "ResolutionDropdown");
            if (dropdown != null)
            {
                SetRect(dropdown, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(75, 195), new Vector2(500, 48));
                var dImg = dropdown.GetComponent<Image>();
                if (dImg != null) { dImg.sprite = null; dImg.color = new Color(Walnut.r, Walnut.g, Walnut.b, 0.95f); }
                var dLabel = FT(dropdown, "Label");
                if (dLabel != null) SetText(dLabel, _display, Parchment, 20, TextAlignmentOptions.MidlineLeft);
                var arrow = FT(dropdown, "Arrow");
                if (arrow != null)
                {
                    var aTmp = arrow.GetComponent<TMP_Text>();
                    if (aTmp == null)
                    {
                        var aImg = arrow.GetComponent<Image>();
                        if (aImg != null) UnityEngine.Object.DestroyImmediate(aImg);
                        arrow.gameObject.AddComponent<TextMeshProUGUI>();
                    }
                    SetText(arrow, _body, Sepia, 16, TextAlignmentOptions.Center);
                    arrow.GetComponent<TMP_Text>().text = "\u25BC"; // ▼
                }
                var tpl = FT(dropdown, "Template");
                if (tpl != null)
                {
                    var tImg = tpl.GetComponent<Image>();
                    if (tImg != null) { tImg.sprite = null; tImg.color = new Color(Walnut.r, Walnut.g, Walnut.b, 0.98f); }
                    // old structure: Viewport/Content/Item Background; new: Viewport/Content/Item/Item Background
                    var itemBg = tpl.Find("Viewport/Content/Item Background");
                    if (itemBg == null) itemBg = tpl.Find("Viewport/Content/Item/Item Background");
                    if (itemBg != null)
                    {
                        var ibImg = itemBg.GetComponent<Image>();
                        if (ibImg != null) { ibImg.sprite = null; ibImg.color = new Color(0.20f, 0.16f, 0.11f, 1f); }
                    }
                    var itemLabel = tpl.Find("Viewport/Content/Item Background/Item Label");
                    if (itemLabel == null) itemLabel = tpl.Find("Viewport/Content/Item/Item Label");
                    if (itemLabel != null) SetText(itemLabel, _body, Parchment, 20, TextAlignmentOptions.MidlineLeft);
                    var itemCheck = tpl.Find("Viewport/Content/Item/Item Checkmark");
                    if (itemCheck != null)
                    {
                        var cImg = itemCheck.GetComponent<Image>();
                        if (cImg != null) { cImg.sprite = null; cImg.color = Brass; }
                    }
                }
            }

            // Fullscreen toggle row
            var toggle = FT(inner, "FullscreenToggle");
            if (toggle != null)
            {
                SetRect(toggle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 130), new Vector2(700, 42));
                var tBg = FT(toggle, "Background");
                if (tBg != null)
                {
                    var bImg = tBg.GetComponent<Image>();
                    if (bImg == null) bImg = tBg.gameObject.AddComponent<Image>();
                    bImg.sprite = null; bImg.color = new Color(Walnut.r, Walnut.g, Walnut.b, 0.95f);
                    var check = FT(tBg, "Checkmark");
                    if (check != null)
                    {
                        var cImg = check.GetComponent<Image>();
                        if (cImg != null) { cImg.sprite = null; cImg.color = Brass; }
                    }
                }
                var tLabel = FT(toggle, "Label");
                if (tLabel != null)
                {
                    SetText(tLabel, _body, Ink, 22, TextAlignmentOptions.MidlineLeft);
                    tLabel.GetComponent<TMP_Text>().text = "\u5168\u5C4F\u6A21\u5F0F:"; // 全屏模式:
                    SetRect(tLabel, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(36, 0), new Vector2(-48, 0));
                }
            }

            // Audio sliders (nested Label + Slider structure)
            var sliderRows = new[] { "MasterSlider", "SfxSlider", "BgmSlider" };
            var sliderYs = new[] { 70f, 12f, -46f };
            var sliderTexts = new[]
            {
                "\u4E3B\u97F3\u91CF", // 主音量
                "\u97F3\u6548",       // 音效
                "\u97F3\u4E50"        // 音乐
            };
            for (int i = 0; i < sliderRows.Length; i++)
            {
                var row = FT(inner, sliderRows[i]);
                if (row == null) continue;
                SetRect(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, sliderYs[i]), new Vector2(480, 44));
                var label = row.Find("Label");
                if (label != null)
                {
                    SetText(label, _body, Ink, 22, TextAlignmentOptions.MidlineRight);
                    label.GetComponent<TMP_Text>().text = sliderTexts[i];
                    SetRect(label, new Vector2(0, 0), new Vector2(0.4f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                }
                var sliderT = row.Find("Slider");
                if (sliderT != null)
                {
                    SetRect(sliderT, new Vector2(0.42f, 0.15f), new Vector2(1, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                    StyleSliderTrack(sliderT, new Color(Walnut.r, Walnut.g, Walnut.b, 0.75f));
                }
            }

            // Key bindings help (recreated if removed)
            var help = inner.Find("ControlsHelp");
            if (help == null)
            {
                var go = new GameObject("ControlsHelp", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(inner, false);
                help = go.transform;
            }
            var hBg = help.GetComponent<Image>();
            if (hBg == null) hBg = help.gameObject.AddComponent<Image>();
            hBg.sprite = null; hBg.color = new Color(WalnutDeep.r, WalnutDeep.g, WalnutDeep.b, 0.85f);
            hBg.raycastTarget = false;
            SetRect(help, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -200), new Vector2(660, 210));

            var hText = help.Find("ControlsText");
            if (hText == null)
            {
                var go = new GameObject("ControlsText", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(help, false);
                hText = go.transform;
            }
            SetText(hText, _body, new Color(0.90f, 0.84f, 0.70f), 16, TextAlignmentOptions.TopLeft);
            hText.GetComponent<TMP_Text>().text =
                "<b>\u6309\u952E\u8BF4\u660E</b>\n" +
                "<color=#C9A227>W A S D</color> \u2014 \u79FB\u52A8\n" +
                "<color=#C9A227>\u5DE6\u952E</color> \u2014 \u84DD\u8272\u4F20\u9001\u95E8\n" +
                "<color=#C9A227>\u53F3\u952E</color> \u2014 \u6A59\u8272\u4F20\u9001\u95E8\n" +
                "<color=#C9A227>E</color> \u2014 \u4EA4\u4E92 (NPC/\u62FE\u53D6)\n" +
                "<color=#C9A227>T</color> \u2014 \u804A\u5929\n" +
                "<color=#C9A227>ESC</color> \u2014 \u6682\u505C\u83DC\u5355\n" +
                "<color=#C9A227>Tab</color> \u2014 \u663E\u793A/\u9690\u85CF HUD";
            SetRect(hText, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-44, -22));

            var backBtn = FT(inner, "BackButton") ?? FT(settings, "SettingsBackButton");
            if (backBtn != null)
            {
                SetRect(backBtn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(330, 265), new Vector2(170, 45));
                SetButton(backBtn);
            }
        }
    }

    // ---------- room item prefab ----------

    static void ApplyToRoomItemPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RoomItemPrefab.prefab");
        if (prefab == null) { Debug.LogWarning("[DetectiveTheme] RoomItemPrefab not found"); return; }

        var rootImg = prefab.GetComponent<Image>();
        if (rootImg != null) { rootImg.sprite = null; rootImg.color = new Color(0.19f, 0.15f, 0.10f, 0.95f); }

        var nameText = prefab.transform.Find("RoomNameText");
        if (nameText != null) SetText(nameText, _display, Parchment, 20, TextAlignmentOptions.MidlineLeft);
        var hostText = prefab.transform.Find("HostNameText");
        if (hostText != null) SetText(hostText, _body, new Color(0.75f, 0.66f, 0.50f), 15, TextAlignmentOptions.MidlineLeft);
        var countText = prefab.transform.Find("PlayerCountText");
        if (countText != null) SetText(countText, _display, Parchment, 18, TextAlignmentOptions.MidlineLeft);
        var joinBtn = prefab.transform.Find("JoinButton");
        if (joinBtn != null)
        {
            var btn = joinBtn.GetComponent<Button>();
            var img = joinBtn.GetComponent<Image>();
            if (img != null) { img.sprite = _button; img.type = Image.Type.Sliced; img.color = Color.white; }
            if (btn != null)
            {
                var cb = btn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(1.12f, 1.05f, 0.86f);
                cb.pressedColor = new Color(0.82f, 0.72f, 0.55f);
                cb.selectedColor = Color.white;
                cb.disabledColor = new Color(0.55f, 0.50f, 0.42f, 0.6f);
                cb.fadeDuration = 0.12f;
                btn.colors = cb;
            }
            var jl = joinBtn.GetComponentInChildren<TMP_Text>(true);
            if (jl != null && _display != null) { jl.font = _display; jl.color = Ink; }
        }

        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();
        Debug.Log("[DetectiveTheme] RoomItemPrefab styled.");
    }
}
