using System;
using System.Collections.Generic;
using UnityEngine;

public enum StrategyAction
{
    None, Cash20, Cash40, Card, EatFirst, ShopHungry, CheckStock, WriteList,
    WaitPurchase, BuyNow, SetReminder, NoReminder, Reserve20, Reserve40, NoReserve
}

[DisallowMultipleComponent]
public sealed class StrategySystem : MonoBehaviour
{
    public int ShoppingLimit { get; private set; } = -1;
    public bool AteBeforeShopping { get; private set; }
    public bool CheckedStock { get; private set; }
    public bool WroteList { get; private set; }
    public bool WaitingForPurchase { get; private set; }
    public bool ReminderSet { get; private set; }
    public int PlannedReserve { get; private set; }
    public readonly HashSet<string> encountered = new();
    public void ResetRun()
    {
        ShoppingLimit = -1; AteBeforeShopping = CheckedStock = WroteList = WaitingForPurchase = ReminderSet = false;
        PlannedReserve = 0; encountered.Clear();
    }
    public void Apply(StrategyAction action)
    {
        switch (action)
        {
            case StrategyAction.Cash20: ShoppingLimit = 20; break;
            case StrategyAction.Cash40: ShoppingLimit = 40; break;
            case StrategyAction.Card: ShoppingLimit = -1; break;
            case StrategyAction.EatFirst: AteBeforeShopping = true; break;
            case StrategyAction.CheckStock: CheckedStock = true; break;
            case StrategyAction.WriteList: WroteList = true; break;
            case StrategyAction.WaitPurchase: WaitingForPurchase = true; break;
            case StrategyAction.BuyNow: WaitingForPurchase = false; break;
            case StrategyAction.SetReminder: ReminderSet = true; break;
            case StrategyAction.Reserve20: PlannedReserve = 20; break;
            case StrategyAction.Reserve40: PlannedReserve = 40; break;
            case StrategyAction.NoReserve: PlannedReserve = 0; break;
        }
    }
    public static FinancialChoice Choice(string label, string result, StrategyAction action = StrategyAction.None,
        int cost = 0, int food = 0, int mood = 0) => new FinancialChoice(label, result, cost, food, mood: mood) { action = action };
    public static ScheduledFinancialEvent Event(int day, string id, string title, string body, string tip, params FinancialChoice[] choices)
        => new ScheduledFinancialEvent(day, id, title, body, choices) { tipKey = tip };

