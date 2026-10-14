using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ThirtyDayUpgrade
{
    [MenuItem("Tools/Financial Game/Install 30 Day Components")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainGame.unity") throw new InvalidOperationException("Open MainGame first.");
        if (UnityEngine.Object.FindFirstObjectByType<RoomInterface>() != null)
        { RoomUpgrade.Install(); return; }
        var game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        Undo.RegisterFullObjectHierarchyUndo(game.gameObject, "Install 30 day components");
        var rules = Ensure<FinanceRules>(game.gameObject);
        var mood = Ensure<MoodSystem>(game.gameObject);
        var schedule = Ensure<GameEventSchedule>(game.gameObject);
        var fields = new SerializedObject(game);
        fields.FindProperty("rules").objectReferenceValue = rules;
        fields.FindProperty("mood").objectReferenceValue = mood;
        fields.FindProperty("schedule").objectReferenceValue = schedule;
        fields.ApplyModifiedProperties();
        CompleteGamePanels.Complete();
        var ui = UnityEngine.Object.FindFirstObjectByType<GameUI>();
        var main = ui.transform.Find("GamePanel");
        var font = main.Find("FoodText").GetComponent<TMP_Text>().font;
        var text = GetRect(main, "MoodText");
        Place(text, 1430, -310, 440, 88);
        text.anchorMin = text.anchorMax = text.pivot = Vector2.one;
        text.anchoredPosition = new Vector2(-50, -300); text.sizeDelta = new Vector2(400, 88);
        var label = Ensure<TextMeshProUGUI>(text.gameObject);
        label.font = font; label.fontSize = 28; label.color = Color.white;
        label.text = "MOOD 65/100"; label.raycastTarget = false;
        var bar = GetRect(main, "MoodBar"); Place(bar, 1430, -410, 360, 18);
        bar.anchorMin = bar.anchorMax = bar.pivot = Vector2.one;
        bar.anchoredPosition = new Vector2(-50, -405); bar.sizeDelta = new Vector2(400, 18);
        Ensure<Image>(bar.gameObject).color = new Color32(45, 50, 51, 255);
        var fill = GetRect(bar, "Fill");
        fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        Ensure<Image>(fill.gameObject).color = new Color32(117, 178, 143, 255);
        var slider = Ensure<Slider>(bar.gameObject);
        slider.minValue = 0; slider.maxValue = 100; slider.wholeNumbers = true;
        slider.value = 65; slider.interactable = false; slider.fillRect = fill;
        foreach (var image in bar.GetComponentsInChildren<Image>()) image.raycastTarget = false;
        var uiFields = new SerializedObject(ui);
        uiFields.FindProperty("moodText").objectReferenceValue = label;
        uiFields.FindProperty("moodBar").objectReferenceValue = slider;
        uiFields.ApplyModifiedProperties();

        var book = ui.transform.Find("LedgerPanel/BookWindow");
        var agenda = book.Find("AgendaText") ?? book.Find("PlannerScroll/Viewport/Content/AgendaText");
        var root = GetRect(book, "PlannerScroll"); Place(root, 45, -125, 540, 675);
        var bg = Ensure<Image>(root.gameObject); bg.color = new Color32(232, 220, 193, 255);
        var viewport = GetRect(root, "Viewport");
        viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
        viewport.offsetMin = viewport.offsetMax = Vector2.zero;
        Ensure<RectMask2D>(viewport.gameObject);
        var content = GetRect(viewport, "Content");
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1); content.anchoredPosition = content.sizeDelta = Vector2.zero;
        var layout = Ensure<VerticalLayoutGroup>(content.gameObject);
        layout.padding = new RectOffset(15, 15, 15, 15);
        layout.childControlHeight = layout.childControlWidth = true;
        layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
        var fitter = Ensure<ContentSizeFitter>(content.gameObject); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        // CompleteGamePanels creates a root-level agenda; migrate it into the scroll view.
        var previous = content.Find("AgendaText");
        if (previous != null && previous != agenda) Undo.DestroyObjectImmediate(previous.gameObject);
        agenda.SetParent(content, false);
        var agendaLabel = agenda.GetComponent<TMP_Text>(); agendaLabel.fontSize = 23;
        var scroll = Ensure<ScrollRect>(root.gameObject);
        scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
        scroll.vertical = true; scroll.scrollSensitivity = 35; scroll.movementType = ScrollRect.MovementType.Clamped;
        foreach (var t in ui.transform.Find("JournalPanel/JournalWindow").GetComponentsInChildren<TMP_Text>(true))
        {
            t.enableAutoSizing = true; t.fontSizeMin = t.name == "JournalTitleText" ? 28 : 20;
            t.fontSizeMax = t.name == "JournalTitleText" ? 38 : t.name == "JournalBodyText" ? 26 : 24;
            t.overflowMode = TextOverflowModes.Overflow;
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("THIRTY_DAY_INSTALL_OK: components, mood HUD, scrollable planner and existing buttons saved.");
    }
    public static void BatchInstallAndTest()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainGame.unity");
        var rules = UnityEngine.Object.FindFirstObjectByType<FinanceRules>();
        if (rules != null && rules.wages == 460 && rules.openingBalances[0] == 680)
        { rules.wages = 540; rules.openingBalances = new[] { 740, 780, 820 }; }
        Install();
        ThirtyDayValidation.Run();
    }
    private static T Ensure<T>(GameObject go) where T : Component => go.GetComponent<T>() ?? Undo.AddComponent<T>(go);
    private static RectTransform GetRect(Transform parent, string name)
    {
        var found = parent.Find(name); if (found) return (RectTransform)found;
        var go = new GameObject(name, typeof(RectTransform)); Undo.RegisterCreatedObjectUndo(go, "Create mood/planner UI");
        go.layer = LayerMask.NameToLayer("UI"); go.transform.SetParent(parent, false); return (RectTransform)go.transform;
    }
    private static void Place(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h);
    }
}
