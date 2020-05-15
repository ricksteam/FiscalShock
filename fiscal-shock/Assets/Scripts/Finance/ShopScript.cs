using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using FiscalShock.Procedural;
using FiscalShock.GUI;

/// <summary>
/// This is the script that runs the shop. Weapons are generated and
/// the shop sells these to the player.
/// </summary>
public class ShopScript : MonoBehaviour {
    [Tooltip("Reference to the weapon shop tutorial GUI panel.")]
    public GameObject tutorial;

    [Tooltip("Reference to the debug menu used to text procedural weapon generation.")]
    public GameObject debugMenu;

    /// <summary>
    /// Audio source used to play sound effects.
    /// </summary>
    private AudioSource audioS;

    [Tooltip("Sound effect played on successful transactions.")]
    public AudioClip paymentSound;

    [Tooltip("Sound effect played on failed transactions.")]
    public AudioClip failureSound;

    [Tooltip("Reference to NPC's dialog text box.")]
    public TextMeshProUGUI dialogText;

    [Tooltip("Reference to the GUI panel of this shop.")]
    public GameObject shopPanel;

    /// <summary>
    /// Reference to a procedural weapon generator.
    /// </summary>
    private WeaponGenerator genny;

    [Tooltip("List of inventory slots that will be filled with weapons by the weapon generator.")]
    public List<ShopInventorySlot> inventorySlots;

    /// <summary>
    /// Track whether the player can interact with this shop.
    /// </summary>
    public bool playerIsInTriggerZone { get; private set; } = false;

    /// <summary>
    /// Weapons currently in stock. Updated whenever new weapons are
    /// generated.
    /// </summary>
    private List<GameObject> inventory = new List<GameObject>();

    /// <summary>
    /// Reference to the player's shoot script. Needed to add new weapons
    /// on purchase.
    /// </summary>
    private PlayerShoot playerShoot;

    /// <summary>
    /// Reference to the player's inventory. Needed to validate whether the
    /// player can purchase more weapons.
    /// </summary>
    private Inventory playerInventory;

    /// <summary>
    /// Hard cap on maximum number of guns the player can hold. Based on the
    /// inventory GUI having only 9 slots. The inventory GUI must be redone,
    /// similar to the loan GUI, to allow more than the hard cap here.
    /// </summary>
    private readonly int MAX_GUNS = 9;

    /// <summary>
    /// Dialog options for the shopkeeper, selected at random.
    /// </summary>
    private readonly string[] purchaseDialogs = {
        "Alright, here ya go, try not to get yourself kilt. No, really, I mean kilt, not killed, but don't do that either.",
        "Pretty weird that the only way to make money around here is scrapping robots, innit?"
    };
    private readonly string[] brokeDialogs = {
        "You sure you have enough there, pal? I ain't running a charity here...",
        "I think you are a bit short today. Go scrap some bots and come back."
    };
    private readonly string[] fullInventoryDialogs = {
        "Greedy, aren'cha? Why don't you lend me some of your stock?",
        "I don't think you could fit another in your extra-dimensional satchel.",
        "I'll pay more than anybody for your leftovers. 10% is still more than 0%!"
    };

    /// <summary>
    /// Selects a dialog string from the dialog sets up above.
    /// </summary>
    private string getRandomDialog(string[] dialogs) {
        int val = Random.Range(0, dialogs.Length);
        return dialogs[val];
    }

    /// <summary>
    /// Detects the player and shows the tutorial if not already seen.
    /// </summary>
    /// <param name="col">collider that entered the trigger zone</param>
    private void OnTriggerEnter(Collider col) {
        if (col.gameObject.tag == "Player") {
            playerIsInTriggerZone = true;
            if (!Settings.values.sawShopTutorial) {
                Time.timeScale = 0;
                Settings.forceUnlockCursorState();
                tutorial.SetActive(true);
                Settings.values.sawShopTutorial = true;
            }
        }
    }

