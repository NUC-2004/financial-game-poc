using System;
using System.Collections.Generic;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public class Transaction
    {
        public DateTime Date { get; }
        public string Description { get; }
        public int Amount { get; }
        public int BalanceAfter { get; }

        public Transaction(
            DateTime date,
            string description,
            int amount,
            int balanceAfter)
        {
            Date = date;
            Description = description;
            Amount = amount;
            BalanceAfter = balanceAfter;
        }
    }

    public int Balance { get; private set; }

    private readonly List<Transaction> transactions = new();

    public IReadOnlyList<Transaction> Transactions =>
        transactions.AsReadOnly();

    public void StartNewAccount(DateTime date, int openingBalance)
    {
        if (openingBalance < 0)
            throw new ArgumentOutOfRangeException(nameof(openingBalance));

        Balance = 0;
        transactions.Clear();

        AddIncome(date, openingBalance, "Opening balance");
    }

    public void AddIncome(
        DateTime date,
        int amount,
        string description)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        Balance += amount;

        transactions.Add(new Transaction(
            date,
            description,
            amount,
            Balance));
    }

    public bool TrySpend(
        DateTime date,
        int amount,
        string description)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (Balance < amount)
            return false;

        Balance -= amount;

        transactions.Add(new Transaction(
            date,
            description,
            -amount,
            Balance));

        return true;
    }
}