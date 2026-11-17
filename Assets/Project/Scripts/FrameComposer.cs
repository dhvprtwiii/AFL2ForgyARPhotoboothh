using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FrameDef
{
    public string name = "Frame";
    public Texture2D image;
    [Tooltip("Centang jika slot foto ditandai kotak magenta #FF00FF pada gambar frame.")]
    public bool autoDetect = true;
    [Tooltip("Dipakai bila Auto Detect mati. Satuan 0..1 dari pojok kiri-atas: X, Y, Lebar, Tinggi.")]
    public Rect[] manualSlots;
}

public class FrameData
{
    public int w;
    public int h;
    public Color32[] pixels;
    public Rect[] slots;   // 0..1, titik asal kiri-bawah
}

public class Shot
{
    public Color32[] px;
    public int w;
    public int h;
}

public static class FrameComposer
{
    // Mengubah ukuran lewat GPU (bekerja juga untuk texture yang tidak Read/Write).
    public static Texture2D Resize(Texture src, int w, int h)
    {
        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        rt.filterMode = FilterMode.Bilinear;
        Graphics.Blit(src, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        t.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return t;
    }

    public static Shot MakeShot(Texture2D screenshot, int maxWidth)
    {
        int w = Mathf.Min(maxWidth, screenshot.width);
        int h = Mathf.RoundToInt(screenshot.height * (w / (float)screenshot.width));
        Texture2D small = Resize(screenshot, w, h);
        Shot s = new Shot();
        s.px = small.GetPixels32();
        s.w = w;
        s.h = h;
        Object.Destroy(small);
        return s;
    }

    public static bool IsBlank(Shot s)
    {
        if (s == null || s.px == null || s.px.Length == 0) return true;
        for (int i = 0; i < s.px.Length; i += 97)
        {
            Color32 c = s.px[i];
            if (c.r > 3 || c.g > 3 || c.b > 3) return false;
        }
        return true;
    }

    public static Texture2D ToTexture(Color32[] px, int w, int h)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.SetPixels32(px);
        t.Apply();
        return t;
    }

    // Frame bawaan jika Anda belum punya gambar frame.
    public static FrameData Plain(int slotCount, int width, Color32 bg)
    {
        FrameData fd = new FrameData();
        fd.w = width;
        if (slotCount <= 1)
        {
            fd.h = Mathf.RoundToInt(width * 2f / 3f);
            fd.slots = new Rect[] { new Rect(0.04f, 0.06f, 0.92f, 0.88f) };
        }
        else
        {
            fd.h = width * 3;
            fd.slots = new Rect[slotCount];
            float top = 0.05f;
            float gap = 0.02f;
            float sh = (0.90f - gap * (slotCount - 1)) / slotCount;
            for (int i = 0; i < slotCount; i++)
                fd.slots[i] = new Rect(0.08f, 1f - top - (i + 1) * sh - i * gap, 0.84f, sh);
        }
        fd.pixels = new Color32[fd.w * fd.h];
        for (int i = 0; i < fd.pixels.Length; i++) fd.pixels[i] = bg;
        return fd;
    }

    public static FrameData Prepare(FrameDef def, int maxWidth, int slotCount, Color32 plainColor)
    {
        if (def == null || def.image == null) return Plain(slotCount, maxWidth, plainColor);

        Texture2D src = def.image;
        int w = Mathf.Min(src.width, maxWidth);
        int h = Mathf.RoundToInt(src.height * (w / (float)src.width));
        Texture2D t = Resize(src, w, h);
        FrameData fd = new FrameData();
        fd.w = w;
        fd.h = h;
        fd.pixels = t.GetPixels32();
        Object.Destroy(t);

        if (def.autoDetect) fd.slots = DetectSlots(fd);
        if ((fd.slots == null || fd.slots.Length == 0) && def.manualSlots != null && def.manualSlots.Length > 0)
        {
            fd.slots = new Rect[def.manualSlots.Length];
            for (int i = 0; i < fd.slots.Length; i++)
            {
                Rect r = def.manualSlots[i];
                fd.slots[i] = new Rect(r.x, 1f - (r.y + r.height), r.width, r.height);
            }
        }
        if (fd.slots == null || fd.slots.Length == 0)
        {
            Debug.LogWarning("Slot foto tidak ditemukan pada frame " + def.name + ". Pakai posisi bawaan.");
            fd.slots = Plain(slotCount, maxWidth, plainColor).slots;
        }
        return fd;
    }

