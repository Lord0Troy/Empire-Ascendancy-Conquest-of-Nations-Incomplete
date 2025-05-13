using System.Collections.Generic;
using UnityEngine;

public class HexCellShaderData : MonoBehaviour {

    // Speed at which transitions occur
    const float transitionSpeed = 255f;

    // Texture to store cell data
    Texture2D cellTexture;
    // Array to store color data for each cell
    Color32[] cellTextureData;

    // List of cells that are currently transitioning
    List<HexCell> transitioningCells = new List<HexCell>();

    // Flag to indicate if visibility needs to be reset
    bool needsVisibilityReset;

    // Property to get or set the HexGrid
    public HexGrid Grid { get; set; }

    // Property to enable or disable immediate mode
    public bool ImmediateMode { get; set; }

    // Initialize the shader data with the given dimensions
    public void Initialize (int x, int z) {
        if (cellTexture) {
            // Resize the existing texture if it exists
            cellTexture.Resize(x, z);
        }
        else {
            // Create a new texture if it doesn't exist
            cellTexture = new Texture2D(
                x, z, TextureFormat.RGBA32, false, true
            );
            cellTexture.filterMode = FilterMode.Point;
            cellTexture.wrapModeU = TextureWrapMode.Repeat;
            cellTexture.wrapModeV = TextureWrapMode.Clamp;
            Shader.SetGlobalTexture("_HexCellData", cellTexture);
        }
        Shader.SetGlobalVector(
            "_HexCellData_TexelSize",
            new Vector4(1f / x, 1f / z, x, z)
        );

        // Initialize or clear the cell texture data array
        if (cellTextureData == null || cellTextureData.Length != x * z) {
            cellTextureData = new Color32[x * z];
        }
        else {
            for (int i = 0; i < cellTextureData.Length; i++) {
                cellTextureData[i] = new Color32(0, 0, 0, 0);
            }
        }

        // Clear the list of transitioning cells and enable the component
        transitioningCells.Clear();
        enabled = true;
    }

    // Refresh the terrain type of a specific cell
    public void RefreshTerrain (HexCell cell) {
        cellTextureData[cell.Index].a = (byte)cell.TerrainTypeIndex;
        enabled = true;
    }

    // Refresh the visibility of a specific cell
    public void RefreshVisibility (HexCell cell) {
        int index = cell.Index;
        if (ImmediateMode) {
            // Update visibility and exploration status immediately
            cellTextureData[index].r = cell.IsVisible ? (byte)255 : (byte)0;
            cellTextureData[index].g = cell.IsExplored ? (byte)255 : (byte)0;
        }
        else if (cellTextureData[index].b != 255) {
            // Mark the cell as transitioning if not already
            cellTextureData[index].b = 255;
            transitioningCells.Add(cell);
        }
        enabled = true;
    }

    // Set additional map data for a specific cell
    public void SetMapData (HexCell cell, float data) {
        cellTextureData[cell.Index].b =
            data < 0f ? (byte)0 : (data < 1f ? (byte)(data * 254f) : (byte)254);
        enabled = true;
    }

    // Mark that the view elevation has changed and visibility needs to be reset
    public void ViewElevationChanged () {
        needsVisibilityReset = true;
        enabled = true;
    }

    // Update method called once per frame
    void LateUpdate () {
        if (needsVisibilityReset) {
            // Reset visibility if needed
            needsVisibilityReset = false;
            Grid.ResetVisibility();
        }

        // Calculate the delta value for transitions
        int delta = (int)(Time.deltaTime * transitionSpeed);
        if (delta == 0) {
            delta = 1;
        }
        // Update each transitioning cell
        for (int i = 0; i < transitioningCells.Count; i++) {
            if (!UpdateCellData(transitioningCells[i], delta)) {
                // Remove the cell from the list if it's done transitioning
                transitioningCells[i--] =
                    transitioningCells[transitioningCells.Count - 1];
                transitioningCells.RemoveAt(transitioningCells.Count - 1);
            }
        }

        // Apply the updated cell texture data to the texture
        cellTexture.SetPixels32(cellTextureData);
        cellTexture.Apply();
        // Enable or disable the component based on whether there are transitioning cells
        enabled = transitioningCells.Count > 0;
    }

    // Update the data for a specific cell based on the delta value
    bool UpdateCellData (HexCell cell, int delta) {
        int index = cell.Index;
        Color32 data = cellTextureData[index];
        bool stillUpdating = false;

        // Update exploration status
        if (cell.IsExplored && data.g < 255) {
            stillUpdating = true;
            int t = data.g + delta;
            data.g = t >= 255 ? (byte)255 : (byte)t;
        }

        // Update visibility status
        if (cell.IsVisible) {
            if (data.r < 255) {
                stillUpdating = true;
                int t = data.r + delta;
                data.r = t >= 255 ? (byte)255 : (byte)t;
            }
        }
        else if (data.r > 0) {
            stillUpdating = true;
            int t = data.r - delta;
            data.r = t < 0 ? (byte)0 : (byte)t;
        }

        // Reset the transition flag if no longer updating
        if (!stillUpdating) {
            data.b = 0;
        }
        cellTextureData[index] = data;
        return stillUpdating;
    }
}