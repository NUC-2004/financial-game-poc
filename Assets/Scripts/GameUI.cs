using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private GameManager game;
    [SerializeField] private EconomyManager economy;
    [Tooltip("Test MainGame directly before the separate menu scene is ready.")]
    [SerializeField] private bool startRunOnPlay = true;
    [Header("Panels")]
    [SerializeField] private GameObject ledgerPanel;
    [SerializeField] private GameObject journalPanel;
    [Header("Main screen")]
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text foodText;
    [SerializeField] private TMP_Text statusText;
    [Header("Ledger")]
    [SerializeField] private TMP_Text agendaText;
    [SerializeField] private TMP_Text transactionsText;
    [SerializeField] private ScrollRect transactionScroll;
    [Header("Journal")]
    [SerializeField] private TMP_Text journalTitleText;
    [SerializeField] private TMP_Text journalBodyText;
    [SerializeField] private Button endDayButton;
    [SerializeField] private Button newRunButton;
    [SerializeField] private TMP_Text journalFinanceText;
    [SerializeField] private TMP_Text journalFooterText;
    [SerializeField] private Button[] choiceButtons;
    [SerializeField] private TMP_Text[] choiceLabels;

    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-AU");
    private bool endingShown;
    private void OnEnable() { if (game != null) game.StateChanged += Refresh; }
    private void OnDisable() { if (game != null) game.StateChanged -= Refresh; }

    private void Start()
    {
        if (game == null || economy == null || ledgerPanel == null || journalPanel == null)
        {
            Debug.LogError("GameUI is missing its scene references.", this);
            enabled = false;
            return;
        }
        ledgerPanel.SetActive(false);
        journalPanel.SetActive(false);
        if (startRunOnPlay && !game.HasStarted) game.StartRandomRun();
        else Refresh();
    }

    public void StartRandomRun()
    {
        if (game.HasStarted && !game.HasEnded) return;
        endingShown = false;
        ledgerPanel.SetActive(false);
        journalPanel.SetActive(false);
        game.StartRandomRun();
    }
    public void OpenLedger()
    {
        if (!game.HasStarted) return;
        Refresh();
        journalPanel.SetActive(false);
        ledgerPanel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        transactionScroll.verticalNormalizedPosition = 1f;
        transactionScroll.StopMovement();
    }
    public void CloseLedger() => ledgerPanel.SetActive(false);
    public void OpenJournal()
    {
        if (!game.HasStarted) return;
        Refresh();
        ledgerPanel.SetActive(false);
        journalPanel.SetActive(true);
    }
    public void CloseJournal() => journalPanel.SetActive(false);
    public void EndDay()
    {
        // A second queued click cannot advance another day after the panel closes.
        if (!journalPanel.activeSelf || game.HasEnded || game.HasPendingChoice) return;
        journalPanel.SetActive(false);
        game.EndDay();
    }
    private static string Date(DateTime d) => d.ToString("dd MMM yyyy", English);
    private static string ShortDate(DateTime d) => d.ToString("dd MMM", English);

    private void Refresh()
    {
        if (!game.HasStarted) return;
        dateText.text = $"DAY {game.DayNumber} / {game.TotalDays}\n{Date(game.CurrentDate)}";
        balanceText.text = $"BALANCE\nAUD ${economy.Balance:N0}";
        foodText.text = $"FOOD SUPPLY\n{game.FoodDays} days";
        statusText.text = game.HasEnded ? game.EndingMessage
            : game.HasPendingChoice ? "A decision is waiting in your Daily Journal." : game.LastSettlement;
        RefreshLedger();
        RefreshJournal();
        if (game.HasEnded && !endingShown)
        {
            endingShown = true;
            ledgerPanel.SetActive(false);
            journalPanel.SetActive(true);
        }
    }
    private void RefreshLedger()
    {
        bool due = game.CurrentDate.Date == game.NextRentDate.Date;
        string pay = game.SalaryReceived
            ? $"{ShortDate(game.Payday)} / Wages received: ${game.SalaryAmount}"
            : $"{ShortDate(game.Payday)} / Wages due: +${game.SalaryAmount}";
        string birthday = game.CurrentDate.Date == game.Birthday.Date ? "Today"
            : game.CurrentDate.Date > game.Birthday.Date ? "Past date" : "Upcoming";
        agendaText.text = "<b>UPCOMING & RECENT</b>\n\n" +
            $"{ShortDate(game.NextRentDate)} / Rent: ${game.RentCost}" +
            (due ? " <b>DUE TODAY</b>" : "") + "\n\n" + pay + "\n\n" +
            $"{ShortDate(game.Birthday)} / Friend's birthday\n{birthday}\n\n" +
            "<b>DAILY COSTS</b>\n\nFood: $20 without stored groceries\nOther essentials: $10\n\n" +
            $"Food supply: {game.FoodDays} days\n" +
            (game.HasEnded ? "Run closed." : $"Today's living costs: ${game.LivingCostToday}") +
            "\n\n<b>EVENT PAYMENTS PENDING</b>\n" +
            (game.PendingPayments.Count == 0 ? "None." : string.Join("\n", System.Linq.Enumerable.Select(
                game.PendingPayments, p => $"{ShortDate(p.Date)} / +${p.Amount} / {p.Description}")));
        var text = new StringBuilder();
        for (int i = economy.Transactions.Count - 1; i >= 0; i--)
        {
            var e = economy.Transactions[i];
            string sign = e.Amount >= 0 ? "+" : "-";
            string colour = e.Amount >= 0 ? "#276451" : "#923F32";
            text.AppendLine($"<b>{ShortDate(e.Date)}</b> / {e.Description}");
            text.AppendLine($"<color={colour}>{sign}${Math.Abs(e.Amount):N0}</color>    Balance: ${e.BalanceAfter:N0}");
            text.AppendLine();
        }
        text.AppendLine("<b>DECISION HISTORY / NEWEST FIRST</b>");
        for (int i = game.Decisions.Count - 1; i >= 0; i--)
        {
            var d = game.Decisions[i];
            text.AppendLine($"\n<b>{ShortDate(d.Date)} / {d.Title}</b>\n{d.Choice}\n{d.Result}");
        }
        transactionsText.text = text.ToString();
    }
    private void RefreshJournal()
    {
        journalBodyText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, game.HasPendingChoice ? 280 : 500);
        endDayButton.gameObject.SetActive(!game.HasEnded);
        endDayButton.interactable = !game.HasPendingChoice;
        newRunButton.gameObject.SetActive(game.HasEnded);
        journalFooterText.text = game.HasEnded ? "Your ledger keeps the full story."
            : game.HasPendingChoice ? "Choose a response. You can check your ledger first."
            : "Living costs, then any rent due, are paid at End Day.";
        journalFinanceText.text = $"BALANCE ${economy.Balance:N0}  |  FOOD {game.FoodDays} days  |  LIVING TODAY ${game.LivingCostToday}\n" +
            $"RENT ${game.RentCost}: {ShortDate(game.NextRentDate)}" +
            (game.CurrentDate.Date == game.NextRentDate.Date ? " (TODAY)" : "") +
            $"  |  WAGES +${game.SalaryAmount}: {ShortDate(game.Payday)}" + (game.SalaryReceived ? " (received)" : "");
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            var button = choiceButtons[i];
            button.onClick.RemoveAllListeners();
            bool show = game.HasPendingChoice && i < game.CurrentEvent.Options.Count;
            button.gameObject.SetActive(show);
            if (!show) continue;
            int index = i;
            string eventId = game.CurrentEvent.Id;
            var option = game.CurrentEvent.Options[i];
            bool affordable = game.CanChooseEvent(eventId, i);
            choiceLabels[i].text = option.Label + (affordable ? "" : "  [Not enough cash]");
            button.interactable = affordable;
            button.onClick.AddListener(() => game.TryChooseEvent(eventId, index));
        }
        if (game.HasEnded)
        {
            journalTitleText.text = game.Completed ? "Run complete" : "Run ended";
            journalBodyText.text = game.EndingMessage +
                "\n\nOpen the ledger to review your spending and decisions.";
            return;
        }
        journalTitleText.text = $"Day {game.DayNumber} / {Date(game.CurrentDate)}";
        if (game.CurrentEvent != null)
        {
            journalBodyText.text = $"<b>{game.CurrentEvent.Title}</b>\n\n" +
                (game.HasPendingChoice ? game.CurrentEvent.Body : game.EventResult);
            return;
        }
        journalBodyText.text = "<b>A SUSPICIOUSLY ORDINARY DAY</b>\n\n" + game.LastSettlement +
            "\n\nNo new decision today. The fridge hums. Your landlord remains committed to the concept of rent. " +
            "Check the ledger before turning the page.";
    }
}
