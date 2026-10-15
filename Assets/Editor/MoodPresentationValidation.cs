using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MoodPresentationValidation
{
    private static double started;
    private const string ActiveKey = "MoodPresentationValidation.Active";
    [InitializeOnLoadMethod]
    private static void ResumeAfterReload()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        started = SessionState.GetFloat(ActiveKey + ".Started", 0);
        EditorApplication.update -= WaitForPlay; EditorApplication.update += WaitForPlay;
    }
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception("MOOD DISPLAY: " + message); }
    public static void Batch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainGame.unity"); RoomUpgrade.Install();
        foreach (int value in new[] { 100, 80, 79, 60, 59, 40, 39, 1, 0 })
        {
            int expected = value >= 80 ? 0 : value >= 60 ? 1 : value >= 40 ? 2 : 3;
            Check(MoodSystem.FaceIndexFor(value) == expected, "Threshold " + value);
        }
        foreach (string name in new[] { "Happy", "Calm", "Tired", "Exhausted" })
        {
            string path = "Assets/Art/Mood/" + name + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Check(sprite != null && sprite.rect.width == 64 && sprite.rect.height == 64, "Original 64px art " + name);
            Check(importer.filterMode == FilterMode.Point && !importer.mipmapEnabled &&
                importer.textureCompression == TextureImporterCompression.Uncompressed, "Pixel import " + name);
        }
        started = EditorApplication.timeSinceStartup;
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetFloat(ActiveKey + ".Started", (float)started);
        EditorApplication.update += WaitForPlay;
        EditorApplication.isPlaying = true;
    }
    private static void WaitForPlay()
    {
        if (EditorApplication.timeSinceStartup - started > 90)
        { EditorApplication.update -= WaitForPlay; SessionState.SetBool(ActiveKey, false); Debug.LogError("Mood Play test timed out"); EditorApplication.Exit(1); return; }
        var g = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (!EditorApplication.isPlaying || g == null || !g.HasStarted) return;
        EditorApplication.update -= WaitForPlay;
        SessionState.SetBool(ActiveKey, false);
        try
        {
            var ui = UnityEngine.Object.FindFirstObjectByType<GameUI>();
            var tutorial = ui.GetComponent<OpeningTutorial>();
            Check(tutorial != null && tutorial.panel.activeSelf, "Tutorial appears on new run");
            Canvas.ForceUpdateCanvases();
            foreach (var text in tutorial.panel.GetComponentsInChildren<TMP_Text>())
            { text.ForceMeshUpdate(); Check(!text.isTextOverflowing, "Tutorial text fits: " + text.name); }
            int openingCash = g.GetComponent<EconomyManager>().Balance;
            tutorial.panel.transform.Find("Page/Continue").GetComponent<Button>().onClick.Invoke();
            Check(!tutorial.panel.activeSelf && g.DayNumber == 1 && g.GetComponent<EconomyManager>().Balance == openingCash, "Continue changes no game state");
            var root = ui.transform.Find("GamePanel/IllustratedRoom");
            var portrait = root.Find("MoodCard/Portrait").GetComponent<Image>();
            var label = root.Find("MoodCard/MoodText").GetComponent<TMP_Text>();
            Check(!root.Find("MoodBar").gameObject.activeSelf, "Numeric meter hidden");
            foreach (int value in new[] { 100, 80, 79, 60, 59, 40, 39, 1 })
            {
                g.Mood.ResetRun(); g.Mood.Apply(value - g.Mood.Value, false, g.DayNumber);
                ui.OpenJournal();
                Check(portrait.sprite.name == MoodSystem.StatusFor(value) && label.text == g.Mood.Status, "Actual face at " + value);
                CheckNoNumbers(ui.transform);
            }
            g.Mood.ResetRun(); ui.OpenJournal();
            FinalGameplayValidation.CheckLiveTools(ui, g);
            RoomValidation.Live(); CheckNoNumbers(ui.transform);
            Check(tutorial.panel.activeSelf, "Tutorial returns on restart");
            int restartCash = g.GetComponent<EconomyManager>().Balance;
            tutorial.panel.transform.Find("Page/SkipTutorial").GetComponent<Button>().onClick.Invoke();
            Check(!tutorial.panel.activeSelf && g.DayNumber == 1 && g.GetComponent<EconomyManager>().Balance == restartCash, "Skip tutorial closes without changing run");
            Debug.Log("OPENING_TUTORIAL_PASSED: initial display, text bounds, continue without charges or date changes, restart.");
            Debug.Log("MOOD_PRESENTATION_PASSED: all boundary faces, original pixel art, no numerical mood, hidden meter, complete 30-day UI route and restart.");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
    private static void CheckNoNumbers(Transform root)
    {
        foreach (var text in root.GetComponentsInChildren<TMP_Text>())
            Check(!Regex.IsMatch(text.text, @"(?i)\bmood\s*[:+-]?\s*\d|/100|mood reached 0"), "Numerical mood in " + text.name);
    }
}
