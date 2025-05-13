using UnityEngine;

[System.Serializable] // This attribute allows the struct to be serialized and visible in the Unity Inspector.
public struct HexFeatureCollection {

    public Transform[] prefabs; // An array of Transform objects to hold different prefab options.

    // Method to pick a prefab based on a float choice value.
    // The choice value is multiplied by the length of the prefabs array and cast to an integer to get the index.
    public Transform Pick (float choice) {
        return prefabs[(int)(choice * prefabs.Length)];
    }
}