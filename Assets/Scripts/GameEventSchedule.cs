using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FinancialChoice
{
    public string label;
    [TextArea(2, 4)] public string result;
    [Min(0)] public int cost;
    [Min(0)] public int foodDays;
    [Min(0)] public int income;
    [Min(0)] public int delayDays;
    [Range(-100, 100)] public int mood;
    public bool isWork;
    public StrategyAction action;
    public FinancialChoice(string label, string result, int cost = 0, int food = 0,
        int income = 0, int delay = 0, int mood = 0, bool work = false)
    {
        this.label = label; this.result = result; this.cost = cost; foodDays = food;
        this.income = income; delayDays = delay; this.mood = mood; isWork = work;
    }
}

[Serializable]
public sealed class ScheduledFinancialEvent
{
    [Min(1)] public int day;
    public string id;
    public string title;
    [TextArea(2, 5)] public string body;
    public FinancialChoice[] choices;
    public string tipKey;
    public ScheduledFinancialEvent(int day, string id, string title, string body, params FinancialChoice[] choices)
    { this.day = day; this.id = id; this.title = title; this.body = body; this.choices = choices; }
}

[DisallowMultipleComponent]
public sealed class GameEventSchedule : MonoBehaviour
{
    [Tooltip("One event per day; exactly three choices. Edit amounts here, not in button text.")]
    public List<ScheduledFinancialEvent> events = Defaults();
    public ScheduledFinancialEvent ForDay(int day) => events.Find(e => e.day == day);
    public IEnumerable<ScheduledFinancialEvent> Upcoming(int day) => events.FindAll(e => e.day > day);

    public bool Validate(out string problem)
    {
        var days = new HashSet<int>(); var ids = new HashSet<string>();
        if (events == null) { problem = "Missing event schedule."; return false; }
        foreach (var e in events)
        {
            if (e == null || e.day < 1 || string.IsNullOrWhiteSpace(e.id) || !days.Add(e.day) || !ids.Add(e.id) ||
                e.choices == null || e.choices.Length != 3)
            { problem = "Events need unique days/IDs and three choices."; return false; }
            bool free = false;
            foreach (var c in e.choices)
            {
                if (c == null || c.cost < 0 || c.foodDays < 0 || c.income < 0 || c.delayDays < 0)
                { problem = "Invalid event amounts."; return false; }
                if (c.cost == 0) free = true;
            }
            if (!free) { problem = "Every event needs an option with no upfront cost."; return false; }
        }
        problem = ""; return true;
    }
    private static ScheduledFinancialEvent Shop(int day, int bulkCost, int food) => new ScheduledFinancialEvent(
        day, "shop_" + day, "FRIDGE CHECK",
        "The fridge light still works. Food remains an optional extra.\n\n" +
        "Compare the upfront price with daily food costs. Stored meals are used tonight; they stack and do not expire in this prototype.",
        new FinancialChoice("Batch-cook", "Meals sorted. Washing up has filed an appeal.", bulkCost, food),
        new FinancialChoice("Small shop", "Two days of meals. The rest is a future-you problem.", 36, 2),
        new FinancialChoice("Keep the cash", "No groceries added. Empty shelves do not cancel daily food costs."));
    private static ScheduledFinancialEvent Work(int day, string title, string body) => new ScheduledFinancialEvent(
        day, "work_" + day, title, body + "\n\nOnly one booking fits today. Quick jobs pay less, today. Longer shifts pay more, three days later. Check rent before choosing.",
        new FinancialChoice("Take the quick job", "Paid today. Your feet would also like compensation.", income: 80, mood: -10, work: true),
        new FinancialChoice("Take the longer shift", "Payment is confirmed. Unfortunately, confirmed is not cash.", income: 140, delay: 3, mood: -18, work: true),
        new FinancialChoice("Take a free afternoon", "You sit down. Nothing asks for your card details.", mood: 12));
    public static List<ScheduledFinancialEvent> Defaults() => new List<ScheduledFinancialEvent>
    {
        Shop(1, 64, 4),
        Work(3, "HELP WANTED", "Two jobs need filling. You had planned to do nothing. Apparently that position is unpaid."),
        Work(7, "WEEKEND PLANS", "Your manager asks if you are free. This is not an invitation to lunch."),
        Shop(8, 64, 4),
        Work(10, "ONE MORE SHIFT", "Another shift is available. Your bank account says yes. Check with the rest of you."),
        new ScheduledFinancialEvent(12, "birthday", "CAKE AND RENT", "Alex's birthday is today. So is rent. Only one of them accepts cake.\n\nChoose a plan you can afford after tonight's essentials and rent.",
            new FinancialChoice("Dinner with friends", "Dinner was good. The bill was very confident.", 35, 1, mood: 24),
            new FinancialChoice("Bring a dish", "Pasta in, leftovers out. A rare successful group project.", 20, 2, mood: 12),
            new FinancialChoice("Call, then rest at home", "You catch up by phone. Friendship survives the lack of a booking fee.", mood: 6)),
        Shop(15, 64, 4),
        Work(17, "PAYDAY IS NOT TODAY", "The events team needs help. Their payment system believes in suspense."),
        Work(21, "SUNDAY SERVICE", "The cafe is short-staffed again. Apparently this is a tradition."),
        Shop(23, 80, 5),
        new ScheduledFinancialEvent(25, "recharge", "LEAVE THE HOUSE?", "Your friends have three plans. Your budget has opinions.\n\nRent is tomorrow. Recovery breaks the extra-work penalty streak, even when the activity is free.",
            new FinancialChoice("Meal and a movie", "Two hours without checking your balance. It was still there afterwards.", 35, 1, mood: 24),
            new FinancialChoice("Shared lunch at home", "Everyone brought something. Someone even brought a container back.", 20, 2, mood: 12),
            new FinancialChoice("Free walk and a chat", "Fresh air remains free. Management is investigating.", mood: 6)),
        Work(27, "THE LAST BOOKING", "One final shift this month. Next month has already started sending emails.")
    };
}
