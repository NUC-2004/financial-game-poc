using System;
using System.Globalization;
using System.Text;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private GameManager game;
    [SerializeField] private EconomyManager economy;
    [SerializeField] private RoomInterface room;
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
    [SerializeField] private TMP_Text moodText;
    [SerializeField] private Slider moodBar;
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
        if (room != null) room.ClosePanels();
        ledgerPanel.SetActive(false);
        journalPanel.SetActive(false);
        game.StartRandomRun();
    }
    public void OpenLedger()
    {
        if (!game.HasStarted) return;
        if (room != null) room.ClosePanels();
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
        if (room != null) room.ClosePanels();
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
        dateText.text = $"DAY {game.DayNumber}/{game.TotalDays}  |  {ShortDate(game.CurrentDate)}";
        balanceText.text = $"AUD ${economy.Balance:N0}";
        foodText.text = $"FOOD SUPPLY\n{game.FoodDays} days";
        if (moodText != null) moodText.text = $"MOOD {game.Mood.Value}/100" + (game.Mood.IsLow ? "  LOW" : "");
        if (moodBar != null) moodBar.value = game.Mood.Value;
        if (moodText != null) moodText.color = game.Mood.IsLow ? new Color32(255, 185, 122, 255) : Color.white;
        statusText.text = game.HasEnded ? (game.Completed ? "Month complete. Open the journal to review." : "Run ended. Open the journal to review.")
            : game.HasPendingChoice ? "A new decision is waiting. Open your journal." : "Decision recorded. End the day from your journal.";
        RefreshLedger();
        RefreshJournal();
        if (game.HasEnded && !endingShown)
        {
            endingShown = true;
            if (room != null) room.ClosePanels();
            ledgerPanel.SetActive(false);
            journalPanel.SetActive(true);
        }
    }
    private void RefreshLedger()
    {
        bool due = game.CurrentDate.Date == game.NextRentDate.Date;
        string pay = $"{ShortDate(game.Payday)} / Wages due: +${game.SalaryAmount}";
        agendaText.text = "<b>AT A GLANCE</b>\n\n" +
            $"Rent ${game.RentCost}: {ShortDate(game.NextRentDate)}" + (due ? " / TODAY" : "") + "\n" + pay +
            $"\n\nTonight's living costs: ${game.LivingCostToday}\nStored food: {game.FoodDays} days\n\n" +
            "<b>BUFFER PLAN</b>\n" + $"Earmarked: ${game.Strategies.PlannedReserve}\n" +
            (game.Strategies.PlannedReserve > economy.Balance ? "Below your earmarked target.\n" : "") +
            "Included in your balance; not locked or transferred.\n\n<b>PENDING INCOME</b>\n" +
            (game.PendingPayments.Count == 0 ? "None." : string.Join("\n", System.Linq.Enumerable.Select(
                game.PendingPayments, p => $"{ShortDate(p.Date)} / +${p.Amount} / {p.Description}"))) +
            "\n\nOpen the wall calendar for all dates.\nScroll the history for past decisions.";
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
            : game.Mood.IsLow ? "LOW MOOD: check recovery choices. Reaching 0 ends the run."
            : game.HasPendingChoice ? "Choose a response. You can check your ledger first."
            : "Living costs, then any rent due, are paid at End Day.";
        journalFinanceText.text = $"CASH ${economy.Balance:N0} | MOOD {game.Mood.Value}/100 | FOOD {game.FoodDays} days | LIVING ${game.LivingCostToday}\n" +
            $"RENT ${game.RentCost}: {ShortDate(game.NextRentDate)}" +
            (game.CurrentDate.Date == game.NextRentDate.Date ? " (TODAY)" : "") +
            $"  |  WAGES +${game.SalaryAmount}: {ShortDate(game.Payday)}";
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
            choiceLabels[i].text = game.OptionLabel(option) + (affordable ? "" : "  [" + game.ChoiceBlockedReason(eventId, i) + "]");
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
        journalBodyText.text = "<b>NO NEW DISASTERS</b>\n\n" + game.LastSettlement;
    }
}
