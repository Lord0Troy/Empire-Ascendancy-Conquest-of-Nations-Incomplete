using UnityEngine;

public static class Bezier {

    // Calculates a point on a quadratic Bezier curve
    // a: The start point of the curve
    // b: The control point of the curve
    // c: The end point of the curve
    // t: The parameter (0 <= t <= 1) that determines the position on the curve
    public static Vector3 GetPoint (Vector3 a, Vector3 b, Vector3 c, float t) {
        float r = 1f - t; // Calculate the inverse of t
        // Return the point on the curve at parameter t
        return r * r * a + 2f * r * t * b + t * t * c;
    }

    // Calculates the derivative of a point on a quadratic Bezier curve
    // a: The start point of the curve
    // b: The control point of the curve
    // c: The end point of the curve
    // t: The parameter (0 <= t <= 1) that determines the position on the curve
    public static Vector3 GetDerivative (
        Vector3 a, Vector3 b, Vector3 c, float t
    ) {
        // Return the derivative of the point on the curve at parameter t
        return 2f * ((1f - t) * (b - a) + t * (c - b));
    }
}