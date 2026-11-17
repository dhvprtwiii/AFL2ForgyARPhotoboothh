using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ForgyArmRig : MonoBehaviour
{
    [Serializable]
    public class ArmSet
    {
        public string name = "Menyapa";
        public GameObject model;   // pembungkus ekspresi (Menyapa / Marah)

        [Header("Tulang tangan (seret semua tulang milik satu tangan)")]
        public Transform[] leftBones;
        public Transform[] rightBones;

        [Header("Pose tersimpan (diisi lewat menu kalibrasi)")]
        public Quaternion[] leftDown;
        public Quaternion[] leftUp;
        public Quaternion[] rightDown;
        public Quaternion[] rightUp;

        [Header("Perilaku")]
        public bool raiseNear = true;       // tangan dekat tengah terangkat
        public bool raiseFar = false;       // tangan jauh dari tengah terangkat
        public bool oscillateNear = true;   // tangan dekat bergoyang/melambai
        public bool oscillateFar = false;
        public float oscAngle = 20f;        // besar goyangan (derajat)
        public float oscSpeed = 6f;
        public Vector3 oscAxis = new Vector3(0f, 0f, 1f);
        public int oscBoneIndex = 0;        // tulang ke berapa yang bergoyang

        [NonSerialized] public Quaternion[] leftNow;
        [NonSerialized] public Quaternion[] rightNow;
    }

    [Header("Referensi")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform forgyRoot;

    [Header("Ekspresi")]
    [SerializeField] private ArmSet[] sets;

    [Header("Gerak")]
    [SerializeField] private float armSpeed = 8f;

    [Header("Kalibrasi (menu titik tiga komponen ini)")]
    [SerializeField] private int setYangDiedit = 0;

    private ArmSet lastSet;

    private void LateUpdate()
    {
        if (!Application.isPlaying || cam == null || forgyRoot == null) return;

        ArmSet s = ActiveSet();
        if (s == null) return;
        if (!Valid(s.leftBones, s.leftDown, s.leftUp)) return;
        if (!Valid(s.rightBones, s.rightDown, s.rightUp)) return;

        if (s != lastSet || s.leftNow == null)
        {
            s.leftNow = Read(s.leftBones);
            s.rightNow = Read(s.rightBones);
            lastSet = s;
        }

        // Forgy di sisi mana layar, dan tangan mana yang lebih dekat ke tengah.
        bool forgyOnRight = cam.WorldToViewportPoint(forgyRoot.position).x > 0.5f;
        float lx = cam.WorldToViewportPoint(s.leftBones[0].position).x;
        float rx = cam.WorldToViewportPoint(s.rightBones[0].position).x;
        bool leftIsNear = forgyOnRight ? lx < rx : lx > rx;

        Drive(s, s.leftBones, s.leftDown, s.leftUp, s.leftNow, leftIsNear);
        Drive(s, s.rightBones, s.rightDown, s.rightUp, s.rightNow, !leftIsNear);
    }

    private void Drive(ArmSet s, Transform[] bones, Quaternion[] down, Quaternion[] up,
                       Quaternion[] now, bool isNear)
    {
        bool raise = isNear ? s.raiseNear : s.raiseFar;
        bool osc = isNear ? s.oscillateNear : s.oscillateFar;
        Quaternion[] target = raise ? up : down;
        float t = 1f - Mathf.Exp(-armSpeed * Time.deltaTime);

        for (int i = 0; i < bones.Length; i++)
        {
            now[i] = Quaternion.Slerp(now[i], target[i], t);
            Quaternion r = now[i];
            if (osc && raise && i == s.oscBoneIndex)
            {
                float a = Mathf.Sin(Time.time * s.oscSpeed) * s.oscAngle;
                r = r * Quaternion.AngleAxis(a, s.oscAxis);
            }
            bones[i].localRotation = r;
        }
    }

    private ArmSet ActiveSet()
    {
        if (sets == null) return null;
        for (int i = 0; i < sets.Length; i++)
        {
            if (sets[i].model != null && sets[i].model.activeInHierarchy) return sets[i];
        }
        return null;
    }

    private static bool Valid(Transform[] bones, Quaternion[] a, Quaternion[] b)
    {
        if (bones == null || bones.Length == 0) return false;
        if (a == null || a.Length != bones.Length) return false;
        if (b == null || b.Length != bones.Length) return false;
        for (int i = 0; i < bones.Length; i++)
            if (bones[i] == null) return false;
        return true;
    }

    private static Quaternion[] Read(Transform[] bones)
    {
        Quaternion[] result = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++)
            result[i] = bones[i] != null ? bones[i].localRotation : Quaternion.identity;
        return result;
    }

    // ---------------- Menu kalibrasi (dipakai di Editor, bukan saat Play) ----------------

    private ArmSet EditSet()
    {
        if (sets == null || setYangDiedit < 0 || setYangDiedit >= sets.Length)
        {
            Debug.LogWarning("Set Yang Diedit di luar jangkauan.");
            return null;
        }
        return sets[setYangDiedit];
    }

    private void MarkDirty()
    {
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("1. Simpan pose TURUN (kedua tangan)")]
    private void SaveDown()
    {
        ArmSet s = EditSet();
        if (s == null) return;
        s.leftDown = Read(s.leftBones);
        s.rightDown = Read(s.rightBones);
        MarkDirty();
        Debug.Log("Pose TURUN tersimpan.");
    }

    [ContextMenu("2. Simpan pose NAIK tangan kiri")]
    private void SaveLeftUp()
    {
        ArmSet s = EditSet();
        if (s == null) return;
        s.leftUp = Read(s.leftBones);
        MarkDirty();
        Debug.Log("Pose NAIK tangan kiri tersimpan.");
    }

    [ContextMenu("3. Simpan pose NAIK tangan kanan")]
    private void SaveRightUp()
    {
        ArmSet s = EditSet();
        if (s == null) return;
        s.rightUp = Read(s.rightBones);
        MarkDirty();
        Debug.Log("Pose NAIK tangan kanan tersimpan.");
    }

    [ContextMenu("Lihat pose: kedua tangan TURUN")]
    private void ViewDown()
    {
        ArmSet s = EditSet();
        if (s == null) return;
        Apply(s.leftBones, s.leftDown);
        Apply(s.rightBones, s.rightDown);
    }

    [ContextMenu("Lihat pose: kedua tangan NAIK")]
    private void ViewUp()
    {
        ArmSet s = EditSet();
        if (s == null) return;
        Apply(s.leftBones, s.leftUp);
        Apply(s.rightBones, s.rightUp);
    }

    private static void Apply(Transform[] bones, Quaternion[] pose)
    {
        if (bones == null || pose == null || pose.Length != bones.Length) return;
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] == null) continue;
#if UNITY_EDITOR
            Undo.RecordObject(bones[i], "Lihat pose");
#endif
            bones[i].localRotation = pose[i];
        }
    }
}