using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System;

/// <summary>
/// Runs the loan businesses in the hub; allows for payment and
/// acquisition of debt. Also manages the GUI text fields.
/// </summary>
public class Creditor : MonoBehaviour {
    [Tooltip("Sound to play when positive cash events happen.")]
    public AudioClip paymentSound;

    [Tooltip("Sound to play when negative events or failures happen.")]
    public AudioClip failureSound;

    /// <summary>
    /// Whether the player is close enough to interact with this creditor.
    /// </summary>
    private bool playerIsInTriggerZone = false;

    /// <summary>
    /// Audio source to play the sound effects on.
    /// </summary>
    private AudioSource audioS;

    [Tooltip("References to the dialog and loan information text on the GUI.")]
    public TextMeshProUGUI dialogText, maxLoanText;

    [Tooltip("Reference to the main GUI panel of this creditor.")]
    public GameObject creditorPanel;

    [Tooltip("References to the input fields for the loan ID and amount for paying off a loan.")]
    public TMP_InputField paymentId, paymentAmount;

    [Tooltip("List of GUI input elements for each type of available loan.")]
    public List<LoanEntry> loanEntries;

    [Tooltip("Data about each different type of loan available here.")]
    public List<ValidLoan> validLoans;

    [Tooltip("Base maximum amount this lender will offer. Modified by credit rating.")]
    public float maxLoanAmount = 3000f;

    [Tooltip("Lender will not lend loans anymore once their threat level has passed this")]
    public int threatThreshold;

    [Tooltip("Unique ID for this creditor. Multiple NPCs could share a creditor ID if they are intended to be different interfaces to the same financial institution, e.g., different branches of the same bank.")]
    public string creditorId;

    [Tooltip("Threat level this creditor starts at.")]
    public int baseThreatLevel;

    /// <summary>
    /// Reference to this creditor's current threat level, stored in the state
    /// manager.
    /// </summary>
    public int threatLevel => StateManager.lenders[creditorId].threatLevel;

    /// <summary>
    /// Reference to loans associated with this creditor, stored in the state
    /// manager.
    /// </summary>
    public List<Loan> myLoans => StateManager.loanList.Where(l => l.lender == creditorId).ToList();

    /// <summary>
    /// Number of loans taken out against this creditor.
    /// </summary>
    public int numberOfLoans => myLoans.Count;

    /// <summary>
    /// Total debt owed to this creditor.
    /// </summary>
    public float loanTotal => myLoans.Sum(l => l.total);

    /// <summary>
    /// No support for scrolling loan list yet. Hard cap on the
    /// number of loans this creditor may lend.
    /// </summary>
    private readonly int loanHardCap = 3;

    [Tooltip("Whether the player begins the game indebted to this creditor.")]
    public bool initiallyIndebted;

    [Tooltip("The amount of debt owed to this creditor, if the player is initially indebted.")]
    public float initialDebtAmount;

    [Tooltip("Type of loan of the initial debt.")]
    public ValidLoan initialDebtType;

    [Tooltip("Added to the player's cash on hand when starting the game. The starting cash on hand is the sum of this field from all creditors in the starting hub.")]
    public float initialRemainingCash;

    /// <summary>
    /// Default dialog text; saved to reset when the player leaves the GUI.
    /// </summary>
    private String defaultText;

    /// <summary>
    /// Don't blow out my ears with initial loans as soon as you walk
    /// into the hub. Just a modifier on the cash out sound played when
    /// a loan is taken out, since that same function is called when you
    /// first start the game and have initial debt applied.
    /// </summary>
    private int debtIsLoud = 1;

    [Tooltip("Reference to the tutorial GUI panel for creditors.")]
    public GameObject tutorial;

    /// <summary>
    /// Update state when the player is close enough to interact.
    /// </summary>
    /// <param name="col">collider that entered the trigger zone</param>
    private void OnTriggerEnter(Collider col) {
        if (col.gameObject.tag == "Player") {
            playerIsInTriggerZone = true;
        }
    }

    /// <summary>
    /// Update state when the player leaves the interactable area.
    /// </summary>
    /// <param name="col">collider that exited the trigger zone</param>
    private void OnTriggerExit(Collider col) {
        if (col.gameObject.tag == "Player") {
            playerIsInTriggerZone = false;
        }
    }

