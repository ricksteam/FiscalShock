using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using TMPro;

/// <summary>
/// Represents a single loan.
/// </summary>
public class Loan
{
    /// <summary>
    /// Unique numeric ID of this loan. Must be unique to pay off the
    /// correct loan!
    /// </summary>
    public int ID { get; }

    /// <summary>
    /// Amount due on this loan. Increases by day with interest.
    /// </summary>
    public float total { get; set; }

    /// <summary>
    /// Interest rate on this loan. The rate remains the same, even if the
    /// player's credit rating drops or rises.
    /// </summary>
    public float rate { get; }

    /// <summary>
    /// Whether the player made a payment on this loan recently.
    /// </summary>
    public bool paid { get; set; }

    /// <summary>
    /// The type of loan.
    /// </summary>
    public LoanType type { get; }

    /// <summary>
    /// How many days ago this loan was taken out. Factored into credit rating.
    /// </summary>
    public int age { get; set; }

    /// <summary>
    /// Original amount due on this loan, with no interest applied.
    /// </summary>
    public float originalAmount { get; }

    /// <summary>
    /// Creditor ID.
    /// </summary>
    public string lender { get; }

    /// <summary>
    /// Amount of collateral/security deposit. Will be paid back to the player
    /// when the full balance of the loan is paid off.
    /// </summary>
    public float collateral { get; }

    /// <summary>
    /// Whether this loan is in the grace period. Loans in the grace period
    /// don't accumulate interest. Currently, payday loans have no grace period,
    /// and other loan types have a 1 day grace period before interest begins
    /// accruing.
    /// </summary>
    public bool inGracePeriod { get; set; }

    /// <summary>
    /// Basic constructor for a loan.
    /// </summary>
    /// <param name="num">next available ID</param>
    /// <param name="tot">amount of debt this loan adds</param>
    /// <param name="rat">interest rate</param>
    /// <param name="t">type of loan</param>
    /// <param name="securityDeposit">collateral amount on this loan, should be zero if this isn't a secured loan</param>
    /// <param name="creditorId">creditor this loan was borrowed from</param>
    public Loan(int num, float tot, float rat, LoanType t, float securityDeposit, string creditorId) {
        ID = num;
        total = tot;
        rate = rat;
        paid = false;
        type = t;
        age = 0;
        inGracePeriod = t != LoanType.Payday;
        originalAmount = tot;
        collateral = securityDeposit;
        lender = creditorId;
    }
}

/// <summary>
/// Valid implemented loan types.
/// </summary>
public enum LoanType {
    Payday,
    Unsecured,
    Secured
}

/// <summary>
/// Valid dungeon types. Used to communicate to the Dungeoneer
/// what kind of dungeon to load.
/// </summary>
public enum DungeonTypeEnum {
    Temple,
    Mine
}

/// <summary>
/// Creditor objects used to determine the threat level and
/// whether they have been paid recently or not.
/// </summary>
public class CreditorData {
    /// <summary>
    /// Whether all of the loans lent by this creditor have had
    /// a payment made against them recently. Factored into threat
    /// accumulation. They're happy if you make regular payments!
    /// </summary>
    public bool paid = false;

    /// <summary>
    /// Current threat level.
    /// </summary>
    public int threatLevel = 0;

    /// <summary>
    /// Constructor. Should be called only once for each creditor ID, or
    /// bad things will happen.
    /// </summary>
    /// <param name="beenPaid">whether this creditor should consider themselves as having been paid to start with</param>
    /// <param name="baseThreat">base threat level</param>
    public CreditorData(bool beenPaid, int baseThreat) {
        paid = beenPaid;
        threatLevel = baseThreat;
    }
}

/// <summary>
/// Represents a play session. Consider this the player's
/// "save data." Save games could be implemented by serializing
/// this to JSON (may require a separate backing data class with
/// no methods).
/// </summary>
public static class StateManager
{
    /// <summary>
    /// Current cash on hand. Cash on hand is the player's health and ammo.
    /// </summary>
    public static float cashOnHand { get; set; } = DefaultState.cashOnHand;

    /// <summary>
    /// All active loans the player owes money on.
    /// </summary>
    public static List<Loan> loanList = new List<Loan>();

    /// <summary>
    /// Amount of money the player has earned in the dungeon each day.
    /// Factored into credit score.
    /// </summary>
    public static List<float> income = new List<float>();

    /// <summary>
    /// List of creditor IDs, so state manager can handle processing of
    /// due amounts.
    /// </summary>
    public static Dictionary<string, CreditorData> lenders = new Dictionary<string, CreditorData>();

