using UnityEngine;

// HexHash struct represents a hash with five float values
public struct HexHash {

    // Public float fields to store hash values
    public float a, b, c, d, e;

    // Static method to create a new HexHash with random values
    public static HexHash Create () {
        HexHash hash;
        // Assign random values to each field, scaled by 0.999 to avoid the maximum value of 1
        hash.a = Random.value * 0.999f;
        hash.b = Random.value * 0.999f;
        hash.c = Random.value * 0.999f;
        hash.d = Random.value * 0.999f;
        hash.e = Random.value * 0.999f;
        return hash; // Return the created HexHash
    }
}