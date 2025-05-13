using UnityEngine;

// The EdgeVertices struct represents a set of vertices along an edge.
public struct EdgeVertices {

    // The vertices along the edge.
    public Vector3 v1, v2, v3, v4, v5;

    // Constructor that initializes the vertices by interpolating between two corners.
    public EdgeVertices (Vector3 corner1, Vector3 corner2) {
        v1 = corner1;
        v2 = Vector3.Lerp(corner1, corner2, 0.25f); // Interpolates at 25% between corner1 and corner2
        v3 = Vector3.Lerp(corner1, corner2, 0.5f);  // Interpolates at 50% between corner1 and corner2
        v4 = Vector3.Lerp(corner1, corner2, 0.75f); // Interpolates at 75% between corner1 and corner2
        v5 = corner2;
    }

    // Constructor that initializes the vertices by interpolating between two corners with a custom step.
    public EdgeVertices (Vector3 corner1, Vector3 corner2, float outerStep) {
        v1 = corner1;
        v2 = Vector3.Lerp(corner1, corner2, outerStep); // Interpolates at outerStep between corner1 and corner2
        v3 = Vector3.Lerp(corner1, corner2, 0.5f);      // Interpolates at 50% between corner1 and corner2
        v4 = Vector3.Lerp(corner1, corner2, 1f - outerStep); // Interpolates at (1 - outerStep) between corner1 and corner2
        v5 = corner2;
    }

    // Static method that interpolates between two EdgeVertices instances.
    public static EdgeVertices TerraceLerp (
        EdgeVertices a, EdgeVertices b, int step)
    {
        EdgeVertices result;
        result.v1 = HexMetrics.TerraceLerp(a.v1, b.v1, step); // Interpolates v1 between a and b
        result.v2 = HexMetrics.TerraceLerp(a.v2, b.v2, step); // Interpolates v2 between a and b
        result.v3 = HexMetrics.TerraceLerp(a.v3, b.v3, step); // Interpolates v3 between a and b
        result.v4 = HexMetrics.TerraceLerp(a.v4, b.v4, step); // Interpolates v4 between a and b
        result.v5 = HexMetrics.TerraceLerp(a.v5, b.v5, step); // Interpolates v5 between a and b
        return result;
    }
}