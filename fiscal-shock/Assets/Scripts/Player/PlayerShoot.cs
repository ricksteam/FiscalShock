﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Mutable player firing state
/// </summary>
public class PlayerFiringState {
    public bool firingAutomatic;
    public bool alreadyFired;

    public bool needToUpdateLossFeed;
    public float nextLossCost;

    public bool weaponCooling;
    public bool holsteringWeapon;
    public bool drawingWeapon;

    public int lastWeapon = -1;
}

/// <summary>
/// Handles player attack logic and processes related input.
/// </summary>
public class PlayerShoot : MonoBehaviour {
    [Tooltip("Sound effect to play when out of ammo (not enough cash to fire bullet of currently equipped weapon)")]
    public AudioClip outOfAmmo;

    [Tooltip("Sound effect to play when the weapon is cooling down (firing before the fire delay has passed, for non-automatic weapons)")]
    public AudioClip fireRateSound;

    [Tooltip("Current list of guns the player has access to.")]
    public List<GameObject> guns;

    /* Assigned at runtime */
    /// <summary>
    /// Current weapon.
    /// </summary>
    public GameObject weapon { get; private set; }

    /// <summary>
    /// Reference to the HUD crosshair, so it can be hidden or shown.
    /// </summary>
    public Image crossHair { get; private set; }

    /// <summary>
    /// Reference to the current gun's audio source.
    /// </summary>
    private AudioSource gunAudioSource => currentWeaponStats?.gunAudioSource;

    /// <summary>
    /// Reference to the current gun's weapon stats.
    /// </summary>
    private WeaponStats currentWeaponStats;

    /// <summary>
    /// Reference to the visual feedback controller on the HUD.
    /// </summary>
    private FeedbackController feed;

    /* Related to current state */
    /// <summary>
    /// Mutable state data to be passed to weaponstats
    /// </summary>
    public PlayerFiringState state = new PlayerFiringState();

    /// <summary>
    /// Currently equipped weapon slot, equal to guns[slot-1]
    /// </summary>
    public int slot = 0;

    /// <summary>
    /// Whether the player is currently firing, kept private here for
    /// the input action callbacks
    /// </summary>
    private bool firing;

    /// <summary>
    /// How long the weapon animation time has played
    /// </summary>
    private float animatedTime;

    /// <summary>
    /// Reference to the animation object for a weapon, needed to play
    /// draw/holster animations
    /// </summary>
    private Animation weaponAnimator;

    /// <summary>
    /// Handle input events for firing the weapon
    /// </summary>
    /// <param name="cont">context</param>
    public void OnFire(InputAction.CallbackContext cont) {
        if (cont.phase == InputActionPhase.Canceled || Time.timeScale == 0) {
            state.alreadyFired = false;
            firing = false;
            return;
        }
        firing = cont.phase == InputActionPhase.Performed || cont.phase == InputActionPhase.Started;
        state.alreadyFired = !firing;
    }

    /// <summary>
    /// Handle input events for scrolling through the weapon list
    /// </summary>
    /// <param name="cont">context</param>
    public void OnWeaponSwap(InputAction.CallbackContext cont) {
        if (cont.phase != InputActionPhase.Performed || Time.timeScale == 0 || state.drawingWeapon || state.holsteringWeapon) {
            return;
        }
        slot += (int)cont.ReadValue<float>();
        if (slot >= guns.Count) {  // Wrap around after the last weapon
            slot = 0;
        } else if (slot < 0) {
            slot = guns.Count-1;
        }
    }

    /// <summary>
    /// Handle input events for swapping to a specific weapon
    /// </summary>
    /// <param name="cont"></param>
    public void OnWeaponSelect(InputAction.CallbackContext cont) {
        if (state.drawingWeapon || state.holsteringWeapon) {
            return;
        }
        int selectedSlot = (int)cont.ReadValue<float>();
        if (selectedSlot > 0 && selectedSlot <= guns.Count) {
            slot = selectedSlot - 1;
        }
    }

    /// <summary>
    /// Initialize references and states
    /// </summary>
    private void Start() {
        GameObject hud = GameObject.FindGameObjectWithTag("HUD");
        if (hud != null) {
            HUD hudScript = hud.GetComponent<HUD>();
            if (hudScript != null) {
                hudScript.playerTransform = transform;
            }
            feed = hud.GetComponentInChildren<FeedbackController>(true);
        }
        GameObject tmp = GameObject.FindGameObjectWithTag("Crosshair");
        if (tmp != null) {
            crossHair = tmp.GetComponentInChildren<Image>(true);
            crossHair.enabled = false;
        }
        if (guns == null || guns.Count < 1) {
            guns = new List<GameObject>();
        }
    }

    /// <summary>
    /// Reset the reference to the HUD feedback controller
    /// </summary>
    public void resetFeed() {
        feed = GameObject.FindGameObjectWithTag("HUD").GetComponent<FeedbackController>();
    }

    /// <summary>
    /// When this script is disabled (as when the hub is entered),
    /// stop firing and hide things
    /// </summary>
    private void OnDisable() {
        stopFiringAuto();
        hideWeapon();
    }

    /// <summary>
    /// When this script is enabled (as when entering a dungeon),
    /// show things and equip the appropriate weapon if one wasn't equipped
    /// </summary>
    private void OnEnable() {
        if (guns == null || guns.Count < 1) {
            return;
        }
        if (guns.Count <= slot) {
            slot = 0;
        }
        if (state.lastWeapon >= 0 && state.lastWeapon < guns.Count && weapon == guns[state.lastWeapon]) {
            weapon.SetActive(true);
            crossHair.enabled = currentWeaponStats.showCrosshair;
        }
    }

