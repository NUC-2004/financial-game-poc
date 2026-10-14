using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ThirtyDayPlayValidation
{
    private static void Check(bool ok, string message) { if (!ok) throw new Exception("30 DAY UI: " + message); }
    // Historical twelve-event UI fixture; use RoomValidation.Live for the current scene.
    public static void Run()
    {
        Check(EditorApplication.isPlaying, "Enter Play mode first.");
        var g = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var ui = UnityEngine.Object.FindFirstObjectByType<GameUI>();
        var cash = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
        Check(g.DayNumber == 1 && g.HasPendingChoice, "Use a fresh run.");
        int[] path = { 0, 1, 2, 1, 1, 1, 0, 2, 1, 1, 1, 2 };
        int index = 0;
        var page = ui.transform.Find("JournalPanel/JournalWindow");
        var end = page.Find("EndDayButton").GetComponent<Button>();
        ui.OpenLedger(); Canvas.ForceUpdateCanvases();
        var planner = ui.transform.Find("LedgerPanel/BookWindow/PlannerScroll").GetComponent<ScrollRect>();
        Check(planner.content.rect.height > planner.viewport.rect.height, "Planner grows and scrolls");
        ui.CloseLedger();
        while (!g.HasEnded)
        {
            ui.OpenJournal();
            CheckText(page);
            if (g.HasPendingChoice)
            {
                Check(!end.interactable, "End Day is blocked before choice");
                var button = page.Find("ChoiceButton" + (path[index++] + 1)).GetComponent<Button>();
                Check(button.interactable, "Planned choice is affordable");
                button.onClick.Invoke();
                Check(!g.HasPendingChoice && end.interactable, "Choice unlocks End Day");
                int balance = cash.Balance;
                ui.CloseJournal(); ui.OpenJournal();
                Check(cash.Balance == balance && !g.HasPendingChoice, "Reopening cannot claim twice");
                CheckText(page);
            }
            end.onClick.Invoke();
            int day = g.DayNumber; end.onClick.Invoke();
            Check(g.DayNumber == day, "Queued double-click cannot skip another day");
        }
        Check(g.Completed && g.Decisions.Count == 12 && g.Mood.Value == 71, "Planned route completes with recovery");
        CheckText(page);
        ui.OpenLedger(); Canvas.ForceUpdateCanvases();
        var history = ui.transform.Find("LedgerPanel/BookWindow/TransactionScrollView").GetComponent<ScrollRect>();
        Check(history.content.rect.height > history.viewport.rect.height, "30-day history scrolls");
        Check(history.verticalNormalizedPosition > .99f, "History opens at newest entries");
        ui.OpenJournal();
        page.Find("NewRunButton").GetComponent<Button>().onClick.Invoke();
        Check(g.DayNumber == 1 && g.Mood.Value == 65 && cash.Transactions.Count == 1, "New Run resets UI/model");
        ui.OpenJournal();
        Debug.Log("THIRTY_DAY_LIVE_UI_PASSED: 30 days, 12 buttons, mood 71, planner/history scrolling, text bounds, panel reopening, double-click protection and clean restart.");
    }
    private static void CheckText(Transform root)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var t in root.GetComponentsInChildren<TMP_Text>())
        {
            t.ForceMeshUpdate();
            Check(!t.isTextOverflowing, t.name + " overflows");
        }
    }
}
