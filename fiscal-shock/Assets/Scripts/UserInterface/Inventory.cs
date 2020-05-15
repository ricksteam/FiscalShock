using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace FiscalShock.GUI {
    /// <summary>
    /// Player inventory of weapons. Coordinates with the PlayerShoot and
    /// ShopScript scripts to update values. Also handles selling weapons
    /// from the inventory screen.
    /// </summary>
    public class Inventory : MonoBehaviour {
        [Tooltip("List of inventory slot GUI objects")]
        public List<PlayerInventorySlot> inventorySlots;

        [Tooltip("Sound effect to play when a weapon is sold.")]
        public AudioClip sellSound;

        /// <summary>
        /// Reference to an audio source to play sound effects on
        /// </summary>
        private AudioSource audioS => GetComponent<AudioSource>();

        /// <summary>
        /// Reference to the player shoot script for management of currently
        /// available weapons.
        /// </summary>
        private PlayerShoot playerShoot;

        /// <summary>
        /// Reference to the weapon shop script, so that selling of weapons
        /// can only happen when appropriate.
        /// </summary>
        private ShopScript shopkeep;

        /// <summary>
        /// Last equipped slot. Used to highlight the inventory grid cell
        /// of the currently equipped weapon.
        /// </summary>
        private int latestSlot = -1;

        /// <summary>
        /// Highlight the equipped weapon in the inventory. Not the best place
        /// for this. Ideally fired off asynchronously, but async coroutines
        /// are killed when their parent GameObjects are disabled, which is how
        /// we handle a lot of the "hide this GUI panel" logic.
        /// </summary>
        private void FixedUpdate() {
            updateSelectedGrid();
        }

        /// <summary>
        /// Highlight the currently selected weapon's associated grid
        /// </summary>
        private void updateSelectedGrid() {
            if (playerShoot != null && latestSlot != playerShoot.slot) {
                if (latestSlot >= 0) {
                    inventorySlots[latestSlot].vignette.color = Color.black;
                }
                latestSlot = playerShoot.slot;
                inventorySlots[latestSlot].vignette.color = Color.white;
            }
        }

        /// <summary>
        /// Initialize references and populate the weapon slots.
        /// </summary>
        private void Start() {
            playerShoot = GameObject.FindGameObjectWithTag("Player")?.GetComponentInChildren<PlayerShoot>(true);
            updateWeaponSlots();
        }

        /// <summary>
        /// Callback for when the cursor is hovered over a weapon in the
        /// inventory. Displays that weapon's information.
        /// </summary>
        /// <param name="slot"></param>
        public void showWeaponInfo(int slot) {
            if (slot < playerShoot.guns.Count) {
                inventorySlots[slot].infoBlock.SetActive(true);
            }
        }

        /// <summary>
        /// Callback for when the cursor is no longer hovered over a
        /// weapon in the inventory. Hides that weapon's information.
        /// </summary>
        /// <param name="slot"></param>
        public void hideWeaponInfo(int slot) {
            inventorySlots[slot].infoBlock.SetActive(false);
        }

        /// <summary>
        /// Update inventory slots to match the player's actual available
        /// weapons. Also called by the ShopScript when the player purchases
        /// a weapon.
        /// </summary>
        public void updateWeaponSlots() {
            if (playerShoot == null) {
                // try to find it again
                playerShoot = GameObject.FindGameObjectWithTag("Player")?.GetComponentInChildren<PlayerShoot>(true);
            }

            if (playerShoot == null || playerShoot.guns == null) {
                return;
            }

            // filled slots
            for (int i = 0; i < playerShoot.guns.Count; ++i) {
                updateWeaponSlot(i);
            }
            // empty slots
            for (int i = playerShoot.guns.Count; i < inventorySlots.Count; ++i) {
                PlayerInventorySlot p = inventorySlots[i];
                p.sprite.color = new Color(0, 0, 0, 0);
                p.infoBlock.SetActive(false);
            }
            updateSelectedGrid();
        }

        /// <summary>
        /// Update a specific inventory slot.
        /// </summary>
        /// <param name="slot">associated weapon slot</param>
        private void updateWeaponSlot(int slot) {
            PlayerInventorySlot p = inventorySlots[slot];
            WeaponStats w = playerShoot.guns[slot].GetComponentInChildren<WeaponStats>(true);

            p.sprite.sprite = w.image;
            p.sprite.color = new Color(1, 1, 1, 1);
            p.name.text = w.fullName;
            p.family.text = $"{w.actionToString()} {w.weaponFamily}";
            // Strength, jitter, delay
            p.statsLeft.text = $"Strength: {w.strength}\nDeviation: {w.deviation}\nDelay: {w.fireDelay}";
            // Projectile type, bullet cost, value (modify based on what the shopkeep will pay for it)
            p.statsRight.text = $"{w.projectileTypeToString()}\nBullet Cost: {w.bulletCost}\nValue: {w.buybackPrice}";
        }

        /// <summary>
        /// Callback for clicking on an inventory cell (should be renamed, but
        /// won't be, to avoid last-minute bugs from not updating all
        /// references).
        /// If the player is in front of the weapon shop, the weapon is sold.
        /// If the player is in the dungeon, the weapon will be equipped when
        /// the game is unpaused.
        /// </summary>
        /// <param name="slot">associated weapon slot</param>
        public void sellWeapon(int slot) {
            if (playerShoot == null) {
                // try to find it again
                playerShoot = GameObject.FindGameObjectWithTag("Player")?.GetComponentInChildren<PlayerShoot>(true);
            }
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Dungeon" && playerShoot.guns.Count > slot) {
                playerShoot.slot = slot;
                return;
            }
            if (playerShoot.guns.Count <= 1) {  // disallow selling last weapon
                return;
            }
            if (shopkeep == null) {
                GameObject shop = GameObject.Find("WeaponShop");
                if (shop == null) {
                    return;
                }
                shopkeep = shop.GetComponentInChildren<ShopScript>();
            }
            if (!shopkeep.playerIsInTriggerZone || playerShoot.guns.Count <= slot) {
                return;
            }
            GameObject gun = playerShoot.guns[slot];
            WeaponStats w = gun.GetComponentInChildren<WeaponStats>(true);

            playerShoot.guns.Remove(gun);
            StateManager.cashOnHand += w.buybackPrice;
            Debug.Log($"Sold {w.fullName} for {w.buybackPrice}");
            audioS.PlayOneShot(sellSound, Settings.volume);
            // Guns should be attached to a transform that positions them on the player
            Destroy(w.bulletPoolObject);
            Destroy(gun.transform.parent.gameObject);
            updateWeaponSlots();
        }

        /// <summary>
        /// Add a weapon to the player's inventory. Updates the PlayerShoot list
        /// of available weapons, as well as the inventory screen.
        /// </summary>
        /// <param name="selection"></param>
        public void addWeapon(GameObject selection) {
            if (playerShoot == null) {
                // try to find it again
                playerShoot = GameObject.FindGameObjectWithTag("Player")?.GetComponentInChildren<PlayerShoot>(true);
            }
            if (playerShoot.guns.Count > 9) {
                Debug.Log("You don't have enough arms and bags of holding for another weapon.");
                return;
            }
            // add the gun models!
            // when the camera is parented to the gun, the gun's transform is reset to be relative to the camera, so we need to fix it after the parenting
            Vector3 correctPosition = selection.transform.localPosition;
            Vector3 correctScale = selection.transform.localScale;
            Quaternion correctRotation = selection.transform.localRotation;
            selection.transform.parent = GameObject.FindGameObjectWithTag("MainCamera").transform;
            selection.transform.localPosition = correctPosition;
            selection.transform.localRotation= correctRotation;
            selection.transform.localScale = correctScale;
            // The gun model itself that needs to be added to the list is not the topmost object
            GameObject actualGun = selection.GetComponentInChildren<WeaponStats>(true).gameObject;
            playerShoot.guns.Add(actualGun);
            updateWeaponSlots();
        }
    }

    /// <summary>
    /// Data class to expose inventory slot GUI elements.
    /// </summary>
    [System.Serializable]
    public class PlayerInventorySlot {
        [Tooltip("Weapon image")]
        public Image sprite;

        [Tooltip("Weapon name")]
        public TextMeshProUGUI name;

        [Tooltip("Weapon family")]
        public TextMeshProUGUI family;

        [Tooltip("Reference to the left half of the info text block")]
        public TextMeshProUGUI statsLeft;

        [Tooltip("Reference to the right half of the info text block")]
        public TextMeshProUGUI statsRight;

        [Tooltip("Reference to the entire info block")]
        public GameObject infoBlock;

        [Tooltip("Reference to the vignette/highlight image. Currently a green vignette.")]
        public Image vignette;
    }
}
