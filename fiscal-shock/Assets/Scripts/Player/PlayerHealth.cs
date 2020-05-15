using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Takes care of damage taken by the player and manages misc functions
/// like invincibility and the player flashlight.
/// </summary>
public class PlayerHealth : MonoBehaviour {
    /// <summary>
    /// Reference to the hit vignette on the HUD.
    /// </summary>
    private GameObject hitVignette;

    /// <summary>
    /// Modifier on how long to display the hit vignette, based on damage
    /// taken.
    /// </summary>
    private float timeMultiplier = 0.01f;

    [Tooltip("Whether the player is currently invincible. Public to expose it in the inspector during development and debugging.")]
    public bool invincible;

    /// <summary>
    /// Reference to the player's flashlight. The flashlight color is used to
    /// inform the player that they are invincible by coloring it green.
    /// </summary>
    private Light playerFlashlight;

    /// <summary>
    /// Initialize references and the default state.
    /// </summary>
    private void Start() {
        playerFlashlight = GameObject.FindGameObjectWithTag("Player Flashlight").GetComponent<Light>();
        resetVignette();
    }

    /// <summary>
    /// Update the reference to the vignette object and hide it.
    /// </summary>
    public void resetVignette(){
        hitVignette = GameObject.FindGameObjectWithTag("Player Hit Vignette");
        hitVignette.SetActive(false);
    }

    private void OnCollisionEnter(Collision col) {
        if (col.gameObject.tag == "Enemy Projectile") {
            BulletBehavior bullet = col.gameObject.GetComponent<BulletBehavior>();
            if (bullet.hitSomething) {
                return;
            }
            bullet.hitSomething = true;
            takeDamage(bullet.damage);
        }
    }

    /// <summary>
    /// Display the hit vignette HUD object (red effect) to inform the
    /// player that they have taken damage.
    /// </summary>
    /// <param name="duration">in seconds, how long to display the vignette</param>
    private IEnumerator showHitVignette(float duration) {
        hitVignette.SetActive(true);
        yield return new WaitForSeconds(duration);
        hitVignette.SetActive(false);

        yield return null;
    }

    /// <summary>
    /// Activates when the player is hit, if not dead the player loses money
    /// equal to the damage taken. If the player is out of money, they lose.
    /// </summary>
    public void takeDamage(float damage) {
        if (!invincible && !StateManager.playerDead) {
            StateManager.cashOnHand -= damage;
            StartCoroutine(showHitVignette(damage * timeMultiplier));
        }
        if (StateManager.cashOnHand < 0) {
            hitVignette.SetActive(false);
            StateManager.playerDead = true;
            Destroy(GameObject.Find("DungeonMusic"));
            GameObject.FindGameObjectWithTag("Loading Screen").GetComponent<LoadingScreen>().startLoadingScreen("LoseGame");
        }
    }

    public void endGameByDebtCollector() {
        if (!StateManager.playerDead && !invincible) {
            Debug.Log($"Player was caught by debt collector on floor {StateManager.currentFloor} with {StateManager.totalDebt} debt");
            hitVignette.SetActive(false);
            StateManager.playerDead = true;
            float brokenKneecapPayment = StateManager.cashOnHand * 0.5f;
            GameObject.FindGameObjectWithTag("HUD").GetComponentInChildren<FeedbackController>().shoot(brokenKneecapPayment);
            StateManager.cashOnHand -= brokenKneecapPayment;
            Destroy(GameObject.Find("DungeonMusic"));
            GameObject.FindGameObjectWithTag("Loading Screen").GetComponent<LoadingScreen>().startLoadingScreen("LoseGame");
        }
    }

    /// <summary>
    /// Enables invincibility when the player loads in.
    /// </summary>
    public IEnumerator enableIframes(float duration) {
        if (playerFlashlight == null) {  // if start in dungeon scene
            playerFlashlight = GameObject.FindGameObjectWithTag("Player Flashlight").GetComponent<Light>();
        }
        invincible = true;
        // Set the color to green as an indicator that something's up
        playerFlashlight.color = new Color(0, 1, 0.75f, 0.5f);
        yield return new WaitForFixedUpdate();  // そして時は動き出す…
        yield return new WaitForSeconds(duration);
        invincible = false;
        playerFlashlight.color = Color.white;
        yield return null;
    }

    /// <summary>
    /// Used to disable invincibility after players have a few seconds to
    /// position themselves, or if they try to attack an enemy.
    /// </summary>
    private void Update() {
        // Disable invincibility:
        // - after 2 seconds when any key is pressed (you get 5 seconds to reposition)
        // - when LMB is pressed and time is flowing (assumed to be firing at enemy)
        if (invincible && ((Input.anyKey && Time.timeSinceLevelLoad > 5f) || (Input.GetMouseButton(0) && Time.timeSinceLevelLoad > 0.1f))) {
            invincible = false;
            playerFlashlight.color = Color.white;
        }
        if (hitVignette == null) {
            resetVignette();
        }
    }
}
