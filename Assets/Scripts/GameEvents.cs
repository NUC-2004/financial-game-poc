using System;
using System.Collections.Generic;

public partial class GameManager
{
    public sealed class DailyEvent
    {
        public string Id { get; }
        public string Title { get; }
        public string Body { get; }
        public string TipKey { get; }
        public IReadOnlyList<FinancialChoice> Options { get; }
        public DailyEvent(ScheduledFinancialEvent e)
        { Id = e.id; Title = e.title; Body = e.body; TipKey = e.tipKey; Options = Array.AsReadOnly(e.choices); }
    }
    public sealed class DecisionRecord
    {
        public DateTime Date { get; }
        public string Title { get; }
        public string Choice { get; }
        public string Result { get; }
        public int CashBefore { get; }
        public int CashAfter { get; }
        public int MoodBefore { get; }
        public int MoodAfter { get; }
        public int MoodChange => MoodAfter - MoodBefore;
        public bool IsWork { get; }
        public DecisionRecord(DateTime date, string title, string choice, string result,
            int cashBefore, int cashAfter, int moodBefore, int moodAfter, bool isWork)
        {
            Date = date; Title = title; Choice = choice; Result = result;
            CashBefore = cashBefore; CashAfter = cashAfter; MoodBefore = moodBefore; MoodAfter = moodAfter; IsWork = isWork;
        }
    }
    public sealed class EventPayment
    {
        public DateTime Date { get; }
        public int Amount { get; }
        public string Description { get; }
        public EventPayment(DateTime date, int amount, string description)
        { Date = date; Amount = amount; Description = description; }
    }
    private readonly List<DecisionRecord> decisions = new();
    private readonly List<EventPayment> pendingPayments = new();
    public IReadOnlyList<DecisionRecord> Decisions => decisions.AsReadOnly();
    public IReadOnlyList<EventPayment> PendingPayments => pendingPayments.AsReadOnly();
    public DailyEvent CurrentEvent { get; private set; }
    public bool EventResolved { get; private set; }
    public string EventResult { get; private set; } = "";
    public string LastTipKey { get; private set; } = "";
    public bool HasPendingChoice => HasStarted && !HasEnded && CurrentEvent != null && !EventResolved;
    private void ResetEvents() { decisions.Clear(); pendingPayments.Clear(); LastTipKey = ""; PrepareDayEvent(); }
    private void PrepareDayEvent()
    {
        var entry = Strategies.OverrideDay(DayNumber) ?? schedule.ForDay(DayNumber);
        CurrentEvent = entry == null ? null : new DailyEvent(entry);
        EventResolved = false; EventResult = "";
    }
    public bool CanChooseEvent(string eventId, int index) => ChoiceBlockedReason(eventId, index) == "";
    public string ChoiceBlockedReason(string eventId, int index)
    {
        if (!HasPendingChoice || CurrentEvent.Id != eventId || index < 0 || index >= CurrentEvent.Options.Count) return "Unavailable";
        var c = CurrentEvent.Options[index];
        if (economy.Balance < c.cost) return "Not enough cash";
        if (CurrentEvent.Id == "market_buy" && Strategies.ShoppingLimit >= 0 && c.cost > Strategies.ShoppingLimit) return "Over trip limit";
        if (c.action == StrategyAction.EatFirst && (FoodDays == 0 || AteTodaysMeal)) return "No stored meal";
        if (c.action == StrategyAction.Reserve40 && economy.Balance < 40) return "Need $40 available";
        if (c.action == StrategyAction.Reserve20 && economy.Balance < 20) return "Need $20 available";
        return "";
    }
    public string OptionLabel(FinancialChoice c)
    {
        string label = c.label + (c.cost > 0 ? $" / -${c.cost} now" : " / Free");
        if (c.income > 0) label = c.label + (c.cost > 0 ? $" / -${c.cost}" : "") +
            $" / +${c.income} " + (c.delayDays == 0 ? "today" : ShortDate(CurrentDate.AddDays(c.delayDays)));
        if (c.foodDays > 0) label += $" / +{c.foodDays} food days";
        int delta = mood.Preview(c.mood, c.isWork, DayNumber);
        if (c.mood != 0 || delta != 0) label += $" / Mood {delta:+0;-0;0}";
        if (mood.Value + delta == 0) label += " [ENDS RUN]";
        return label;
    }
    public bool TryChooseEvent(string eventId, int index)
    {
        if (!CanChooseEvent(eventId, index)) return false;
        var c = CurrentEvent.Options[index];
        var resolvedEvent = CurrentEvent;
        int cashBefore = economy.Balance, moodBefore = mood.Value;
        if (c.cost > 0 && !economy.TrySpend(CurrentDate, c.cost, CurrentEvent.Title + " / " + c.label)) return false;
        FoodDays += c.foodDays;
        if (c.action == StrategyAction.EatFirst) { FoodDays--; AteTodaysMeal = true; }
        Strategies.Apply(c.action);
        string incomeInfo = "";
        if (c.income > 0)
        {
            string description = CurrentEvent.Title + " / " + c.label;
            if (c.delayDays == 0)
            {
                economy.AddIncome(CurrentDate, c.income, description);
                incomeInfo = $"\n+${c.income} arrived today.";
            }
            else
            {
                var due = CurrentDate.AddDays(c.delayDays);
                pendingPayments.Add(new EventPayment(due, c.income, description));
                pendingPayments.Sort((a, b) => a.Date.CompareTo(b.Date));
                incomeInfo = $"\n+${c.income} due {ShortDate(due)}. Not spendable yet.";
            }
        }
        mood.Apply(c.mood, c.isWork, DayNumber);
        EventResolved = true;
        EventResult = c.result + $"\n\nCash: ${cashBefore} -> ${economy.Balance}. Mood: {moodBefore} -> {mood.Value}." +
            (c.foodDays > 0 ? $"\nFood: +{c.foodDays} days, usable tonight." : "") + incomeInfo;
        decisions.Add(new DecisionRecord(CurrentDate, CurrentEvent.Title, c.label, EventResult,
            cashBefore, economy.Balance, moodBefore, mood.Value, c.isWork));
        LastTipKey = resolvedEvent.TipKey;
        if (!string.IsNullOrEmpty(LastTipKey)) Strategies.encountered.Add(LastTipKey);
        var followUp = Strategies.FollowUp(resolvedEvent.Id, c.action, DayNumber, FoodDays);
        if (followUp != null && mood.Value > 0)
        {
            CurrentEvent = new DailyEvent(followUp); EventResolved = false;
        }
        LastSettlement = "Decision recorded. Check tonight's costs before ending the day.";
        if (mood.Value <= 0)
            FinishRun(false, "mood", "You cannot keep up this schedule. You pause your plans and ask for support.\nMood reached 0. Money alone could not sustain this run.");
        else StateChanged?.Invoke();
        return true;
    }
    private void SettleEventPayments()
    {
        for (int i = 0; i < pendingPayments.Count;)
        {
            var p = pendingPayments[i];
            if (p.Date.Date > CurrentDate.Date) { i++; continue; }
            economy.AddIncome(CurrentDate, p.Amount, p.Description);
            LastSettlement += $"\n{p.Description}: +${p.Amount} arrived.";
            pendingPayments.RemoveAt(i);
        }
    }
}