    public static List<ScheduledFinancialEvent> CreateSchedule()
    {
        var events = GameEventSchedule.Defaults();
        foreach (var e in events)
        {
            if (e.id.StartsWith("work_"))
                e.body = e.body.Split(new[] { "\n\n" }, StringSplitOptions.None)[0];
            if (e.id.StartsWith("shop_"))
            {
                e.tipKey = "unit";
                e.body = "The large pack says VALUE. It has graded itself.\nCompare price per food day, then check your rent budget.";
            }
        }
        events.RemoveAll(e => e.day == 15);
        events.Add(Event(2, "market_prep", "BEFORE THE MARKET", "You need lunch. The market also sells things you did not know you needed.\nChoose a spending limit for this visit. No withdrawal fee.", "cash",
            Choice("Carry $20 cash", "Trip limit: $20. This is your money, not a bonus or a fee.", StrategyAction.Cash20),
            Choice("Carry $40 cash", "Trip limit: $40. Cash still spends remarkably well.", StrategyAction.Cash40),
            Choice("Use your debit card", "No separate trip limit. You still choose every purchase.", StrategyAction.Card)));
        events.Add(Event(4, "hungry_prep", "SHOPPING ON EMPTY", "You skipped lunch. Every shelf now looks persuasive.\nIf you have stored food, eat today's portion before shopping.", "hunger",
            Choice("Eat a stored meal first", "Today's meal eaten. It will not be charged again tonight.", StrategyAction.EatFirst),
            Choice("Go straight to the shop", "You head out hungry. Check the basket before paying.", StrategyAction.ShopHungry),
            Choice("Stay home today", "No shopping. Normal meal settlement still applies tonight.")));
        events.Add(Event(6, "reminder_setup", "FUTURE YOU CALLED", "Set a $40 emergency-buffer goal for Day 24.\nWould a reminder after your next allowance payment help?", "reminder",
            Choice("Remind me on Day 16", "Reminder added to your calendar. It will not save money for you.", StrategyAction.SetReminder),
            Choice("Plan to keep $40 now", "A $40 target is noted. This is not a transfer or a spending lock.", StrategyAction.Reserve40),
            Choice("Keep the goal, no reminder", "Goal noted. You can review it on Day 24.", StrategyAction.NoReminder)));
        events.Add(Event(9, "sale", "TEN MINUTES LEFT", "Headphones: $30. Yours still work. The timer is very upset about this.\nBuy now, wait one day, or pass?", "pause",
            Choice("Buy the headphones", "You paid $30. Your old pair remains employed.", StrategyAction.BuyNow, 30, mood: 4),
            Choice("Save to wishlist", "No purchase. Tomorrow you can reconsider without a countdown.", StrategyAction.WaitPurchase),
            Choice("Keep the pair you have", "No purchase. The timer will recover.")));
        events.Add(Event(15, "stock_prep", "BEFORE YOU BUY MORE", "Another grocery trip. First, check what is already in the fridge.\nPlan food for the next four days.", "list",
            Choice("Check fridge and write a list", "Inventory checked; the shopping screen will show your exact shortfall.", StrategyAction.WriteList),
            Choice("Check the fridge only", "You checked the stock. Work out the missing days before buying.", StrategyAction.CheckStock),
            Choice("Head straight to the shop", "The fridge is still available from the room if you want to check.")));
        events.Add(Event(24, "goal_review", "THE EMERGENCY BUFFER", "Your $40 buffer goal is due for review.\nA target is useful only if the money is still there.", "reminder",
            Choice("Plan to keep $40", "Target updated. Check the calendar before treating it as spare money.", StrategyAction.Reserve40),
            Choice("Plan to keep $20", "A smaller target. Your bills still need their own budget.", StrategyAction.Reserve20),
            Choice("Keep funds unallocated", "No buffer earmarked. No money has moved.", StrategyAction.NoReserve)));
        events.Sort((a, b) => a.day.CompareTo(b.day));
        return events;
    }
    public ScheduledFinancialEvent OverrideDay(int day)
    {
        if (day == 10 && WaitingForPurchase)
            return null; // Wishlist follow-up is inserted after today's scheduled job, so neither disappears.
        if (day == 16 && ReminderSet)
            return Event(day, "reminder", "THE REMINDER YOU ASKED FOR", "Yesterday's family allowance has arrived.\nYour goal is a $40 buffer by Day 24. Decide what to earmark.", "reminder",
                Choice("Earmark $40", "A $40 plan, not a second bank balance.", StrategyAction.Reserve40),
                Choice("Earmark $20", "A $20 plan. You can revise it on Day 24.", StrategyAction.Reserve20),
                Choice("Not this time", "Reminder read. Nothing saved automatically.", StrategyAction.NoReserve));
        return null;
    }
    public ScheduledFinancialEvent FollowUp(string id, StrategyAction action, int day, int food)
    {
        if (id == "market_prep")
            return Event(day, "market_buy", "AT THE MARKET", "Lunch costs $12. Lunch plus a small souvenir costs $30.\n" +
                (ShoppingLimit >= 0 ? $"You brought ${ShoppingLimit} for this visit." : "You are paying by card. Check your total before paying."), "cash",
                Choice("Lunch only", "Only the planned lunch. Total trip spending: $12.", cost: 12, food: 1),
                Choice("Lunch and a souvenir", "Trip spending: $30. The extra purchase was $18.", cost: 30, food: 1, mood: 4),
                Choice("Leave without buying", "Trip spending: $0. Dinner still needs a plan."));
        if (id == "hungry_prep" && action != StrategyAction.None)
            return Event(day, "hungry_basket", "CHECK THE BASKET", "Two days of groceries cost $36. Snacks add another $12.\n" +
                (AteBeforeShopping ? "You ate first. The snacks are still optional." : "You are hungry. Take a moment before checking out."), "hunger",
                Choice("Groceries only", "You left the unplanned snacks. No mysterious hunger fee.", cost: 36, food: 2),
                Choice("Groceries and snacks", "You chose the extra snacks: $12 beyond the grocery plan.", cost: 48, food: 2, mood: 2),
                Choice("Leave the basket", "Nothing bought. Existing supplies and daily food charges still apply."));
        if (id == "stock_prep")
            return Event(day, "stock_buy", "YOUR FOUR-DAY PLAN", (CheckedStock || WroteList ? $"Stored food: {food} day(s). " : "You can check the fridge before choosing. ") +
                (WroteList ? $"List: cover {Math.Max(0, 4 - food)} more day(s)." : "How many of the next four days are already covered?"), "list",
                Choice("Buy four days", "Four food days added. Compare your new stock with the plan.", cost: 64, food: 4),
                Choice("Buy two days", "Two food days added. Check the remaining gap.", cost: 36, food: 2),
                Choice("Use what you have", "No new groceries. Existing stock is used first."));
        if (id == "work_10" && WaitingForPurchase)
            return Event(day, "wishlist_review", "STILL WANT THEM?", "The timer is gone. Your current headphones still work.\nThe new pair is still $30. Waiting was for thinking, not a promised discount.", "pause",
                Choice("Buy after reviewing the budget", "Purchase reconsidered, then paid. Waiting does not mean you must refuse.", StrategyAction.BuyNow, 30, mood: 4),
                Choice("Keep the old pair", "Wishlist cleared. You kept $30 for other needs.", StrategyAction.BuyNow),
                Choice("Leave it for after this run", "No purchase this month.", StrategyAction.BuyNow));
        return null;
    }
    public static string TipTitle(string key) => key switch
    {
        "cash" => "PAYMENT & SPENDING LIMITS", "hunger" => "NOTICE YOUR SHOPPING STATE",
        "list" => "CHECK STOCK, THEN MAKE A LIST", "unit" => "COMPARE UNIT COST",
        "pause" => "PAUSE BEFORE A PURCHASE", "reminder" => "REMINDERS NEED ACTION", _ => "PLAN AHEAD"
    };
    public static string Tip(string key) => key switch
    {
        "cash" => "Research shows that people tend to spend slightly more with cashless payments, on average. Cash does not guarantee lower spending.\n\nSet a trip budget, then compare it with what you actually buy.",
        "hunger" => "Research shows that hunger can increase the desire to acquire things, including nonfood items.\n\nBefore paying, check which items you planned to buy and which ones you added along the way.",
        "list" => "Budgeting guidance recommends checking your supplies and writing a shopping list.\n\nFood days needed minus food already stored tells you how much more to buy.",
        "unit" => "Budgeting guidance recommends comparing unit prices.\n\nDivide the price by the food days provided. A larger pack may cost less per day, but check what you need and whether you can still pay rent.",
        "pause" => "Budgeting guidance recommends taking time to check whether you need a purchase and can afford it.\n\nWaiting gives you time to think. It does not guarantee a discount or mean you must give up the item.",
        "reminder" => "Research shows that reminders can help some savers reach their goals.\n\nA reminder brings your plan back to your attention. You still need to set money aside, and earmarked money can still be spent.",
        _ => "Check your income dates, necessary costs and mood before committing. Pending income is not spendable cash.\n\nWorking again within four days costs an extra 6 mood. Rest and recovery activities clear this penalty; buying things does not. Mood does not drop automatically each day."
    };
}