    /// <summary>
    /// Hide the currently equipped weapon and crosshair
    /// </summary>
    private void hideWeapon() {
        if (weapon != null) {
            weapon.SetActive(false);
            // sometimes, it's null, strangely
            if (crossHair != null) {
                crossHair.enabled = false;
            }
        }
    }

    /// <summary>
    /// Stop playing auto-fire sound effects and update the state
    /// </summary>
    private void stopFiringAuto() {
        state.firingAutomatic = false;
        gunAudioSource?.Stop();
    }

    /// <summary>
    /// Process current player shooting state each frame
    /// </summary>
    private void Update() {
        /* Check to see if we need to stop automatic firing */
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Hub" || StateManager.playerDead || StateManager.playerWon) {
            stopFiringAuto();
            return;
        }

        // Released button on auto-fire weapons
        if (state.firingAutomatic && !firing && currentWeaponStats.fireDelay < 0.03f) {
            stopFiringAuto();
            gunAudioSource.PlayOneShot(currentWeaponStats.autoStop, Settings.volume);
        }

        // Time stopped, do nothing
        if (Time.timeScale == 0) {
            return;
        }

        if (state.needToUpdateLossFeed) {
            feed?.shoot(state.nextLossCost);
            state.needToUpdateLossFeed = false;
        }

        // Start weapon swap if necessary
        HolsterWeapon();
        LoadWeapon();
        playWeaponAnimation();

        // Don't proceed to try firing until we're done changing weapons
        // If we don't have a weapon by this point, we can't continue
        if (weaponAnimator.isPlaying || state.drawingWeapon || state.holsteringWeapon || weapon == null || !weapon.activeSelf) {
            return;
        }

        /**********************************************************************/

        // After this point, only continue if we're trying to fire
        if (!firing || state.alreadyFired) {
            return;
        }

        // Make sure player has enough money to fire
        if (!StateManager.inStoryTutorial && StateManager.cashOnHand < currentWeaponStats.bulletCost) {
            //if (!state.alreadyFired) {  // otherwise, the sound is auto fired
                stopFiringAuto();
                gunAudioSource.PlayOneShot(outOfAmmo, Settings.volume);
                state.alreadyFired = true;
            //}
            return;
        }

        // Can we fire yet?
        if (state.weaponCooling) {
            if (!state.firingAutomatic) {  // otherwise, the sound is auto fired
                gunAudioSource.PlayOneShot(fireRateSound, Settings.volume);
                state.alreadyFired = true;
            }
            return;
        }

        // Finally, we can fire...
        currentWeaponStats.fire(state);
        StartCoroutine(firingCooldown());
    }

    /// <summary>
    /// Fade the crosshair while we're not allowed to shoot
    /// </summary>
    private IEnumerator firingCooldown() {
        if (currentWeaponStats.fireDelay <= 0 || state.weaponCooling) {
            yield return null;
        }
        state.weaponCooling = true;

        crossHair.color = new Color(1f, 1f, 1f, 0.3f);
        for (float i = 0; i < currentWeaponStats.fireDelay; i += Time.deltaTime) {
            crossHair.fillAmount = i/currentWeaponStats.fireDelay;
            yield return null;
        }

        state.weaponCooling = false;
        crossHair.fillAmount = 1;
        crossHair.color = new Color(1f, 1f, 1f, 0.8f);
        yield return null;
    }

    /// <summary>
    /// Updates weapon slot and enables weapon object
    /// </summary>
    public void LoadWeapon() {
        if (state.lastWeapon >= 0 && (guns == null || state.holsteringWeapon || guns[slot] == weapon)) {
            return;
        }
        state.drawingWeapon = true;
        // Update weapon slot and enable weapon object
        // Shut up roslyn, Unity doesn't like conditional access here
        if (weapon != null) {
            weapon.SetActive(false);
        }

        // The weapon select functions already verify the new slot is good
        weapon = guns[slot];
        weaponAnimator = weapon.GetComponent<Animation>();
        weapon.SetActive(true);
        state.lastWeapon = slot;
        currentWeaponStats = weapon.GetComponent<WeaponStats>();
    }

    /// <summary>
    /// If player is currently holstering or drawing a weapon, alter weapon
    /// position to animate the process.
    /// </summary>
    private void playWeaponAnimation() {
        if (weapon == null) {
            return;
        }

        if (state.drawingWeapon) {
            state.holsteringWeapon = false;
            if (!weaponAnimator.isPlaying && animatedTime == 0) {
                weaponAnimator["draw"].speed = 2;
                weaponAnimator.Play("draw");
            }
            // After animation has run set weapon changing to false
            if (animatedTime >= (weaponAnimator.clip.length * 0.5f)) {
                animatedTime = 0f;
                state.drawingWeapon = false;
                crossHair.enabled = currentWeaponStats.showCrosshair;
                weapon.transform.rotation = weapon.transform.parent.rotation;
                return;
            }
            animatedTime += Time.deltaTime;
        // ---------------------------------------------------------------------
        } else if (state.holsteringWeapon) {
            state.drawingWeapon = false;
            if (!weaponAnimator.isPlaying && animatedTime == 0) {
                weaponAnimator.Play("holster");
            }
            // After animation has run, get the new weapon
            if (animatedTime >= weaponAnimator.clip.length) {
                animatedTime = 0f;
                state.holsteringWeapon = false;
                LoadWeapon();
                return;
            }
            animatedTime += Time.deltaTime;
        }
    }

    /// <summary>
    /// Update the state to holster the current weapon when appropriate
    /// </summary>
    public void HolsterWeapon() {
        // If weapon is already selected, do nothing
        if (guns[slot] == weapon || state.drawingWeapon) {
            return;
        }
        state.holsteringWeapon = true;
        // Hide crossHair while weapon is changing
        crossHair.enabled = false;
    }
}
