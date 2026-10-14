using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ThirtyDayValidation
{
    private static void Check(bool ok, string message) { if (!ok) throw new Exception("30 DAY TEST: " + message); }
    [MenuItem("Tools/Financial Game/Validate 30 Day Rules")]
    public static void Run()
    {
        var holder = new GameObject("30-day test fixture") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var g = holder.AddComponent<GameManager>(); var cash = holder.GetComponent<EconomyManager>();
            var rng = new System.Random(4319);
            var stats = new List<string>();
            for (int tier = 0; tier < 3; tier++)
            {
                int wins = 0, rent = 0, living = 0, mood = 0;
                string example = "";
                for (int trial = 0; trial < 1000; trial++)
                {
                    Check(g.StartRun(tier), "Start fixture run");
                    var choices = new List<int>();
                    while (!g.HasEnded)
                    {
                        if (g.HasPendingChoice)
                        {
                            string id = g.CurrentEvent.Id;
                            int index = rng.Next(3);
                            if (!g.CanChooseEvent(id, index)) index = 2;
                            int balanceBefore = cash.Balance, moodBefore = g.Mood.Value;
                            g.EndDay(); Check(g.HasPendingChoice && cash.Balance == balanceBefore, "Unresolved events block End Day");
                            Check(!g.TryChooseEvent("stale", index) && !g.TryChooseEvent(id, 3), "Reject invalid event/index");
                            var option = g.CurrentEvent.Options[index];
                            int expectedMood = moodBefore + g.Mood.Preview(option.mood, option.isWork, g.DayNumber);
                            Check(g.TryChooseEvent(id, index), "Choice accepted"); choices.Add(index);
                            Check(cash.Balance == balanceBefore - option.cost + (option.delayDays == 0 ? option.income : 0), "Immediate money only");
                            Check(g.Mood.Value == expectedMood, "Mood preview matches applied change");
                            Check(!g.TryChooseEvent(id, index), "Duplicate choice rejected");
                        }
                        if (!g.HasEnded)
                        {
                            int before = cash.Balance, food = g.FoodDays, cost = g.LivingCostToday;
                            bool rentDue = g.CurrentDate == g.NextRentDate;
                            var tomorrow = g.CurrentDate.AddDays(1);
                            int income = g.PendingPayments.Where(p => p.Date == tomorrow).Sum(p => p.Amount) +
                                (g.Payday == tomorrow ? g.SalaryAmount : 0);
                            int day = g.DayNumber;
                            g.EndDay();
                            if (before < cost) Check(g.FailureReason == "living" && cash.Balance == before && g.FoodDays == food, "Atomic living failure");
                            else if (rentDue && before - cost < g.RentCost)
                                Check(g.FailureReason == "rent" && cash.Balance == before - cost, "Rent fails after essentials");
                            else Check(cash.Balance == before - cost - (rentDue ? g.RentCost : 0) + (day < g.TotalDays ? income : 0), "Daily order, recurring wages, deferred income");
                        }
                        Check(cash.Balance >= 0 && cash.Transactions.Sum(t => t.Amount) == cash.Balance, "Ledger reconciliation");
                    }
                    if (g.Completed)
                    {
                        wins++; Check(g.DayNumber == 30 && g.Decisions.Count == 12 && g.RentsPaid == 4 && g.WagesReceived == 4, "Full 30 day schedule");
                        if (example.Length == 0) example = string.Join(",", choices) + $" -> cash {cash.Balance}, mood {g.Mood.Value}";
                    }
                    else if (g.FailureReason == "rent") rent++;
                    else if (g.FailureReason == "living") living++;
                    else if (g.FailureReason == "mood") mood++;
                }
                stats.Add($"opening={g.Rules.openingBalances[tier]}: random wins={wins}/1000; rent={rent}, living={living}, mood={mood}; example [{example}]");
                Debug.Log(stats[stats.Count - 1]);
                Check(wins > 0 && wins < 950, "Some random paths succeed and some fail");
            }
            Check(g.StartRun(0), "Restart");
            Check(g.Decisions.Count == 0 && g.PendingPayments.Count == 0 && g.Mood.Value == 65 && g.FoodDays == 0, "Clean restart");
            Check(!g.StartRun(1), "Cannot reset active run");
            cash.TrySpend(g.CurrentDate, cash.Balance, "Test expense");
            Check(!g.TryChooseEvent(g.CurrentEvent.Id, 0) && g.Decisions.Count == 0, "Unaffordable choice rejected");
            g.TryChooseEvent(g.CurrentEvent.Id, 2); g.EndDay();
            Check(g.FailureReason == "living", "Zero money fails essentials");
            g.Mood.ResetRun();
            Check(g.Mood.Apply(-18, true, 3) == -18 && g.Mood.Preview(-18, true, 7) == -24, "Repeat-work penalty");
            g.Mood.Apply(12, false, 5);
            Check(g.Mood.Preview(-18, true, 7) == -18, "Recovery clears penalty");
            Check(g.Mood.Apply(100, false, 8) <= 100 && g.Mood.Value == 100, "Mood capped");
            Debug.Log("THIRTY_DAY_VALIDATION_PASSED\n" + string.Join("\n", stats));
        }
        finally { UnityEngine.Object.DestroyImmediate(holder); }
    }
}
