using UnityEngine;
using FiscalShock.AI;

/// <summary>
/// Behavior of an environmental object that explodes when struck by a
/// projectile.
/// </summary>
public class ExplodingObject : MonoBehaviour {
    [Tooltip("Sound effect to be played when exploding")]
    public AudioClip explosionNoise;

    [Tooltip("Particle effect to display when exploding")]
    public GameObject explosionEffect;

    /// <summary>
    /// Reference to an audio source to play sound from
    /// </summary>
    private AudioSource aso;

    [Tooltip("Radius of the explosion. Actors caught in this radius will take damage.")]
    public float explosionRadius = 6f;

    [Tooltip("Base damage of the explosion. The actual value is randomized, and also doubled for the player.")]
    public float explosionDamage = 30f;

    /* Layer masks */
    private int PLAYER;
    private int ENEMY;
    private int EXPLOSIVE;
    private int DEBT_COLLECTOR;
    private int hitMask;

    /* References of things to hide/disable during explosion */
    private Renderer[] meshes;
    private Collider[] colliders;

    private bool exploding;
    private PlayerHealth player;

    private void Start() {
        aso = gameObject.AddComponent<AudioSource>();
        PLAYER = LayerMask.NameToLayer("Player");
        ENEMY = LayerMask.NameToLayer("Enemy");
        EXPLOSIVE = LayerMask.NameToLayer("Explosive");
        DEBT_COLLECTOR = LayerMask.NameToLayer("DC Movement");
        hitMask = (1 << PLAYER) | (1 << ENEMY) | (1 << EXPLOSIVE) | (1 << DEBT_COLLECTOR);
        meshes = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
        player = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<PlayerHealth>();
    }

    private void OnTriggerEnter(Collider col) {
        if (exploding) {
            return;
        }
        switch (col.gameObject.tag) {
            case "Enemy Projectile":
            case "Bullet":
            case "Missile":
                explode();
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Explode this object. Typically called when struck by a projectile, but
    /// it could be called by something else. Some ideas include remote or
    /// proximity mines.
    /// </summary>
    public void explode() {
        exploding = true;
        // Damage things that were hit
        Collider[] hitObjects = Physics.OverlapSphere(transform.position, explosionRadius, hitMask);
        foreach (Collider hit in hitObjects) {
            if (hit.gameObject == gameObject) {
                continue;
            }
            float randomizedDamage = explosionDamage * Random.Range(0.5f, 2f);
            int layerHit = hit.gameObject.layer;
            if (layerHit == PLAYER) {  // Player takes extra damage. It's really easy to profit with the dumb AI. Might change it back if the smart AI doesn't clump up.
                player.takeDamage(randomizedDamage * 2);
            }

            // Damage enemies, stun them, and make them flop around
            if (layerHit == ENEMY) {
                EnemyHealth eh = hit.gameObject.GetComponentInChildren<EnemyHealth>();
                if (eh != null) {
                    eh.addImpact(hit.gameObject.transform.position - transform.position, randomizedDamage * 2f);
                    eh?.takeDamage(randomizedDamage);
                    eh?.showDamageExplosion(null, 0f);
                    eh?.stun(3f);
                }
            }
            if (layerHit == DEBT_COLLECTOR) {
                DebtCollectorMovement dcm = hit.gameObject.GetComponentInChildren<DebtCollectorMovement>() ?? hit.gameObject.GetComponentInParent<DebtCollectorMovement>();
                dcm?.externalStun(Random.Range(3f, 8f));
            }

            // Chain explosions
            if (layerHit == EXPLOSIVE) {
                ExplodingObject boom = hit.gameObject.GetComponentInChildren<ExplodingObject>() ?? hit.gameObject.GetComponentInParent<ExplodingObject>();
                if (boom != null && !boom.exploding) {
                    boom.explode();
                }
            }
        }

        // Hide game object, can't destroy it just yet
        foreach (Renderer r in meshes) {
            r.enabled = false;
        }
        foreach (Collider c in colliders) {
            c.enabled = false;
        }

        Destroy(Instantiate(explosionEffect, transform.position, transform.rotation), 2f);

        // Show a light
        GameObject light = new GameObject();
        light.transform.position = transform.position;
        Light l = light.AddComponent<Light>();
        l.color = new Color(1f, 0.29f, 0, 0.5f);
        l.intensity = 5f;
        l.range = explosionRadius * 2;
        l.type = LightType.Point;
        Destroy(light, 1f);

        // Play sound
        aso.PlayOneShot(explosionNoise, Settings.volume);
        Destroy(gameObject, 2f);
    }
}
