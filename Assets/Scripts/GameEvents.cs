using System;
using System.Collections.Generic;

// Fictional prototype scenarios. See Assets/Documentation/EventDesign.md for
// the distinction between financial guidance and authored game assumptions.
public partial class GameManager
{
    public sealed class EventOption
    {
        public string Label { get; }
        public string Result { get; }
        public int Cost { get; }
        public int Food { get; }
        public int LaterIncome { get; }
        public int Delay { get; }
        public string PaymentDescription { get; }
        public EventOption(string label, string result, int cost = 0, int food = 0,
            int laterIncome = 0, int delay = 0, string paymentDescription = "")
        {
            Label = label; Result = result; Cost = cost; Food = food;
            LaterIncome = laterIncome; Delay = delay; PaymentDescription = paymentDescription;
        }
    }
    public sealed class DailyEvent
    {
        public string Id { get; }
        public string Title { get; }
        public string Body { get; }
        public IReadOnlyList<EventOption> Options { get; }
        public DailyEvent(string id, string title, string body, params EventOption[] options)
        { Id = id; Title = title; Body = body; Options = Array.AsReadOnly(options); }
    }
    public sealed class DecisionRecord
    {
        public DateTime Date { get; }
        public string Title { get; }
        public string Choice { get; }
        public string Result { get; }
        public DecisionRecord(DateTime date, string title, string choice, string result)
        { Date = date; Title = title; Choice = choice; Result = result; }
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
    public bool HasPendingChoice => HasStarted && !HasEnded && CurrentEvent != null && !EventResolved;