    // Mencari kotak magenta (#FF00FF), diurutkan dari atas ke bawah.
    private static Rect[] DetectSlots(FrameData fd)
    {
        int w = fd.w;
        int h = fd.h;
        List<Rect> result = new List<Rect>();
        bool inRun = false;
        int runTop = 0;
        int minX = w;
        int maxX = 0;

        for (int y = h - 1; y >= -1; y--)
        {
            int count = 0;
            int rowMin = w;
            int rowMax = -1;
            if (y >= 0)
            {
                for (int x = 0; x < w; x++)
                {
                    Color32 c = fd.pixels[y * w + x];
                    if (c.r > 200 && c.b > 200 && c.g < 90)
                    {
                        count++;
                        if (x < rowMin) rowMin = x;
                        if (x > rowMax) rowMax = x;
                    }
                }
            }
            bool hit = count > w * 0.1f;
            if (hit)
            {
                if (!inRun) { inRun = true; runTop = y; minX = w; maxX = 0; }
                if (rowMin < minX) minX = rowMin;
                if (rowMax > maxX) maxX = rowMax;
            }
            else if (inRun)
            {
                inRun = false;
                int runBottom = y + 1;
                int rh = runTop - runBottom + 1;
                if (rh > h * 0.03f)
                    result.Add(new Rect(minX / (float)w, runBottom / (float)h, (maxX - minX + 1) / (float)w, rh / (float)h));
            }
        }
        return result.ToArray();
    }

    // Menempel foto ke slot (dipotong dari tengah agar pas, tanpa gepeng).
    public static Color32[] Compose(FrameData fd, IList<Shot> shots)
    {
        Color32[] outPx = (Color32[])fd.pixels.Clone();
        if (shots == null || shots.Count == 0) return outPx;

        for (int i = 0; i < fd.slots.Length; i++)
        {
            Shot s = shots[Mathf.Min(i, shots.Count - 1)];
            Rect r = fd.slots[i];
            int x0 = Mathf.RoundToInt(r.x * fd.w);
            int y0 = Mathf.RoundToInt(r.y * fd.h);
            int sw = Mathf.RoundToInt(r.width * fd.w);
            int sh = Mathf.RoundToInt(r.height * fd.h);
            if (sw < 2 || sh < 2) continue;

            float slotAspect = sw / (float)sh;
            float cw = s.w;
            float ch = s.h;
            if (cw / ch > slotAspect) cw = ch * slotAspect; else ch = cw / slotAspect;
            float cx = (s.w - cw) * 0.5f;
            float cy = (s.h - ch) * 0.5f;

            for (int y = 0; y < sh; y++)
            {
                int dy = y0 + y;
                if (dy < 0 || dy >= fd.h) continue;
                float v = cy + (y + 0.5f) / sh * ch;
                for (int x = 0; x < sw; x++)
                {
                    int dx = x0 + x;
                    if (dx < 0 || dx >= fd.w) continue;
                    float u = cx + (x + 0.5f) / sw * cw;
                    outPx[dy * fd.w + dx] = Bilinear(s.px, s.w, s.h, u, v);
                }
            }
        }
        return outPx;
    }

    private static Color32 Bilinear(Color32[] p, int w, int h, float u, float v)
    {
        u = Mathf.Clamp(u - 0.5f, 0f, w - 1.001f);
        v = Mathf.Clamp(v - 0.5f, 0f, h - 1.001f);
        int x0 = (int)u;
        int y0 = (int)v;
        float fx = u - x0;
        float fy = v - y0;
        Color32 a = p[y0 * w + x0];
        Color32 b = p[y0 * w + x0 + 1];
        Color32 c = p[(y0 + 1) * w + x0];
        Color32 d = p[(y0 + 1) * w + x0 + 1];
        float w00 = (1f - fx) * (1f - fy);
        float w10 = fx * (1f - fy);
        float w01 = (1f - fx) * fy;
        float w11 = fx * fy;
        return new Color32(
            (byte)(a.r * w00 + b.r * w10 + c.r * w01 + d.r * w11 + 0.5f),
            (byte)(a.g * w00 + b.g * w10 + c.g * w01 + d.g * w11 + 0.5f),
            (byte)(a.b * w00 + b.b * w10 + c.b * w01 + d.b * w11 + 0.5f),
            255);
    }
}