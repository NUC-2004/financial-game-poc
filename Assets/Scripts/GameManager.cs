using System;
using UnityEngine;

[RequireComponent(typeof(EconomyManager), typeof(FinanceRules), typeof(MoodSystem))]
[RequireComponent(typeof(GameEventSchedule))]
[RequireComponent(typeof(StrategySystem))]
public partial class GameManager : MonoBehaviour
{
    [SerializeField] private EconomyManager economy;
    [SerializeField] private FinanceRules rules;
    [SerializeField] private MoodSystem mood;
    [SerializeField] private GameEventSchedule schedule;
    public FinanceRules Rules => rules;
    public MoodSystem Mood => mood;
    public GameEventSchedule Schedule => schedule;
    public StrategySystem Strategies { get; private set; }
    public bool AteTodaysMeal { get; private set; }
    public event Action StateChanged;
    public bool HasStarted { get; private set; }
    public bool HasEnded { get; private set; }
    public bool Completed { get; private set; }
    public int DayNumber { get; private set; }
    public int FoodDays { get; private set; }
    public DateTime CurrentDate { get; private set; }
    public DateTime NextRentDate { get; private set; }
    public DateTime Payday { get; private set; }
    public DateTime Birthday { get; private set; }
    public bool SalaryReceived { get; private set; }
    public string LastSettlement { get; private set; }
    public string EndingMessage { get; private set; }
    public string FailureReason { get; private set; }
    public int FoodCostToday => AteTodaysMeal || FoodDays > 0 ? 0 : rules.dailyFood;
    public int LivingCostToday => FoodCostToday + rules.dailyEssentials;
    public int RentCost => rules.rent;
    public int SalaryAmount => rules.wages;
    public int TotalDays => rules.runDays;
    public int OpeningBalance { get; private set; }
    public int RentsPaid { get; private set; }
    public int WagesReceived { get; private set; }
    private void Awake() => BindComponents();
    private void BindComponents()
    {
        if (!economy) economy = GetComponent<EconomyManager>();
        if (!rules) rules = GetComponent<FinanceRules>();
        if (!mood) mood = GetComponent<MoodSystem>();
        if (!schedule) schedule = GetComponent<GameEventSchedule>();
        if (!Strategies) Strategies = GetComponent<StrategySystem>();
    }
    public void StartRandomRun()
    {
        if (HasStarted && !HasEnded) return;
        BindComponents();
        if (!rules.IsValid) { Debug.LogError("Invalid Finance Rules.", this); return; }
        StartRun(UnityEngine.Random.Range(0, rules.openingBalances.Length));
    }
    public bool StartRun(int openingIndex)
    {
        if (HasStarted && !HasEnded) return false;
        BindComponents();
        if (!rules.IsValid || openingIndex < 0 || openingIndex >= rules.openingBalances.Length) return false;
        if (!schedule.Validate(out var error)) { Debug.LogError(error, this); return false; }
        CurrentDate = new DateTime(2026, 9, 11);
        NextRentDate = CurrentDate.AddDays(rules.firstRentDay - 1);
        Payday = CurrentDate.AddDays(rules.firstPayDay - 1);
        Birthday = CurrentDate.AddDays(11);
        DayNumber = 1; FoodDays = 0; RentsPaid = 0; WagesReceived = 0;
        HasStarted = true; HasEnded = false; Completed = false; SalaryReceived = false;
        EndingMessage = ""; FailureReason = "";
        OpeningBalance = rules.openingBalances[openingIndex];
        mood.ResetRun(); Strategies.ResetRun(); AteTodaysMeal = false;
        LastSettlement = "A new month. Same landlord. Check the ledger before spending.";
        economy.StartNewAccount(CurrentDate, OpeningBalance);
        ResetEvents(); StateChanged?.Invoke(); return true;
    }
    public bool TryBuyFood(int price, int days)
    {
        if (!HasStarted || HasEnded || price <= 0 || days <= 0) return false;
        if (!economy.TrySpend(CurrentDate, price, $"Groceries: {days} days of food")) return false;
        FoodDays += days; StateChanged?.Invoke(); return true;
    }
    public void EndDay()
    {
        if (!HasStarted || HasEnded || HasPendingChoice) return;
        int foodCost = FoodCostToday, dailyTotal = LivingCostToday;
        if (economy.Balance < dailyTotal)
        {
            FinishRun(false, "living", $"Today's essentials need ${dailyTotal}. You have ${economy.Balance}.\nShortfall: ${dailyTotal - economy.Balance}. No partial payment was taken."); return;
        }
        if (foodCost > 0) economy.TrySpend(CurrentDate, foodCost, "Daily food");
        else if (!AteTodaysMeal) FoodDays--;
        economy.TrySpend(CurrentDate, rules.dailyEssentials, "Other daily essentials");
        LastSettlement = $"Living costs paid: ${dailyTotal}.";
        if (CurrentDate.Date >= NextRentDate.Date)
        {
            if (!economy.TrySpend(CurrentDate, rules.rent, "Weekly rent"))
            {
                FinishRun(false, "rent", $"Rent needs ${rules.rent}. After living costs, you have ${economy.Balance}.\nShortfall: ${rules.rent - economy.Balance}. Tomorrow's money cannot pay today's rent."); return;
            }
            RentsPaid++; LastSettlement += $"\nRent paid: ${rules.rent}. Your landlord remains a fan.";
            NextRentDate = NextRentDate.AddDays(rules.rentInterval);
        }
        if (DayNumber >= rules.runDays)
        {
            FinishRun(true, "", $"You made it through {rules.runDays} days.\nCash: ${economy.Balance} | Mood: {mood.Value}/100 | Food: {FoodDays} days\nRent payments made: {RentsPaid}. Lowest mood: {mood.Lowest}.\nNext rent: ${rules.rent} on {ShortDate(NextRentDate)}. The bills continue after the credits."); return;
        }
        DayNumber++; CurrentDate = CurrentDate.AddDays(1); AteTodaysMeal = false;
        if (CurrentDate.Date >= Payday.Date)
        {
            economy.AddIncome(CurrentDate, rules.wages, "Regular part-time wages");
            WagesReceived++; SalaryReceived = true;
            LastSettlement += $"\nWages arrived: +${rules.wages}.";
            Payday = Payday.AddDays(rules.payInterval);
        }
        SettleEventPayments(); PrepareDayEvent(); StateChanged?.Invoke();
    }
    private void FinishRun(bool completed, string reason, string message)
    {
        HasEnded = true; Completed = completed; FailureReason = reason;
        EndingMessage = message + "\n\n" + BuildReview(); StateChanged?.Invoke();
    }
    private string BuildReview()
    {
        int shifts = 0, recovery = 0;
        foreach (var d in decisions) { if (d.IsWork) shifts++; if (d.MoodChange > 0) recovery++; }
        string review = $"Your plan: {shifts} extra jobs; {recovery} recovery choices.";
        int pending = 0; foreach (var p in pendingPayments) pending += p.Amount;
        if (pending > 0) review += $"\n${pending} is still pending. It was never included in your spendable balance.";
        if (decisions.Count > 0)
        {
            var d = decisions[decisions.Count - 1];
            review += $"\nLast choice: {d.Choice}. Cash ${d.CashBefore} to ${d.CashAfter}; mood {d.MoodBefore} to {d.MoodAfter}.";
        }
        return review;
    }
    public static string ShortDate(DateTime date) => date.ToString("dd MMM", System.Globalization.CultureInfo.InvariantCulture);
}
