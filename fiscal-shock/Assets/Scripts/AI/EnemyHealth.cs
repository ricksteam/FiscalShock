using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FiscalShock.Procedural;
using FiscalShock.AI;

/// <summary>
/// Manages AI health and reactions to being damaged.
/// </summary>
public class EnemyHealth : MonoBehaviour {
    [Tooltip("Base/maximum health of this enemy.")]
    public float startingHealth = 30;
    [Tooltip("Reference to the particle effect used for visual hit feedback (bullets).")]
    public GameObject explosion;
    [Tooltip("Reference to the particle effect used for visual hit feedback (missiles).")]
    public GameObject bigExplosion;
    [Tooltip("Audio clip played when this enemy is struck.")]
    public AudioClip hitSoundClip;
    [Tooltip("Reference to audio source that handles playing sound effects.")]
    public AudioSource hitSound;
    [Tooltip("Base cash amount earned when this enemy is defeated.")]
    public float pointValue = 20;

    private float currentHealth;

    /// <summary>
    /// Last object that hit this bot. Disables "piercing" effect of bullets
    /// where they keep hurting while in the enemy's hitbox.
    /// </summary>
    private GameObject lastBulletCollision;

    [Tooltip("Reference to this bot's own animation manager script.")]
    public AnimationManager animationManager;

    /// <summary>
    /// Whether this bot is dead, to prevent things from continuing or re-dying
    /// </summary>
    private bool dead;

    /// <summary>
    /// Queue for object pooling
    /// </summary>
    public Queue<GameObject> explosions = new Queue<GameObject>();

    /// <summary>
    /// Number of explosions to pool to avoid garbage collection spikes
    /// </summary>
    private readonly int smallExplosionLimit = 12;

    /// <summary>
    /// Queue for object pooling
    /// </summary>
    public Queue<GameObject> bigExplosions = new Queue<GameObject>();

    /// <summary>
    /// Number of explosions to pool to avoid garbage collection spikes
    /// </summary>
    private readonly int bigExplosionLimit = 6;

    [Tooltip("Reference to this bot's stun effect object.")]
    public GameObject stunEffect;

    /// <summary>
    /// Reference to the visual feedback controller
    /// </summary>
    private FeedbackController feed;

    /// <summary>
    /// Reference to this bot's controller, used for explosives
    /// </summary>
    public CharacterController ragdoll { get; set; }

    [Tooltip("Mass, used as part of calculating simulated physics from explosion forces.")]
    public float mass = 3.0f;

    /// <summary>
    /// Track the current impact that blew away this bot.
    /// </summary>
    private Vector3 impact;

    /// <summary>
    /// Counter checked against max enmity duration
    /// </summary>
    private float enmityCounter;

    [Tooltip("Whether the bot is actively pursuing the player.")]
    public bool enmityActive;

    [Tooltip("How long after being ambushed should this bot try to find the player?")]
    public float maxEnmityDuration;

    [Tooltip("When ambushed, the enemy will try to alert other enemies willing to assist within this radius.")]
    public float cryForHelpRadius;

    /// <summary>
    /// Initialize variables and references to external objects and create
    /// object pools.
    /// </summary>
    private void Start() {
        feed = GameObject.FindGameObjectWithTag("HUD").GetComponent<FeedbackController>();
        currentHealth = startingHealth;
        ragdoll = gameObject.GetComponent<CharacterController>();

        for (int i = 0; i < smallExplosionLimit; ++i) {
            GameObject splode = Instantiate(explosion, gameObject.transform.position + transform.up, gameObject.transform.rotation);
            splode.transform.parent = transform;
            splode.SetActive(false);
            explosions.Enqueue(splode);
        }

        for (int i = 0; i < bigExplosionLimit; ++i) {
            GameObject splode = Instantiate(bigExplosion, gameObject.transform.position + transform.up, gameObject.transform.rotation);
            splode.transform.parent = transform;
            splode.SetActive(false);
            bigExplosions.Enqueue(splode);
        }
    }

    /// <summary>
    /// Update values every frame.
    /// </summary>
    private void Update() {
        if (enmityActive) {
            enmityCounter += Time.deltaTime;
        }
        if (enmityCounter >= maxEnmityDuration) {
            enmityActive = false;
        }
    }

    /// <summary>
    /// Fire off the stun routine if this enemy is not already dead.
    /// Should be called after taknig damage to prevent stunned corpses.
    /// </summary>
    /// <param name="duration">time to remain stunned for</param>
    public void stun(float duration) {
        if (!dead) {
            StartCoroutine(stunRoutine(duration));
        }
    }

