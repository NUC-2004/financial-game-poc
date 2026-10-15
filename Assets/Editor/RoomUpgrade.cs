using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class RoomUpgrade
{
    private static readonly Color Paper = new Color32(242, 231, 207, 255);
    private static readonly Color Ink = new Color32(44, 39, 31, 255);
    private static readonly Color Green = new Color32(48, 74, 63, 255);
    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
    [MenuItem("Tools/Financial Game/Install Room and Strategy Events")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainGame.unity") throw new InvalidOperationException("Open MainGame first.");
        var g = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var strategy = g.GetComponent<StrategySystem>() ?? Undo.AddComponent<StrategySystem>(g.gameObject);
        var schedule = g.GetComponent<GameEventSchedule>();
        // One-time migration. Re-running UI installation preserves edited event data.
        if (!schedule.events.Exists(e => e.id == "market_prep"))
        { Undo.RecordObject(schedule, "Add strategy events"); schedule.events = StrategySystem.CreateSchedule(); EditorUtility.SetDirty(schedule); }
        if (schedule.balanceRevision < 1)
        {
            Undo.RecordObject(schedule, "Update work and recovery choices");
            foreach (var source in GameEventSchedule.Defaults())
            {
                if (!source.id.StartsWith("work_") && source.id != "shop_8" && source.id != "birthday" && source.id != "recharge") continue;
                var target = schedule.events.Find(e => e.id == source.id);
                if (target == null) continue;
                target.choices = source.choices;
                if (source.id == "work_17") target.title = source.title;
            }
            schedule.balanceRevision = 1; EditorUtility.SetDirty(schedule);
        }
        if (schedule.balanceRevision < 2)
        {
            Undo.RecordObject(schedule, "Adjust friend's birthday mood");
            var birthday = schedule.events.Find(e => e.id == "birthday");
            if (birthday != null)
                birthday.choices = GameEventSchedule.Defaults().Find(e => e.id == "birthday").choices;
            schedule.balanceRevision = 2; EditorUtility.SetDirty(schedule);
        }
        if (schedule.balanceRevision < 3)
        {
            Undo.RecordObject(schedule, "Different grocery planning contexts");
            StrategySystem.ApplyGroceryContexts(schedule.events);
            schedule.balanceRevision = 3; EditorUtility.SetDirty(schedule);
        }
        if (schedule.balanceRevision < 4)
        {
            Undo.RecordObject(schedule, "Event-only food shopping and snack offer");
            StrategySystem.ApplySnackOffer(schedule.events);
            StrategySystem.ApplyGroceryContexts(schedule.events);
            schedule.balanceRevision = 4; EditorUtility.SetDirty(schedule);
        }
        var ui = UnityEngine.Object.FindFirstObjectByType<GameUI>();
        var room = ui.GetComponent<RoomInterface>() ?? Undo.AddComponent<RoomInterface>(ui.gameObject);
        Undo.RegisterFullObjectHierarchyUndo(ui.gameObject, "Install illustrated room");
        room.game = g; room.economy = g.GetComponent<EconomyManager>(); room.gameUI = ui;
        var uiFields = new SerializedObject(ui); uiFields.FindProperty("room").objectReferenceValue = room; uiFields.ApplyModifiedProperties();
        var main = ui.transform.Find("GamePanel");
        main.GetComponent<Image>().color = new Color32(24, 25, 25, 255);
        var root = Rect(main, "IllustratedRoom"); Stretch(root);
        var fit = Ensure<AspectRatioFitter>(root.gameObject);
        fit.aspectMode = AspectRatioFitter.AspectMode.None; fit.enabled = false; Stretch(root);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/StudentRoom.png");
        if (!texture) throw new InvalidOperationException("Room texture is missing.");
        var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Art/StudentRoom.png");
        if (importer.maxTextureSize != 2048 || importer.mipmapEnabled)
        { importer.maxTextureSize = 2048; importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport(); }
        var backdrop = Ensure<RawImage>(root.gameObject); backdrop.texture = texture; backdrop.color = Color.white; backdrop.raycastTarget = false;
        root.SetAsFirstSibling();
        var hud = Rect(root, "TopBar"); Anchors(hud, 0, .9f, 1, 1); Paint(hud, new Color(0.04f, .045f, .04f, .83f), false);
        MoveText(main, root, "DateText", .025f, .925f, .27f, .985f, 27);
        MoveText(main, root, "DataText", .025f, .925f, .27f, .985f, 27);
        MoveText(main, root, "BalanceText", .55f, .925f, .72f, .985f, 31);
        MoveText(main, root, "MoodText", .76f, .925f, .98f, .985f, 26);
        var moodBar = main.Find("MoodBar") ?? root.Find("MoodBar");
        moodBar.SetParent(root, false); Anchors((RectTransform)moodBar, .76f, .915f, .965f, .928f);
        moodBar.gameObject.SetActive(false);
        var moodCard = Rect(root, "MoodCard");
        moodCard.anchorMin = moodCard.anchorMax = moodCard.pivot = Vector2.one;
        moodCard.anchoredPosition = new Vector2(-24, -10); moodCard.sizeDelta = new Vector2(160, 174);
        Paint(moodCard, new Color(.04f, .045f, .04f, .94f), false);
        var moodImageRect = Rect(moodCard, "Portrait"); Place(moodImageRect, 16, -8, 128, 128);
        var moodImage = Ensure<Image>(moodImageRect.gameObject);
        moodImage.color = Color.white; moodImage.raycastTarget = false; moodImage.preserveAspect = true;
        uiFields.Update();
        var moodLabel = (TMP_Text)uiFields.FindProperty("moodText").objectReferenceValue;
        moodLabel.transform.SetParent(moodCard, false); Place(moodLabel.rectTransform, 8, -140, 144, 28);
        moodLabel.alignment = TextAlignmentOptions.Center; moodLabel.text = "Calm";
        uiFields.FindProperty("moodPortrait").objectReferenceValue = moodImage;
        var faces = uiFields.FindProperty("moodFaces"); faces.arraySize = 4;
        string[] faceNames = { "Happy", "Calm", "Tired", "Exhausted" };
        for (int i = 0; i < faceNames.Length; i++)
        {
            string path = "Assets/Art/Mood/" + faceNames[i] + ".png";
            var moodImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (moodImporter == null) throw new InvalidOperationException("Missing mood artwork: " + path);
            if (moodImporter.textureType != TextureImporterType.Sprite || moodImporter.filterMode != FilterMode.Point ||
                moodImporter.mipmapEnabled || moodImporter.textureCompression != TextureImporterCompression.Uncompressed ||
                !moodImporter.alphaIsTransparency || moodImporter.spriteImportMode != SpriteImportMode.Single)
            {
                moodImporter.textureType = TextureImporterType.Sprite; moodImporter.spriteImportMode = SpriteImportMode.Single;
                moodImporter.filterMode = FilterMode.Point; moodImporter.mipmapEnabled = false;
                moodImporter.textureCompression = TextureImporterCompression.Uncompressed;
                moodImporter.alphaIsTransparency = true; moodImporter.spritePixelsPerUnit = 64;
                moodImporter.SaveAndReimport();
            }
            faces.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        uiFields.ApplyModifiedProperties();
        moodImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Mood/Calm.png");
        var food = main.Find("FoodText"); if (food) food.gameObject.SetActive(false);
        foreach (string old in new[] { "LedgerButton", "JournalButton" })
        { var child = main.Find(old); if (child) child.gameObject.SetActive(false); }
        var bottom = Rect(root, "BottomStrip"); Anchors(bottom, 0, 0, 1, .08f); Paint(bottom, new Color(.04f, .045f, .04f, .8f), false);
        MoveText(main, root, "StatusText", .03f, .015f, .69f, .065f, 24);
        room.nextBillText = Text(root, "NextBill", "RENT / OPEN CALENDAR", 24, Color.white);
        Anchors(room.nextBillText.rectTransform, .285f, .93f, .54f, .975f);
        var hint = Text(root, "RoomHint", "CLICK OBJECTS TO EXPLORE", 20, new Color32(225, 207, 167, 255));
        Anchors(hint.rectTransform, .73f, .018f, .98f, .06f);
        var fridge = Hotspot(root, "FridgeHotspot", "FRIDGE", .115f, .25f, .255f, .79f, room.OpenFridge);
        room.fridgeLabel = fridge.GetComponentInChildren<TMP_Text>();
        Hotspot(root, "CalendarHotspot", "CALENDAR", .485f, .60f, .61f, .78f, room.OpenCalendar);
        Hotspot(root, "LedgerHotspot", "MONEY TRACKER", .52f, .475f, .585f, .535f, ui.OpenLedger);
        var journal = Hotspot(root, "JournalHotspot", "DAILY JOURNAL", .79f, .21f, .94f, .33f, ui.OpenJournal);
        room.journalLabel = journal.GetComponentInChildren<TMP_Text>();

        var overlay = Rect(ui.transform, "CalendarPanel"); Stretch(overlay); Paint(overlay, new Color(0, 0, 0, .75f));
        room.calendarPanel = overlay.gameObject;
        var page = Rect(overlay, "CalendarWindow"); Centre(page, 1500, 910); Paint(page, Paper);
        room.calendarTitle = Label(page, "MonthTitle", "SEPTEMBER 2026", 36, 230, -30, 900, 70);
        Wire(Button(page, "PreviousMonth", "<", 50, -35, 70, 55), room.PreviousMonth);
        Wire(Button(page, "NextMonth", ">", 135, -35, 70, 55), room.NextMonth);
        Wire(Button(page, "Close", "Close", 1310, -30, 140, 60), room.ClosePanels);
        Label(page, "Legend", "GREEN: TODAY    GOLD: SELECTED    Click a date for details", 21, 50, -100, 1400, 40);
        string[] names = { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };
        for (int i = 0; i < 7; i++) Label(page, "Weekday" + i, names[i], 22, 50 + i * 139, -150, 130, 40);
        room.calendarDays = new Button[42]; room.calendarLabels = new TMP_Text[42];
        for (int i = 0; i < 42; i++)
        {
            var b = Button(page, "Day" + i, "", 50 + (i % 7) * 139, -195 - (i / 7) * 105, 131, 97);
            var t = b.GetComponentInChildren<TMP_Text>(); t.color = Ink; t.fontSize = 23;
            t.alignment = TextAlignmentOptions.TopLeft;
            room.calendarDays[i] = b; room.calendarLabels[i] = t;
        }
        room.calendarDetail = Label(page, "DateDetails", "Select a date.", 24, 1060, -195, 385, 620);
        Label(page, "Footer", "Income arrives in the morning. Living costs and rent are paid at End Day.", 21, 50, -845, 1350, 40);

        var info = Rect(ui.transform, "RoomInfoPanel"); Stretch(info); Paint(info, new Color(0, 0, 0, .75f)); room.infoPanel = info.gameObject;
        var sheet = Rect(info, "InfoWindow"); Centre(sheet, 1120, 830); Paint(sheet, Paper);
        room.infoTitle = Label(sheet, "Title", "FRIDGE", 32, 50, -35, 830, 70);
        Wire(Button(sheet, "Close", "Close", 935, -30, 135, 60), room.ClosePanels);
        var scrollRoot = Rect(sheet, "InfoScroll"); Place(scrollRoot, 50, -135, 1020, 550); Paint(scrollRoot, new Color(1, 1, 1, .01f));
        var viewport = Rect(scrollRoot, "Viewport"); Stretch(viewport); Ensure<RectMask2D>(viewport.gameObject);
        var content = Rect(viewport, "Content"); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1); content.anchoredPosition = content.sizeDelta = Vector2.zero;
        var layout = Ensure<VerticalLayoutGroup>(content.gameObject); layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false; layout.padding = new RectOffset(8, 8, 8, 8);
        Ensure<ContentSizeFitter>(content.gameObject).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        room.infoBody = Text(content, "Body", "", 28, Ink);
        room.infoScroll = Ensure<ScrollRect>(scrollRoot.gameObject); room.infoScroll.viewport = viewport; room.infoScroll.content = content;
        room.infoScroll.horizontal = false; room.infoScroll.scrollSensitivity = 35; room.infoScroll.movementType = ScrollRect.MovementType.Clamped;
        room.returnJournalButton = Button(sheet, "BackToJournal", "Back to journal", 750, -720, 320, 65);
        Wire(room.returnJournalButton, room.ReturnToJournal);
        Wire(Button(sheet, "CalendarShortcut", "Calendar", 50, -720, 240, 65), room.OpenCalendar);
        foreach (string name in new[] { "GroceryShopButton", "ShopTrackerButton", "BuySmallButton", "BuyBulkButton" })
        {
            var old = sheet.Find(name);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        room.groceryShopButton = room.shopTrackerButton = room.smallGroceryButton = room.bulkGroceryButton = null;

        var book = ui.transform.Find("LedgerPanel/BookWindow");
        room.rentPlanText = Label(book, "RentPlanSummary", "TOTAL / RENT PLAN / UNALLOCATED", 24, 45, -125, 540, 155);
        room.reserveRentButton = Button(book, "ReserveRentButton", "Reserve for rent", 45, -300, 255, 65);
        Wire(room.reserveRentButton, room.ReserveRent);
        room.releaseRentButton = Button(book, "ReleaseRentButton", "Release reserve", 315, -300, 255, 65);
        Wire(room.releaseRentButton, room.ReleaseRent);
        var planner = book.Find("PlannerScroll") as RectTransform;
        if (planner != null) Place(planner, 45, -390, 540, 405);
        Wire(Button(book, "BackToJournal", "Back to journal", 45, -810, 300, 45), ui.OpenJournal);

        var journalPage = ui.transform.Find("JournalPanel/JournalWindow");
        Wire(Button(journalPage, "CalendarShortcut", "Calendar", 60, -770, 145, 65), room.OpenCalendar);
        Wire(Button(journalPage, "FridgeShortcut", "Fridge", 215, -770, 120, 65), room.OpenFridge);
        Wire(Button(journalPage, "MoneyTrackerShortcut", "Money Tracker", 345, -770, 210, 65), ui.OpenLedger);
        room.tipButton = Button(journalPage, "TipButton", "Why it matters", 565, -770, 200, 65); Wire(room.tipButton, room.OpenTip);
        Place((RectTransform)journalPage.Find("FooterNote"), 60, -855, 970, 35);
        journalPage.Find("FooterNote").GetComponent<TMP_Text>().fontSize = 20;
        ui.ApplyMoneyTrackerLabels(); room.ApplyPresentation();
        overlay.gameObject.SetActive(false); info.gameObject.SetActive(false);


        var tutorial = ui.GetComponent<OpeningTutorial>() ?? Undo.AddComponent<OpeningTutorial>(ui.gameObject);
        tutorial.game = g;
        var welcome = Rect(ui.transform, "OpeningTutorialPanel"); Stretch(welcome);
        Paint(welcome, new Color(0, 0, 0, .82f)); welcome.SetAsLastSibling();
        tutorial.panel = welcome.gameObject;
        var welcomePage = Rect(welcome, "Page"); Centre(welcomePage, 1200, 940); Paint(welcomePage, Paper);
        Label(welcomePage, "Title", "WELCOME TO YOUR NEW PLACE", 38, 60, -40, 820, 65);
        Label(welcomePage, "Intro",
            "You're a university student living on your own abroad. Your family sends you a weekly allowance. Your landlord collects weekly rent. Neither has agreed to coordinate.\n\n" +
            "Your goal: <b>make it through 30 days</b>, pay your bills, and look after yourself.", 28, 60, -125, 1080, 210);
        Label(welcomePage, "Tools",
            "<b>Fridge:</b> Check your food supply. Stored meals cover your daily food costs, but other essentials still cost money.\n\n" +
            "<b>Calendar:</b> Check important dates, including rent, allowance payments, and your friend's birthday.\n\n" +
            "<b>Money Tracker:</b> Review every payment, check upcoming bills, and set aside money for rent.\n\n" +
            "<b>Journal:</b> Read today's events, make choices, and end the day. Some choices affect you later.", 27, 60, -350, 1080, 360);
        Label(welcomePage, "Reminder",
            "Keep an eye on your cash, food, and how you're feeling. Tomorrow's money cannot pay today's rent.", 26, 60, -740, 1080, 80);
        Wire(Button(welcomePage, "Continue", "LET'S TRY THIS", 400, -840, 400, 65), tutorial.Continue);
        Wire(Button(welcomePage, "SkipTutorial", "Skip tutorial", 930, -45, 220, 55), tutorial.Continue);
        welcome.gameObject.SetActive(false); EditorUtility.SetDirty(tutorial);

        EditorUtility.SetDirty(room); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("ROOM_INSTALL_OK: illustrated room, object hotspots, calendar, fridge, strategy events and optional explanations.");
    }
    public static void BatchInstall()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainGame.unity"); Install(); RoomValidation.Run();
    }
    private static Button Hotspot(Transform parent, string name, string caption, float x0, float y0, float x1, float y1, UnityAction action)
    {
        var r = Rect(parent, name); Anchors(r, x0, y0, x1, y1); Paint(r, Color.white);
        var b = Ensure<Button>(r.gameObject); b.targetGraphic = r.GetComponent<Image>();
        var colors = b.colors; colors.normalColor = new Color(1, 1, 1, 0); colors.highlightedColor = new Color(1, .92f, .7f, .2f);
        colors.pressedColor = new Color(1, .92f, .7f, .35f); colors.selectedColor = new Color(1, 1, 1, 0); b.colors = colors;
        var tag = Rect(r, "Tag"); tag.anchorMin = tag.anchorMax = new Vector2(.5f, 0); tag.pivot = new Vector2(.5f, 1);
        tag.anchoredPosition = new Vector2(0, -6); tag.sizeDelta = new Vector2(250, 42);
        // The caption also routes clicks to the parent hotspot.
        Paint(tag, new Color(.1f, .13f, .11f, .92f), true);
        var label = Text(tag, "Label", caption, 23, Paper); Stretch(label.rectTransform); label.alignment = TextAlignmentOptions.Center;
        Wire(b, action); return b;
    }
    private static void MoveText(Transform oldParent, Transform parent, string name, float x0, float y0, float x1, float y1, int size)
    {
        var t = oldParent.Find(name) ?? parent.Find(name); if (!t) return;
        t.SetParent(parent, false); Anchors((RectTransform)t, x0, y0, x1, y1);
        var label = t.GetComponent<TMP_Text>(); label.fontSize = size; label.enableAutoSizing = true; label.fontSizeMin = 18; label.fontSizeMax = size;
        label.color = Color.white; label.alignment = TextAlignmentOptions.MidlineLeft; label.raycastTarget = false;
    }
    private static T Ensure<T>(GameObject go) where T : Component => go.GetComponent<T>() ?? Undo.AddComponent<T>(go);
    private static RectTransform Rect(Transform parent, string name)
    {
        var found = parent.Find(name); if (found) return (RectTransform)found;
        var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; go.transform.SetParent(parent, false); Undo.RegisterCreatedObjectUndo(go, "Room UI"); return (RectTransform)go.transform;
    }
    private static void Anchors(RectTransform r, float x0, float y0, float x1, float y1)
    { r.anchorMin = new Vector2(x0, y0); r.anchorMax = new Vector2(x1, y1); r.pivot = new Vector2(.5f, .5f); r.offsetMin = r.offsetMax = Vector2.zero; }
    private static void Stretch(RectTransform r) => Anchors(r, 0, 0, 1, 1);
    private static void Place(RectTransform r, float x, float y, float w, float h)
    { r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h); }
    private static void Centre(RectTransform r, float w, float h)
    { r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); r.anchoredPosition = Vector2.zero; r.sizeDelta = new Vector2(w, h); }
    private static void Paint(RectTransform r, Color c, bool raycast = true)
    { var img = Ensure<Image>(r.gameObject); img.color = c; img.raycastTarget = raycast; }
    private static TMP_Text Text(Transform parent, string name, string value, int size, Color colour)
    {
        var t = Ensure<TextMeshProUGUI>(Rect(parent, name).gameObject); t.font = Font; t.text = value; t.fontSize = size;
        t.color = colour; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; return t;
    }
    private static TMP_Text Label(Transform parent, string name, string value, int size, float x, float y, float w, float h)
    { var t = Text(parent, name, value, size, Ink); Place(t.rectTransform, x, y, w, h); return t; }
    private static Button Button(Transform parent, string name, string value, float x, float y, float w, float h)
    {
        var r = Rect(parent, name); Place(r, x, y, w, h); Paint(r, Green);
        var b = Ensure<Button>(r.gameObject); b.targetGraphic = r.GetComponent<Image>();
        var t = Text(r, "Label", value, 24, Color.white); Stretch(t.rectTransform);
        t.rectTransform.offsetMin = new Vector2(8, 5); t.rectTransform.offsetMax = new Vector2(-8, -5); t.alignment = TextAlignmentOptions.Center;
        return b;
    }
    private static void Wire(Button b, UnityAction a)
    {
        for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(b.onClick, i);
        UnityEventTools.AddPersistentListener(b.onClick, a);
    }
}
