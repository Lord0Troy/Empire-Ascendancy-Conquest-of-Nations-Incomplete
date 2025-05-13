using UnityEngine;

public static class HexMetrics {

    // Conversion constants between outer and inner radius
    public const float outerToInner = 0.866025404f;
    public const float innerToOuter = 1f / outerToInner;

    // Hexagon dimensions
    public const float outerRadius = 10f;
    public const float innerRadius = outerRadius * outerToInner;
    public const float innerDiameter = innerRadius * 2f;

    // Factors for solid and blended areas
    public const float solidFactor = 0.8f;
    public const float blendFactor = 1f - solidFactor;

    // Water factors
    public const float waterFactor = 0.6f;
    public const float waterBlendFactor = 1f - waterFactor;

    // Elevation step size
    public const float elevationStep = 3f;

    // Terrace configuration
    public const int terracesPerSlope = 2;
    public const int terraceSteps = terracesPerSlope * 2 + 1;
    public const float horizontalTerraceStepSize = 1f / terraceSteps;
    public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);

    // Perturbation strengths
    public const float cellPerturbStrength = 4f;
    public const float elevationPerturbStrength = 1.5f;

    // Elevation offsets
    public const float streamBedElevationOffset = -1.75f;
    public const float waterElevationOffset = -0.5f;

    // Wall dimensions and offsets
    public const float wallHeight = 4f;
    public const float wallYOffset = -1f;
    public const float wallThickness = 0.75f;
    public const float wallElevationOffset = verticalTerraceStepSize;
    public const float wallTowerThreshold = 0.5f;

    // Bridge design length
    public const float bridgeDesignLength = 7f;

    // Noise scale
    public const float noiseScale = 0.003f;

    // Chunk size
    public const int chunkSizeX = 5, chunkSizeZ = 5;

    // Hash grid configuration
    public const int hashGridSize = 256;
    public const float hashGridScale = 0.25f;

    // Hash grid array
    static HexHash[] hashGrid;

    // Hexagon corners
    static Vector3[] corners = {
        new Vector3(0f, 0f, outerRadius),
        new Vector3(innerRadius, 0f, 0.5f * outerRadius),
        new Vector3(innerRadius, 0f, -0.5f * outerRadius),
        new Vector3(0f, 0f, -outerRadius),
        new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
        new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
        new Vector3(0f, 0f, outerRadius)
    };

    // Feature thresholds for different levels
    static float[][] featureThresholds = {
        new float[] {0.0f, 0.0f, 0.4f},
        new float[] {0.0f, 0.4f, 0.6f},
        new float[] {0.4f, 0.6f, 0.8f}
    };

    // Noise texture
    public static Texture2D noiseSource;

    // Sample noise at a given position
    public static Vector4 SampleNoise (Vector3 position) {
        Vector4 sample = noiseSource.GetPixelBilinear(
            position.x * noiseScale,
            position.z * noiseScale
        );

        // Handle wrapping
        if (Wrapping && position.x < innerDiameter * 1.5f) {
            Vector4 sample2 = noiseSource.GetPixelBilinear(
                (position.x + wrapSize * innerDiameter) * noiseScale,
                position.z * noiseScale
            );
            sample = Vector4.Lerp(
                sample2, sample, position.x * (1f / innerDiameter) - 0.5f
            );
        }

        return sample;
    }

    // Wrap size for hex grid
    public static int wrapSize;

    // Check if wrapping is enabled
    public static bool Wrapping {
        get {
            return wrapSize > 0;
        }
    }

    // Initialize the hash grid with a given seed
    public static void InitializeHashGrid (int seed) {
        hashGrid = new HexHash[hashGridSize * hashGridSize];
        Random.State currentState = Random.state;
        Random.InitState(seed);
        for (int i = 0; i < hashGrid.Length; i++) {
            hashGrid[i] = HexHash.Create();
        }
        Random.state = currentState;
    }

    // Sample the hash grid at a given position
    public static HexHash SampleHashGrid (Vector3 position) {
        int x = (int)(position.x * hashGridScale) % hashGridSize;
        if (x < 0) {
            x += hashGridSize;
        }
        int z = (int)(position.z * hashGridScale) % hashGridSize;
        if (z < 0) {
            z += hashGridSize;
        }
        return hashGrid[x + z * hashGridSize];
    }

    // Get feature thresholds for a given level
    public static float[] GetFeatureThresholds (int level) {
        return featureThresholds[level];
    }

    // Get the first corner of a hexagon in a given direction
    public static Vector3 GetFirstCorner (HexDirection direction) {
        return corners[(int)direction];
    }

    // Get the second corner of a hexagon in a given direction
    public static Vector3 GetSecondCorner (HexDirection direction) {
        return corners[(int)direction + 1];
    }

    // Get the first solid corner of a hexagon in a given direction
    public static Vector3 GetFirstSolidCorner (HexDirection direction) {
        return corners[(int)direction] * solidFactor;
    }

    // Get the second solid corner of a hexagon in a given direction
    public static Vector3 GetSecondSolidCorner (HexDirection direction) {
        return corners[(int)direction + 1] * solidFactor;
    }

    // Get the middle of the solid edge of a hexagon in a given direction
    public static Vector3 GetSolidEdgeMiddle (HexDirection direction) {
        return
            (corners[(int)direction] + corners[(int)direction + 1]) *
            (0.5f * solidFactor);
    }

    // Get the first water corner of a hexagon in a given direction
    public static Vector3 GetFirstWaterCorner (HexDirection direction) {
        return corners[(int)direction] * waterFactor;
    }

    // Get the second water corner of a hexagon in a given direction
    public static Vector3 GetSecondWaterCorner (HexDirection direction) {
        return corners[(int)direction + 1] * waterFactor;
    }

    // Get the bridge vector for a hexagon in a given direction
    public static Vector3 GetBridge (HexDirection direction) {
        return (corners[(int)direction] + corners[(int)direction + 1]) *
            blendFactor;
    }

    // Get the water bridge vector for a hexagon in a given direction
    public static Vector3 GetWaterBridge (HexDirection direction) {
        return (corners[(int)direction] + corners[(int)direction + 1]) *
            waterBlendFactor;
    }

    // Interpolate between two vectors for terraces
    public static Vector3 TerraceLerp (Vector3 a, Vector3 b, int step) {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;
        float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
        a.y += (b.y - a.y) * v;
        return a;
    }

    // Interpolate between two colors for terraces
    public static Color TerraceLerp (Color a, Color b, int step) {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

    // Interpolate between two vectors for walls
    public static Vector3 WallLerp (Vector3 near, Vector3 far) {
        near.x += (far.x - near.x) * 0.5f;
        near.z += (far.z - near.z) * 0.5f;
        float v =
            near.y < far.y ? wallElevationOffset : (1f - wallElevationOffset);
        near.y += (far.y - near.y) * v + wallYOffset;
        return near;
    }

    // Get the offset for wall thickness
    public static Vector3 WallThicknessOffset (Vector3 near, Vector3 far) {
        Vector3 offset;
        offset.x = far.x - near.x;
        offset.y = 0f;
        offset.z = far.z - near.z;
        return offset.normalized * (wallThickness * 0.5f);
    }

    // Get the edge type between two elevations
    public static HexEdgeType GetEdgeType (int elevation1, int elevation2) {
        if (elevation1 == elevation2) {
            return HexEdgeType.Flat;
        }
        int delta = elevation2 - elevation1;
        if (delta == 1 || delta == -1) {
            return HexEdgeType.Slope;
        }
        return HexEdgeType.Cliff;
    }

    // Perturb a position based on noise
    public static Vector3 Perturb (Vector3 position) {
        Vector4 sample = SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * cellPerturbStrength;
        return position;
    }
}