    /// <summary>
    /// Dismisses the tutorial and allows normal gameplay to continue.
    /// </summary>
    public void dismissTutorial() {
        tutorial.SetActive(false);
        Time.timeScale = 1;
    }

    /// <summary>
    /// Handles colliders leaving the trigger zone.
    /// </summary>
    /// <param name="col">collider that left the trigger zone</param>
    private void OnTriggerExit(Collider col) {
        if (col.gameObject.tag == "Player") {
            playerIsInTriggerZone = false;
        }
    }

    /// <summary>
    /// Sets up the shops inventory and shooting script for later use.
    /// Generates weapons for the shop's inventory whenever the player enters
    /// the hub.
    /// </summary>
    private void Start() {
        audioS = GetComponent<AudioSource>();
        shopPanel.SetActive(false);
        playerShoot = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<PlayerShoot>();
        playerInventory = GameObject.FindGameObjectWithTag("Player Inventory").GetComponentInChildren<Inventory>();
        genny = GetComponent<WeaponGenerator>();

        // On the first day, the weapons generated should be reasonably affordable, otherwise, the game is impossible or unusually difficult, as the player would be forced to take out a payday loan
        do {
            generateNewWeapons();
        } while (!affordableWeaponsDayZero());
    }

    /// <summary>
    /// With random generation, the player could, in rare cases, be unable to
    /// purchase any weapons on day 0 (due to loan caps). Make sure there's at
    /// least one weapon that's a reasonable price on day 0.
    /// </summary>
    private bool affordableWeaponsDayZero() {
        if (StateManager.sawEntryTutorial) {
            return true;
        }
        const float maxAllowedValue = 1333f;
        bool acceptablePrices = false;
        foreach (GameObject weapon in inventory) {
            WeaponStats stats = weapon.GetComponentInChildren<WeaponStats>(true);
            if (stats.price <= maxAllowedValue) {
                acceptablePrices = true;
                break;
            }
        }
        return acceptablePrices;
    }

    /// <summary>
    /// Generates new weapons in the shop. Every slot in the inventory gets
    /// a new weapon and all buttons and markers are set to defaults.
    /// </summary>
    public void generateNewWeapons() {
        Debug.Log($"Populating {inventorySlots.Count} shop slots");
        inventory.Clear();
        foreach (ShopInventorySlot slot in inventorySlots) {
            GameObject ran = genny.generateRandomWeapon();
            WeaponStats stats = ran.GetComponentInChildren<WeaponStats>(true);
            inventory.Add(ran);

            slot.buttonObject.GetComponent<Button>().image.sprite = stats.image;

            slot.purchased = false;
            slot.soldout.SetActive(false);
            slot.buttonObject.GetComponent<Button>().interactable = true;
            slot.name.text = stats.fullName;
            slot.pricetag.text = stats.price.ToString("F2");
            slot.family.text = $"{stats.actionToString()} {stats.weaponFamily}";
            slot.stats.text = $"Projectile: {stats.projectileTypeToString()}\nStrength: {stats.strength}\nDeviation: {stats.deviation}\nDelay: {stats.fireDelay}\nPrice per Shot: {stats.bulletCost}";
        }
    }

    /// <summary>
    /// Display the weapon information. Should be attached to an event listener
    /// that deals with hovering over the inventory button.
    /// </summary>
    /// <param name="slot">associated inventory slot</param>
    public void hoverInventory(int slot) {
        if (!inventorySlots[slot].purchased) {
            inventorySlots[slot].infoBlock.SetActive(true);
        }
    }

    /// <summary>
    /// Hide the previously-displayed weapon information. Should be attached to
    /// an event listener that deals with moving the cursor off of the inventory
    /// button.
    /// </summary>
    /// <param name="slot">associated inventory slot</param>
    public void blurInventory(int slot) {
        inventorySlots[slot].infoBlock.SetActive(false);
    }

