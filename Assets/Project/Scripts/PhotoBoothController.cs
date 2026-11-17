using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PhotoBoothController : MonoBehaviour
{
    public enum State { Idle, ChooseExpression, ChooseLayout, Live, Countdown, Preview }

    [Serializable]
    public class SlotTransformData
    {
        public int photoIndex = -1;
        public Vector2 offset = Vector2.zero;
        public float zoom = 1f;
        public float rotation = 0f;
        public bool flipX = false;
    }

    [Header("Referensi Utama & Forgy Asli")]
    [SerializeField] private ForgyController forgy;
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private GameObject expressionPanel;
    [SerializeField] private GameObject layoutChoosePanel;
    [SerializeField] private GameObject controlsBar;
    [SerializeField] private GameObject previewPanel;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text toastText;
    [SerializeField] private Image flashImage;
    [SerializeField] private int countdownSeconds = 3;
    [SerializeField] private string folderName = "ForgyPhotoBooth";

    [Header("Referensi Preview Layout Side-by-Side")]
    [SerializeField] private RectTransform frameCardContainer;
    [SerializeField] private Transform photoGridContainer;
    [SerializeField] private Image mainFrameOverlayImage;
    [SerializeField] private Transform frameSelectorBarContainer;

    [Header("Referensi Studio Editor (Dihubungkan UIBuilder)")]
    public Transform photoGalleryContainer;
    public RawImage editPreviewRawImage;
    public GameObject editPreviewBoxObj;
    public TMP_Text editSlotStatusLabel;
    public TMP_Text zoomInfoLabel;
    public TMP_Text rotateInfoLabel;

    public Button moveUpButton;
    public Button moveDownButton;
    public Button moveLeftButton;
    public Button moveRightButton;
    public Button zoomInButton;
    public Button zoomOutButton;
    public Button rotateLeftButton;
    public Button rotateRightButton;

    public Button flipButton;
    public Button clearSlotButton;
    public Button resetTransformButton;
    public Button openSavedFolderButton;

    [Header("Pengaturan Boomerang & Multi-Shot")]
    [SerializeField] private int boomerangFrames = 20;
    [SerializeField] private float frameDelay = 0.05f;

    private State state;
    private Texture2D lastShot;
    private List<Texture2D> multiShotsList = new List<Texture2D>();
    private List<Texture2D> boomerangList = new List<Texture2D>();
    private Coroutine boomerangPlayRoutine;

    private readonly List<SlotTransformData> slotDataList = new List<SlotTransformData>();
    private readonly List<RawImage> slotRawImages = new List<RawImage>();
    private readonly List<GameObject> slotPlaceholders = new List<GameObject>();
    private readonly List<Outline> slotOutlines = new List<Outline>();
    private readonly Dictionary<Sprite, List<Rect>> detectedHolesCache = new Dictionary<Sprite, List<Rect>>();

    private int targetPhotoCount = 1;
    private int currentPhotoIndex = 0;
    private int currentSelectedSlotIndex = 0;
    private string saveFolder;
    private string lastSavedFilePath;
    private List<GameObject> activeFrameButtons = new List<GameObject>();
    private Sprite currentSelectedFrameSprite;
    private bool isEventsBound = false;
    private bool isSavingComposite = false;

    private void Start()
    {
        if (uiCanvas == null)
        {
            GameObject cObj = GameObject.Find("UICanvas");
            if (cObj != null) uiCanvas = cObj.GetComponent<Canvas>();
            if (uiCanvas == null) uiCanvas = FindAnyObjectByType<Canvas>();
        }

        // Pastikan UIBuilder sudah membangun & menghubungkan UI baru (gambar kedua)
        PhotoBoothUIBuilder uiBuilder = FindAnyObjectByType<PhotoBoothUIBuilder>(FindObjectsInactive.Include);
        if (uiBuilder != null)
        {
            uiBuilder.BuildAllUI();
        }

        if (forgy == null)
        {
            forgy = FindAnyObjectByType<ForgyController>(FindObjectsInactive.Include);
        }

        saveFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            folderName);
        Directory.CreateDirectory(saveFolder);

        BindStudioEditorEvents();

        if (toastText != null) toastText.text = "";
        SetState(State.Idle);
    }

    public void BindStudioEditorEvents()
    {
        if (moveUpButton != null) { moveUpButton.onClick.RemoveAllListeners(); moveUpButton.onClick.AddListener(() => NudgeSlotOffset(new Vector2(0f, 12f))); }
        if (moveDownButton != null) { moveDownButton.onClick.RemoveAllListeners(); moveDownButton.onClick.AddListener(() => NudgeSlotOffset(new Vector2(0f, -12f))); }
        if (moveLeftButton != null) { moveLeftButton.onClick.RemoveAllListeners(); moveLeftButton.onClick.AddListener(() => NudgeSlotOffset(new Vector2(-12f, 0f))); }
        if (moveRightButton != null) { moveRightButton.onClick.RemoveAllListeners(); moveRightButton.onClick.AddListener(() => NudgeSlotOffset(new Vector2(12f, 0f))); }

        if (zoomInButton != null) { zoomInButton.onClick.RemoveAllListeners(); zoomInButton.onClick.AddListener(() => AdjustSlotZoom(0.15f)); }
        if (zoomOutButton != null) { zoomOutButton.onClick.RemoveAllListeners(); zoomOutButton.onClick.AddListener(() => AdjustSlotZoom(-0.15f)); }

        if (rotateLeftButton != null) { rotateLeftButton.onClick.RemoveAllListeners(); rotateLeftButton.onClick.AddListener(() => AdjustSlotRotation(15f)); }
        if (rotateRightButton != null) { rotateRightButton.onClick.RemoveAllListeners(); rotateRightButton.onClick.AddListener(() => AdjustSlotRotation(-15f)); }

        if (flipButton != null) { flipButton.onClick.RemoveAllListeners(); flipButton.onClick.AddListener(ToggleSlotFlip); }
        if (clearSlotButton != null) { clearSlotButton.onClick.RemoveAllListeners(); clearSlotButton.onClick.AddListener(ClearCurrentSlot); }
        if (resetTransformButton != null) { resetTransformButton.onClick.RemoveAllListeners(); resetTransformButton.onClick.AddListener(ResetCurrentSlotTransform); }

        if (editPreviewBoxObj != null)
        {
            EventTrigger trigger = editPreviewBoxObj.GetComponent<EventTrigger>();
            if (trigger == null) trigger = editPreviewBoxObj.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            EventTrigger.Entry dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener((data) =>
            {
                PointerEventData ped = (PointerEventData)data;
                NudgeSlotOffset(ped.delta * 0.65f);
            });
            trigger.triggers.Add(dragEntry);
        }

        isEventsBound = true;
    }

    public void SetState(State s)
    {
        state = s;

        if (hintPanel != null) hintPanel.SetActive(s == State.Idle);
        if (expressionPanel != null) expressionPanel.SetActive(s == State.ChooseExpression);
        if (layoutChoosePanel != null) layoutChoosePanel.SetActive(s == State.ChooseLayout);
        if (controlsBar != null) controlsBar.SetActive(s == State.Live);
        if (previewPanel != null) previewPanel.SetActive(s == State.Preview);

        // Sembunyikan Forgy 3D jika bukan di layar kamera (Live / Countdown)
        if (forgy != null && s != State.Live && s != State.Countdown)
        {
            forgy.Hide();
        }

        if (s != State.Countdown && countdownText != null) countdownText.text = "";

        if (s != State.Preview && boomerangPlayRoutine != null)
        {
            StopCoroutine(boomerangPlayRoutine);
            boomerangPlayRoutine = null;
        }
    }

    private void Update()
    {
        switch (state)
        {
            case State.Idle:
                if (Input.GetKeyDown(KeyCode.Space)) OpenChooseExpression();
                break;
            case State.Live:
                if (Input.GetKeyDown(KeyCode.Space)) StartPhotoCaptureProcess(targetPhotoCount);
                break;
            case State.Preview:
                if (Input.GetKeyDown(KeyCode.Return)) Save();
                if (Input.GetKeyDown(KeyCode.R)) Retake();
                break;
        }
    }

    public void OpenChooseExpression() { SetState(State.ChooseExpression); }

    public void ChooseExpression(int index)
    {
        if (forgy == null)
            forgy = FindAnyObjectByType<ForgyController>(FindObjectsInactive.Include);

        if (forgy != null)
        {
            // Simpan pilihan dulu & sembunyikan saat masih di halaman Pilih Tata Letak
            forgy.SelectExpressionPending(index);
        }
        SetState(State.ChooseLayout);
    }

    public void EnterLiveCameraReady(int count)
    {
        targetPhotoCount = Mathf.Max(1, count);
        currentPhotoIndex = 0;
        ClearMultiShots();

        SetState(State.Live);

        if (forgy == null)
            forgy = FindAnyObjectByType<ForgyController>(FindObjectsInactive.Include);

        if (forgy != null)
        {
            // Forgy terbang masuk saat halaman kamera muncul!
            forgy.SpawnOnCameraWithFlyIn();
        }
    }

    public void StartPhotoCaptureProcess(int count)
    {
        targetPhotoCount = Mathf.Max(1, count);
        currentPhotoIndex = 0;
        ClearMultiShots();

        if (forgy != null)
        {
            forgy.StopRoaming(true);
        }

        StartCoroutine(MultiShotCaptureRoutine());
    }

    public void TakePhoto() => StartPhotoCaptureProcess(targetPhotoCount);

    public void TakeBoomerang()
    {
        if (forgy != null)
            forgy.StopRoaming(true);

        SetState(State.Live);
        StartCoroutine(BoomerangRoutine());
    }

    public void ChangeFrameSprite(Sprite frameSprite, GameObject selectedButton)
    {
        currentSelectedFrameSprite = frameSprite;
        if (mainFrameOverlayImage != null)
        {
            mainFrameOverlayImage.sprite = frameSprite;
            mainFrameOverlayImage.enabled = (frameSprite != null);
            mainFrameOverlayImage.color = (frameSprite != null) ? Color.white : Color.clear;
            mainFrameOverlayImage.preserveAspect = false;
            mainFrameOverlayImage.transform.SetAsLastSibling();
        }

        AdjustPhotoGridForCurrentFrame();

        foreach (var btn in activeFrameButtons)
        {
            if (btn == null) continue;
            bool isSelected = (btn == selectedButton);
            btn.transform.localScale = isSelected ? Vector3.one * 1.12f : Vector3.one * 0.96f;

            Outline outline = btn.GetComponent<Outline>();
            if (outline != null) outline.enabled = isSelected;
        }
    }

    public void Retake()
    {
        ClearBoomerangFrames();
        ClearMultiShots();
        if (lastShot != null) Destroy(lastShot);
        if (openSavedFolderButton != null) openSavedFolderButton.gameObject.SetActive(false);
        SetState(State.ChooseLayout);
    }

    public void Save()
    {
        if (isSavingComposite) return;
        if (lastShot == null && boomerangList.Count == 0 && multiShotsList.Count == 0) return;

        StartCoroutine(SaveFramedResultRoutine());
    }

    private IEnumerator SaveFramedResultRoutine()
    {
        isSavingComposite = true;
        Directory.CreateDirectory(saveFolder);

        if (boomerangList.Count > 0)
        {
            string gifFile = $"Boomerang_{DateTime.Now:yyyyMMdd_HHmmss}.gif";
            string gifPath = Path.Combine(saveFolder, gifFile);
            SaveSingleGifFile(gifPath);
            lastSavedFilePath = gifPath;
            ClearBoomerangFrames();

            if (openSavedFolderButton != null)
                openSavedFolderButton.gameObject.SetActive(true);

            isSavingComposite = false;
            StartCoroutine(Toast("Boomerang Berhasil Tersimpan! ✨"));
            yield break;
        }

        for (int i = 0; i < slotOutlines.Count; i++)
        {
            if (slotOutlines[i] != null) slotOutlines[i].enabled = false;
        }

        if (mainFrameOverlayImage != null)
            mainFrameOverlayImage.transform.SetAsLastSibling();

        yield return new WaitForEndOfFrame();

        if (frameCardContainer != null)
        {
            Vector3[] corners = new Vector3[4];
            frameCardContainer.GetWorldCorners(corners);

            Canvas rootCanvas = frameCardContainer.GetComponentInParent<Canvas>();
            Camera uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? rootCanvas.worldCamera : null;

            Vector2 minScreen = RectTransformUtility.WorldToScreenPoint(uiCam, corners[0]);
            Vector2 maxScreen = RectTransformUtility.WorldToScreenPoint(uiCam, corners[2]);

            int x = Mathf.Clamp(Mathf.RoundToInt(minScreen.x), 0, Screen.width - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(minScreen.y), 0, Screen.height - 1);
            int w = Mathf.Clamp(Mathf.RoundToInt(maxScreen.x - minScreen.x), 1, Screen.width - x);
            int h = Mathf.Clamp(Mathf.RoundToInt(maxScreen.y - minScreen.y), 1, Screen.height - y);

            Texture2D compositeTex = new Texture2D(w, h, TextureFormat.RGB24, false);
            compositeTex.ReadPixels(new Rect(x, y, w, h), 0, 0);
            compositeTex.Apply();

            string file = $"ForgyBooth_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string path = Path.Combine(saveFolder, file);
            File.WriteAllBytes(path, compositeTex.EncodeToPNG());
            lastSavedFilePath = path;

            Destroy(compositeTex);
            Debug.Log($"[ForgyBooth] Foto berbingkai tersimpan di: {path}");
        }

        SelectSlot(currentSelectedSlotIndex);

        if (openSavedFolderButton != null)
            openSavedFolderButton.gameObject.SetActive(true);

        isSavingComposite = false;
        StartCoroutine(Toast("Foto & Bingkai Tersimpan! ✨"));
    }

    public void OpenLastSavedFolder()
    {
        if (!string.IsNullOrEmpty(saveFolder) && Directory.Exists(saveFolder))
        {
            Application.OpenURL("file://" + saveFolder);
        }
    }

    private IEnumerator MultiShotCaptureRoutine()
    {
        int shotsToTake = 6;

        while (currentPhotoIndex < shotsToTake)
        {
            currentPhotoIndex++;
            SetState(State.Countdown);

            for (int s = countdownSeconds; s > 0; s--)
            {
                if (countdownText != null) countdownText.text = s.ToString();
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime;
                    float k = Mathf.Clamp01(t * 4f);
                    if (countdownText != null)
                        countdownText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, k);
                    yield return null;
                }
            }

            if (countdownText != null) countdownText.text = "";
            if (uiCanvas != null) uiCanvas.enabled = false;
            yield return new WaitForEndOfFrame();

            Texture2D shot = ReadScreenToTexture();
            multiShotsList.Add(shot);
            lastShot = shot;

            if (uiCanvas != null) uiCanvas.enabled = true;
            yield return StartCoroutine(Flash());

            if (currentPhotoIndex < shotsToTake)
            {
                yield return new WaitForSeconds(0.55f);
            }
        }

        RenderPreviewGrid();
        LoadAndBuildFrameOptions(targetPhotoCount);
        BuildRightGalleryThumbnails();
        SetState(State.Preview);
    }

    private void RenderPreviewGrid()
    {
        if (photoGridContainer == null) return;
        foreach (Transform child in photoGridContainer) Destroy(child.gameObject);

        GridLayoutGroup existingGrid = photoGridContainer.GetComponent<GridLayoutGroup>();
        if (existingGrid != null) existingGrid.enabled = false;

        slotDataList.Clear();
        slotRawImages.Clear();
        slotPlaceholders.Clear();
        slotOutlines.Clear();

        photoGridContainer.SetAsFirstSibling();
        if (mainFrameOverlayImage != null) mainFrameOverlayImage.transform.SetAsLastSibling();

        for (int i = 0; i < targetPhotoCount; i++)
        {
            int slotIdx = i;
            slotDataList.Add(new SlotTransformData());

            GameObject slotObj = new GameObject($"PhotoSlot_{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline), typeof(RectMask2D));
            slotObj.transform.SetParent(photoGridContainer, false);
            slotObj.GetComponent<Image>().color = new Color32(223, 238, 249, 255);

            Outline outl = slotObj.GetComponent<Outline>();
            outl.effectColor = new Color32(97, 157, 198, 255);
            outl.effectDistance = new Vector2(3, -3);
            outl.enabled = (i == 0);
            slotOutlines.Add(outl);

            GameObject rawObj = new GameObject("SlotPhotoRaw", typeof(RectTransform), typeof(RawImage));
            rawObj.transform.SetParent(slotObj.transform, false);
            RectTransform rawRt = rawObj.GetComponent<RectTransform>();
            rawRt.anchorMin = new Vector2(0.5f, 0.5f);
            rawRt.anchorMax = new Vector2(0.5f, 0.5f);
            rawRt.pivot = new Vector2(0.5f, 0.5f);

            RawImage rawImg = rawObj.GetComponent<RawImage>();
            rawImg.raycastTarget = false;
            slotRawImages.Add(rawImg);

            GameObject phObj = new GameObject("PlaceholderText", typeof(RectTransform), typeof(TextMeshProUGUI));
            phObj.transform.SetParent(slotObj.transform, false);
            RectTransform phRt = phObj.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = Vector2.zero;
            phRt.offsetMax = Vector2.zero;

            TextMeshProUGUI phTmp = phObj.GetComponent<TextMeshProUGUI>();
            phTmp.text = $"+ SLOT {i + 1}";
            phTmp.fontSize = 15f;
            phTmp.color = new Color32(68, 96, 132, 255);
            phTmp.alignment = TextAlignmentOptions.Center;
            phTmp.raycastTarget = false;
            phObj.SetActive(false);
            slotPlaceholders.Add(phObj);

            slotObj.GetComponent<Button>().onClick.AddListener(() => SelectSlot(slotIdx));

            if (i < multiShotsList.Count)
            {
                slotDataList[i].photoIndex = i;
            }
        }

        AdjustPhotoGridForCurrentFrame();
        for (int i = 0; i < targetPhotoCount; i++) RefreshSlotVisual(i);
        SelectSlot(0);
    }

    private List<Rect> DetectTransparentHoles(Sprite sprite, int expectedCount)
    {
        if (sprite == null || expectedCount <= 0) return null;
        if (detectedHolesCache.TryGetValue(sprite, out var cached) && cached.Count == expectedCount)
            return cached;

        Texture2D tex = sprite.texture;
        if (tex == null || !tex.isReadable) return null;

        Rect r = sprite.textureRect;
        int rx = Mathf.RoundToInt(r.x);
        int ry = Mathf.RoundToInt(r.y);
        int rw = Mathf.RoundToInt(r.width);
        int rh = Mathf.RoundToInt(r.height);
        if (rw <= 8 || rh <= 8) return null;

        int step = Mathf.Max(1, Mathf.Min(rw, rh) / 260);
        int gw = rw / step;
        int gh = rh / step;

        Color32[] pixels = tex.GetPixels32();
        int texW = tex.width;

        bool[] isHole = new bool[gw * gh];
        bool[] visited = new bool[gw * gh];

        for (int gy = 0; gy < gh; gy++)
        {
            int py = ry + gy * step;
            for (int gx = 0; gx < gw; gx++)
            {
                int px = rx + gx * step;
                byte a = pixels[py * texW + px].a;
                isHole[gy * gw + gx] = (a < 55);
            }
        }

        List<(Rect rect, int area)> candidates = new List<(Rect, int)>();
        Queue<int> queue = new Queue<int>();
        int minArea = Mathf.Max(20, (gw * gh) / 60);

        for (int gy = 1; gy < gh - 1; gy++)
        {
            for (int gx = 1; gx < gw - 1; gx++)
            {
                int idx = gy * gw + gx;
                if (!isHole[idx] || visited[idx]) continue;

                visited[idx] = true;
                queue.Clear();
                queue.Enqueue(idx);

                int minX = gx, maxX = gx, minY = gy, maxY = gy;
                int area = 0;
                bool touchesBorder = false;

                while (queue.Count > 0)
                {
                    int cur = queue.Dequeue();
                    int cx = cur % gw;
                    int cy = cur / gw;
                    area++;

                    if (cx <= 0 || cx >= gw - 1 || cy <= 0 || cy >= gh - 1)
                        touchesBorder = true;

                    if (cx < minX) minX = cx;
                    if (cx > maxX) maxX = cx;
                    if (cy < minY) minY = cy;
                    if (cy > maxY) maxY = cy;

                    int n1 = cur - 1;
                    if (cx > 0 && isHole[n1] && !visited[n1]) { visited[n1] = true; queue.Enqueue(n1); }
                    int n2 = cur + 1;
                    if (cx < gw - 1 && isHole[n2] && !visited[n2]) { visited[n2] = true; queue.Enqueue(n2); }
                    int n3 = cur - gw;
                    if (cy > 0 && isHole[n3] && !visited[n3]) { visited[n3] = true; queue.Enqueue(n3); }
                    int n4 = cur + gw;
                    if (cy < gh - 1 && isHole[n4] && !visited[n4]) { visited[n4] = true; queue.Enqueue(n4); }
                }

                if (touchesBorder || area < minArea) continue;

                float nxMin = (float)minX / gw;
                float nxMax = (float)(maxX + 1) / gw;
                float nyMin = (float)minY / gh;
                float nyMax = (float)(maxY + 1) / gh;

                candidates.Add((Rect.MinMaxRect(nxMin, nyMin, nxMax, nyMax), area));
            }
        }

        if (candidates.Count < expectedCount)
        {
            Debug.LogWarning($"[ForgyBooth] Hanya terdeteksi {candidates.Count} lubang pada '{sprite.name}' (target: {expectedCount}).");
            return null;
        }

        candidates.Sort((a, b) => b.area.CompareTo(a.area));
        List<Rect> holes = new List<Rect>(expectedCount);
        for (int i = 0; i < expectedCount; i++)
        {
            holes.Add(candidates[i].rect);
        }

        holes.Sort((a, b) =>
        {
            float rowTolerance = 0.08f;
            if (Mathf.Abs(a.center.y - b.center.y) > rowTolerance)
                return b.center.y.CompareTo(a.center.y);
            return a.center.x.CompareTo(b.center.x);
        });

        detectedHolesCache[sprite] = holes;
        return holes;
    }

    private void AdjustPhotoGridForCurrentFrame()
    {
        if (photoGridContainer == null || frameCardContainer == null) return;

        if (currentSelectedFrameSprite != null && currentSelectedFrameSprite.rect.height > 0f)
        {
            float aspect = currentSelectedFrameSprite.rect.width / currentSelectedFrameSprite.rect.height;
            float maxH = (targetPhotoCount == 3) ? 595f : 540f;
            float maxW = (targetPhotoCount == 3) ? 260f : 450f;

            float targetH = maxH;
            float targetW = targetH * aspect;
            if (targetW > maxW)
            {
                targetW = maxW;
                targetH = targetW / aspect;
            }
            frameCardContainer.sizeDelta = new Vector2(targetW, targetH);
        }
        else
        {
            if (targetPhotoCount == 3) frameCardContainer.sizeDelta = new Vector2(215f, 585f);
            else if (targetPhotoCount == 4) frameCardContainer.sizeDelta = new Vector2(390f, 530f);
            else frameCardContainer.sizeDelta = new Vector2(390f, 515f);
        }

        float fw = frameCardContainer.sizeDelta.x;
        float fh = frameCardContainer.sizeDelta.y;
        int count = targetPhotoCount;
        int totalChildren = photoGridContainer.childCount;
        int startIdx = Mathf.Max(0, totalChildren - count);

        List<Rect> holes = DetectTransparentHoles(currentSelectedFrameSprite, count);

        for (int i = 0; i < count; i++)
        {
            int childIdx = startIdx + i;
            if (childIdx >= totalChildren) break;

            RectTransform rt = photoGridContainer.GetChild(childIdx) as RectTransform;
            if (rt == null) continue;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            if (holes != null && i < holes.Count)
            {
                Rect h = holes[i];
                float bleed = 4f;
                float slotW = (h.width * fw) + bleed;
                float slotH = (h.height * fh) + bleed;
                float posX = (h.center.x - 0.5f) * fw;
                float posY = (h.center.y - 0.5f) * fh;

                rt.sizeDelta = new Vector2(slotW, slotH);
                rt.anchoredPosition = new Vector2(posX, posY);
            }
            else
            {
                if (count == 3)
                {
                    float slotW = fw * 0.85f;
                    float slotH = fh * 0.236f;
                    float startY = fh * 0.258f;
                    float stepY = fh * 0.254f;
                    rt.sizeDelta = new Vector2(slotW, slotH);
                    rt.anchoredPosition = new Vector2(0f, startY - (i * stepY));
                }
                else if (count == 4)
                {
                    float slotW = fw * 0.43f;
                    float slotH = fh * 0.39f;
                    int col = i % 2;
                    int row = i / 2;
                    float posX = (col == 0 ? -1f : 1f) * (fw * 0.228f);
                    float posY = (row == 0 ? 1f : -1f) * (fh * 0.205f) + (fh * 0.035f);
                    rt.sizeDelta = new Vector2(slotW, slotH);
                    rt.anchoredPosition = new Vector2(posX, posY);
                }
                else
                {
                    rt.sizeDelta = new Vector2(fw * 0.86f, fh * 0.78f);
                    rt.anchoredPosition = new Vector2(0f, fh * 0.035f);
                }
            }
        }

        for (int i = 0; i < slotDataList.Count; i++)
        {
            RefreshSlotVisual(i);
        }
    }

    private void BuildRightGalleryThumbnails()
    {
        if (photoGalleryContainer == null) return;
        foreach (Transform child in photoGalleryContainer) Destroy(child.gameObject);

        for (int i = 0; i < multiShotsList.Count; i++)
        {
            int photoIdx = i;
            GameObject cardObj = new GameObject($"GalleryPhoto_{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            cardObj.transform.SetParent(photoGalleryContainer, false);
            cardObj.GetComponent<Image>().color = Color.white;

            Outline outl = cardObj.GetComponent<Outline>();
            outl.effectColor = new Color32(27, 54, 93, 255);
            outl.effectDistance = new Vector2(2, -2);

            GameObject rawObj = new GameObject("ThumbRaw", typeof(RectTransform), typeof(RawImage));
            rawObj.transform.SetParent(cardObj.transform, false);
            RectTransform rawRt = rawObj.GetComponent<RectTransform>();
            rawRt.anchorMin = Vector2.zero;
            rawRt.anchorMax = Vector2.one;
            rawRt.offsetMin = new Vector2(6, 6);
            rawRt.offsetMax = new Vector2(-6, -6);

            RawImage raw = rawObj.GetComponent<RawImage>();
            raw.texture = multiShotsList[i];
            raw.raycastTarget = false;

            GameObject badgeObj = new GameObject("NumBadge", typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(cardObj.transform, false);
            RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0f, 1f);
            badgeRt.anchorMax = new Vector2(0f, 1f);
            badgeRt.pivot = new Vector2(0f, 1f);
            badgeRt.anchoredPosition = new Vector2(10, -10);
            badgeRt.sizeDelta = new Vector2(28, 28);
            badgeObj.GetComponent<Image>().color = new Color32(27, 54, 93, 220);

            GameObject badgeTxtObj = new GameObject("NumText", typeof(RectTransform), typeof(TextMeshProUGUI));
            badgeTxtObj.transform.SetParent(badgeObj.transform, false);
            RectTransform btRt = badgeTxtObj.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.offsetMin = Vector2.zero;
            btRt.offsetMax = Vector2.zero;
            TextMeshProUGUI bt = badgeTxtObj.GetComponent<TextMeshProUGUI>();
            bt.text = (i + 1).ToString();
            bt.fontSize = 14f;
            bt.color = Color.white;
            bt.alignment = TextAlignmentOptions.Center;

            cardObj.GetComponent<Button>().onClick.AddListener(() => AssignPhotoToCurrentSlot(photoIdx));
        }
    }

    public void SelectSlot(int slotIndex)
    {
        if (slotDataList.Count == 0) return;
        currentSelectedSlotIndex = Mathf.Clamp(slotIndex, 0, slotDataList.Count - 1);

        for (int i = 0; i < slotOutlines.Count; i++)
        {
            if (slotOutlines[i] != null) slotOutlines[i].enabled = (i == currentSelectedSlotIndex);
        }
        UpdateLeftEditorPanel();
    }

    public void AssignPhotoToCurrentSlot(int photoIndex)
    {
        if (slotDataList.Count == 0 || multiShotsList.Count == 0) return;
        int safeSlot = Mathf.Clamp(currentSelectedSlotIndex, 0, slotDataList.Count - 1);
        int safePhoto = Mathf.Clamp(photoIndex, 0, multiShotsList.Count - 1);

        slotDataList[safeSlot].photoIndex = safePhoto;
        slotDataList[safeSlot].offset = Vector2.zero;
        slotDataList[safeSlot].zoom = 1f;
        slotDataList[safeSlot].rotation = 0f;
        slotDataList[safeSlot].flipX = false;

        RefreshSlotVisual(safeSlot);
        UpdateLeftEditorPanel();
    }

    private void NudgeSlotOffset(Vector2 delta)
    {
        if (!IsCurrentSlotValidAndFilled()) return;
        slotDataList[currentSelectedSlotIndex].offset += delta;
        RefreshSlotVisual(currentSelectedSlotIndex);
        UpdateLeftEditorPanel();
    }

    private void AdjustSlotZoom(float deltaZoom)
    {
        if (!IsCurrentSlotValidAndFilled()) return;
        SlotTransformData d = slotDataList[currentSelectedSlotIndex];
        d.zoom = Mathf.Clamp(d.zoom + deltaZoom, 0.5f, 3.0f);
        RefreshSlotVisual(currentSelectedSlotIndex);
        UpdateLeftEditorPanel();
    }

    private void AdjustSlotRotation(float deltaAngle)
    {
        if (!IsCurrentSlotValidAndFilled()) return;
        SlotTransformData d = slotDataList[currentSelectedSlotIndex];
        d.rotation = (d.rotation + deltaAngle) % 360f;
        RefreshSlotVisual(currentSelectedSlotIndex);
        UpdateLeftEditorPanel();
    }

    private void ToggleSlotFlip()
    {
        if (!IsCurrentSlotValidAndFilled()) return;
        slotDataList[currentSelectedSlotIndex].flipX = !slotDataList[currentSelectedSlotIndex].flipX;
        RefreshSlotVisual(currentSelectedSlotIndex);
        UpdateLeftEditorPanel();
    }

    private void ClearCurrentSlot()
    {
        if (slotDataList.Count == 0) return;
        int safeSlot = Mathf.Clamp(currentSelectedSlotIndex, 0, slotDataList.Count - 1);
        slotDataList[safeSlot].photoIndex = -1;
        RefreshSlotVisual(safeSlot);
        UpdateLeftEditorPanel();
    }

    private void ResetCurrentSlotTransform()
    {
        if (!IsCurrentSlotValidAndFilled()) return;
        SlotTransformData d = slotDataList[currentSelectedSlotIndex];
        d.offset = Vector2.zero;
        d.zoom = 1f;
        d.rotation = 0f;
        d.flipX = false;
        RefreshSlotVisual(currentSelectedSlotIndex);
        UpdateLeftEditorPanel();
    }

    private bool IsCurrentSlotValidAndFilled()
    {
        if (slotDataList.Count == 0 || currentSelectedSlotIndex < 0 || currentSelectedSlotIndex >= slotDataList.Count) return false;
        int pIdx = slotDataList[currentSelectedSlotIndex].photoIndex;
        return pIdx >= 0 && pIdx < multiShotsList.Count;
    }

    private void RefreshSlotVisual(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotDataList.Count) return;
        SlotTransformData d = slotDataList[slotIndex];
        RawImage raw = slotRawImages[slotIndex];
        GameObject ph = slotPlaceholders[slotIndex];

        if (d.photoIndex >= 0 && d.photoIndex < multiShotsList.Count && multiShotsList[d.photoIndex] != null)
        {
            Texture2D tex = multiShotsList[d.photoIndex];
            raw.texture = tex;
            raw.enabled = true;
            if (ph != null) ph.SetActive(false);

            RectTransform slotRt = raw.transform.parent as RectTransform;
            RectTransform rt = raw.rectTransform;
            if (slotRt != null && tex.height > 0)
            {
                float slotW = slotRt.sizeDelta.x;
                float slotH = slotRt.sizeDelta.y;
                float texAspect = (float)tex.width / tex.height;
                float slotAspect = (slotH > 0f) ? (slotW / slotH) : 1f;

                float coverW, coverH;
                if (texAspect > slotAspect)
                {
                    coverH = slotH * 1.04f;
                    coverW = coverH * texAspect;
                }
                else
                {
                    coverW = slotW * 1.04f;
                    coverH = coverW / texAspect;
                }
                rt.sizeDelta = new Vector2(coverW, coverH);
            }

            rt.anchoredPosition = d.offset;
            rt.localEulerAngles = new Vector3(0f, 0f, d.rotation);
            rt.localScale = new Vector3(d.flipX ? -d.zoom : d.zoom, d.zoom, 1f);
        }
        else
        {
            raw.texture = null;
            raw.enabled = false;
            if (ph != null) ph.SetActive(true);
        }
    }

    private void UpdateLeftEditorPanel()
    {
        if (slotDataList.Count == 0) return;
        int safeSlot = Mathf.Clamp(currentSelectedSlotIndex, 0, slotDataList.Count - 1);
        SlotTransformData d = slotDataList[safeSlot];
        bool hasPhoto = (d.photoIndex >= 0 && d.photoIndex < multiShotsList.Count && multiShotsList[d.photoIndex] != null);

        if (editSlotStatusLabel != null)
            editSlotStatusLabel.text = hasPhoto ? $"SLOT {safeSlot + 1} AKTIF • Foto #{d.photoIndex + 1}" : $"SLOT {safeSlot + 1} MASIH KOSONG";
        if (zoomInfoLabel != null)
            zoomInfoLabel.text = $"{Mathf.RoundToInt(d.zoom * 100f)}%";
        if (rotateInfoLabel != null)
            rotateInfoLabel.text = $"{Mathf.RoundToInt(d.rotation)}°";

        if (editPreviewRawImage != null)
        {
            if (hasPhoto)
            {
                Texture2D tex = multiShotsList[d.photoIndex];
                editPreviewRawImage.texture = tex;
                editPreviewRawImage.enabled = true;
                RectTransform rt = editPreviewRawImage.rectTransform;
                float aspect = (tex.height > 0) ? ((float)tex.width / tex.height) : 1f;
                rt.sizeDelta = (aspect >= 1f) ? new Vector2(184f * aspect, 184f) : new Vector2(248f, 248f / aspect);
                rt.anchoredPosition = d.offset;
                rt.localEulerAngles = new Vector3(0f, 0f, d.rotation);
                rt.localScale = new Vector3(d.flipX ? -d.zoom : d.zoom, d.zoom, 1f);
            }
            else
            {
                editPreviewRawImage.texture = null;
                editPreviewRawImage.enabled = false;
            }
        }
    }

    private void LoadAndBuildFrameOptions(int photoCount)
    {
        if (frameSelectorBarContainer == null) return;

        activeFrameButtons.Clear();
        foreach (Transform child in frameSelectorBarContainer) Destroy(child.gameObject);

        string subFolder = "Frames/1Shot";
        if (photoCount == 3) subFolder = "Frames/3Strip";
        else if (photoCount == 4) subFolder = "Frames/4Grid";

        Sprite[] loadedFrames = Resources.LoadAll<Sprite>(subFolder);

        if (loadedFrames != null && loadedFrames.Length > 0)
        {
            for (int i = 0; i < loadedFrames.Length; i++)
            {
                Sprite frameSprite = loadedFrames[i];

                GameObject btnObj = new GameObject($"FrameCardBtn_{i}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline), typeof(LayoutElement));
                btnObj.transform.SetParent(frameSelectorBarContainer, false);
                activeFrameButtons.Add(btnObj);

                LayoutElement le = btnObj.GetComponent<LayoutElement>();
                le.preferredWidth = (photoCount == 3) ? 38f : 64f;
                le.preferredHeight = 76f;

                Image bgImg = btnObj.GetComponent<Image>();
                bgImg.sprite = frameSprite;
                bgImg.preserveAspect = true;

                Outline outline = btnObj.GetComponent<Outline>();
                outline.effectColor = new Color32(27, 54, 93, 255);
                outline.effectDistance = new Vector2(2, -2);
                outline.enabled = false;

                Button btn = btnObj.GetComponent<Button>();
                btn.onClick.AddListener(() => ChangeFrameSprite(frameSprite, btnObj));

                if (i == 0) ChangeFrameSprite(frameSprite, btnObj);
            }
        }
        else
        {
            ChangeFrameSprite(null, null);
        }
    }

    private IEnumerator BoomerangRoutine()
    {
        SetState(State.Countdown);
        for (int s = countdownSeconds; s > 0; s--)
        {
            if (countdownText != null) countdownText.text = s.ToString();
            yield return new WaitForSeconds(1f);
        }

        if (countdownText != null) countdownText.text = "REC!";
        if (uiCanvas != null) uiCanvas.enabled = false;
        ClearBoomerangFrames();

        for (int i = 0; i < boomerangFrames; i++)
        {
            yield return new WaitForEndOfFrame();
            boomerangList.Add(ReadScreenToTexture());
            yield return new WaitForSeconds(frameDelay);
        }

        if (uiCanvas != null) uiCanvas.enabled = true;
        if (countdownText != null) countdownText.text = "";
        yield return StartCoroutine(Flash());

        RenderPreviewGridForBoomerang();
        LoadAndBuildFrameOptions(1);
        SetState(State.Preview);

        boomerangPlayRoutine = StartCoroutine(PlayBoomerangLoop());
    }

    private void RenderPreviewGridForBoomerang()
    {
        if (photoGridContainer == null) return;
        foreach (Transform child in photoGridContainer) Destroy(child.gameObject);

        GameObject imgObj = new GameObject("Boomerang_Preview", typeof(RectTransform), typeof(RawImage));
        imgObj.transform.SetParent(photoGridContainer, false);
        RectTransform rt = imgObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private IEnumerator PlayBoomerangLoop()
    {
        int index = 0;
        bool forward = true;

        while (state == State.Preview && boomerangList.Count > 0)
        {
            if (photoGridContainer != null && photoGridContainer.childCount > 0)
            {
                RawImage raw = photoGridContainer.GetChild(0).GetComponent<RawImage>();
                if (raw != null) raw.texture = boomerangList[index];
            }

            if (forward)
            {
                index++;
                if (index >= boomerangList.Count)
                {
                    index = boomerangList.Count - 1;
                    forward = false;
                }
            }
            else
            {
                index--;
                if (index < 0)
                {
                    index = 0;
                    forward = true;
                }
            }

            yield return new WaitForSeconds(frameDelay);
        }
    }

    private Texture2D ReadScreenToTexture()
    {
        int w = Screen.width;
        int h = Screen.height;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        return tex;
    }

    private void SaveSingleGifFile(string path)
    {
        List<Texture2D> sequence = new List<Texture2D>(boomerangList);
        for (int i = boomerangList.Count - 2; i > 0; i--) sequence.Add(boomerangList[i]);

        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            ushort w = (ushort)sequence[0].width;
            ushort h = (ushort)sequence[0].height;

            bw.Write(new char[] { 'G', 'I', 'F', '8', '9', 'a' });
            bw.Write(w); bw.Write(h);
            bw.Write((byte)0xF7); bw.Write((byte)0); bw.Write((byte)0);

            for (int i = 0; i < 256; i++)
            {
                bw.Write((byte)i); bw.Write((byte)i); bw.Write((byte)i);
            }

            bw.Write(new byte[] { 0x21, 0xFF, 0x0B });
            bw.Write(new char[] { 'N', 'E', 'T', 'S', 'C', 'A', 'P', 'E', '2', '.', '0' });
            bw.Write(new byte[] { 0x03, 0x01, 0x00, 0x00, 0x00 });

            ushort delay = (ushort)(frameDelay * 100);

            foreach (var tex in sequence)
            {
                bw.Write(new byte[] { 0x21, 0xF9, 0x04, 0x04 });
                bw.Write(delay); bw.Write((byte)0); bw.Write((byte)0);
                bw.Write((byte)0x2C); bw.Write((ushort)0); bw.Write((ushort)0);
                bw.Write(w); bw.Write(h); bw.Write((byte)0);

                Color32[] pixels = tex.GetPixels32();
                bw.Write((byte)8);

                byte[] rawData = new byte[pixels.Length];
                for (int p = 0; p < pixels.Length; p++)
                {
                    rawData[p] = (byte)((pixels[p].r * 0.3f) + (pixels[p].g * 0.59f) + (pixels[p].b * 0.11f));
                }

                int offset = 0;
                while (offset < rawData.Length)
                {
                    int blockLen = Math.Min(254, rawData.Length - offset);
                    bw.Write((byte)(blockLen + 1));
                    bw.Write((byte)0x80);
                    bw.Write(rawData, offset, blockLen);
                    offset += blockLen;
                }
                bw.Write((byte)1); bw.Write((byte)0x81); bw.Write((byte)0);
            }
            bw.Write((byte)0x3B);
        }
    }

    private void ClearBoomerangFrames()
    {
        foreach (var tex in boomerangList) if (tex != null) Destroy(tex);
        boomerangList.Clear();
    }

    private void ClearMultiShots()
    {
        foreach (var tex in multiShotsList) if (tex != null) Destroy(tex);
        multiShotsList.Clear();
    }

    private IEnumerator Flash()
    {
        if (flashImage == null) yield break;
        flashImage.transform.SetAsLastSibling();
        flashImage.gameObject.SetActive(true);
        flashImage.color = Color.white;
        for (float t = 0f; t < 1f; t += Time.deltaTime * 4f)
        {
            flashImage.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }
        flashImage.gameObject.SetActive(false);
    }

    private IEnumerator Toast(string message)
    {
        if (toastText == null) yield break;
        toastText.text = message;
        yield return new WaitForSeconds(2f);
        toastText.text = "";
    }
}