using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UIKit
{
    public static readonly Color Ink = new Color32(43, 42, 76, 255);
    public static readonly Color NavyOutline = new Color32(20, 45, 75, 255);
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    // ---------- Dasar ----------
    public static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    public static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    public static void Place(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        r.anchorMin = anchor;
        r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }

    // ---------- Sprite buatan kode ----------
    public static Sprite Rounded(Color fill, Color border, int radius, int borderPx)
    {
        radius = Mathf.Min(radius, 60);
        string key = "r" + ColorUtility.ToHtmlStringRGBA(fill) + ColorUtility.ToHtmlStringRGBA(border) + radius + "_" + borderPx;
        if (cache.TryGetValue(key, out Sprite found) && found != null) return found;

        const int S = 128;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[S * S];
        float half = S * 0.5f;
        float inner = half - radius;
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - half) - inner;
                float qy = Mathf.Abs(y + 0.5f - half) - inner;
                float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                float aOuter = Mathf.Clamp01(0.5f - d);
                float aFill = Mathf.Clamp01(0.5f - (d + borderPx));
                Color c = Color.Lerp(border, fill, aFill);
                c.a = aOuter;
                px[y * S + x] = c;
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        float b = radius + 2;
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        cache[key] = sp;
        return sp;
    }

    private static Sprite StarSprite()
    {
        if (cache.TryGetValue("star", out Sprite found) && found != null) return found;
        const int N = 128;
        Texture2D tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[N * N];
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                float px2 = Mathf.Abs((x + 0.5f) / N * 2f - 1f);
                float py2 = Mathf.Abs((y + 0.5f) / N * 2f - 1f);
                float v = Mathf.Pow(px2, 0.55f) + Mathf.Pow(py2, 0.55f);
                float a = Mathf.Clamp01((1f - v) * 8f);
                px[y * N + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        cache["star"] = sp;
        return sp;
    }

    private static float DistSeg(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (p - (a + ab * t)).magnitude;
    }

    public static Sprite Face(bool angry)
    {
        string key = angry ? "face_angry" : "face_happy";
        if (cache.TryGetValue(key, out Sprite found) && found != null) return found;

        const int S = 256;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[S * S];
        Color face = angry ? new Color(1f, 0.56f, 0.46f) : new Color(1f, 0.85f, 0.28f);
        Color blush = new Color(1f, 0.55f, 0.65f);

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / S * 2f - 1f, (y + 0.5f) / S * 2f - 1f);
                float r = p.magnitude;
                float alpha = Mathf.Clamp01((0.97f - r) * S * 0.5f);
                Color c = Color.clear;
                if (alpha > 0f)
                {
                    c = r > 0.89f ? Ink : face;
                    if (r <= 0.89f)
                    {
                        float eyeY = angry ? 0.08f : 0.14f;
                        if ((p - new Vector2(-0.30f, eyeY)).magnitude < 0.09f || (p - new Vector2(0.30f, eyeY)).magnitude < 0.09f) c = Ink;
                        if (!angry)
                        {
                            Vector2 q = p - new Vector2(0f, -0.02f);
                            if (Mathf.Abs(q.magnitude - 0.40f) < 0.045f && q.y < -0.12f) c = Ink;
                            if ((p - new Vector2(-0.56f, -0.10f)).magnitude < 0.10f || (p - new Vector2(0.56f, -0.10f)).magnitude < 0.10f) c = blush;
                        }
                        else
                        {
                            Vector2 q = p - new Vector2(0f, -0.62f);
                            if (Mathf.Abs(q.magnitude - 0.30f) < 0.045f && q.y > 0.12f) c = Ink;
                            if (DistSeg(p, new Vector2(-0.52f, 0.40f), new Vector2(-0.14f, 0.24f)) < 0.05f) c = Ink;
                            if (DistSeg(p, new Vector2(0.52f, 0.40f), new Vector2(0.14f, 0.24f)) < 0.05f) c = Ink;
                        }
                    }
                    c.a = alpha;
                }
                px[y * S + x] = c;
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        cache[key] = sp;
        return sp;
    }

    // Sprite Kursor Jari Telunjuk Kartun (Pointing Hand Cursor) Sempurna
    public static Sprite HandCursor()
    {
        if (cache.TryGetValue("hand_cursor_perfect", out Sprite found) && found != null) return found;

        const int S = 256;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[S * S];

        Color handFill = Color.white;
        Color handOutline = Ink;

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / S * 2f - 1f, (y + 0.5f) / S * 2f - 1f);
                Color c = Color.clear;

                float dIndex = DistSeg(p, new Vector2(-0.35f, 0.48f), new Vector2(-0.05f, -0.15f));
                float dPalm  = DistSeg(p, new Vector2(-0.05f, -0.30f), new Vector2(0.35f, -0.55f));
                float dFinger1 = DistSeg(p, new Vector2(-0.15f, -0.15f), new Vector2(0.10f, -0.35f));
                float dFinger2 = DistSeg(p, new Vector2(0.00f, -0.05f), new Vector2(0.25f, -0.25f));
                float dThumb = DistSeg(p, new Vector2(-0.30f, 0.05f), new Vector2(-0.10f, -0.15f));

                float minD = Mathf.Min(dIndex, Mathf.Min(dPalm, Mathf.Min(dFinger1, Mathf.Min(dFinger2, dThumb))));

                if (minD < 0.18f)
                {
                    c = minD > 0.11f ? handOutline : handFill;
                }

                px[y * S + x] = c;
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        cache["hand_cursor_perfect"] = sp;
        return sp;
    }

    // ---------- Komponen UI ----------

    public static UnityEngine.UI.Image MakeImage(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size, Color outlineColor = default, int outlineThickness = 0)
    {
        RectTransform r = NewRect(name, parent);
        Place(r, anchor, pos, size);
        UnityEngine.UI.Image im = r.gameObject.AddComponent<UnityEngine.UI.Image>();
        im.sprite = sprite;
        im.preserveAspect = true;

        if (outlineThickness > 0)
        {
            Outline ot = r.gameObject.AddComponent<Outline>();
            ot.effectColor = outlineColor;
            ot.effectDistance = new Vector2(outlineThickness, outlineThickness);
        }
        return im;
    }

    public static TextMeshProUGUI Text(Transform parent, string text, float size, Color color, TMP_FontAsset customFont = null,
        TextAlignmentOptions align = TextAlignmentOptions.Center, FontStyles style = FontStyles.Bold)
    {
        RectTransform r = NewRect("Text", parent);
        Stretch(r);
        TextMeshProUGUI t = r.gameObject.AddComponent<TextMeshProUGUI>();
        if (customFont != null) t.font = customFont;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;

        // Penambahan Efek Outline & Drop Shadow pada Teks TMR
        t.outlineColor = Ink;
        t.outlineWidth = 0.15f; // Memberi garisan luar gelap yang tajam & rapi

        return t;
    }

    public static TextMeshProUGUI Label(Transform parent, string text, float size, Color color,
        Vector2 anchor, Vector2 pos, Vector2 box, TMP_FontAsset customFont = null)
    {
        TextMeshProUGUI t = Text(parent, text, size, color, customFont);
        Place(t.rectTransform, anchor, pos, box);
        return t;
    }

    public static UnityEngine.UI.Image Card(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size,
        Color fill, Color border, int radius = 48, int borderPx = 6, bool shadow = true)
    {
        RectTransform box = NewRect(name, parent);
        Place(box, anchor, pos, size);
        if (shadow)
        {
            RectTransform sh = NewRect("Shadow", box);
            Stretch(sh);
            sh.offsetMin = new Vector2(8f, -10f);
            sh.offsetMax = new Vector2(8f, -10f);
            UnityEngine.UI.Image si = sh.gameObject.AddComponent<UnityEngine.UI.Image>();
            si.sprite = Rounded(Ink, Ink, radius, borderPx);
            si.type = UnityEngine.UI.Image.Type.Sliced;
            si.raycastTarget = false;
        }
        RectTransform body = NewRect("Body", box);
        Stretch(body);
        UnityEngine.UI.Image bi = body.gameObject.AddComponent<UnityEngine.UI.Image>();
        bi.sprite = Rounded(fill, border, radius, borderPx);
        bi.type = UnityEngine.UI.Image.Type.Sliced;
        return bi;
    }

    public static UnityEngine.UI.Image CardButton(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size,
        Color fill, UnityAction onClick, int radius = 48)
    {
        UnityEngine.UI.Image body = Card(parent, name, anchor, pos, size, fill, Ink, radius, 6, true);
        Button b = body.gameObject.AddComponent<Button>();
        b.targetGraphic = body;
        b.transition = Selectable.Transition.None;
        if (onClick != null) b.onClick.AddListener(onClick);
        body.gameObject.AddComponent<UIPop>();
        return body;
    }

    public static Button MakeButton(Transform parent, string label, float fontSize, Color fill,
        Vector2 anchor, Vector2 pos, Vector2 size, UnityAction onClick, bool bounce = false)
    {
        UnityEngine.UI.Image body = CardButton(parent, label, anchor, pos, size, fill, onClick);
        Text(body.transform, label, fontSize, Ink);
        if (bounce) body.transform.parent.gameObject.AddComponent<UIBounce>();
        return body.GetComponent<Button>();
    }

    public static TextMeshProUGUI Pill(Transform parent, string text, float fontSize, Color fill,
        Vector2 anchor, Vector2 pos, Vector2 size)
    {
        UnityEngine.UI.Image body = Card(parent, "Pill", anchor, pos, size, fill, Ink, 48, 6, true);
        return Text(body.transform, text, fontSize, Ink);
    }

    public static UnityEngine.UI.Image Sparkle(Transform parent, Vector2 pos, float size, Color color)
    {
        RectTransform r = NewRect("Sparkle", parent);
        Place(r, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
        UnityEngine.UI.Image im = r.gameObject.AddComponent<UnityEngine.UI.Image>();
        im.sprite = StarSprite();
        im.color = color;
        im.raycastTarget = false;
        r.gameObject.AddComponent<UIBounce>();
        return im;
    }
}

public class UIPop : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public void OnPointerDown(PointerEventData e) { transform.localScale = Vector3.one * 0.94f; }
    public void OnPointerUp(PointerEventData e) { transform.localScale = Vector3.one; }
    public void OnPointerExit(PointerEventData e) { transform.localScale = Vector3.one; }
}

public class UIBounce : MonoBehaviour
{
    public float amount = 0.04f;
    public float speed = 2.2f;
    private float phase;

    private void OnEnable() { phase = Random.value * 6.28f; }

    private void Update()
    {
        transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * speed + phase) * amount);
    }
}