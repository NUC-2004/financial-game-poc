# Playable event slice

Five authored English scenarios on days 1, 3, 5, 7 and 9 of the ten-day prototype. Their dates are fixed for repeatable comparison; only the opening balance is random. No mood score, friendship penalty or hidden random financial reward is introduced.

| Day | Event | Choices and effects | Intended practice |
| --- | --- | --- | --- |
| 1 | The Fridge Has Filed a Complaint | $80 / five food days; $36 / two days; skip | Compare upfront spending with the food costs it replaces; retain cash for bills. |
| 3 | Knowledge: Now with a Price Tag | Own book $90; co-op loan $40 with $30 refunded on day 6; campus-only access $0 | Compare access conditions and total costs; a future refund cannot pay today's bill. |
| 5 | Cake Meets the Landlord | Dinner $45 / one food day; potluck $25 / two food days including leftovers; message $0 | Budget for a social occasion alongside a known rent payment. No option is scored as morally correct. |
| 7 | Two Invitations, One Afternoon | Extra shift pays $120 on day 9; free food parcel gives two food days now; free afternoon | Distinguish present resources from later income. Shift and collection times overlap in this fictional scenario. |
| 9 | The Great Rice Alliance | Shared groceries $35 / two food days; solo $20 / one; skip | Compare unit costs, existing stock and available cash before buying more. |

All amounts are AUD and are prototype assumptions, not estimates of Australian market prices, wages, rental terms or institutional policies. The co-op refund, food-share availability and food yields are invented, explicit scenario conditions. Stored food removes only the $20 daily food charge, including on the purchase day; the other $10 daily essentials remain. No spoilage or resale is modelled. A missed payment ending is a game rule, not a model of real eviction. Free time, convenience and social experience are described but are not numerical outcomes, so money alone cannot establish the overall best choice.

## Evidence boundary

The scenarios apply expert guidance; they are not individually validated interventions. No claim is made that these precise choices, prices or jokes improve learning. Playtesting is needed to assess comprehension and transfer.

- **Events 1 and 9:** ASIC Moneysmart recommends meal planning, checking existing food, comparing unit prices and considering bulk purchases with friends. The food-day mechanic is our simplified model of those practices.
- **Events 3, 5 and 7:** ASIC Moneysmart recommends recording income and expenses, including payment timing, comparing totals, and updating a budget when circumstances change. These scenarios let players practise those steps under fictional conditions; the source does not validate textbook deposits, social benefits or the extra-shift-versus-food-parcel trade-off.

## References

Australian Securities and Investments Commission. (2026, August 31). *How to do a budget*. Moneysmart. https://moneysmart.gov.au/budgeting/how-to-do-a-budget

Australian Securities and Investments Commission. (2026, July 30). *Ways to save on food and fuel*. Moneysmart. https://moneysmart.gov.au/budgeting/ways-to-save-on-food-and-fuel

Accessed 4 October 2026. These are official expert guidance sources, not peer-reviewed causal studies of this game.

## Implementation and checks

- Open Daily Journal to respond; End Day is blocked until a response is selected, including an explicit skip/free option.
- Reopening a panel does not reroll or reset a decision. Insufficient-cash choices are disabled and rejected in the model. Choices are keyed to the current event and can be resolved only once.
- Money movements use EconomyManager. Pending income is shown in the planner but enters transaction history only when received. Zero-cost choices are retained in Decision History, below cash transactions.
- Rebuild/wire editable scene controls: Tools > Financial Game > Complete Journal and Ledger (edit mode).
- Automated integration check: fresh Play session, Tools > Financial Game > Validate Event Choices. Covers all 729 opening/choice combinations plus the real scene buttons and text-height bounds. It restores a fresh day-one journal after success.


## Balance revision: 4 October 2026

Opening balances changed from $850/$900/$950 to $740/$760/$780, with equal selection probability. Rent ($500), food ($20/day without supplies), other essentials ($10/day), payday ($700 on day 8), and all event prices are unchanged.

Exhaustive planned-sequence results:

| Opening | Complete | Rent failure | Living-cost failure before payday |
| --- | --- | --- | --- |
| $740 | 150/243 (61.7%) | 54 | 39 |
| $760 | 165/243 (67.9%) | 27 | 51 |
| $780 | 192/243 (79.0%) | 0 | 51 |
| Total | 507/729 (69.5%) | 81 | 141 |

These are uniformly enumerated *planned* five-choice sequences, not predicted human completion rates. A failed prefix is counted once for each hypothetical continuation of the plan, even though play actually stops on failure. All three starting balances have multiple viable paths. A purchased book or birthday dinner is still individually viable with other spending adjusted; neither action is an automatic failure condition. The food parcel can bridge the gap to payday where extra-shift pay arrives too late.

This tuning adds cash-flow pressure; it does not make every option equally valuable or remove all financially dominant choices. It is a prototype balance assumption, not an empirically established financial threshold.