    /// <summary>
    /// Runs on start. Adds creditors to tracker if not already there,
    /// adds initial debt and sets the interest rate text for that day.
    /// </summary>
    private void Start()
    {
        audioS = GetComponent<AudioSource>();
        defaultText = dialogText.text;
        creditorPanel.SetActive(false);

        // if StateManager isn't already tracking me by my creditorID, add me
        if (!StateManager.lenders.ContainsKey(creditorId)) {
            CreditorData cd = new CreditorData(false, baseThreatLevel);
            StateManager.lenders.Add(creditorId, cd);
        }

        // these assumptions imply this is the first visit to the hub, as the player hasn't seen the dungeon entry tutorial (and therefore hasn't been inside the dungeon, so they can't have gone to the hub more than once)
        // when this is the case, accrue initial debt if applicable
        if (!StateManager.sawEntryTutorial && StateManager.cashOnHand <= StateManager.totalDebt && initiallyIndebted) {
            debtIsLoud = 0;
            initialDebtType.addLoanInput.text = initialDebtAmount.ToString();
            addDebt(initialDebtType);
            StateManager.cashOnHand -= initialDebtAmount - initialRemainingCash;
            debtIsLoud = 1;
            dialogText.text = defaultText;
        }

        updateFields();
        Debug.Log($"{creditorId} threat: {threatLevel}, loans: {numberOfLoans}, sum: {loanTotal}");
        // iterator through valid loans that changes loan text in the GUI based on type
        float helperRate, helperCollateral;
        foreach (ValidLoan item in validLoans) {
            if (item.loanType != LoanType.Secured) {
                helperRate = (float)Math.Round(item.interestRate * StateManager.rateAdjuster * 100f, 2);
                item.loanData.text = $"@ {helperRate.ToString("N2")}%";
            } else {
                helperRate = (float)Math.Round(item.interestRate * StateManager.rateAdjuster * item.collateralRateReduction * 100f, 2);
                helperCollateral = (float)Math.Round(item.collateralAmountPercent * 100f, 2);
                item.loanData.text = $"@ {helperRate.ToString("N2")}%\n+ {helperCollateral.ToString("N2")}%";
            }
        }
    }

    /// <summary>
    /// Checks if the player is in range and is pressing the interaction key.
    /// If so, it will activate the cursor and turn on the menu
    /// </summary>
    private void Update()
    {
        if (playerIsInTriggerZone) {
            if (Input.GetKeyDown(Settings.interactKey) && !tutorial.activeSelf) {
                Time.timeScale = 0;
                Settings.forceUnlockCursorState();
                updateFields();
                creditorPanel.SetActive(true);
                StateManager.pauseAvailable = false;
            }
            if (Input.GetKeyDown(Settings.pauseKey)) {
                BackClick();
            }
            if (!Settings.values.sawLoanTutorial) {
                Time.timeScale = 0;
                Settings.forceUnlockCursorState();
                tutorial.SetActive(true);
                Settings.values.sawLoanTutorial = true;
            }
        }
    }

    /// <summary>
    /// Attempts to add a loan of the specified amount of money,
    /// failure conditions included and will affect the dialog
    /// </summary>
    public void addDebt(ValidLoan associatedLoanData) {
        try{
            float amount = float.Parse(associatedLoanData.addLoanInput.text, CultureInfo.InvariantCulture.NumberFormat);
            LoanType loanType = associatedLoanData.loanType;
            if (amount < 0.0f) {
                dialogText.text = associatedLoanData.errorText;
                audioS.PlayOneShot(failureSound, Settings.volume);
                Debug.Log($"{creditorId}: You're supposed to borrow from *me*!");
                return;
            }

            if (
                (threatLevel < threatThreshold)
                && ((maxLoanAmount * StateManager.maxLoanAdjuster) >= (loanTotal + amount))  // secured loans bypass this a bit...
                && (numberOfLoans < loanHardCap)
                ) {
                float modifiedInterest = associatedLoanData.interestRate * associatedLoanData.collateralRateReduction * StateManager.rateAdjuster;  // collateralRateReduction should always be 1 for unsecured loans
                float collateral = (float)Math.Round(associatedLoanData.collateralAmountPercent * amount, 2);  // collateralAmountPercent should always be 0 for unsecured loans
                float modifiedAmount = (float)Math.Round(collateral + amount, 2);
                Loan newLoan = new Loan(StateManager.nextID, modifiedAmount, modifiedInterest, loanType, collateral, creditorId);
                Debug.Log($"{creditorId}: adding ${modifiedAmount} loan with a  {collateral} deposit @ {modifiedInterest*100}%");
                StateManager.loanList.Add(newLoan);
                StateManager.cashOnHand += amount;
                StateManager.nextID++;
                initialDebtType.addLoanInput.text = "";
                updateFields();

                dialogText.text = associatedLoanData.successText;
                audioS.PlayOneShot(paymentSound, Settings.volume * debtIsLoud);
                return;
            }
            Debug.Log($"{creditorId}: Bad dog, no biscuit!");
            dialogText.text = associatedLoanData.failureText;
            audioS.PlayOneShot(failureSound, Settings.volume);
            return;
        }
        catch(Exception e){
            dialogText.text = "Oh, hi, who are you? And why do you smell like motor oil?";
            Debug.Log($"Exception found: {e}");
        }
    }