    /// <summary>
    /// Total amount due to all creditors. This value is displayed on the HUD.
    /// </summary>
    public static float totalDebt => loanList.Sum(l => l.total);

    /// <summary>
    /// Next available loan ID.
    /// </summary>
    public static int nextID { get; set; } = DefaultState.nextID;

    /// <summary>
    /// Total active loans.
    /// </summary>
    public static int totalLoans => loanList.Count;

    /// <summary>
    /// Number of times the player has entered the dungeon.
    /// </summary>
    public static int timesEntered { get; set; } = DefaultState.timesEntered;

    /// <summary>
    /// Number of floors the player has visited inside the dungeon. A low ratio
    /// of timesEntered : totalFloorsVisited implies a more successful player,
    /// as they were able to go deeper in the dungeon.
    /// </summary>
    public static int totalFloorsVisited { get; set; } = DefaultState.totalFloorsVisited;

    /// <summary>
    /// Current dungeon floor the player is on. Used for difficulty scaling.
    /// </summary>
    public static int currentFloor { get; set; } = DefaultState.currentFloor;

    /// <summary>
    /// Used for calculating credit score.
    /// </summary>
    public static int scoreChangeFactor { get; set; } = DefaultState.scoreChangeFactor;

    /// <summary>
    /// Current credit score of the player.
    /// </summary>
    public static float creditScore { get; set; } = DefaultState.creditScore;

    /// <summary>
    /// Number of days in a row that the player has made regular payments on
    /// all of their active loans.
    /// </summary>
    public static int paymentStreak { get; set; } = DefaultState.paymentStreak;

    /// <summary>
    /// Amount of money the player had before entering the dungeon. Used to
    /// calculate income.
    /// </summary>
    public static float cashOnEntrance { get; set; } = DefaultState.cashOnEntrance;

    /// <summary>
    /// Average income earned across all trips into the dungeon. Used to
    /// calculate credit score.
    /// </summary>
    public static float averageIncome => income.Average();

    /// <summary>
    /// Interest rate modifier. Affected by credit rating.
    /// </summary>
    public static float rateAdjuster = DefaultState.rateAdjuster;

    /// <summary>
    /// Maximum loan amount modifier. Affected by credit rating.
    /// </summary>
    public static float maxLoanAdjuster = DefaultState.maxLoanAdjuster;

    /// <summary>
    /// Currently selected dungeon type. Used by the dungeon generator.
    /// </summary>
    public static DungeonTypeEnum selectedDungeon { get; set; }

    /// <summary>
    /// Whether the player has already see the dungeon entry tutorial
    /// explaining the portals (arrows) in the dungeon.
    /// </summary>
    public static bool sawEntryTutorial = DefaultState.sawEntryTutorial;

    /// <summary>
    /// Whether the player is currently in the story tutorial. The player
    /// should not spend any money while in the tutorial.
    /// </summary>
    public static bool inStoryTutorial = DefaultState.inStoryTutorial;

    /// <summary>
    /// List of singletons that the state manager is tracking. These objects
    /// will be destroyed when the state manager is reset to default.
    /// </summary>
    public static List<GameObject> singletons = new List<GameObject>();

    /// <summary>
    /// Whether the player can pause right now.
    /// </summary>
    public static bool pauseAvailable = DefaultState.pauseAvailable;

    /// <summary>
    /// Whether the player's cash on hand has dropped below 0 in a dungeon.
    /// </summary>
    public static bool playerDead = DefaultState.playerDead;

    /// <summary>
    /// Whether the player has won the game and transitioning to or viewing the
    /// win screen.
    /// </summary>
    public static bool playerWon = DefaultState.playerWon;

    /// <summary>
    /// Whether the player started from the dungeon scene. This is only true
    /// if a developer launches the Dungeon scene directly while inside the
    /// editor, or if a special build of the game is made.
    /// </summary>
    public static bool startedFromDungeon = DefaultState.startedFromDungeon;

    /// <summary>
    /// The last credit score the player had. Used to show the change in
    /// credit score from the last.
    /// </summary>
    public static float lastCreditScore = 0;

    /// <summary>
    /// Reference to the script that handles display of the credit score bar.
    /// </summary>
    public static CreditRatingGUI creditBarScript;

    /// <summary>
    /// Hitting "esc" to exit GUIs sometimes hits the pause code too,
    /// depending on order of execution. Bad things happen when the pause menu
    /// has a different order of execution. So, this is the nicest way to
    /// avoid bringing up the pause menu when someone exits a shop via
    /// keyboard.
    /// </summary>
    public static IEnumerator makePauseAvailableAgain() {
        yield return new WaitForSeconds(0.5f);
        StateManager.pauseAvailable = true;
        yield return null;
    }

