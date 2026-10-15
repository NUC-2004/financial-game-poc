using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class FinalGameplayValidation
{
    private static void Check(bool ok, string message) { if (!ok) throw new Exception("FINAL GAME: " + message); }
    public static void Run()
    {
        Scenario(550, g =>
        {
            var cash = g.GetComponent<EconomyManager>();
            int transactions = cash.Transactions.Count;
            Check(g.ReserveForRent() && g.RentReserve == 500 && g.UnallocatedCash == 50, "Reserve splits available cash");
            Check(cash.Balance == 550 && cash.Transactions.Count == transactions, "Reserve is not income or an expense");
            Check(!g.ReserveForRent(), "Repeated reserve creates no funds");
            Check(!g.CanChooseEvent(g.CurrentEvent.Id, 0) && g.ChoiceBlockedReason(g.CurrentEvent.Id, 0) == "Release rent funds first", "Reserved money blocks voluntary purchases");
            Check(!g.TryBuyFood(96, 6) && g.TryBuyFood(36, 2), "Daily shop respects rent plan");
            Check(g.TryChooseEvent(g.CurrentEvent.Id, 2), "Skip special offer after regular purchase");
            g.EndDay();
            Check(g.Completed && cash.Balance == 4 && g.RentReserve == 0 && g.RentPaidFromReserve == 500, "Rent settles once from total, reserve clears");
            Check(g.EndingMessage.Contains("$500 of your rent payments") && !g.EndingMessage.Contains("Mood:"), "Review reports actual planning without mood numbers");
            Check(cash.Transactions.Sum(t => t.Amount) == cash.Balance, "Cash history reconciles");
            Check(g.StartRun(0) && g.RentReserve == 0 && g.PlanHistory.Count == 0, "New run clears plan history");
        });
        Scenario(510, g =>
        {
            g.ReserveForRent(); g.TryChooseEvent(g.CurrentEvent.Id, 2); g.EndDay();
            Check(g.FailureReason == "rent" && g.RentReserve == 480 && g.RentReserveUsedForLiving == 20, "Essentials can draw reserved funds transparently");
            Check(g.EndingMessage.Contains("Daily living used $20"), "Reserve shortfall explanation is grounded in actual spending");
        });
        Scenario(450, g =>
        {
            Check(g.ReserveForRent() && g.RentReserve == 450 && g.UnallocatedCash == 0, "Partial reserve never exceeds total");
            Check(g.ReleaseRentReserve() && g.UnallocatedCash == 450 && !g.ReleaseRentReserve(), "Release neither creates nor deletes money");
            Check(g.TryBuyFood(96, 6), "Release permits purchase");
        });
        SnackTests();
        var schedule = StrategySystem.CreateSchedule();
        Check(schedule.First(e => e.id == "shop_1").title != schedule.First(e => e.id == "shop_8").title &&
              schedule.First(e => e.id == "shop_8").body != schedule.First(e => e.id == "shop_23").body, "Three distinct grocery contexts");
        Debug.Log("FINAL_GAME_RULES_PASSED: reserve/release, protected purchases, partial reserves, essentials, rent, restart, factual review and grocery contexts.");
    }
    private static void SnackTests()
    {
        foreach (int opening in new[] { 560, 1000 })
        foreach (bool buy in new[] { false, true })
        {
            var holder = new GameObject("Snack plan fixture") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var g = holder.AddComponent<GameManager>(); var rules = holder.GetComponent<FinanceRules>();
                rules.openingBalances = new[] { opening }; rules.runDays = opening == 560 ? 2 : 17;
                rules.firstRentDay = opening == 560 ? 2 : 40; rules.firstPayDay = 40;
                var offer = StrategySystem.CreateSchedule().First(e => e.id == "snack_plan"); offer.day = 1;
                holder.GetComponent<GameEventSchedule>().events = new() { offer };
                Check(g.StartRun(0) && g.TryChooseEvent(offer.id, buy ? 0 : 1), "Snack choice accepted");
                Check(g.FoodCostToday == 20 && g.FoodDiscountSavings == 0, "Offer saves nothing on purchase day");
                while (!g.HasEnded)
                {
                    int day = g.DayNumber;
                    Check(g.FoodCostToday == (buy && day >= 2 && day <= 16 ? 17 : 20), "Discount boundary day " + day);
                    g.EndDay();
                }
                if (opening == 560) Check(g.Completed == !buy, "Affordable upfront offer can cause tomorrow's rent failure; skipping survives");
                else Check(g.Completed && g.FoodDiscountSavings == (buy ? 45 : 0), "15 charged days, capped $45 saving");
                Check(g.StartRun(0) && g.FoodDiscountStartDay == 0 && g.FoodDiscountSavings == 0, "Discount resets on restart");
            }
            finally { UnityEngine.Object.DestroyImmediate(holder); }
        }
        var stockHolder = new GameObject("Snack stock fixture") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var g = stockHolder.AddComponent<GameManager>(); var rules = stockHolder.GetComponent<FinanceRules>();
            rules.openingBalances = new[] { 1000 }; rules.runDays = 17; rules.firstRentDay = rules.firstPayDay = 40;
            var offer = StrategySystem.CreateSchedule().First(e => e.id == "snack_plan"); offer.day = 1;
            stockHolder.GetComponent<GameEventSchedule>().events = new() { offer };
            Check(g.StartRun(0) && g.TryBuyFood(1, 17) && g.TryChooseEvent(offer.id, 0), "Stock overlap setup");
            while (!g.HasEnded) { Check(g.FoodCostToday == 0, "Stored meals have no food charge"); g.EndDay(); }
            Check(g.FoodDiscountSavings == 0, "Stocked days neither refund money nor extend expiry");
        }
        finally { UnityEngine.Object.DestroyImmediate(stockHolder); }
    }
    private static void Scenario(int opening, Action<GameManager> test)
    {
        var holder = new GameObject("Final planning fixture") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var g = holder.AddComponent<GameManager>(); var rules = holder.GetComponent<FinanceRules>();
            rules.openingBalances = new[] { opening }; rules.runDays = 1; rules.firstRentDay = 1;
            holder.GetComponent<GameEventSchedule>().events = StrategySystem.CreateSchedule();
            Check(g.StartRun(0), "Fixture starts"); test(g);
        }
        finally { UnityEngine.Object.DestroyImmediate(holder); }
    }
    public static void CheckLiveTools(GameUI ui, GameManager g)
    {
        var room = ui.GetComponent<RoomInterface>(); var cash = g.GetComponent<EconomyManager>();
        int before = cash.Balance;
        ui.OpenLedger(); room.reserveRentButton.onClick.Invoke();
        Check(g.RentReserve == g.RentCost && room.rentPlanText.text.Contains("$500"), "Real reserve button");
        Check(cash.Balance == before, "Button does not create transactions");
        room.releaseRentButton.onClick.Invoke(); Check(g.RentReserve == 0, "Real release button");
        room.OpenFridge();
        Check(room.groceryShopButton == null && room.smallGroceryButton == null && room.bulkGroceryButton == null, "Fridge is inventory only");
        Check(cash.Balance == before && g.FoodDays == 0 && room.infoBody.text.Contains("journal events"), "Fridge cannot spend or add stock");
        room.OpenTip();
        ui.OpenJournal();
        Canvas.ForceUpdateCanvases();
        foreach (var t in ui.transform.Find("LedgerPanel/BookWindow").GetComponentsInChildren<TMP_Text>())
        { t.ForceMeshUpdate(); Check(!t.isTextOverflowing, "Money Tracker layout: " + t.name); }
        Debug.Log("FINAL_GAME_UI_TOOLS_PASSED: reserve/release and inventory-only fridge through saved scene buttons.");
    }
    public static void BatchRules()
    { Run(); ChoiceTradeoffValidation.Run(); RoomValidation.Run(); RoomValidation.CalendarAppointments(); }
}