    /// <summary>
    /// Pays the selected loan by the specified amount.
    /// If the player pays it off, the win condition is triggered.
    /// </summary>
    public void payDebt() {
        try{
            float amount = float.Parse(paymentAmount.text, CultureInfo.InvariantCulture.NumberFormat);
            int loanNum = int.Parse(paymentId.text);
            Debug.Log($"{creditorId}: receiving ${amount} payment on loan ${loanNum}");
            Loan selectedLoan = myLoans.First(l => l.ID == loanNum);
            ValidLoan thisLoan = validLoans.First(m => m.loanType == selectedLoan.type);
            // Don't pay off another lender's loans here...
            if(thisLoan == null){
                dialogText.text = "I think you are looking for the other guy.";
                audioS.PlayOneShot(failureSound, Settings.volume);
                return;
            }
            if (amount < 0.0f || selectedLoan.Equals(null)) {
                dialogText.text = thisLoan.errorPaidText;
                audioS.PlayOneShot(failureSound, Settings.volume);
                return;
            }
            if (StateManager.cashOnHand < amount) { // amount is more than money on hand
                dialogText.text = thisLoan.failurePaidText;
                audioS.PlayOneShot(failureSound, Settings.volume);
                return;
            }
            else if (selectedLoan.total <= amount) { // amount is more than the debt
                StateManager.cashOnHand -= selectedLoan.total;
                StateManager.cashOnHand += selectedLoan.collateral;  // get back extra amount paid on secured loans
                StateManager.loanList.Remove(selectedLoan);
                checkWin();
                updateFields();
                dialogText.text = thisLoan.successPaidText;
                audioS.PlayOneShot(paymentSound, Settings.volume);
                return;
            }
            else { // none of the above
                // reduce debt and money by amount
                selectedLoan.total -= amount;
                StateManager.cashOnHand -= amount;
                updateFields();
                dialogText.text = thisLoan.successPaidText;
                audioS.PlayOneShot(paymentSound, Settings.volume);
                return;
            }
        }
        catch (Exception e) {
            dialogText.text = "Wait... What happened? Where am I?";
            Debug.Log($"Exception found: {e}");
        }
    }

    /// <summary>
    /// Updates the GUI so that the player can see their loans.
    /// Allows for accurate information to be displayed.
    /// </summary>
    private void updateFields() {
        float tempRate;
        for (int i = 0; i < loanEntries.Count; ++i) {
            if (i >= myLoans.Count) {
                loanEntries[i].id.text = "";
                loanEntries[i].amount.text = "";
                loanEntries[i].type.text = "";
            }
            else {
                tempRate = (float)Math.Round(myLoans[i].rate * 100f, 2);
                loanEntries[i].id.text = myLoans[i].ID.ToString();
                loanEntries[i].amount.text = myLoans[i].total.ToString("N2");
                string typetext = "dummy";
                switch (myLoans[i].type) {
                    case LoanType.Unsecured:
                    case LoanType.Payday:
                        typetext = $"{tempRate.ToString("N2")}%";
                        break;
                    case LoanType.Secured:
                        typetext = $"{tempRate.ToString("N2")}% ({myLoans[i].collateral.ToString("N2")} down)";
                        break;
                }
                loanEntries[i].type.text = typetext;
            }
        }
        maxLoanText.text = $"Max Credit: {(maxLoanAmount * StateManager.maxLoanAdjuster).ToString("N2")}\nTotal Loans: {loanTotal.ToString("N2")}";
    }

    /// <summary>
    /// Determines if the player has won the game. Called after any loan is
    /// fully paid off
    /// </summary>
    public void checkWin() {
        if (StateManager.loanList.Count == 0) {
            StateManager.playerWon = true;
            GameObject.FindGameObjectWithTag("Loading Screen").GetComponent<LoadingScreen>().startLoadingScreen("WinGame");
        }
    }

    /// <summary>
    /// Turns off the panel and allows full movement/pause control
    /// </summary>
    public void BackClick() {
        Time.timeScale = 1;
        dialogText.text = defaultText;
        tutorial.SetActive(false);
        creditorPanel.SetActive(false);
        Settings.forceLockCursorState();
        StartCoroutine(StateManager.makePauseAvailableAgain());
    }

    /// <summary>
    /// Disables the tutorial window and unpauses the game.
    /// </summary>
    public void dismissTutorial() {
        tutorial.SetActive(false);
        Time.timeScale = 1;
    }
}

/// <summary>
/// Helper class to expose similar collections of GUI elements in the
/// Unity inspector. Makes building prefabs easier.
/// </summary>
[System.Serializable]
public class LoanEntry {
    public TextMeshProUGUI id, type, amount;
}
