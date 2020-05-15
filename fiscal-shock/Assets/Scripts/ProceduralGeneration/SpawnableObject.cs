using UnityEngine;

/// <summary>
/// Data class to expose fields for a particular object that can be
/// spawned during procedural generation.
/// </summary>
[System.Serializable]
public class SpawnableObject {
    [Tooltip("Object prefab.")]
    public GameObject prefab;

    [Tooltip("Weight on [0, 1] applied against spawn chance. Higher weights indicate more likely spawning. A weight of 1 indicates that this item will always spawn when picked.")]
    [Range(0f, 1f)]
    public float weight = 1;

    [Tooltip("Apply a random color to the first mesh renderer encountered in the game object. The prefab should have the colorable mesh renderer at the top.")]
    public bool randomizeColor;
    public bool alsoColorLight;
}

/// <summary>
/// Data class to expose additional fields for enemies that can be spawned
/// during procedural generation.
/// This is currently not used and not up to date with all parameters of
/// an enemy. The original intent was that certain dungeon themes could have
/// different base stats for the same enemy.
/// The enemy stat adjustment for difficulty adjustment does not use these
/// values.
/// </summary>
[System.Serializable]
public class SpawnableEnemy : SpawnableObject {
    [Tooltip("Base health value.")]
    public float baseHealth = 30f;

    [Tooltip("Base damage value.")]
    public float baseDamage = 3f;

    [Tooltip("The speed at which the object moves.")]
    public float movementSpeed = 3f;

    [Tooltip("The speed at which the object turns.")]
    public float rotationSpeed = 7f;

    [Tooltip("The absolute minimum distance away from the player.")]
    public float safeRadiusMin = 4f;

    [Tooltip("Creates safe radius in case object ends up too close to player.")]
    public float safeRadiusMax = 5f;
}
