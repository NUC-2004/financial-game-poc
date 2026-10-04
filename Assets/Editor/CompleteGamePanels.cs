using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Editor-only helper: completes existing named objects instead of replacing the scene.
public static class CompleteGamePanels
{
    private static readonly Color Paper = new Color32(241, 230, 204, 255);
    private static readonly Color Ink = new Color32(48, 41, 31, 255);
    private static readonly Color Accent = new Color32(54, 78, 70, 255);
    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

    [MenuItem("Tools/Financial Game/Complete Journal and Ledger")]
    public static void Complete()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainGame.unity")
            throw new InvalidOperationException("Open MainGame before completing its UI.");
        var canvas = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Canvas>(true)).Single();
        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Complete Journal and Ledger");
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = Ensure<CanvasScaler>(canvas.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        Ensure<GraphicRaycaster>(canvas.gameObject);
        var ledger = Child(canvas.transform, "LedgerPanel"); Overlay(ledger);
        var book = Child(ledger, "BookWindow"); Centre(book, 1500, 880); Paint(book, Paper);
        Label(book, "TitleText", "LEDGER & PLANNER", 40, 50, -30, 1100, 70);
        var agenda = Label(book, "AgendaText", "<b>UPCOMING & RECENT</b>\n\n15 Sep / Rent: $500\n\n18 Sep / Wages due: +$700\n\n15 Sep / Friend's birthday\nUpcoming\n\n<b>DAILY COSTS</b>\n\nFood: $20 without stored groceries\nOther essentials: $10\n\nYour completed transactions will\nappear on the right during play.", 24, 50, -130, 520, 680);
        var divider = Child(book, "PageDivider"); TopLeft(divider, 600, -130, 2, 680); Paint(divider, new Color32(192, 176, 146, 255));
        divider.GetComponent<Image>().raycastTarget = false;
        Label(book, "HistoryHeading", "TRANSACTIONS / NEWEST FIRST", 23, 640, -100, 750, 35);

        var scrollRoot = Child(book, "TransactionScrollView");
        // The existing placeholder is a TMP text, not a Scroll View.
        var placeholder = scrollRoot.GetComponent<TextMeshProUGUI>();
        if (placeholder != null) Undo.DestroyObjectImmediate(placeholder);
        TopLeft(scrollRoot, 640, -150, 800, 660);
        Paint(scrollRoot, new Color32(232, 220, 193, 255));
        var scroll = Ensure<ScrollRect>(scrollRoot.gameObject);
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 35;
        var viewport = Child(scrollRoot, "Viewport"); Stretch(viewport, 0, 0, 22, 0);
        Paint(viewport, new Color(1, 1, 1, .01f)); Ensure<RectMask2D>(viewport.gameObject);
        var content = Child(viewport, "Content");
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        var layout = Ensure<VerticalLayoutGroup>(content.gameObject);
        layout.padding = new RectOffset(18, 18, 18, 18);
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperLeft;
        var fit = Ensure<ContentSizeFitter>(content.gameObject);
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var transactions = Label(content, "TransactionsText", "No transactions yet.\n\nStart a run to record income, daily costs\nand rent payments here.", 25, 0, 0, 740, 150);
        var bar = Child(scrollRoot, "Scrollbar Vertical");
        bar.anchorMin = new Vector2(1, 0); bar.anchorMax = Vector2.one;
        bar.pivot = new Vector2(1, .5f); bar.sizeDelta = new Vector2(16, 0); bar.anchoredPosition = Vector2.zero;
        Paint(bar, new Color32(209, 195, 166, 255));
        var area = Child(bar, "Sliding Area"); Stretch(area, 2, 2, 2, 2);
        var handle = Child(area, "Handle"); Stretch(handle); Paint(handle, Accent);
        var scrollbar = Ensure<Scrollbar>(bar.gameObject);
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handle; scrollbar.targetGraphic = handle.GetComponent<Image>();
        scroll.viewport = viewport; scroll.content = content;
        scroll.horizontalScrollbar = null; scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scroll.verticalNormalizedPosition = 1;
        var closeLedger = MakeButton(book, "CloseLedgerButton", "Close", 1320, -25, 150, 60);

        var journal = Child(canvas.transform, "JournalPanel"); Overlay(journal);
        var page = Child(journal, "JournalWindow"); Centre(page, 1100, 900); Paint(page, Paper);
        var title = Label(page, "JournalTitleText", "Day 1 / 11 Sep 2026", 38, 60, -40, 760, 70);
        var body = Label(page, "JournalBodyText", "<b>TODAY'S NOTE</b>\n\nCheck your balance and upcoming commitments before ending the day.\n\nNext rent: $500 on 15 Sep.\nNext wages: +$700 on 18 Sep.\n\n<b>AT THE END OF TODAY</b>\nFood: $20 without stored groceries.\nOther essentials: $10.\n\nLiving costs are paid first, followed by rent if it is due. Open your ledger at any time to review the figures.", 26, 60, -220, 980, 280);
        var finance = Label(page, "JournalFinanceText", "Your balance, food supply and bill dates appear here.", 22, 60, -120, 980, 85);
        var options = new Button[3];
        for (int i = 0; i < options.Length; i++)
        {
            options[i] = MakeButton(page, "ChoiceButton" + (i + 1), "Event choice " + (i + 1), 60, -515 - i * 75, 980, 64);
            options[i].GetComponentInChildren<TMP_Text>().fontSize = 24;
            options[i].GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.MidlineLeft;
            var colors = options[i].colors;
            colors.disabledColor = new Color(.65f, .65f, .65f, 1);
            options[i].colors = colors;
            options[i].gameObject.SetActive(false);
        }
        var line = Child(page, "FooterRule"); TopLeft(line, 60, -750, 980, 2);
        Paint(line, new Color32(192, 176, 146, 255)); line.GetComponent<Image>().raycastTarget = false;
        var footer = Label(page, "FooterNote", "Review your plans before moving on.", 22, 60, -795, 620, 55);
        var end = MakeButton(page, "EndDayButton", "End Day", 810, -770, 230, 80);
        var again = MakeButton(page, "NewRunButton", "New Run", 810, -770, 230, 80);
        again.gameObject.SetActive(false);
        var closeJournal = MakeButton(page, "CloseJournalButton", "Close", 920, -30, 150, 60);

        var systems = scene.GetRootGameObjects().Single(g => g.name == "GameSystems");
        var economy = Ensure<EconomyManager>(systems);
        var game = Ensure<GameManager>(systems);
        var gm = new SerializedObject(game); gm.FindProperty("economy").objectReferenceValue = economy; gm.ApplyModifiedProperties();
        var ui = Ensure<GameUI>(canvas.gameObject);
        var fields = new SerializedObject(ui);
        void Assign(string name, UnityEngine.Object value) => fields.FindProperty(name).objectReferenceValue = value;
        Assign("game", game); Assign("economy", economy);
        Assign("ledgerPanel", ledger.gameObject); Assign("journalPanel", journal.gameObject);
        var main = canvas.transform.Find("GamePanel");
        Assign("dateText", (main.Find("DateText") ?? main.Find("DataText")).GetComponent<TMP_Text>());
        Assign("balanceText", main.Find("BalanceText").GetComponent<TMP_Text>());
        Assign("foodText", main.Find("FoodText").GetComponent<TMP_Text>());
        Assign("statusText", main.Find("StatusText").GetComponent<TMP_Text>());
        Assign("agendaText", agenda); Assign("transactionsText", transactions); Assign("transactionScroll", scroll);
        Assign("journalTitleText", title); Assign("journalBodyText", body);
        Assign("endDayButton", end); Assign("newRunButton", again);
        Assign("journalFinanceText", finance); Assign("journalFooterText", footer);
        var buttonsProperty = fields.FindProperty("choiceButtons"); buttonsProperty.arraySize = 3;
        var labelsProperty = fields.FindProperty("choiceLabels"); labelsProperty.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            buttonsProperty.GetArrayElementAtIndex(i).objectReferenceValue = options[i];
            labelsProperty.GetArrayElementAtIndex(i).objectReferenceValue = options[i].GetComponentInChildren<TMP_Text>(true);
        }
        fields.ApplyModifiedProperties();
        Wire(closeLedger, ui.CloseLedger); Wire(closeJournal, ui.CloseJournal);
        Wire(end, ui.EndDay); Wire(again, ui.StartRandomRun);
        var ledgerButton = main.Find("LedgerButton").GetComponent<Button>();
        var journalButton = main.Find("JournalButton").GetComponent<Button>();
        Wire(ledgerButton, ui.OpenLedger); Wire(journalButton, ui.OpenJournal);
        ledgerButton.GetComponentInChildren<TMP_Text>(true).text = "Ledger";
        journalButton.GetComponentInChildren<TMP_Text>(true).text = "Daily Journal";
        foreach (var text in main.GetComponentsInChildren<TMP_Text>(true)) text.raycastTarget = false;

        var eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
        var old = eventSystem.GetComponent<StandaloneInputModule>();
        if (old != null) Undo.DestroyObjectImmediate(old);
        var input = Ensure<InputSystemUIInputModule>(eventSystem.gameObject);
        input.AssignDefaultActions();
        ledger.gameObject.SetActive(false); journal.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("UI_COMPLETION_OK: Journal and Ledger completed, wired and saved.");
    }

    [MenuItem("Tools/Financial Game/Validate Panels in Play Mode")]
    public static void ValidateInPlayMode()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
        var ui = UnityEngine.Object.FindFirstObjectByType<GameUI>();
        var game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
        void Check(bool ok, string message) { if (!ok) throw new Exception("UI validation: " + message); }
        Check(game.DayNumber == 1 && economy.Transactions.Count == 1, "Start with a fresh run.");
        int opening = economy.Balance;
        var canvas = ui.transform;
        var ledger = canvas.Find("LedgerPanel");
        var journal = canvas.Find("JournalPanel");
        var close = ledger.Find("BookWindow/CloseLedgerButton").GetComponent<Button>();
        var end = journal.Find("JournalWindow/EndDayButton").GetComponent<Button>();
        ui.OpenLedger();
        Check(ledger.gameObject.activeSelf && !journal.gameObject.activeSelf, "Ledger opens exclusively.");
        close.onClick.Invoke();
        Check(!ledger.gameObject.activeSelf && economy.Balance == opening, "Close does not spend money.");
        for (int i = 0; i < 10; i++)
        {
            ui.OpenJournal();
            Check(journal.gameObject.activeSelf && !ledger.gameObject.activeSelf, "Journal opens exclusively.");
            if (game.HasPendingChoice) game.TryChooseEvent(game.CurrentEvent.Id, 2);
            end.onClick.Invoke();
        }
        Check(game.HasEnded && game.Completed, "Ten days finish.");
        Check(economy.Balance == opening - 300 - 500 + 700, "Accounting totals.");
        Check(economy.Transactions.Count == 23, "Opening + 20 daily costs + rent + wages.");
        Check(!end.gameObject.activeSelf, "End Day hidden after completion.");
        ui.OpenLedger();
        Canvas.ForceUpdateCanvases();
        var scroll = ledger.Find("BookWindow/TransactionScrollView").GetComponent<ScrollRect>();
        Check(scroll.content.rect.height > scroll.viewport.rect.height, "Ledger grows and scrolls.");
        Check(scroll.verticalNormalizedPosition > .99f, "Ledger opens at newest transaction.");
        ui.CloseLedger();
        Check(!journal.gameObject.activeSelf, "Closing ledger after ending leaves main screen accessible.");
        ui.OpenJournal();
        journal.Find("JournalWindow/NewRunButton").GetComponent<Button>().onClick.Invoke();
        Check(game.DayNumber == 1 && economy.Transactions.Count == 1, "Restart clears history.");
        economy.TrySpend(game.CurrentDate, economy.Balance - 600, "Validation expense");
        for (int i = 0; i < 5; i++) { ui.OpenJournal(); if (game.HasPendingChoice) game.TryChooseEvent(game.CurrentEvent.Id, 2); end.onClick.Invoke(); }
        Check(game.HasEnded && !game.Completed && economy.Balance == 450, "Rent failure displayed.");
        Check(journal.gameObject.activeSelf, "Failure opens journal.");
        ui.StartRandomRun();
        Check(game.DayNumber == 1 && economy.Transactions.Count == 1, "Clean run restored after validation.");
        Debug.Log("UI_VALIDATION_PASSED: panel buttons, day progression, 23 transactions, scroll overflow, wage/rent totals, failure screen, and restart.");
    }

    private static T Ensure<T>(GameObject go) where T : Component => go.GetComponent<T>() ?? Undo.AddComponent<T>(go);
    private static RectTransform Child(Transform parent, string name)
    {
        var found = parent.Find(name);
        if (found != null) return (RectTransform)found;
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create UI element");
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
    private static void Stretch(RectTransform r, float left = 0, float top = 0, float right = 0, float bottom = 0)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.pivot = new Vector2(.5f, .5f);
        r.offsetMin = new Vector2(left, bottom); r.offsetMax = new Vector2(-right, -top);
    }
    private static void Centre(RectTransform r, float width, float height)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
        r.anchoredPosition = Vector2.zero; r.sizeDelta = new Vector2(width, height);
    }
    private static void TopLeft(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h);
    }
    private static void Paint(RectTransform r, Color color)
    {
        var img = Ensure<Image>(r.gameObject); img.sprite = null; img.color = color; img.raycastTarget = true;
    }
    private static void Overlay(RectTransform r) { Stretch(r); Paint(r, new Color(0, 0, 0, .63f)); }
    private static TextMeshProUGUI Label(Transform parent, string name, string value, int size, float x, float y, float w, float h)
    {
        var r = Child(parent, name); TopLeft(r, x, y, w, h);
        var text = Ensure<TextMeshProUGUI>(r.gameObject);
        text.font = Font; text.text = value; text.fontSize = size; text.enableAutoSizing = false;
        text.color = Ink; text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow; text.raycastTarget = false;
        return text;
    }
    private static Button MakeButton(Transform parent, string name, string caption, float x, float y, float w, float h)
    {
        var r = Child(parent, name); TopLeft(r, x, y, w, h); Paint(r, Accent);
        var button = Ensure<Button>(r.gameObject); button.targetGraphic = r.GetComponent<Image>();
        var oldText = r.GetComponentInChildren<TextMeshProUGUI>(true);
        string textName = oldText != null ? oldText.name : "Text (TMP)";
        var text = Label(r, textName, caption, 26, 0, 0, w, h);
        Stretch(text.rectTransform, 12, 6, 12, 6);
        text.color = Color.white; text.alignment = TextAlignmentOptions.Center;
        return button;
    }
    private static void Wire(Button button, UnityAction action)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            if (button.onClick.GetPersistentTarget(i) == (action.Target as UnityEngine.Object) &&
                button.onClick.GetPersistentMethodName(i) == action.Method.Name) return;
        UnityEventTools.AddPersistentListener(button.onClick, action);
    }
}
