using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager
{
    public int FoodDiscountStartDay { get; private set; }
    public int FoodDiscountEndDay { get; private set; }
    public int FoodDiscountSavings { get; private set; }
    public bool HasFoodDiscount => FoodDiscountStartDay > 0 && DayNumber >= FoodDiscountStartDay && DayNumber <= FoodDiscountEndDay;
    public string FoodDiscountSummary => FoodDiscountStartDay == 0 ? "No snack-box plan." :
        $"Snack-box plan: $36 paid; $3 off charged food days, {ShortDate(CurrentDate.AddDays(FoodDiscountStartDay - DayNumber))} to {ShortDate(CurrentDate.AddDays(FoodDiscountEndDay - DayNumber))}. Stored meals receive no extra discount. Saved so far: ${FoodDiscountSavings}.";
    public int RentReserve { get; private set; }
    public int UnallocatedCash => Math.Max(0, economy.Balance - RentReserve);
    public int RentReserveUsedForLiving { get; private set; }
    public int RentPaidFromReserve { get; private set; }
    public IReadOnlyList<string> PlanHistory => planHistory.AsReadOnly();
    private readonly List<string> planHistory = new();
    private readonly List<FoodPurchase> foodPurchases = new();
    private sealed class FoodPurchase
    {
        public DateTime Date, RentDate;
        public int StockBefore, Added, Cost;
    }
    private void ResetPlanning()
    {
        FoodDiscountStartDay = FoodDiscountEndDay = FoodDiscountSavings = 0;
        RentReserve = RentReserveUsedForLiving = RentPaidFromReserve = 0;
        planHistory.Clear(); foodPurchases.Clear();
    }
    public bool ReserveForRent()
    {
        if (!HasStarted || HasEnded) return false;
        int target = Math.Min(RentCost, economy.Balance);
        if (target <= RentReserve) return false;
        int added = target - RentReserve; RentReserve = target;
        planHistory.Add($"{ShortDate(CurrentDate)} / Set aside ${added} for rent. Total balance unchanged.");
        StateChanged?.Invoke(); return true;
    }
    public bool ReleaseRentReserve()
    {
        if (!HasStarted || HasEnded || RentReserve == 0) return false;
        planHistory.Add($"{ShortDate(CurrentDate)} / Released ${RentReserve} from the rent plan. Total balance unchanged.");
        RentReserve = 0; StateChanged?.Invoke(); return true;
    }
    private void SettleReserveAfterLiving()
    {
        int used = Math.Max(0, RentReserve - economy.Balance);
        if (used == 0) return;
        RentReserve -= used; RentReserveUsedForLiving += used;
        planHistory.Add($"{ShortDate(CurrentDate)} / Daily living used ${used} of the rent plan.");
        LastSettlement += $"\nDaily living used ${used} of your rent plan.";
    }
    private void RecordFoodPurchase(int stockBefore, int added, int cost)
    {
        foodPurchases.Add(new FoodPurchase { Date = CurrentDate, RentDate = NextRentDate,
            StockBefore = stockBefore, Added = added, Cost = cost });
    }
    private string BuildReview()
    {
        var notes = new List<string>();
        if (FailureReason == "mood")
        {
            var work = decisions.LastOrDefault(d => d.IsWork);
            if (work != null) notes.Add($"Your last job began while you felt {MoodSystem.StatusFor(work.MoodBefore)}. Its effort left you unable to continue.");
        }
        if (pendingPayments.Count > 0)
        {
            var next = pendingPayments.OrderBy(p => p.Date).First();
            notes.Add($"${next.Amount} from a job is due {ShortDate(next.Date)}. It could not pay bills due today.");
        }
        if (FoodDiscountStartDay > 0) notes.Add($"Your snack-box plan cost $36 upfront and saved ${FoodDiscountSavings} in actual food charges before this run ended.");
        if (RentReserveUsedForLiving > 0)
            notes.Add($"Daily living used ${RentReserveUsedForLiving} of your rent plan. Food and essentials need a budget alongside rent.");
        else if (RentPaidFromReserve > 0)
            notes.Add($"${RentPaidFromReserve} of your rent payments came from money you set aside. Reserving it changed the plan, not your total money.");
        var purchase = foodPurchases.LastOrDefault(p => p.StockBefore > 0) ?? foodPurchases.LastOrDefault();
        if (purchase != null)
            notes.Add($"On {ShortDate(purchase.Date)}, you bought {purchase.Added} food days for ${purchase.Cost} with {purchase.StockBefore} already stored. Rent was due {ShortDate(purchase.RentDate)}.");
        else
        {
            int dailyFood = economy.Transactions.Where(t => t.Description == "Daily food").Sum(t => -t.Amount);
            if (dailyFood > 0) notes.Add($"You spent ${dailyFood} buying food day by day. Groceries were offered through journal events.");
        }
        if (notes.Count < 2)
            notes.Add($"You took {decisions.Count(d => d.IsWork)} extra jobs and chose {decisions.Count(d => d.IsRecovery)} recovery activities.");
        return "YOUR PLAN IN PRACTICE\n" + string.Join("\n\n", notes.Take(3));
    }
}