    /// <summary>
    /// Calls creditor functions to accumulate interest and gets income earned
    /// from the last dungeon dive.
    /// </summary>
    /// <returns></returns>
    public static void startNewDay() {
        Debug.Log($"Accumulating interest for day {StateManager.timesEntered}");
        // in case cash precision got fudged in the dungeon
        cashOnHand = (float)Math.Round(cashOnHand, 2);
        processDueInvoices();

        income.Add(cashOnHand - cashOnEntrance);
        calcCreditScore();
        Debug.Log($"New debt total: {totalDebt}");
    }

    /// <summary>
    /// Determines if the loans have been paid regularly.
    /// There are consequences to falling behind and slight rewards for keeping
    /// up with payments.
    /// </summary>
    private static void processDueInvoices() {
        // go through all loans and raise the threat level if nothing was paid on them
        // while you're at it, apply interest
        Debug.Log($"Processing {loanList.Count} loans");
        foreach (Loan l in loanList) {
            CreditorData cd = lenders[l.lender];
            if (!l.paid) {
                cd.paid = false;
                cd.threatLevel++;
                paymentStreak = 0;
            }
            l.age++;
            l.paid = false;
            if (!l.inGracePeriod) {
                l.total += (float)Math.Round(l.rate * l.total, 2);
            }
            l.inGracePeriod = false;
        }

        // update creditor threat levels if their loans were paid
        foreach (KeyValuePair<string, CreditorData> entry in lenders) {
            CreditorData cd = entry.Value;
            if (cd.paid) {
                paymentStreak++;
                cd.threatLevel--;
            }
        }
    }

    /// <summary>
    /// Calculates the player's credit score.
    /// Used to apply bonuses/penalties to interest rates and maximum loans
    /// </summary>
    public static void calcCreditScore() {
        lastCreditScore = creditScore;
        int sharkPen = 0;
        int oldestLoan = 0;
        foreach (Loan item in loanList) {
            if (item.age > oldestLoan) {
                oldestLoan = item.age;
            }
            if (item.type == LoanType.Payday) {
                sharkPen++;
            }
        }
        if (oldestLoan > 10) {
            creditScore -= scoreChangeFactor * (oldestLoan - 10);
        }
        creditScore -= sharkPen * scoreChangeFactor;
        if (totalLoans > 5) {
            creditScore -= scoreChangeFactor * 2;
        }
        if (totalDebt > 10000) {
            creditScore -= scoreChangeFactor * 8;
        } else if (totalDebt < 5000) {
            creditScore += scoreChangeFactor * 8;
        }
        creditScore += paymentStreak * scoreChangeFactor;
        if (averageIncome <= 0) {
            creditScore -= scoreChangeFactor * 10;
        } else if (averageIncome > totalDebt * 0.03) {
            creditScore += scoreChangeFactor * 15;
        } else {
            creditScore += scoreChangeFactor * 5;
        }

        // Excellent -------------
        if (creditScore > ExcellentCredit.max) {
            creditScore = ExcellentCredit.max;
        }
        if (creditScore >= ExcellentCredit.min && creditScore <= ExcellentCredit.max) {
            currentRating = ExcellentCredit;
        // Good ------------------
        } else if (creditScore >= GoodCredit.min && creditScore < GoodCredit.max) {
            currentRating = GoodCredit;
        // Fair ------------------
        } else if (creditScore >= FairCredit.min && creditScore < FairCredit.max) {
            currentRating = FairCredit;
        // Poor ------------------
        } else if (creditScore < PoorCredit.max && creditScore >= PoorCredit.min) {
            currentRating = PoorCredit;
        // Abysmal ---------------
        } else if (creditScore < AbysmalCredit.max) {
            currentRating = AbysmalCredit;
        }
        if (creditScore < AbysmalCredit.min) {
            creditScore = AbysmalCredit.min;
        }

        rateAdjuster = DefaultState.rateAdjuster * currentRating.rateModifier;
        maxLoanAdjuster = DefaultState.maxLoanAdjuster * currentRating.loanModifier;
        creditScore = Mathf.Round(creditScore);
        Debug.Log($"Credit score for day {timesEntered}: {creditScore}, delta: {creditScore-lastCreditScore}");
        if (creditBarScript == null) {
            creditBarScript = GameObject.FindObjectOfType<CreditRatingGUI>();
            Debug.Log("it was nul");
        }
        creditBarScript.updateRatingBar();
    }