    /// <summary>
    /// Determines if the player is in range of the shop and pressing the
    /// interact key.
    /// If so, it activates the menu and unlocks the cursor.
    /// </summary>
    private void Update() {
        if (playerIsInTriggerZone) {
            if (Input.GetKeyDown(Settings.interactKey) && !tutorial.activeSelf) {
                Time.timeScale = 0;
                Settings.forceUnlockCursorState();
                shopPanel.SetActive(true);
                StateManager.pauseAvailable = false;
            }
            if (Input.GetKeyDown(Settings.pauseKey)) {
                BackClick();
            }
            if (Input.GetKeyDown("f5")) {
                debugMenu.SetActive(!debugMenu.activeSelf);
            }
        }
    }

    /// <summary>
    /// Attempts to purchase a weapon and displays dialog based on the result
    /// of the attempt.
    /// </summary>
    /// <param name="slot">associated inventory slot</param>
    public void purchaseWeapon(int slot) {
        ShopInventorySlot shopSlot = inventorySlots[slot];
        GameObject selection = inventory[slot];
        WeaponStats stats = selection.GetComponentInChildren<WeaponStats>(true);

        if (stats.price > StateManager.cashOnHand) {
            audioS.PlayOneShot(failureSound, Settings.volume);
            dialogText.text = getRandomDialog(brokeDialogs);
            return;
        }

        if (playerShoot.guns.Count >= MAX_GUNS) {
            audioS.PlayOneShot(failureSound, Settings.volume);
            dialogText.text = getRandomDialog(fullInventoryDialogs);
            return;
        }

        // can purchase
        playerInventory.addWeapon(selection);
        shopSlot.purchased = true;

        // only remove cash if everything above that succeeded and the player received the gun
        StateManager.cashOnHand -= stats.price;
        shopSlot.buttonObject.GetComponent<Button>().interactable = false;
        shopSlot.infoBlock.SetActive(false);
        shopSlot.soldout.SetActive(true);
        shopSlot.pricetag.text = "-";
        audioS.PlayOneShot(paymentSound, Settings.volume);
        dialogText.text = getRandomDialog(purchaseDialogs);
    }

    /// <summary>
    /// Handles exiting the weapon shop by hiding GUI panels and resetting
    /// player state.
    /// </summary>
    public void BackClick()
    {
        dialogText.text = "What are ya buyin'?";
        shopPanel.SetActive(false);
        tutorial.SetActive(false);
        StateManager.pauseAvailable = true;
        Settings.forceLockCursorState();
        Time.timeScale = 1;
    }

    /// <summary>
    /// Debug function. Update the StateManager.timesEntered value.
    /// This value affects weapon generation.
    /// </summary>
    /// <param name="inp">input field GUI element</param>
    public void setStateTimesEntered(TMP_InputField inp) {
        int val = int.Parse(inp.text);
        StateManager.timesEntered = val;
        Debug.Log($"StateManager.timesEntered: {StateManager.timesEntered}");
    }

    /// <summary>
    /// Debug function. Update the StateManager.value.totalFloorsVisited value.
    /// This value affects weapon generation.
    /// </summary>
    public void setStateTotalFloors(TMP_InputField inp) {
        int val = int.Parse(inp.text);
        StateManager.totalFloorsVisited = val;
        Debug.Log($"StateManager.timesEntered: {StateManager.totalFloorsVisited}");
    }
}

/// <summary>
/// Data for each shop inventory slot. Shows some basic text and data
/// about the weapon in question.
/// </summary>
[System.Serializable]
public class ShopInventorySlot {
    public GameObject buttonObject;
    public TextMeshProUGUI pricetag;
    public TextMeshProUGUI name;
    public TextMeshProUGUI family;
    public TextMeshProUGUI stats;
    public GameObject infoBlock;
    public GameObject soldout;
    public bool purchased;
}