    private void ResetEvents()
    {
        decisions.Clear(); pendingPayments.Clear(); PrepareDayEvent();
    }
    private void PrepareDayEvent()
    {
        CurrentEvent = EventForDay(DayNumber);
        EventResolved = false; EventResult = "";
    }
    public bool CanChooseEvent(string eventId, int optionIndex)
    {
        return HasPendingChoice && CurrentEvent.Id == eventId && optionIndex >= 0 &&
            optionIndex < CurrentEvent.Options.Count && economy.Balance >= CurrentEvent.Options[optionIndex].Cost;
    }
    public bool TryChooseEvent(string eventId, int optionIndex)
    {
        // Validate in the model as well as disabling buttons: no duplicate or stale claims.
        if (!CanChooseEvent(eventId, optionIndex)) return false;
        var option = CurrentEvent.Options[optionIndex];
        if (option.Cost > 0 && !economy.TrySpend(CurrentDate, option.Cost, CurrentEvent.Title + " / " + option.Label))
            return false;
        FoodDays += option.Food;
        if (option.LaterIncome > 0)
            pendingPayments.Add(new EventPayment(CurrentDate.AddDays(option.Delay), option.LaterIncome, option.PaymentDescription));
        EventResolved = true;
        EventResult = option.Result + "\n\n" +
            (option.Cost > 0 ? $"Paid now: ${option.Cost}." : "No money spent now.") +
            (option.Food > 0 ? $" Food supply: +{option.Food} day(s), usable from today." : "") +
            (option.LaterIncome > 0 ? $" +${option.LaterIncome} due {CurrentDate.AddDays(option.Delay).ToString("dd MMM", System.Globalization.CultureInfo.InvariantCulture)}; it is not spendable yet." : "") +
            "\nFood supplies replace the $20 daily food charge. Other essentials still cost $10.";
        decisions.Add(new DecisionRecord(CurrentDate, CurrentEvent.Title, option.Label, EventResult));
        LastSettlement = "Decision recorded. The ledger has the details.";
        StateChanged?.Invoke();
        return true;
    }
    private void SettleEventPayments()
    {
        for (int i = 0; i < pendingPayments.Count;)
        {
            var payment = pendingPayments[i];
            if (payment.Date.Date > CurrentDate.Date) { i++; continue; }
            economy.AddIncome(CurrentDate, payment.Amount, payment.Description);
            LastSettlement += $"\n{payment.Description}: +${payment.Amount} arrived.";
            pendingPayments.RemoveAt(i);
        }
    }
    private static DailyEvent EventForDay(int day)
    {
        switch (day)
        {
            case 1:
                return new DailyEvent("groceries", "THE FRIDGE HAS FILED A COMPLAINT",
                    "You open the fridge. The light works. This concludes the good news.\n\n" +
                    "A meal plan could turn groceries into several days of food. A bigger shop costs more today; rent is still due on 15 Sep.",
                    new EventOption("Batch-cook / $80 now / +5 food days",
                        "Five days of meals are secured. Your freezer is now a very small branch of meal-prep government. Compare the upfront cost with daily food spending.", 80, 5),
                    new EventOption("Small shop / $36 now / +2 food days",
                        "Two days of meals. Less cash tied up today, and a shorter truce with the empty fridge.", 36, 2),
                    new EventOption("Keep the cash / $0 now / daily food costs continue",
                        "The fridge remains a well-lit cupboard. You keep the cash for now; ordinary food charges still apply when you have no supplies."));
            case 3:
                return new DailyEvent("textbook", "KNOWLEDGE: NOW WITH A PRICE TAG",
                    "The reading list has arrived. Apparently wisdom is sold separately.\n\n" +
                    "The student co-op offers a short loan: pay $40 now, get $30 back automatically on 16 Sep. Free library access is available on campus only.",
                    new EventOption("Buy your own copy / $90 now",
                        "You own the book and can read it anywhere. It does not, regrettably, complete the assignment for you. You keep the copy; no resale is arranged.", 90),
                    new EventOption("Co-op loan / $40 now / $30 back on 16 Sep",
                        "You borrow the book. The $30 refund is scheduled after rent day, so it cannot help pay rent on 15 Sep. The loan's final cost is $10.", 40, 0, 30, 3, "Textbook deposit refund"),
                    new EventOption("Use the campus library / $0 / no take-home copy",
                        "The library provides access for free. The catch is the commute to a building full of people aggressively not talking. Plan your study visits."));
            case 5:
                return new DailyEvent("birthday", "CAKE MEETS THE LANDLORD",
                    "Alex's birthday and rent have landed on the same day. One wants candles. The other wants $500.\n\n" +
                    "You can join dinner, bring a dish to a home gathering, or send a message. Any food gained can cover today's meal cost; rent is collected tonight.",
                    new EventOption("Join dinner / $45 / +1 food day",
                        "Dinner was lively. The cake survived for eleven seconds. Today's food is covered, but the evening still costs money beyond your usual meal budget.", 45, 1),
                    new EventOption("Bring a dish / $25 / +2 food days with leftovers",
                        "You brought pasta and returned with leftovers. A respectable exchange rate. The meals cover two days; the landlord still prefers cash.", 25, 2),
                    new EventOption("Send a birthday message / $0 / no food added",
                        "You send a heartfelt message with a financially responsible number of emojis. No spending, no food gained. Dinner is still your own responsibility."));
            case 7:
                return new DailyEvent("shift", "TWO INVITATIONS, ONE AFTERNOON",
                    "The cafe needs help today. So does the campus food-share collection queue. You can only make one.\n\n" +
                    "The shift pays $120 on 19 Sep. The food-share offers two days of supplies today. Or keep the afternoon free. Tomorrow's regular wages are separate.",
                    new EventOption("Work the extra shift / +$120 on 19 Sep",
                        "You spend the afternoon negotiating with a coffee machine. It wins. Payment is confirmed for 19 Sep, not today.", 0, 0, 120, 2, "Extra cafe shift pay"),
                    new EventOption("Collect the food parcel / $0 / +2 food days",
                        "You collect two days of food. No forms ask whether you deserve pasta. This helps with meals now, but adds no cash to your account.", 0, 2),
                    new EventOption("Keep the afternoon free / no financial change",
                        "You keep time for yourself. Your assignments have been trying to reach you. No extra pay or food supplies are added."));
            case 9:
                return new DailyEvent("group_shop", "THE GREAT RICE ALLIANCE",
                    "Your housemate proposes a bulk shop. For once, the group chat has produced something other than seventeen unread messages.\n\n" +
                    "Your $35 share supplies two days of meals; a $20 solo shop supplies one. Check your existing food first. Leftovers do not turn back into cash when the run ends.",
                    new EventOption("Split the groceries / $35 / +2 food days",
                        "The alliance is signed. You pay $17.50 per food day, but spend $35 upfront. A lower unit cost is useful only if you need the supplies and can spare the cash.", 35, 2),
                    new EventOption("Buy one day's meals / $20 / +1 food day",
                        "One day of food, one smaller payment. No bulk discount, but less money tied up in the cupboard.", 20, 1),
                    new EventOption("Skip this shop / $0 / use your current supplies",
                        "You leave the rice diplomacy to others. Existing supplies are used first; the usual daily food charge returns when they run out."));
            default: return null;
        }
    }
}
