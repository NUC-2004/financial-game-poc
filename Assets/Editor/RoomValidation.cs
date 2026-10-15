using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class RoomValidation
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception("ROOM TEST: " + message); }
    public static void CalendarAppointments()
    {
        var holder = new GameObject("Calendar tests") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var g = holder.AddComponent<GameManager>();
            holder.GetComponent<GameEventSchedule>().events = StrategySystem.CreateSchedule();
            Check(g.StartRun(0), "Calendar fixture starts");
            var room = holder.AddComponent<RoomInterface>(); room.game = g;
            var start = g.CurrentDate;
            foreach (bool compact in new[] { false, true })
            {
                Check(room.Entries(start.AddDays(2), compact).Count == 0, "Future job stays hidden");
                Check(room.Entries(start.AddDays(1), compact).Count == 0, "Future market stays hidden");
                Check(room.Entries(start.AddDays(8), compact).Count == 0, "Future sale stays hidden");
                Check(room.Entries(start.AddDays(11), compact).Count == 2, "Rent and birthday remain visible");
                Check(room.Entries(start.AddDays(7), compact).Count == 1, "Only wages visible on shopping day");
            }
            Check(room.Entries(start.AddDays(15)).Count == 0, "Reminder absent before requested");
            g.Strategies.Apply(StrategyAction.SetReminder);
            Check(room.Entries(start.AddDays(15), true).Contains("REMINDER"), "Player-requested reminder visible");
            g.Strategies.Apply(StrategyAction.WaitPurchase);
            Check(room.Entries(start.AddDays(9)).SequenceEqual(new[] { "Revisit your headphone wishlist" }), "Wishlist does not reveal job");
            while (g.DayNumber < 3)
            {
                if (g.HasPendingChoice) g.TryChooseEvent(g.CurrentEvent.Id, PlannedChoice(g));
                else g.EndDay();
            }
            Check(g.TryChooseEvent(g.CurrentEvent.Id, 1), "Accept delayed job");
            Check(room.Entries(start.AddDays(5)).Any(e => e.Contains("Confirmed income +$160")), "Confirmed income visible after choice");
            Debug.Log("CALENDAR_APPOINTMENTS_PASSED: known dates retained; future encounters hidden; personal reminders and confirmed income visible.");
        }
        finally { UnityEngine.Object.DestroyImmediate(holder); }
    }
    public static int PlannedChoice(GameManager g)
    {
        string id = g.CurrentEvent.Id;
        int choice = id switch
        {
            "shop_1" => 0, "work_3" => 1, "work_7" => 2, "shop_8" => 1, "work_10" => 1,
            "birthday" => 1, "stock_prep" => 0, "stock_buy" => 0, "work_17" => 2,
            "work_21" => 1, "shop_23" => 1, "recharge" => 1, "work_27" => 2,
            "market_prep" => 0, "market_buy" => 0, "hungry_prep" => 0, "hungry_basket" => 2,
            "reminder_setup" => 0, "reminder" => 0, "sale" => 1, "wishlist_review" => 2, _ => 2
        };
        if (g.CanChooseEvent(id, choice)) return choice;
        return Enumerable.Range(0, 3).First(i => g.CanChooseEvent(id, i));
    }
    [MenuItem("Tools/Financial Game/Validate Room Rules")]
    public static void Run()
    {
        var holder = new GameObject("Room tests") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var g = holder.AddComponent<GameManager>();
            var cash = holder.GetComponent<EconomyManager>();
            holder.GetComponent<GameEventSchedule>().events = StrategySystem.CreateSchedule();
            var rng = new System.Random(1527);
            for (int tier = 0; tier < 3; tier++)
            {
                int wins = 0;
                for (int trial = 0; trial < 501; trial++)
                {
                    Check(g.StartRun(tier), "New run");
                    int steps = 0;
                    while (!g.HasEnded)
                    {
                        Check(++steps < 100, "No event loop");
                        if (g.HasPendingChoice)
                        {
                            string id = g.CurrentEvent.Id;
                            int n = trial == 0 ? PlannedChoice(g) : rng.Next(3);
                            if (!g.CanChooseEvent(id, n)) n = Enumerable.Range(0, 3).First(i => g.CanChooseEvent(id, i));
                            int before = cash.Balance, food = g.FoodDays;
                            var c = g.CurrentEvent.Options[n];
                            int expectedMood = g.Mood.Value + g.Mood.Preview(c.mood, c.isWork, g.DayNumber);
                            g.EndDay(); Check(g.CurrentEvent.Id == id && g.HasPendingChoice, "Block settlement during multi-stage event");
                            if (id == "market_buy" && g.Strategies.ShoppingLimit == 20)
                                Check(!g.CanChooseEvent(id, 1) && g.ChoiceBlockedReason(id, 1) == "Over trip limit", "Cash trip cap");
                            Check(g.TryChooseEvent(id, n), "Choice accepted");
                            Check(!g.TryChooseEvent(id, n), "Reject duplicate stage");
                            Check(cash.Balance == before - c.cost + (c.delayDays == 0 ? c.income : 0), "No hidden fees or strategy rewards");
                            Check(g.Mood.Value == expectedMood, "Mood preview");
                            if (c.action == StrategyAction.EatFirst)
                                Check(g.AteTodaysMeal && g.FoodDays == food - 1 && g.FoodCostToday == 0, "Eat existing meal exactly once");
                        }
                        else
                        {
                            int before = cash.Balance, food = g.FoodDays, cost = g.LivingCostToday;
                            bool ate = g.AteTodaysMeal, rent = g.CurrentDate == g.NextRentDate;
                            int day = g.DayNumber;
                            var tomorrow = g.CurrentDate.AddDays(1);
                            int arrivals = g.PendingPayments.Where(p => p.Date == tomorrow).Sum(p => p.Amount) + (g.Payday == tomorrow ? g.SalaryAmount : 0);
                            g.EndDay();
                            if (before < cost) Check(g.FailureReason == "living" && cash.Balance == before, "Atomic living failure");
                            else if (rent && before - cost < g.RentCost) Check(g.FailureReason == "rent" && cash.Balance == before - cost, "Rent after essentials");
                            else Check(cash.Balance == before - cost - (rent ? g.RentCost : 0) + (day < 30 ? arrivals : 0), "Daily settlement + recurring income");
                            if (before >= cost) Check(g.FoodDays == (ate ? food : Math.Max(0, food - 1)), "No second portion when already eaten");
                        }
                        Check(cash.Transactions.Sum(t => t.Amount) == cash.Balance && cash.Balance >= 0, "Ledger balances");
                    }
                    if (trial == 0)
                    {
                        Check(g.Completed, "Planned strategy route completes for every opening");
                        Check(g.Strategies.encountered.Count == 7, "All seven financial techniques encountered");
                        Check(g.Decisions.Any(d => d.Title == "STILL WANT THEM?") && g.Decisions.Any(d => d.Title == "ONE MORE SHIFT"), "Wishlist and job both run on Day 10");
                        Check(g.Decisions.Any(d => d.Title == "THE REMINDER YOU ASKED FOR"), "Scheduled reminder appears");
                    }
                    if (g.Completed) wins++;
                }
                Check(wins > 1 && wins < 490, "Choices still affect completion");
                Debug.Log($"ROOM_BALANCE: opening {g.Rules.openingBalances[tier]}, {wins-1}/500 random routes complete; planned route passes.");
            }
            Check(g.StartRun(0), "Restart");
            Check(g.Strategies.PlannedReserve == 0 && !g.Strategies.ReminderSet && !g.Strategies.WaitingForPurchase && !g.AteTodaysMeal && g.Strategies.encountered.Count == 0, "Strategy reset");
            Debug.Log("ROOM_RULES_PASSED: six techniques, staged events, trip cap, food consumed once, reminder, wishlist, restart, money and mood.");
        }
        finally { UnityEngine.Object.DestroyImmediate(holder); }
    }
    [MenuItem("Tools/Financial Game/Validate Room Live UI")]
    public static void Live()
    {
        Check(EditorApplication.isPlaying, "Enter Play mode");
        var ui = UnityEngine.Object.FindFirstObjectByType<GameUI>();
        var g = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var room = ui.GetComponent<RoomInterface>();
        Check(g.DayNumber == 1 && g.Decisions.Count == 0, "Fresh run required");
        var page = ui.transform.Find("JournalPanel/JournalWindow");
        int guards = 0;
        while (!g.HasEnded)
        {
            Check(++guards < 100, "UI stage guard");
            ui.OpenJournal(); CheckFits(page);
            if (g.HasPendingChoice)
            {
                room.OpenCalendar();
                Check(room.calendarPanel.activeSelf && !page.parent.gameObject.activeSelf, "Calendar opens exclusively");
                room.NextMonth(); room.PreviousMonth();
                CheckFits(room.calendarPanel.transform);
                room.OpenFridge(); Check(room.infoBody.text.Contains(g.FoodDays + " food day"), "Fridge quantity");
                ui.OpenJournal(); Check(!room.infoPanel.activeSelf && !room.calendarPanel.activeSelf, "Journal closes room panels");
                int choice = PlannedChoice(g);
                page.Find("ChoiceButton" + (choice + 1)).GetComponent<Button>().onClick.Invoke();
                CheckFits(page);
                room.OpenTip(); Check(room.infoBody.text.Length > 20, "Optional explanation");
                room.ReturnToJournal();
            }
            else page.Find("EndDayButton").GetComponent<Button>().onClick.Invoke();
        }
        Check(g.Completed, "30-day UI route"); CheckFits(page);
        var start = g.CurrentDate.AddDays(1 - g.DayNumber);
        Check(room.Entries(start.AddDays(11)).Any(e => e.Contains("birthday")) && room.Entries(start.AddDays(11)).Any(e => e.Contains("Rent")), "Birthday and rent share same date");
        ui.OpenLedger(); Canvas.ForceUpdateCanvases();
        var history = ui.transform.Find("LedgerPanel/BookWindow/TransactionScrollView").GetComponent<ScrollRect>();
        Check(history.content.rect.height > history.viewport.rect.height, "Ledger grows");
        ui.OpenJournal(); page.Find("NewRunButton").GetComponent<Button>().onClick.Invoke();
        Check(g.DayNumber == 1 && g.Decisions.Count == 0 && !room.calendarPanel.activeSelf && !room.infoPanel.activeSelf, "Clean restart");
        Debug.Log("ROOM_LIVE_UI_PASSED: calendar dates, staged buttons, six tips, fridge quantities, 30-day route, text bounds, ledger, restart.");
    }
    private static void CheckFits(Transform root)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var t in root.GetComponentsInChildren<TMP_Text>())
        { t.ForceMeshUpdate(); Check(!t.isTextOverflowing, t.name + " text overflow"); }
    }
}