    /// <summary>
    /// Values associated with the Abysmal credit rating
    /// </summary>
    public static CreditRating AbysmalCredit = new CreditRating {
        min = 300,
        max = 350,
        rating = "Abysmal",
        rateModifier = 2.0f,
        loanModifier = 0.5f
    };

    /// <summary>
    /// Values associated with the Poor credit rating
    /// </summary>
    public static CreditRating PoorCredit = new CreditRating {
        min = 350,
        max = 450,
        rating = "Poor",
        rateModifier = 1.5f,
        loanModifier = 0.75f
    };

    /// <summary>
    /// Values assocaited with the Fair credit rating
    /// </summary>
    public static CreditRating FairCredit = new CreditRating {
        min = 450,
        max = 550,
        rating = "Fair",
        rateModifier = 1f,
        loanModifier = 1f
    };

    /// <summary>
    /// Values associated with the Good credit rating
    /// </summary>
    public static CreditRating GoodCredit = new CreditRating {
        min = 550,
        max = 650,
        rating = "Good",
        rateModifier = 0.9f,
        loanModifier = 1.2f
    };

    /// <summary>
    /// Values associated with the Excellent credit rating
    /// </summary>
    public static CreditRating ExcellentCredit = new CreditRating {
        min = 650,
        max = 850,
        rating = "Excellent",
        rateModifier = 0.75f,
        loanModifier = 1.6f
    };

    /// <summary>
    /// Current player credit rating. Can't be declared until the
    /// CreditRating objects are created.
    /// </summary>
    public static CreditRating currentRating = FairCredit;

    /// <summary>
    /// Resets the StateManager to the default state. Use this when
    /// the player's session needs to be reset. The StateManger is
    /// set up for a "fresh" playthrough.
    /// </summary>
    public static void resetToDefaultState() {
        cashOnHand = DefaultState.cashOnHand;
        loanList.Clear();
        income.Clear();
        nextID = DefaultState.nextID;
        timesEntered = DefaultState.timesEntered;
        totalFloorsVisited = DefaultState.totalFloorsVisited;
        currentFloor = DefaultState.currentFloor;
        scoreChangeFactor = DefaultState.scoreChangeFactor;
        lastCreditScore = 0;
        creditScore = DefaultState.creditScore;
        paymentStreak = DefaultState.paymentStreak;
        cashOnEntrance = DefaultState.cashOnEntrance;
        sawEntryTutorial = DefaultState.sawEntryTutorial;
        inStoryTutorial = DefaultState.inStoryTutorial;
        destroyAllSingletons();
        pauseAvailable = DefaultState.pauseAvailable;
        playerDead = DefaultState.playerDead;
        playerWon = DefaultState.playerWon;
        startedFromDungeon = DefaultState.startedFromDungeon;
        currentRating = FairCredit;
    }

    /// <summary>
    /// Destroy all singletons the state manager is keeping track of.
    /// </summary>
    public static void destroyAllSingletons() {
        foreach (GameObject go in singletons) {
            if (go != null) {
                Debug.Log($"Destroying {go.name} during state reset");
                UnityEngine.Object.Destroy(go);
            }
        }
        singletons.Clear();
        lenders.Clear();
        rateAdjuster = DefaultState.rateAdjuster;
        maxLoanAdjuster = DefaultState.maxLoanAdjuster;
    }
}

/// <summary>
/// Default StateManager values.
/// Value types only!
/// Reference types (lists, gameobjects, etc.) must be cleared inside
/// the reset function.
/// </summary>
public static class DefaultState {
    public readonly static float cashOnHand = 0.0f;
    public readonly static int nextID = 0;
    public readonly static int timesEntered = 0;
    public readonly static int currentFloor = 0;
    public readonly static int totalFloorsVisited = 0;
    public readonly static int scoreChangeFactor = 3;
    public readonly static int creditScore = 500;
    public readonly static int paymentStreak = 0;
    public readonly static float cashOnEntrance = 0.0f;
    public readonly static float rateAdjuster = 1.0f;
    public readonly static float maxLoanAdjuster = 1.0f;
    public readonly static bool sawEntryTutorial = false;
    public readonly static bool inStoryTutorial = false;
    public readonly static bool pauseAvailable = true;
    public readonly static bool playerDead = false;
    public readonly static bool playerWon = false;
    public readonly static bool startedFromDungeon = true;
}

/// <summary>
/// Establishes some basic values for credit score manipulation.
/// </summary>
public struct CreditRating {
    public int min;
    public int max;
    public string rating;

    public float rateModifier;
    public float loanModifier;

    public int range => max - min;
}
