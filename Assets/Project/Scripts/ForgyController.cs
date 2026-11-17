using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForgyController : MonoBehaviour
{
    [Serializable]
    public class Expression
    {
        public string name = "Gembira";
        public GameObject model;
        public float bobAmount = 0.04f;
        public float bobSpeed = 3.5f;
        public float shake = 0f;
        public float shakeSpeed = 40f;
    }

    [Header("Referensi")]
    [SerializeField] private Transform forgyRoot;
    [SerializeField] private Transform visual;
    [SerializeField] private Camera cam;

    [Header("Ekspresi")]
    [SerializeField] private Expression[] expressions;

   [Header("Penempatan & Ukuran")]
    [SerializeField] private float depth = 5.5f;
    [SerializeField] private Vector2 rightViewport = new Vector2(0.75f, 0.04f);
    [SerializeField] private Vector2 leftViewport = new Vector2(0.25f, 0.04f);

    [Header("Animasi Terbang Masuk (Fly-In)")]
    [SerializeField] private float flyDuration = 1.0f;
    [SerializeField] private float flyStartViewportY = 1.25f;
    [SerializeField] private float flyArcHeight = 0.45f;
    [SerializeField] private float flyBankAngle = 26f;

    [Header("Terbang Keliling Atas-Bawah-Kiri-Kanan Sebelum Foto")]
    [SerializeField] private float roamSpeed = 2.2f;
    [SerializeField] private float roamPauseMin = 0.35f;
    [SerializeField] private float roamPauseMax = 0.85f;

    [Header("Gerak & Pose Siap Foto")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float turnSpeed = 6f;
    [SerializeField] private float baseYaw = 180f;
    [SerializeField] private float sideYaw = 22f;

    [Header("Animasi Pose Saat Berfoto")]
    [SerializeField] private float poseHeadTiltAngle = 8f;
    [SerializeField] private float poseHeadTiltSpeed = 2.6f;
    [SerializeField] private float poseBounceAmount = 0.045f;
    [SerializeField] private float poseBounceSpeed = 4.2f;
    [SerializeField] private float snapPopScale = 1.15f;

    private Vector2 targetViewport;
    private Vector2 currentViewport;
    private float targetYaw;
    private bool visible;
    private bool isFlyingIn;
    private bool isRoaming;
    private int current;
    private int selectedExpressionIndex = -1;
    private Coroutine flyRoutine;
    private Coroutine roamRoutine;

    private void Awake()
    {
        AutoFindReferences();
        SetSide(true);
        currentViewport = targetViewport;
        if (forgyRoot != null) forgyRoot.gameObject.SetActive(false);
    }

    private void AutoFindReferences()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();

        if (forgyRoot == null)
        {
            GameObject rootObj = GameObject.Find("ForgyRoot");
            if (rootObj != null) forgyRoot = rootObj.transform;
        }

        if (forgyRoot == null)
        {
            foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (r.transform.root.name.IndexOf("Forgy", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    forgyRoot = r.transform.root;
                    break;
                }
            }
        }

        if (visual == null && forgyRoot != null)
        {
            Transform v = forgyRoot.Find("Visual");
            visual = (v != null) ? v : forgyRoot;
        }

        bool needAutoExpr = (expressions == null || expressions.Length == 0);
        if (!needAutoExpr)
        {
            for (int i = 0; i < expressions.Length; i++)
                if (expressions[i].model == null) needAutoExpr = true;
        }

        if (needAutoExpr && visual != null && visual.childCount > 0)
        {
            List<Expression> autoList = new List<Expression>();
            for (int i = 0; i < visual.childCount; i++)
            {
                GameObject childObj = visual.GetChild(i).gameObject;
                autoList.Add(new Expression
                {
                    name = childObj.name,
                    model = childObj,
                    bobAmount = 0.04f,
                    bobSpeed = 3.5f
                });
            }
            expressions = autoList.ToArray();
        }
    }

    public void SelectExpressionPending(int index)
    {
        selectedExpressionIndex = index;
        Hide();
    }

    public void SetExpression(int index)
    {
        selectedExpressionIndex = index;
        if (index < 0)
        {
            Hide();
            return;
        }

        AutoFindReferences();
        if (forgyRoot == null) return;

        if (!gameObject.activeSelf) gameObject.SetActive(true);
        forgyRoot.gameObject.SetActive(true);

        visible = true;
        if (expressions != null && expressions.Length > 0)
        {
            current = Mathf.Clamp(index, 0, expressions.Length - 1);
            for (int i = 0; i < expressions.Length; i++)
            {
                if (expressions[i].model != null)
                {
                    bool active = (i == current);
                    expressions[i].model.SetActive(active);
                    if (active)
                    {
                        Animator anim = expressions[i].model.GetComponentInChildren<Animator>(true);
                        if (anim != null) { anim.Rebind(); anim.Update(0f); }
                    }
                }
            }
        }

        SetSide(true);
        PlayFlyInAndThenRoam();
    }

    public void SpawnOnCameraWithFlyIn()
    {
        if (selectedExpressionIndex < 0)
        {
            Hide();
            return;
        }
        SetExpression(selectedExpressionIndex);
    }

    public void PlayFlyInAnimation()
    {
        if (!visible || forgyRoot == null) return;
        if (flyRoutine != null) StopCoroutine(flyRoutine);
        flyRoutine = StartCoroutine(FlyInRoutine(false));
    }

    private void PlayFlyInAndThenRoam()
    {
        if (!visible || forgyRoot == null) return;
        if (flyRoutine != null) StopCoroutine(flyRoutine);
        if (roamRoutine != null) { StopCoroutine(roamRoutine); roamRoutine = null; }
        flyRoutine = StartCoroutine(FlyInRoutine(true));
    }

    public void StartRoamingAroundScreen()
    {
        if (selectedExpressionIndex < 0)
        {
            Hide();
            return;
        }
        SetExpression(selectedExpressionIndex);
    }

    public void StopRoaming(bool returnToPosePosition = true)
    {
        isRoaming = false;
        if (roamRoutine != null)
        {
            StopCoroutine(roamRoutine);
            roamRoutine = null;
        }
        if (flyRoutine != null)
        {
            StopCoroutine(flyRoutine);
            flyRoutine = null;
            isFlyingIn = false;
        }

        if (visual != null)
        {
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one;
        }

        if (returnToPosePosition)
        {
            SetSide(true);
        }
    }

    public void Hide()
    {
        StopRoaming(false);
        visible = false;
        if (forgyRoot != null) forgyRoot.gameObject.SetActive(false);
    }

    public void MoveLeft()
    {
        StopRoaming(false);
        SetSide(false);
    }

    public void MoveRight()
    {
        StopRoaming(false);
        SetSide(true);
    }

    private void SetSide(bool right)
    {
        targetViewport = right ? rightViewport : leftViewport;
        targetYaw = baseYaw + (right ? sideYaw : -sideYaw);
    }

    private IEnumerator RoamCornersRoutine()
    {
        // Area aman lebih ke bawah agar sejajar wajah & tidak terpotong di atas
        Vector2[] roamPoints = new Vector2[]
        {
            new Vector2(0.74f, 0.34f), // Kanan atas (lebih rendah)
            new Vector2(0.26f, 0.34f), // Kiri atas (lebih rendah)
            new Vector2(0.24f, 0.18f), // Samping kiri tengah
            new Vector2(0.26f, 0.04f), // Kiri bawah
            new Vector2(0.50f, 0.36f), // Atas tengah
            new Vector2(0.76f, 0.18f), // Samping kanan tengah
            new Vector2(0.74f, 0.04f), // Kanan bawah
            new Vector2(0.50f, 0.06f)  // Bawah tengah
        };

        int lastIdx = -1;
        while (isRoaming && visible)
        {
            int nextIdx = UnityEngine.Random.Range(0, roamPoints.Length);
            if (nextIdx == lastIdx)
                nextIdx = (nextIdx + 1) % roamPoints.Length;
            lastIdx = nextIdx;

            targetViewport = roamPoints[nextIdx];
            bool onRight = targetViewport.x >= 0.5f;
            targetYaw = baseYaw + (onRight ? sideYaw : -sideYaw);

            while (isRoaming && Vector2.Distance(currentViewport, targetViewport) > 0.04f)
            {
                yield return null;
            }

            yield return new WaitForSeconds(UnityEngine.Random.Range(roamPauseMin, roamPauseMax));
        }
    }

    private IEnumerator FlyInRoutine(bool startRoamAfter)
    {
        isFlyingIn = true;
        isRoaming = false;

        float dirSign = (targetViewport.x >= 0.5f) ? 1f : -1f;
        Vector2 startViewport = new Vector2(
            Mathf.Clamp(targetViewport.x + (dirSign * 0.30f), -0.2f, 1.2f),
            flyStartViewportY
        );

        Vector3 startPos = WorldPoint(startViewport);
        forgyRoot.position = startPos;
        if (visual != null) visual.localScale = Vector3.one * 0.25f;

        float elapsed = 0f;
        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / flyDuration);
            float ease = EaseOutBack(u);

            Vector3 endPos = WorldPoint(targetViewport);
            Vector3 pos = Vector3.LerpUnclamped(startPos, endPos, ease);

            float swayX = Mathf.Sin(u * Mathf.PI) * (-dirSign * flyArcHeight);
            pos += cam.transform.right * swayX;
            forgyRoot.position = pos;

            if (visual != null)
            {
                visual.localScale = Vector3.one * Mathf.LerpUnclamped(0.25f, 1f, ease);
                float bankZ = Mathf.Sin(u * Mathf.PI) * flyBankAngle * dirSign;
                float pitchX = Mathf.Sin(u * Mathf.PI) * 16f;
                visual.localRotation = Quaternion.Euler(pitchX, 0f, bankZ);
                visual.localPosition = Vector3.zero;
            }

            float spinYaw = (1f - Mathf.Clamp01(u * 1.25f)) * 360f * dirSign;
            forgyRoot.rotation = Quaternion.Euler(0f, targetYaw + spinYaw, 0f);

            yield return null;
        }

        currentViewport = targetViewport;
        forgyRoot.position = WorldPoint(targetViewport);
        forgyRoot.rotation = Quaternion.Euler(0f, targetYaw, 0f);
        if (visual != null)
        {
            visual.localScale = Vector3.one;
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
        }

        isFlyingIn = false;

        if (startRoamAfter && visible)
        {
            isRoaming = true;
            roamRoutine = StartCoroutine(RoamCornersRoutine());
        }
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.35f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private void Update()
    {
        if (!visible || isFlyingIn || forgyRoot == null) return;

        float spd = isRoaming ? roamSpeed : moveSpeed;
        float tMove = 1f - Mathf.Exp(-spd * Time.deltaTime);
        float tTurn = 1f - Mathf.Exp(-turnSpeed * Time.deltaTime);

        Vector2 prevViewport = currentViewport;
        currentViewport = Vector2.Lerp(currentViewport, targetViewport, tMove);

        forgyRoot.position = WorldPoint(currentViewport);
        forgyRoot.rotation = Quaternion.Slerp(
            forgyRoot.rotation,
            Quaternion.Euler(0f, targetYaw, 0f),
            tTurn
        );

        if (visual != null && expressions != null && expressions.Length > 0)
        {
            Expression e = expressions[current];
            visual.localPosition = Vector3.up * Mathf.Sin(Time.time * e.bobSpeed) * e.bobAmount;

            if (isRoaming)
            {
                float vx = (currentViewport.x - prevViewport.x) / Mathf.Max(Time.deltaTime, 0.0001f);
                float bankZ = Mathf.Clamp(-vx * 18f, -20f, 20f);
                visual.localRotation = Quaternion.Slerp(
                    visual.localRotation,
                    Quaternion.Euler(10f, 0f, bankZ),
                    tTurn
                );
            }
            else
            {
                // Animasi pose hidup saat pengguna sedang berfoto:
                // 1. Memantul ceria (bounce)
                float bounceY = Mathf.Abs(Mathf.Sin(Time.time * poseBounceSpeed)) * poseBounceAmount;
                visual.localPosition = Vector3.up * bounceY;

                // 2. Miringkan badan/kepala dengan gemas + getaran ekspresi (jika marah)
                float tiltZ = Mathf.Sin(Time.time * poseHeadTiltSpeed) * poseHeadTiltAngle;
                float shakeZ = Mathf.Sin(Time.time * e.shakeSpeed) * e.shake;
                visual.localRotation = Quaternion.Slerp(
                    visual.localRotation,
                    Quaternion.Euler(0f, 0f, tiltZ + shakeZ),
                    tTurn
                );

                // 3. Kembalikan skala mulus setelah efek "pop" saat jepret
                visual.localScale = Vector3.Lerp(visual.localScale, Vector3.one, tTurn);
            }
        }
    }

    private Vector3 WorldPoint(Vector2 viewport)
    {
        if (cam == null) cam = Camera.main;
        return cam.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, depth));
    }
}