using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhotoBoothUIBuilder : MonoBehaviour
{
    [Header("Referensi Utama")]
    [SerializeField] private PhotoBoothController controller;
    [SerializeField] private Canvas canvas;

    [Header("Pengaturan Font Kustom")]
    [Tooltip("Seret aset TMP_FontAsset kustom untuk teks umum di sini.")]
    [SerializeField] private TMP_FontAsset customFontAsset;

    [Tooltip("Seret aset TMP_FontAsset 'Junge' khusus logo 'Forgy Booth' di sini.")]
    [SerializeField] private TMP_FontAsset brandLogoFontAsset;

    private readonly Color colBgLightBlue   = new Color32(238, 246, 252, 255); // #EEF6FC
    private readonly Color colGridLine      = new Color32(214, 232, 246, 255); // #D6E8F6
    private readonly Color colSkyBlue       = new Color32(166, 200, 226, 255); // #A6C8E2
    private readonly Color colSteelBlue     = new Color32(97, 157, 198, 255);  // #619DC6
    private readonly Color colDeepNavy      = new Color32(27, 54, 93, 255);    // #1B365D
    private readonly Color colSubTextNavy   = new Color32(68, 96, 132, 255);
    private readonly Color colSoftCardBg    = new Color32(223, 238, 249, 255);

    private GameObject hintPanelGO;
    private GameObject expressionPanelGO;
    private GameObject layoutChoosePanelGO;
    private GameObject livePanelGO;
    private GameObject previewPanelGO;
    private GameObject modePhotoPage;
    private GameObject modeBoomerangPage;

    private void Awake()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        BuildAllUI();
    }

    [ContextMenu("Build UI Now")]
    public void BuildAllUI()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();

        if (brandLogoFontAsset == null)
        {
            brandLogoFontAsset = Resources.Load<TMP_FontAsset>("Junge");
            if (brandLogoFontAsset == null)
                brandLogoFontAsset = Resources.Load<TMP_FontAsset>("Junge SDF");
        }

        for (int i = canvas.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = canvas.transform.GetChild(i);
            if (child.name.StartsWith("UI_Generated_"))
            {
                DestroyImmediate(child.gameObject);
            }
            else if (child.name == "HintPanel" || child.name == "ExpressionPanel" || 
                     child.name == "PreviewPanel" || child.name == "controlsBar" || 
                     child.name == "ControlsBar" || child.name == "CountdownText" || 
                     child.name == "FlashImage")
            {
                child.gameObject.SetActive(false);
            }
        }

        TMP_FontAsset titleFont = (brandLogoFontAsset != null) ? brandLogoFontAsset : customFontAsset;

        // ----------------------------------------------------
        // 1. HALAMAN UTAMA (LANDING PAGE)
        // ----------------------------------------------------
        RectTransform hintPanel = BuatHalamanDasar("UI_Generated_HintPanel", "READY ●");
        hintPanelGO = hintPanel.gameObject;

        RectTransform leftCol = UIKit.NewRect("LeftEditorialColumn", hintPanel);
        UIKit.Place(leftCol, new Vector2(0.29f, 0.47f), Vector2.zero, new Vector2(700, 560));

        TextMeshProUGUI tagLabel = UIKit.Label(leftCol, "(AR PHOTO BOOTH EXPERIENCE)", 20f, colSubTextNavy,
            new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(680, 35), customFontAsset);
        tagLabel.alignment = TextAlignmentOptions.Left;

        TextMeshProUGUI mainTitle = UIKit.Label(leftCol, "Your Cute AR\nPhoto Buddy\nof Dreams", 82f, colDeepNavy,
            new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(680, 280), titleFont);
        mainTitle.alignment = TextAlignmentOptions.Left;
        mainTitle.fontStyle = FontStyles.Normal;
        mainTitle.lineSpacing = -10f;
        mainTitle.raycastTarget = false;

        TextMeshProUGUI descLabel = UIKit.Label(leftCol, "Abadikan momen seru bersama karakter Forgy AR.\nPilih ekspresi favorit, atur bingkai estetik,\ndan cetak kenanganmu seketika.", 22f, colSubTextNavy,
            new Vector2(0.5f, 0.29f), Vector2.zero, new Vector2(680, 95), customFontAsset);
        descLabel.alignment = TextAlignmentOptions.Left;

        Image btnStart = UIKit.CardButton(leftCol, "Btn_StartBooth", new Vector2(0.24f, 0.08f), Vector2.zero, new Vector2(320, 68),
            colSteelBlue, () => BukaHalamanEkspresi(), 14);
        SetOutlineColorSafe(btnStart.gameObject, colDeepNavy, true);
        var startTxt = UIKit.Text(btnStart.transform, "START PHOTO BOOTH", 20f, Color.white, customFontAsset);
        if (startTxt != null) startTxt.raycastTarget = false;

        RectTransform rightCol = UIKit.NewRect("RightMascotColumn", hintPanel);
        UIKit.Place(rightCol, new Vector2(0.71f, 0.48f), Vector2.zero, new Vector2(750, 720));

        UIKit.Card(rightCol, "MascotShadow", new Vector2(0.47f, 0.14f), Vector2.zero, new Vector2(460, 65),
            new Color32(166, 200, 226, 160), Color.clear, 32, 0, false);

        Sprite mascotSprite = LoadSpriteFlex("gembira");
        if (mascotSprite != null)
        {
            Image mascotImg = UIKit.NewRect("ForgyHeroMascot", rightCol).gameObject.AddComponent<Image>();
            UIKit.Place(mascotImg.rectTransform, new Vector2(0.47f, 0.56f), Vector2.zero, new Vector2(620, 620));
            mascotImg.sprite = mascotSprite;
            mascotImg.preserveAspect = true;
            mascotImg.gameObject.AddComponent<UIBounce>();
        }

        UIKit.Sparkle(hintPanel, new Vector2(820, 310), 80f, colDeepNavy);
        UIKit.Sparkle(hintPanel, new Vector2(-810, -240), 65f, colSkyBlue);
        UIKit.Sparkle(hintPanel, new Vector2(40, -320), 55f, colSteelBlue);
        UIKit.Sparkle(hintPanel, new Vector2(-50, 110), 55f, colSkyBlue);

        TextMeshProUGUI sideNote = UIKit.Label(rightCol, "say cheese\nwith Forgy!", 26f, colDeepNavy,
            new Vector2(0.91f, 0.48f), Vector2.zero, new Vector2(200, 80), titleFont);
        sideNote.alignment = TextAlignmentOptions.Center;
        sideNote.fontStyle = FontStyles.Normal;
        sideNote.rectTransform.localRotation = Quaternion.Euler(0, 0, -6f);

        // ----------------------------------------------------
        // 2. HALAMAN PILIH EKSPRESI KARAKTER FORGY
        // ----------------------------------------------------
        RectTransform exprPanel = BuatHalamanDasar("UI_Generated_ExpressionPanel", "STEP 1 • EXPRESSION");
        expressionPanelGO = exprPanel.gameObject;

        UIKit.Sparkle(exprPanel, new Vector2(-720, 220), 65f, colSkyBlue);
        UIKit.Sparkle(exprPanel, new Vector2(720, -220), 65f, colSteelBlue);

        TextMeshProUGUI subExpr = UIKit.Label(exprPanel, "(CHOOSE YOUR BUDDY MOOD)", 18f, colSubTextNavy,
            new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(600, 30), customFontAsset);
        subExpr.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI titleExpr = UIKit.Label(exprPanel, "Pilih Ekspresi Karakter", 56f, colDeepNavy,
            new Vector2(0.5f, 0.77f), Vector2.zero, new Vector2(900, 80), titleFont);
        titleExpr.alignment = TextAlignmentOptions.Center;
        titleExpr.fontStyle = FontStyles.Normal;

        BuatKartuEkspresiForgy(exprPanel, "GEMBIRA", "gembira", new Vector2(0.24f, 0.42f), () => LanjutKePilihLayout(0));
        BuatKartuEkspresiForgy(exprPanel, "MARAH", "marah", new Vector2(0.50f, 0.42f), () => LanjutKePilihLayout(1));
        BuatKartuEkspresiForgy(exprPanel, "TANPA FORGY", "", new Vector2(0.76f, 0.42f), () => LanjutKePilihLayout(-1));

        // ----------------------------------------------------
        // 3. HALAMAN PILIH TATA LETAK FOTO & BOOMERANG
        // ----------------------------------------------------
        RectTransform chooseLayoutPanel = BuatHalamanDasar("UI_Generated_LayoutChoosePanel", "STEP 2 • LAYOUT");
        layoutChoosePanelGO = chooseLayoutPanel.gameObject;

        UIKit.Sparkle(chooseLayoutPanel, new Vector2(-720, -210), 65f, colSkyBlue);
        UIKit.Sparkle(chooseLayoutPanel, new Vector2(720, 210), 65f, colSteelBlue);

        modePhotoPage = UIKit.NewRect("Page_PhotoLayouts", chooseLayoutPanel).gameObject;
        UIKit.Stretch(modePhotoPage.GetComponent<RectTransform>());

        TextMeshProUGUI subLayout = UIKit.Label(modePhotoPage.transform, "(SELECT FRAME FORMAT)", 18f, colSubTextNavy,
            new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(600, 30), customFontAsset);
        subLayout.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI titleLayout = UIKit.Label(modePhotoPage.transform, "Pilih Tata Letak Foto", 56f, colDeepNavy,
            new Vector2(0.5f, 0.77f), Vector2.zero, new Vector2(900, 80), titleFont);
        titleLayout.alignment = TextAlignmentOptions.Center;
        titleLayout.fontStyle = FontStyles.Normal;

        BuatKartuLayout(modePhotoPage.transform, "4-Grid", new Vector2(0.25f, 0.43f), () => PilihLayoutDanMulai(4), 4);
        BuatKartuLayout(modePhotoPage.transform, "STRIP FOTO", new Vector2(0.50f, 0.43f), () => PilihLayoutDanMulai(3), 3);
        BuatKartuLayout(modePhotoPage.transform, "FOTO TUNGGAL", new Vector2(0.75f, 0.43f), () => PilihLayoutDanMulai(1), 1);

        Image btnPrevP = UIKit.CardButton(modePhotoPage.transform, "Btn_Prev", new Vector2(0.06f, 0.45f), Vector2.zero, new Vector2(72, 86),
            colSkyBlue, () => KembaliKeEkspresi(), 24);
        SetOutlineColorSafe(btnPrevP.gameObject, colDeepNavy, true);
        UIKit.Text(btnPrevP.transform, "<", 44f, colDeepNavy, customFontAsset);

        Image btnNextP = UIKit.CardButton(modePhotoPage.transform, "Btn_Next", new Vector2(0.94f, 0.45f), Vector2.zero, new Vector2(72, 86),
            colSteelBlue, () => TampilkanHalamanBoomerang(true), 24);
        SetOutlineColorSafe(btnNextP.gameObject, colDeepNavy, true);
        UIKit.Text(btnNextP.transform, ">", 44f, Color.white, customFontAsset);
        btnNextP.gameObject.SetActive(false);

        modeBoomerangPage = UIKit.NewRect("Page_BoomerangLayout", chooseLayoutPanel).gameObject;
        UIKit.Stretch(modeBoomerangPage.GetComponent<RectTransform>());

        TextMeshProUGUI subBoom = UIKit.Label(modeBoomerangPage.transform, "(MOTION LOOP CAPTURE)", 18f, colSubTextNavy,
            new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(600, 30), customFontAsset);
        subBoom.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI titleBoom = UIKit.Label(modeBoomerangPage.transform, "Mode Boomerang", 56f, colDeepNavy,
            new Vector2(0.5f, 0.77f), Vector2.zero, new Vector2(900, 80), titleFont);
        titleBoom.alignment = TextAlignmentOptions.Center;
        titleBoom.fontStyle = FontStyles.Normal;

        BuatKartuLayout(modeBoomerangPage.transform, "BOOMERANG LOOP", new Vector2(0.50f, 0.43f), () => PilihLayoutDanMulai(0), 0);

        Image btnPrevB = UIKit.CardButton(modeBoomerangPage.transform, "Btn_Prev", new Vector2(0.06f, 0.45f), Vector2.zero, new Vector2(72, 86),
            colSkyBlue, () => TampilkanHalamanBoomerang(false), 24);
        SetOutlineColorSafe(btnPrevB.gameObject, colDeepNavy, true);
        UIKit.Text(btnPrevB.transform, "<", 44f, colDeepNavy, customFontAsset);

        Image btnNextB = UIKit.CardButton(modeBoomerangPage.transform, "Btn_Next", new Vector2(0.94f, 0.45f), Vector2.zero, new Vector2(72, 86),
            colSoftCardBg, null, 24);
        SetOutlineColorSafe(btnNextB.gameObject, colSubTextNavy, true);
        UIKit.Text(btnNextB.transform, ">", 44f, colSubTextNavy, customFontAsset);
        btnNextB.GetComponent<Button>().interactable = false;

        TampilkanHalamanBoomerang(false);

        // ----------------------------------------------------
        // 3B. HALAMAN BERFOTO (HANYA TOMBOL MULAI DI LAYAR KAMERA PENUH)
        // ----------------------------------------------------
        RectTransform livePanel = UIKit.NewRect("UI_Generated_LivePanel", canvas.transform);
        UIKit.Stretch(livePanel);
        livePanelGO = livePanel.gameObject;

        Image btnStartCapture = UIKit.CardButton(livePanel, "Btn_StartCapture", new Vector2(0.50f, 0.085f), Vector2.zero, new Vector2(360, 76),
            colSteelBlue, () => { if (controller != null) controller.TakePhoto(); }, 22);
        SetOutlineColorSafe(btnStartCapture.gameObject, colDeepNavy, true);
        btnStartCapture.transform.parent.gameObject.AddComponent<UIBounce>();
        UIKit.Text(btnStartCapture.transform, "MULAI BERFOTO", 24f, Color.white, customFontAsset);

        livePanel.gameObject.SetActive(false);

        // ----------------------------------------------------
        // 4. HALAMAN PREVIEW & STUDIO EDITOR 3-KOLOM (PRESISI)
        // ----------------------------------------------------
        RectTransform previewPanel = BuatHalamanDasar("UI_Generated_PreviewPanel", "STEP 3 • EDIT & FRAME");
        previewPanelGO = previewPanel.gameObject;

        // === KOLOM KIRI: EDIT FOTO ===
        RectTransform editCol = UIKit.NewRect("LeftEditPhotoColumn", previewPanel);
        UIKit.Place(editCol, new Vector2(0.155f, 0.455f), Vector2.zero, new Vector2(460, 830));

        Image editBg = UIKit.Card(editCol, "EditPanelBg", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(450, 820),
            colSteelBlue, colDeepNavy, 28, 3, false).GetComponent<Image>();
        editBg.raycastTarget = false;

        TextMeshProUGUI editTitle = UIKit.Label(editCol, "Edit Foto", 34f, Color.white,
            new Vector2(0.5f, 0.945f), Vector2.zero, new Vector2(380, 42), titleFont);
        editTitle.alignment = TextAlignmentOptions.Center;

        Image slotStatusPill = UIKit.Card(editCol, "SlotStatusPill", new Vector2(0.5f, 0.892f), Vector2.zero, new Vector2(394, 30),
            colDeepNavy, Color.clear, 15, 0, false).GetComponent<Image>();
        TextMeshProUGUI lblSlotStatus = UIKit.Label(slotStatusPill.transform, "SLOT 1 MASIH KOSONG", 14f, Color.white,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(370, 26), customFontAsset);
        lblSlotStatus.alignment = TextAlignmentOptions.Center;

        Image editPreviewBox = UIKit.Card(editCol, "EditPreviewMaskBox", new Vector2(0.5f, 0.742f), Vector2.zero, new Vector2(248, 184),
            colSoftCardBg, Color.white, 18, 3, false).GetComponent<Image>();
        editPreviewBox.gameObject.AddComponent<RectMask2D>();
        editPreviewBox.raycastTarget = true;

        RawImage editPreviewRaw = UIKit.NewRect("EditPreviewRaw", editPreviewBox.transform).gameObject.AddComponent<RawImage>();
        RectTransform editRawRt = editPreviewRaw.rectTransform;
        editRawRt.anchorMin = new Vector2(0.5f, 0.5f);
        editRawRt.anchorMax = new Vector2(0.5f, 0.5f);
        editRawRt.pivot = new Vector2(0.5f, 0.5f);
        editRawRt.sizeDelta = new Vector2(248, 184);
        editPreviewRaw.raycastTarget = false;

        TextMeshProUGUI dragHintLbl = UIKit.Label(editCol, "Seret foto di atas atau pakai kontrol di bawah:", 13f, colSoftCardBg,
            new Vector2(0.5f, 0.608f), Vector2.zero, new Vector2(394, 22), customFontAsset);
        dragHintLbl.alignment = TextAlignmentOptions.Center;

        Image posCard = UIKit.Card(editCol, "PositionControlCard", new Vector2(0.275f, 0.445f), Vector2.zero, new Vector2(188, 225),
            new Color32(76, 136, 178, 255), colDeepNavy, 20, 2, false).GetComponent<Image>();
        posCard.raycastTarget = false;

        TextMeshProUGUI lblPosHeader = UIKit.Label(posCard.transform, "POSISI FOTO", 12f, colSoftCardBg,
            new Vector2(0.5f, 0.90f), Vector2.zero, new Vector2(160, 20), customFontAsset);
        lblPosHeader.alignment = TextAlignmentOptions.Center;

        Image dpadCenterDot = UIKit.Card(posCard.transform, "DPadCenterHub", new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(34, 34),
            new Color32(27, 54, 93, 180), Color.clear, 17, 0, false).GetComponent<Image>();
        dpadCenterDot.raycastTarget = false;

        Image btnUp = UIKit.CardButton(posCard.transform, "Btn_MoveUp", new Vector2(0.50f, 0.69f), Vector2.zero, new Vector2(46, 44),
            Color.white, null, 12);
        SetOutlineColorSafe(btnUp.gameObject, colDeepNavy, true);
        UIKit.Text(btnUp.transform, "▲", 18f, colDeepNavy, customFontAsset);

        Image btnDown = UIKit.CardButton(posCard.transform, "Btn_MoveDown", new Vector2(0.50f, 0.21f), Vector2.zero, new Vector2(46, 44),
            Color.white, null, 12);
        SetOutlineColorSafe(btnDown.gameObject, colDeepNavy, true);
        UIKit.Text(btnDown.transform, "▼", 18f, colDeepNavy, customFontAsset);

        Image btnLeft = UIKit.CardButton(posCard.transform, "Btn_MoveLeft", new Vector2(0.22f, 0.45f), Vector2.zero, new Vector2(46, 44),
            Color.white, null, 12);
        SetOutlineColorSafe(btnLeft.gameObject, colDeepNavy, true);
        UIKit.Text(btnLeft.transform, "◄", 18f, colDeepNavy, customFontAsset);

        Image btnRight = UIKit.CardButton(posCard.transform, "Btn_MoveRight", new Vector2(0.78f, 0.45f), Vector2.zero, new Vector2(46, 44),
            Color.white, null, 12);
        SetOutlineColorSafe(btnRight.gameObject, colDeepNavy, true);
        UIKit.Text(btnRight.transform, "►", 18f, colDeepNavy, customFontAsset);

        Image transformCard = UIKit.Card(editCol, "TransformControlCard", new Vector2(0.725f, 0.445f), Vector2.zero, new Vector2(188, 225),
            new Color32(76, 136, 178, 255), colDeepNavy, 20, 2, false).GetComponent<Image>();
        transformCard.raycastTarget = false;

        TextMeshProUGUI lblZoomHeader = UIKit.Label(transformCard.transform, "ZOOM FOTO", 12f, colSoftCardBg,
            new Vector2(0.5f, 0.90f), Vector2.zero, new Vector2(160, 20), customFontAsset);
        lblZoomHeader.alignment = TextAlignmentOptions.Center;

        Image zoomPillBg = UIKit.Card(transformCard.transform, "ZoomValuePill", new Vector2(0.5f, 0.69f), Vector2.zero, new Vector2(162, 44),
            colDeepNavy, Color.clear, 12, 0, false).GetComponent<Image>();
        zoomPillBg.raycastTarget = false;

        Image btnZoomOut = UIKit.CardButton(transformCard.transform, "Btn_ZoomOut", new Vector2(0.21f, 0.69f), Vector2.zero, new Vector2(42, 38),
            colSkyBlue, null, 10);
        SetOutlineColorSafe(btnZoomOut.gameObject, colDeepNavy, false);
        UIKit.Text(btnZoomOut.transform, "-", 24f, colDeepNavy, customFontAsset);

        TextMeshProUGUI lblZoomVal = UIKit.Label(transformCard.transform, "100%", 15f, Color.white,
            new Vector2(0.50f, 0.69f), Vector2.zero, new Vector2(54, 36), customFontAsset);
        lblZoomVal.alignment = TextAlignmentOptions.Center;
        lblZoomVal.raycastTarget = false;

        Image btnZoomIn = UIKit.CardButton(transformCard.transform, "Btn_ZoomIn", new Vector2(0.79f, 0.69f), Vector2.zero, new Vector2(42, 38),
            colSkyBlue, null, 10);
        SetOutlineColorSafe(btnZoomIn.gameObject, colDeepNavy, false);
        UIKit.Text(btnZoomIn.transform, "+", 24f, colDeepNavy, customFontAsset);

        Image dividerTransform = UIKit.Card(transformCard.transform, "DividerTransform", new Vector2(0.5f, 0.51f), Vector2.zero, new Vector2(150, 2),
            new Color32(166, 200, 226, 100), Color.clear, 1, 0, false).GetComponent<Image>();
        dividerTransform.raycastTarget = false;

        TextMeshProUGUI lblRotHeader = UIKit.Label(transformCard.transform, "PUTAR FOTO", 12f, colSoftCardBg,
            new Vector2(0.5f, 0.41f), Vector2.zero, new Vector2(160, 20), customFontAsset);
        lblRotHeader.alignment = TextAlignmentOptions.Center;

        Image rotPillBg = UIKit.Card(transformCard.transform, "RotValuePill", new Vector2(0.5f, 0.21f), Vector2.zero, new Vector2(162, 44),
            colDeepNavy, Color.clear, 12, 0, false).GetComponent<Image>();
        rotPillBg.raycastTarget = false;

        Image btnRotLeft = UIKit.CardButton(transformCard.transform, "Btn_RotLeft", new Vector2(0.21f, 0.21f), Vector2.zero, new Vector2(42, 38),
            colSkyBlue, null, 10);
        SetOutlineColorSafe(btnRotLeft.gameObject, colDeepNavy, false);
        UIKit.Text(btnRotLeft.transform, "<", 20f, colDeepNavy, customFontAsset);

        TextMeshProUGUI lblRotVal = UIKit.Label(transformCard.transform, "0°", 15f, Color.white,
            new Vector2(0.50f, 0.21f), Vector2.zero, new Vector2(54, 36), customFontAsset);
        lblRotVal.alignment = TextAlignmentOptions.Center;
        lblRotVal.raycastTarget = false;

        Image btnRotRight = UIKit.CardButton(transformCard.transform, "Btn_RotRight", new Vector2(0.79f, 0.21f), Vector2.zero, new Vector2(42, 38),
            colSkyBlue, null, 10);
        SetOutlineColorSafe(btnRotRight.gameObject, colDeepNavy, false);
        UIKit.Text(btnRotRight.transform, ">", 20f, colDeepNavy, customFontAsset);

        Image btnFlipImg = UIKit.CardButton(editCol, "Btn_FlipImage", new Vector2(0.275f, 0.225f), Vector2.zero, new Vector2(188, 50),
            colSkyBlue, null, 14);
        SetOutlineColorSafe(btnFlipImg.gameObject, colDeepNavy, true);
        UIKit.Text(btnFlipImg.transform, "Flip (Cermin)", 16f, colDeepNavy, customFontAsset);

        Image btnClearSlot = UIKit.CardButton(editCol, "Btn_ClearSlot", new Vector2(0.725f, 0.225f), Vector2.zero, new Vector2(188, 50),
            colSkyBlue, null, 14);
        SetOutlineColorSafe(btnClearSlot.gameObject, colDeepNavy, true);
        UIKit.Text(btnClearSlot.transform, "Kosongkan Slot", 16f, colDeepNavy, customFontAsset);

        Image btnResetImg = UIKit.CardButton(editCol, "Btn_CenterImage", new Vector2(0.5f, 0.115f), Vector2.zero, new Vector2(390, 50),
            Color.white, null, 14);
        SetOutlineColorSafe(btnResetImg.gameObject, colDeepNavy, true);
        UIKit.Text(btnResetImg.transform, "Reset Posisi Tengah", 17f, colDeepNavy, customFontAsset);

        // === KOLOM TENGAH ===
        RectTransform centerCol = UIKit.NewRect("CenterCanvasColumn", previewPanel);
        UIKit.Place(centerCol, new Vector2(0.50f, 0.455f), Vector2.zero, new Vector2(720, 845));

        Image frameBarBg = UIKit.Card(centerCol, "TopFrameSelectorBg", new Vector2(0.5f, 0.888f), Vector2.zero, new Vector2(440, 96),
            colSkyBlue, colDeepNavy, 20, 2, false).GetComponent<Image>();
        frameBarBg.raycastTarget = false;

        TextMeshProUGUI lblPilihBingkai = UIKit.Label(frameBarBg.transform, "PILIH\nBINGKAI:", 13f, colDeepNavy,
            new Vector2(0.14f, 0.50f), Vector2.zero, new Vector2(96, 56), customFontAsset);
        lblPilihBingkai.rectTransform.gameObject.name = "LblPilihBingkai";
        lblPilihBingkai.alignment = TextAlignmentOptions.Center;

        RectTransform frameScrollViewport = UIKit.NewRect("FrameScrollViewport", frameBarBg.transform);
        UIKit.Place(frameScrollViewport, new Vector2(0.62f, 0.50f), Vector2.zero, new Vector2(316, 82));
        frameScrollViewport.gameObject.AddComponent<RectMask2D>();
        Image vpBg = frameScrollViewport.gameObject.AddComponent<Image>();
        vpBg.color = new Color(1, 1, 1, 0.002f);

        ScrollRect frameScroll = frameScrollViewport.gameObject.AddComponent<ScrollRect>();
        frameScroll.horizontal = true;
        frameScroll.vertical = false;
        frameScroll.movementType = ScrollRect.MovementType.Elastic;
        frameScroll.scrollSensitivity = 25f;

        RectTransform frameBar = UIKit.NewRect("FrameSelectorBarContainer", frameScrollViewport);
        frameBar.anchorMin = new Vector2(0f, 0.5f);
        frameBar.anchorMax = new Vector2(0f, 0.5f);
        frameBar.pivot = new Vector2(0f, 0.5f);
        frameBar.anchoredPosition = Vector2.zero;
        frameBar.sizeDelta = new Vector2(316, 78);

        HorizontalLayoutGroup hLayoutFrameBar = frameBar.gameObject.AddComponent<HorizontalLayoutGroup>();
        hLayoutFrameBar.spacing = 12f;
        hLayoutFrameBar.padding = new RectOffset(6, 10, 3, 3);
        hLayoutFrameBar.childAlignment = TextAnchor.MiddleLeft;
        hLayoutFrameBar.childControlWidth = true;
        hLayoutFrameBar.childControlHeight = true;
        hLayoutFrameBar.childForceExpandWidth = false;
        hLayoutFrameBar.childForceExpandHeight = false;

        ContentSizeFitter csfFrameBar = frameBar.gameObject.AddComponent<ContentSizeFitter>();
        csfFrameBar.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csfFrameBar.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        frameScroll.viewport = frameScrollViewport;
        frameScroll.content = frameBar;

        RectTransform frameCardContainer = UIKit.NewRect("FrameCardContainer", centerCol);
        UIKit.Place(frameCardContainer, new Vector2(0.5f, 0.478f), Vector2.zero, new Vector2(212, 575));

        RectTransform photoHolder = UIKit.NewRect("Card_PhotoHolder", frameCardContainer);
        UIKit.Stretch(photoHolder);

        Image mainFrameOverlay = UIKit.NewRect("MainFrameOverlayImage", frameCardContainer).gameObject.AddComponent<Image>();
        UIKit.Stretch(mainFrameOverlay.rectTransform);
        mainFrameOverlay.raycastTarget = false;
        mainFrameOverlay.preserveAspect = false;

        Image btnSave = UIKit.CardButton(centerCol, "Btn_Save", new Vector2(0.325f, 0.067f), Vector2.zero, new Vector2(215, 56),
            colDeepNavy, () => controller.Save(), 18);
        SetOutlineColorSafe(btnSave.gameObject, Color.white, true);
        UIKit.Text(btnSave.transform, "SIMPAN", 19f, Color.white, customFontAsset);

        Image btnRetake = UIKit.CardButton(centerCol, "Btn_Retake", new Vector2(0.675f, 0.067f), Vector2.zero, new Vector2(215, 56),
            colSkyBlue, () => controller.Retake(), 18);
        SetOutlineColorSafe(btnRetake.gameObject, colDeepNavy, true);
        UIKit.Text(btnRetake.transform, "ULANGI", 19f, colDeepNavy, customFontAsset);

        Image btnOpenFolder = UIKit.CardButton(centerCol, "Btn_OpenSavedFolder", new Vector2(0.50f, 0.002f), Vector2.zero, new Vector2(310, 44),
            colSteelBlue, () => controller.OpenLastSavedFolder(), 15);
        SetOutlineColorSafe(btnOpenFolder.gameObject, colDeepNavy, true);
        UIKit.Text(btnOpenFolder.transform, "BUKA DI FINDER", 16f, Color.white, customFontAsset);
        btnOpenFolder.gameObject.SetActive(false);

        // === KOLOM KANAN: GALERI PILIH 6 FOTO ===
        RectTransform rightGalleryCol = UIKit.NewRect("RightGalleryColumn", previewPanel);
        UIKit.Place(rightGalleryCol, new Vector2(0.845f, 0.455f), Vector2.zero, new Vector2(460, 830));

        Image galleryBg = UIKit.Card(rightGalleryCol, "GalleryPanelBg", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(450, 820),
            colSteelBlue, colDeepNavy, 28, 3, false).GetComponent<Image>();
        galleryBg.raycastTarget = false;

        TextMeshProUGUI galleryTitle = UIKit.Label(rightGalleryCol, "Pilih Foto", 34f, Color.white,
            new Vector2(0.5f, 0.945f), Vector2.zero, new Vector2(380, 42), titleFont);
        galleryTitle.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI galleryHint = UIKit.Label(rightGalleryCol, "1. Klik slot di frame tengah\n2. Klik foto di bawah untuk mengisi", 14f, colSoftCardBg,
            new Vector2(0.5f, 0.882f), Vector2.zero, new Vector2(400, 40), customFontAsset);
        galleryHint.alignment = TextAlignmentOptions.Center;

        RectTransform galleryGridContainer = UIKit.NewRect("PhotoGalleryGridContainer", rightGalleryCol);
        UIKit.Place(galleryGridContainer, new Vector2(0.5f, 0.435f), Vector2.zero, new Vector2(390, 635));

        GridLayoutGroup galleryGrid = galleryGridContainer.gameObject.AddComponent<GridLayoutGroup>();
        galleryGrid.cellSize = new Vector2(178, 178);
        galleryGrid.spacing = new Vector2(16, 14);
        galleryGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        galleryGrid.constraintCount = 2;
        galleryGrid.childAlignment = TextAnchor.UpperCenter;

        // ----------------------------------------------------
        // 5. TEKS COUNTDOWN KAMERA & OVERLAY FLASH PUTIH
        // ----------------------------------------------------
        TextMeshProUGUI countText = UIKit.Label(canvas.transform, "", 130f, colSkyBlue, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 320), titleFont);
        countText.alignment = TextAlignmentOptions.Center;
        countText.fontStyle = FontStyles.Normal;
        countText.rectTransform.gameObject.name = "UI_Generated_CountdownText";
        Outline countOutline = countText.gameObject.AddComponent<Outline>();
        countOutline.effectColor = colDeepNavy;
        countOutline.effectDistance = new Vector2(5, -5);

        RectTransform flashRect = UIKit.NewRect("UI_Generated_FlashImage", canvas.transform);
        UIKit.Stretch(flashRect);
        flashRect.SetAsLastSibling();
        Image flashImg = flashRect.gameObject.AddComponent<Image>();
        flashImg.color = Color.white;
        flashImg.raycastTarget = false;
        flashImg.gameObject.SetActive(false);

        ResetKeHalamanUtama();
        HubungkanKeController(
            hintPanel.gameObject,
            exprPanel.gameObject,
            chooseLayoutPanel.gameObject,
            livePanel.gameObject,
            previewPanel.gameObject,
            frameCardContainer,
            photoHolder,
            mainFrameOverlay,
            frameBar,
            countText,
            flashImg,
            galleryGridContainer,
            editPreviewRaw,
            editPreviewBox.gameObject,
            lblSlotStatus,
            lblZoomVal,
            lblRotVal,
            btnUp.GetComponent<Button>(),
            btnDown.GetComponent<Button>(),
            btnLeft.GetComponent<Button>(),
            btnRight.GetComponent<Button>(),
            btnZoomIn.GetComponent<Button>(),
            btnZoomOut.GetComponent<Button>(),
            btnRotLeft.GetComponent<Button>(),
            btnRotRight.GetComponent<Button>(),
            btnFlipImg.GetComponent<Button>(),
            btnClearSlot.GetComponent<Button>(),
            btnResetImg.GetComponent<Button>(),
            btnOpenFolder.GetComponent<Button>()
        );
    }

    private void SetOutlineColorSafe(GameObject obj, Color col, bool enabled)
    {
        if (obj == null) return;
        Outline outline = obj.GetComponentInChildren<Outline>(true);
        if (outline != null)
        {
            outline.effectColor = col;
            outline.enabled = enabled;
        }
    }

    private RectTransform BuatHalamanDasar(string panelName, string rightBadgeText)
    {
        RectTransform panel = UIKit.NewRect(panelName, canvas.transform);
        UIKit.Stretch(panel);

        Image bgLight = panel.gameObject.AddComponent<Image>();
        bgLight.color = colBgLightBlue;

        BuatGridBackground(panel, 24, 14);
        BuatScallopedHeader(panel, rightBadgeText);

        return panel;
    }

    private void BuatGridBackground(RectTransform parent, int cols, int rows)
    {
        RectTransform gridLayer = UIKit.NewRect("GridLinesLayer", parent);
        UIKit.Stretch(gridLayer);

        for (int c = 1; c < cols; c++)
        {
            float xNorm = (float)c / cols;
            RectTransform vLine = UIKit.NewRect("VLine_" + c, gridLayer);
            vLine.anchorMin = new Vector2(xNorm, 0);
            vLine.anchorMax = new Vector2(xNorm, 1);
            vLine.sizeDelta = new Vector2(2f, 0);
            Image img = vLine.gameObject.AddComponent<Image>();
            img.color = colGridLine;
            img.raycastTarget = false;
        }

        for (int r = 1; r < rows; r++)
        {
            float yNorm = (float)r / rows;
            RectTransform hLine = UIKit.NewRect("HLine_" + r, gridLayer);
            hLine.anchorMin = new Vector2(0, yNorm);
            hLine.anchorMax = new Vector2(1, yNorm);
            hLine.sizeDelta = new Vector2(0, 2f);
            Image img = hLine.gameObject.AddComponent<Image>();
            img.color = colGridLine;
            img.raycastTarget = false;
        }
    }

    private void BuatScallopedHeader(RectTransform parent, string badgeText)
    {
        RectTransform topBar = UIKit.NewRect("ScallopTopBar", parent);
        topBar.anchorMin = new Vector2(0, 0.91f);
        topBar.anchorMax = Vector2.one;
        topBar.offsetMin = Vector2.zero;
        topBar.offsetMax = Vector2.zero;
        Image barImg = topBar.gameObject.AddComponent<Image>();
        barImg.color = colSkyBlue;

        int scallopCount = 34;
        for (int i = 0; i <= scallopCount; i++)
        {
            float xNorm = (float)i / scallopCount;
            Image circle = UIKit.Card(topBar, "Scallop_" + i, new Vector2(xNorm, 0f), Vector2.zero, new Vector2(64, 44),
                colSkyBlue, Color.clear, 22, 0, false).GetComponent<Image>();
            circle.raycastTarget = false;
            SetOutlineColorSafe(circle.gameObject, Color.clear, false);
        }

        TextMeshProUGUI navLeft1 = UIKit.Label(topBar, "AR CAMERA", 16f, colDeepNavy, new Vector2(0.08f, 0.50f), Vector2.zero, new Vector2(160, 30), customFontAsset);
        navLeft1.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI navLeft2 = UIKit.Label(topBar, "3 LAYOUTS", 16f, colDeepNavy, new Vector2(0.19f, 0.50f), Vector2.zero, new Vector2(160, 30), customFontAsset);
        navLeft2.alignment = TextAlignmentOptions.Center;

        TMP_FontAsset logoFont = (brandLogoFontAsset != null) ? brandLogoFontAsset : customFontAsset;
        TextMeshProUGUI brandLogo = UIKit.Label(topBar, "Forgy Booth", 48f, colDeepNavy, new Vector2(0.50f, 0.50f), Vector2.zero, new Vector2(520, 68), logoFont);
        brandLogo.alignment = TextAlignmentOptions.Center;
        brandLogo.fontStyle = FontStyles.Normal;
        brandLogo.characterSpacing = 1f;

        float badgeWidth = badgeText.Length > 12 ? 230f : 150f;
        Image statusBadge = UIKit.Card(topBar, "StatusBadge", new Vector2(0.90f, 0.50f), Vector2.zero, new Vector2(badgeWidth, 34),
            colSteelBlue, colDeepNavy, 17, 2, false).GetComponent<Image>();
        TextMeshProUGUI badgeLabel = UIKit.Label(statusBadge.transform, badgeText, 14f, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(badgeWidth - 10, 26), customFontAsset);
        badgeLabel.alignment = TextAlignmentOptions.Center;
    }

    private void ResetKeHalamanUtama()
    {
        if (hintPanelGO != null) hintPanelGO.SetActive(true);
        if (expressionPanelGO != null) expressionPanelGO.SetActive(false);
        if (layoutChoosePanelGO != null) layoutChoosePanelGO.SetActive(false);
        if (livePanelGO != null) livePanelGO.SetActive(false);
        if (previewPanelGO != null) previewPanelGO.SetActive(false);
    }

    private void BukaHalamanEkspresi()
    {
        if (controller != null)
        {
            if (!controller.gameObject.activeInHierarchy)
                controller.gameObject.SetActive(true);
            controller.OpenChooseExpression();
        }
        if (hintPanelGO != null) hintPanelGO.SetActive(false);
        if (expressionPanelGO != null) expressionPanelGO.SetActive(true);
        if (layoutChoosePanelGO != null) layoutChoosePanelGO.SetActive(false);
        if (livePanelGO != null) livePanelGO.SetActive(false);
        if (previewPanelGO != null) previewPanelGO.SetActive(false);
    }

    private void LanjutKePilihLayout(int indexEkspresi)
    {
        if (controller != null)
        {
            if (!controller.gameObject.activeInHierarchy)
                controller.gameObject.SetActive(true);

            controller.ChooseExpression(indexEkspresi);

            if (!controller.gameObject.activeSelf)
                controller.gameObject.SetActive(true);
        }

        if (expressionPanelGO != null) expressionPanelGO.SetActive(false);
        if (layoutChoosePanelGO != null) layoutChoosePanelGO.SetActive(true);
        if (livePanelGO != null) livePanelGO.SetActive(false);
        if (previewPanelGO != null) previewPanelGO.SetActive(false);
    }

    private void PilihLayoutDanMulai(int shotCount)
    {
        if (layoutChoosePanelGO != null) layoutChoosePanelGO.SetActive(false);
        
        if (controller != null)
        {
            if (!controller.gameObject.activeInHierarchy)
                controller.gameObject.SetActive(true);

            if (shotCount == 0)
            {
                controller.TakeBoomerang();
            }
            else
            {
                controller.EnterLiveCameraReady(shotCount);
            }
        }
    }

    private void KembaliKeEkspresi()
    {
        if (layoutChoosePanelGO != null) layoutChoosePanelGO.SetActive(false);
        if (expressionPanelGO != null) expressionPanelGO.SetActive(true);
        if (livePanelGO != null) livePanelGO.SetActive(false);
        if (previewPanelGO != null) previewPanelGO.SetActive(false);
    }

    private void TampilkanHalamanBoomerang(bool showBoomerang)
    {
        if (modePhotoPage != null) modePhotoPage.SetActive(!showBoomerang);
        if (modeBoomerangPage != null) modeBoomerangPage.SetActive(showBoomerang);
    }

    private Sprite LoadSpriteFlex(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        Sprite s = Resources.Load<Sprite>(name);
        if (s == null) s = Resources.Load<Sprite>("Project/" + name);
        return s;
    }

    private void BuatKartuEkspresiForgy(Transform parent, string namaEkspresi, string resourceSpriteName, Vector2 positionAnchor, UnityEngine.Events.UnityAction action)
    {
        Image card = UIKit.CardButton(parent, "Card_" + namaEkspresi, positionAnchor, Vector2.zero, new Vector2(310, 380),
            Color.white, action, 35);
        SetOutlineColorSafe(card.gameObject, colDeepNavy, true);

        Image previewBox = UIKit.Card(card.transform, "PreviewBox", new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(260, 260),
            colSoftCardBg, colDeepNavy, 22, 3, false).GetComponent<Image>();

        Sprite charSprite = LoadSpriteFlex(resourceSpriteName);
        if (charSprite != null)
        {
            Image charImg = UIKit.NewRect("ForgyChar", previewBox.transform).gameObject.AddComponent<Image>();
            UIKit.Stretch(charImg.rectTransform);
            charImg.rectTransform.offsetMin = new Vector2(4, 4);
            charImg.rectTransform.offsetMax = new Vector2(-4, -4);
            charImg.sprite = charSprite;
            charImg.preserveAspect = true;
        }
        else if (string.IsNullOrEmpty(resourceSpriteName))
        {
            Image camBody = UIKit.Card(previewBox.transform, "CameraBody", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(155, 110),
                colSkyBlue, colDeepNavy, 18, 4, false).GetComponent<Image>();
            
            Image camLens = UIKit.Card(camBody.transform, "LensOuter", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 60),
                Color.white, colDeepNavy, 30, 3, false).GetComponent<Image>();
            UIKit.Card(camLens.transform, "LensInner", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28, 28),
                colDeepNavy, colDeepNavy, 14, 0, false);

            UIKit.Card(camBody.transform, "Flash", new Vector2(0.80f, 0.78f), Vector2.zero, new Vector2(20, 16),
                colSteelBlue, colDeepNavy, 4, 2, false);
            UIKit.Card(camBody.transform, "Shutter", new Vector2(0.20f, 1.05f), Vector2.zero, new Vector2(24, 12),
                colDeepNavy, colDeepNavy, 4, 0, false);
        }

        TextMeshProUGUI lblExpr = UIKit.Label(card.transform, namaEkspresi, 26f, colDeepNavy,
            new Vector2(0.5f, 0.11f), Vector2.zero, new Vector2(260, 45), customFontAsset);
        lblExpr.alignment = TextAlignmentOptions.Center;
    }

    private void BuatKartuLayout(Transform parent, string namaLayout, Vector2 positionAnchor, UnityEngine.Events.UnityAction action, int layoutType)
    {
        Image card = UIKit.CardButton(parent, "Card_" + namaLayout, positionAnchor, Vector2.zero, new Vector2(275, 340),
            Color.white, action, 35);
        SetOutlineColorSafe(card.gameObject, colDeepNavy, true);

        Image previewBox = UIKit.Card(card.transform, "PreviewBox", new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(225, 225),
            colSoftCardBg, colDeepNavy, 22, 3, false).GetComponent<Image>();

        if (layoutType == 4)
        {
            UIKit.Card(previewBox.transform, "G1", new Vector2(0.3f, 0.7f), Vector2.zero, new Vector2(85, 85), colSkyBlue, colDeepNavy, 12, 2, false);
            UIKit.Card(previewBox.transform, "G2", new Vector2(0.7f, 0.7f), Vector2.zero, new Vector2(85, 85), colSteelBlue, colDeepNavy, 12, 2, false);
            UIKit.Card(previewBox.transform, "G3", new Vector2(0.3f, 0.3f), Vector2.zero, new Vector2(85, 85), colSteelBlue, colDeepNavy, 12, 2, false);
            UIKit.Card(previewBox.transform, "G4", new Vector2(0.7f, 0.3f), Vector2.zero, new Vector2(85, 85), colSkyBlue, colDeepNavy, 12, 2, false);
        }
        else if (layoutType == 3)
        {
            Image stripBox = UIKit.Card(previewBox.transform, "StripFrame", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 190),
                Color.white, colDeepNavy, 15, 3, false).GetComponent<Image>();
            UIKit.Card(stripBox.transform, "S1", new Vector2(0.5f, 0.80f), Vector2.zero, new Vector2(80, 50), colSkyBlue, colDeepNavy, 8, 2, false);
            UIKit.Card(stripBox.transform, "S2", new Vector2(0.5f, 0.50f), Vector2.zero, new Vector2(80, 50), colSteelBlue, colDeepNavy, 8, 2, false);
            UIKit.Card(stripBox.transform, "S3", new Vector2(0.5f, 0.20f), Vector2.zero, new Vector2(80, 50), colSkyBlue, colDeepNavy, 8, 2, false);
        }
        else if (layoutType == 1)
        {
            UIKit.Card(previewBox.transform, "SingleFrame", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 180),
                colSteelBlue, colDeepNavy, 18, 3, false);
        }
        else
        {
            Image bFrame = UIKit.Card(previewBox.transform, "BoomerangFrame", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 180),
                colSkyBlue, colDeepNavy, 18, 3, false).GetComponent<Image>();
            TextMeshProUGUI loopLbl = UIKit.Label(bFrame.transform, "LOOP", 32f, colDeepNavy, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160, 120), customFontAsset);
            loopLbl.alignment = TextAlignmentOptions.Center;
        }

        UIKit.Card(previewBox.transform, "HandleBar", new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(70, 12),
            colDeepNavy, colDeepNavy, 6, 0, false);

        TextMeshProUGUI lblLayout = UIKit.Label(card.transform, namaLayout, 24f, colDeepNavy,
            new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(240, 45), customFontAsset);
        lblLayout.alignment = TextAlignmentOptions.Center;
    }

    private void HubungkanKeController(
        GameObject hint, GameObject expr, GameObject layout, GameObject liveBar, GameObject prevPanel,
        RectTransform frameCard, Transform photoContainer, Image frameOverlay,
        Transform frameBarContainer, TextMeshProUGUI count, Image flashImg,
        Transform galleryContainer, RawImage editRaw, GameObject editBoxObj,
        TMP_Text slotStatusLbl, TMP_Text zoomLbl, TMP_Text rotLbl,
        Button btnUp, Button btnDown, Button btnLeft, Button btnRight,
        Button btnZoomIn, Button btnZoomOut, Button btnRotLeft, Button btnRotRight,
        Button flipBtn, Button clearBtn, Button resetBtn, Button openFolderBtn)
    {
        if (controller == null) controller = GetComponent<PhotoBoothController>();
        if (controller == null) controller = FindAnyObjectByType<PhotoBoothController>();
        if (controller == null) return;

        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        typeof(PhotoBoothController).GetField("hintPanel", flags)?.SetValue(controller, hint);
        typeof(PhotoBoothController).GetField("expressionPanel", flags)?.SetValue(controller, expr);
        typeof(PhotoBoothController).GetField("layoutChoosePanel", flags)?.SetValue(controller, layout);
        typeof(PhotoBoothController).GetField("controlsBar", flags)?.SetValue(controller, liveBar);
        typeof(PhotoBoothController).GetField("previewPanel", flags)?.SetValue(controller, prevPanel);
        typeof(PhotoBoothController).GetField("frameCardContainer", flags)?.SetValue(controller, frameCard);
        typeof(PhotoBoothController).GetField("photoGridContainer", flags)?.SetValue(controller, photoContainer);
        typeof(PhotoBoothController).GetField("mainFrameOverlayImage", flags)?.SetValue(controller, frameOverlay);
        typeof(PhotoBoothController).GetField("frameSelectorBarContainer", flags)?.SetValue(controller, frameBarContainer);
        typeof(PhotoBoothController).GetField("countdownText", flags)?.SetValue(controller, count);
        typeof(PhotoBoothController).GetField("flashImage", flags)?.SetValue(controller, flashImg);

        controller.photoGalleryContainer = galleryContainer;
        controller.editPreviewRawImage = editRaw;
        controller.editPreviewBoxObj = editBoxObj;
        controller.editSlotStatusLabel = slotStatusLbl;
        controller.zoomInfoLabel = zoomLbl;
        controller.rotateInfoLabel = rotLbl;

        controller.moveUpButton = btnUp;
        controller.moveDownButton = btnDown;
        controller.moveLeftButton = btnLeft;
        controller.moveRightButton = btnRight;
        controller.zoomInButton = btnZoomIn;
        controller.zoomOutButton = btnZoomOut;
        controller.rotateLeftButton = btnRotLeft;
        controller.rotateRightButton = btnRotRight;

        controller.flipButton = flipBtn;
        controller.clearSlotButton = clearBtn;
        controller.resetTransformButton = resetBtn;
        controller.openSavedFolderButton = openFolderBtn;
        controller.BindStudioEditorEvents();
    }
}