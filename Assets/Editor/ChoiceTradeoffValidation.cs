using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ChoiceTradeoffValidation
{
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception("TRADEOFF TEST: " + message); }

    [MenuItem("Tools/Financial Game/Validate Choice Tradeoffs")]
    public static void Run()
    {
        foreach (string policy in new[] { "cheapest", "highest income", "quick jobs only" })
            for (int tier = 0; tier < 3; tier++)
            {
                var holder = new GameObject("Policy fixture") { hideFlags = HideFlags.HideAndDontSave };
                try
                {
                    var g = holder.AddComponent<GameManager>();
                    holder.GetComponent<GameEventSchedule>().events = StrategySystem.CreateSchedule();
                    Check(g.StartRun(tier), "Policy starts");
                    int guard = 0;
                    while (!g.HasEnded)
                    {
                        Check(++guard < 100, "Policy loop");
                        if (!g.HasPendingChoice) { g.EndDay(); continue; }
                        string id = g.CurrentEvent.Id;
                        var available = Enumerable.Range(0, 3).Where(i => g.CanChooseEvent(id, i));
                        int index = RoomValidation.PlannedChoice(g);
                        if (policy == "cheapest")
                            index = available.OrderBy(i => g.CurrentEvent.Options[i].cost)
                                .ThenByDescending(i => g.CurrentEvent.Options[i].mood).First();
                        else if (id.StartsWith("work_")) index = policy == "highest income" ? 1 : 0;
                        else if (id == "birthday" || id == "recharge") index = 2;
                        Check(g.TryChooseEvent(id, index), "Policy choice accepted");
                    }
                    Check(!g.Completed, policy + " must require a different plan");
                    Debug.Log($"CHOICE_POLICY: {policy}, opening {g.OpeningBalance}: ends on Day {g.DayNumber} ({g.FailureReason}).");
                }
                finally { UnityEngine.Object.DestroyImmediate(holder); }
            }
        Check(!GroceryScenario(0) && GroceryScenario(1), "Lower unit cost can lose rent money; smaller pack covers the gap");
        Check(JobScenario(0) && !JobScenario(1), "Today's smaller payment can cover rent; larger pending income cannot");
        for (int tier = 0; tier < 3; tier++)
        {
            var holder = new GameObject("Free activity routes") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var g = holder.AddComponent<GameManager>();
                holder.GetComponent<GameEventSchedule>().events = StrategySystem.CreateSchedule();
                var random = new System.Random(952 + tier); int wins = 0;
                for (int trial = 0; trial < 1000 && wins < 3; trial++)
                {
                    Check(g.StartRun(tier), "Alternative run starts");
                    while (!g.HasEnded)
                    {
                        if (!g.HasPendingChoice) { g.EndDay(); continue; }
                        string id = g.CurrentEvent.Id;
                        var available = Enumerable.Range(0, 3).Where(i => g.CanChooseEvent(id, i)).ToArray();
                        int index = id == "birthday" || id == "recharge" ? 2 : available[random.Next(available.Length)];
                        Check(g.TryChooseEvent(id, index), "Alternative choice accepted");
                    }
                    if (g.Completed) wins++;
                }
                Check(wins >= 3, "Multiple successful plans without paid social recovery");
                Debug.Log($"FREE_RECOVERY_ROUTES: opening {g.OpeningBalance}, {wins} successful alternatives found.");
            }
            finally { UnityEngine.Object.DestroyImmediate(holder); }
        }
        var moodHolder = new GameObject("Recovery fixture") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var mood = moodHolder.AddComponent<MoodSystem>(); mood.ResetRun();
            mood.Apply(-12, true, 3);
            mood.Apply(4, false, 4);
            Check(mood.Preview(-12, true, 5) == -18, "Shopping does not clear repeated work penalty");
            mood.Apply(8, false, 4, true);
            Check(mood.Preview(-12, true, 5) == -12, "Rest clears repeated work penalty");
        }
        finally { UnityEngine.Object.DestroyImmediate(moodHolder); }
        Debug.Log("CHOICE_TRADEOFFS_PASSED: nine fixed-policy runs, grocery/rent tradeoff, immediate/deferred income and explicit recovery.");
    }
    private static bool GroceryScenario(int index)
    {
        var holder = new GameObject("Grocery fixture") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var g = holder.AddComponent<GameManager>(); var rules = holder.GetComponent<FinanceRules>();
            rules.runDays = 4; rules.openingBalances = new[] { 231 }; rules.rent = 130; rules.firstRentDay = 4;
            holder.GetComponent<GameEventSchedule>().events = new() { GameEventSchedule.Defaults()[0] };
            Check(g.StartRun(0) && g.TryBuyFood(1, 2), "Set up $230 and two food days");
            Check(g.TryChooseEvent(g.CurrentEvent.Id, index), "Choose grocery pack");
            while (!g.HasEnded) g.EndDay();
            return g.Completed;
        }
        finally { UnityEngine.Object.DestroyImmediate(holder); }
    }
    private static bool JobScenario(int index)
    {
        var holder = new GameObject("Income fixture") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var g = holder.AddComponent<GameManager>(); var rules = holder.GetComponent<FinanceRules>();
            rules.runDays = 1; rules.openingBalances = new[] { 140 }; rules.rent = 150; rules.firstRentDay = 1;
            var work = GameEventSchedule.Defaults().First(e => e.id == "work_3"); work.day = 1;
            holder.GetComponent<GameEventSchedule>().events = new() { work };
            Check(g.StartRun(0) && g.TryChooseEvent(work.id, index), "Choose job"); g.EndDay();
            return g.Completed;
        }
        finally { UnityEngine.Object.DestroyImmediate(holder); }
    }
    public static void Batch()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainGame.unity");
        RoomUpgrade.Install();
        Run(); RoomValidation.Run(); RoomValidation.CalendarAppointments();
    }
}