    /// <summary>
    /// Actual stun routine, run asynchronously. Disables other AI behavior for
    /// the duration of the stun.
    /// </summary>
    /// <param name="duration">time to remain stunned for</param>
    private IEnumerator stunRoutine(float duration) {
        stunEffect.SetActive(true);
        EnemyShoot es = gameObject.GetComponentInChildren<EnemyShoot>();
        EnemyMovement em = gameObject.GetComponentInChildren<EnemyMovement>();
        es.enabled = false;
        em.stunned = true;

        // Apply a primitive physics force to simulate being knocked back by the stun
        // This would be changed from `while` to `if` and moved to the FixedUpdate function if knockbacks should occur while not stunned.
        while (impact.magnitude > 0.2f) {
            ragdoll.Move(impact * Time.deltaTime);
            impact = Vector3.Lerp(impact, Vector3.zero, duration * Time.deltaTime);
            yield return null;
        }

        em.enabled = true;
        em.stunned = false;
        stunEffect.SetActive(false);

        yield return null;
    }

    /// <summary>
    /// Processes this enemy being struck by something.
    /// If the enemy is defeated, the player receives money, a death
    /// animation plays, and this enemy is destroyed.
    /// If the enemy was not active (enemy is ambushed), the enemy
    /// will call for help from nearby enemies.
    /// </summary>
    /// <param name="damage">amount of damage received</param>
    /// <param name="paybackMultiplier">modifier on cash earned from defeating this enemy; zero if the player should not gain more than the base value; currently, this is only the case when the enemy is defeated by an exploding object</param>
    public void takeDamage(float damage, int paybackMultiplier=0) {
        float prevHealth = currentHealth;
        currentHealth -= damage;

        // Process enemy defeat
        if (currentHealth <= 0 && !dead) {
            // Temporary boost to earnings for usability testing as we don't have time to fully adjust enemy stats and it's sometimes very hard to earn cash
            float temporaryBonusMod = Gaussian.next(1.2f, 0.5f);
            // Get up to half the original health as payback, adjusted due to fish cannon scoring too much cash because it OHKOs right now
            float profit = (pointValue + Mathf.Clamp(prevHealth, 1, startingHealth * paybackMultiplier)) * temporaryBonusMod;
            StateManager.cashOnHand += profit;
            float deathDuration = animationManager.playDeathAnimation();
            GetComponent<EnemyMovement>().enabled = false;
            GetComponent<EnemyShoot>().enabled = false;
            animationManager.animator.PlayQueued("shrink");
            Destroy(gameObject, deathDuration + 0.5f);
            dead = true;
            feed.profit(profit);
        }

        // Process ambush reaction asynchronously
        if (!enmityActive) {
            StartCoroutine(cryForHelp(transform.position));
        }

        // Activate enemy hostility
        enmityActive = true;
        enmityCounter = 0;
    }

    /// <summary>
    /// Sends out a delayed call for help from the original position where the
    /// enemy was struck. The call is delayed so that the player can attempt to
    /// defeat the enemy before it successfully attracts its allies.
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    private IEnumerator cryForHelp(Vector3 location) {
        yield return new WaitForSeconds(1f * UnityEngine.Random.Range(1f, 3f));

        if (dead) {
            yield return null;
        }

        // If there are any willing assistants in the cry for help radius around the original location where I was hit, activate their hostility
        foreach (Collider col in Physics.OverlapSphere(location, cryForHelpRadius, (1 << gameObject.layer))) {
            if (col.gameObject.tag == "Assistant") {
                EnemyHealth ally = col.gameObject.GetComponent<EnemyHealth>();
                ally.enmityCounter = 0;
                ally.enmityActive = true;
            }
        }
        yield return null;
    }

    /// <summary>
    /// Play particle effect on being struck by pulling it from the object
    /// pool. Bigger explosions are louder.
    /// </summary>
    /// <param name="queue">explosion queue to pull from</param>
    /// <param name="volumeMultiplier">modifier on audio feedback</param>
    public void showDamageExplosion(Queue<GameObject> queue, float volumeMultiplier = 0.65f) {
        // Play sound effect and explosion particle system
        if (queue == null) {
            queue = bigExplosions;
        }
        GameObject explode = queue.Dequeue();
        explode.SetActive(true);
        hitSound.PlayOneShot(hitSoundClip, volumeMultiplier * Settings.volume);
        explosions.Enqueue(explode);
        explode.transform.position = transform.position + transform.up;
        explode.transform.rotation = transform.rotation;
        explode.transform.parent = gameObject.transform;
        StartCoroutine(explode.GetComponent<Explosion>().timeout());
    }

    /// <summary>
    /// Creates a knockback impact force on this bot. Currently,
    /// the impact force's effect is only simulated while stunned.
    /// </summary>
    /// <param name="direction">direction to be blown away to</param>
    /// <param name="force">amount of force applied</param>
    public void addImpact(Vector3 direction, float force) {
        direction.Normalize();
        if (direction.y < 0) {
            direction.y = -direction.y;
        } else {
            direction.y = 1f;
        }
        impact += direction.normalized * force / mass;
    }
}
