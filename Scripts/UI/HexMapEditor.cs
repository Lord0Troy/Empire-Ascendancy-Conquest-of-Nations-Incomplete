using UnityEngine;
using UnityEngine.EventSystems;
using System.IO;

public class HexMapEditor : MonoBehaviour {

    // Reference to the HexGrid
    public HexGrid hexGrid;

    // Reference to the terrain material
    public Material terrainMaterial;

    // Active elevation level
    int activeElevation;
    // Active water level
    int activeWaterLevel;

    // Active levels for urban, farm, plant, and special index
    int activeUrbanLevel, activeFarmLevel, activePlantLevel, activeSpecialIndex;

    // Active terrain type index
    int activeTerrainTypeIndex;

    // Brush size for editing
    int brushSize;

    // Flags to apply elevation and water level
    bool applyElevation = true;
    bool applyWaterLevel = true;

    // Flags to apply urban, farm, plant, and special index levels
    bool applyUrbanLevel, applyFarmLevel, applyPlantLevel, applySpecialIndex;

    // Enum for optional toggle states
    enum OptionalToggle {
        Ignore, Yes, No
    }

    // Modes for river, road, and walled
    OptionalToggle riverMode, roadMode, walledMode;

    // Dragging state and direction
    bool isDrag;
    HexDirection dragDirection;
    HexCell previousCell;

    // Set the active terrain type index
    public void SetTerrainTypeIndex (int index) {
        activeTerrainTypeIndex = index;
    }

    // Set whether to apply elevation
    public void SetApplyElevation (bool toggle) {
        applyElevation = toggle;
    }

    // Set the active elevation level
    public void SetElevation (float elevation) {
        activeElevation = (int)elevation;
    }

    // Set whether to apply water level
    public void SetApplyWaterLevel (bool toggle) {
        applyWaterLevel = toggle;
    }

    // Set the active water level
    public void SetWaterLevel (float level) {
        activeWaterLevel = (int)level;
    }

    // Set whether to apply urban level
    public void SetApplyUrbanLevel (bool toggle) {
        applyUrbanLevel = toggle;
    }

    // Set the active urban level
    public void SetUrbanLevel (float level) {
        activeUrbanLevel = (int)level;
    }

    // Set whether to apply farm level
    public void SetApplyFarmLevel (bool toggle) {
        applyFarmLevel = toggle;
    }

    // Set the active farm level
    public void SetFarmLevel (float level) {
        activeFarmLevel = (int)level;
    }

    // Set whether to apply plant level
    public void SetApplyPlantLevel (bool toggle) {
        applyPlantLevel = toggle;
    }

    // Set the active plant level
    public void SetPlantLevel (float level) {
        activePlantLevel = (int)level;
    }

    // Set whether to apply special index
    public void SetApplySpecialIndex (bool toggle) {
        applySpecialIndex = toggle;
    }

    // Set the active special index
    public void SetSpecialIndex (float index) {
        activeSpecialIndex = (int)index;
    }

    // Set the brush size for editing
    public void SetBrushSize (float size) {
        brushSize = (int)size;
    }

    // Set the river mode
    public void SetRiverMode (int mode) {
        riverMode = (OptionalToggle)mode;
    }

    // Set the road mode
    public void SetRoadMode (int mode) {
        roadMode = (OptionalToggle)mode;
    }

    // Set the walled mode
    public void SetWalledMode (int mode) {
        walledMode = (OptionalToggle)mode;
    }

    // Enable or disable edit mode
    public void SetEditMode (bool toggle) {
        enabled = toggle;
    }

    // Show or hide the grid
    public void ShowGrid (bool visible) {
        if (visible) {
            terrainMaterial.EnableKeyword("GRID_ON");
        }
        else {
            terrainMaterial.DisableKeyword("GRID_ON");
        }
    }

    // Initialize the editor
    void Awake () {
        terrainMaterial.DisableKeyword("GRID_ON");
        Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
        SetEditMode(true);
    }

    // Update the editor state
    void Update () {
        if (!EventSystem.current.IsPointerOverGameObject()) {
            if (Input.GetMouseButton(0)) {
                HandleInput();
                return;
            }
            if (Input.GetKeyDown(KeyCode.U)) {
                if (Input.GetKey(KeyCode.LeftShift)) {
                    DestroyUnit();
                }
                else {
                    CreateUnit();
                }
                return;
            }
        }
        previousCell = null;
    }

    // Get the cell under the cursor
    HexCell GetCellUnderCursor () {
        return hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
    }

    // Create a unit at the current cell
    void CreateUnit () {
        HexCell cell = GetCellUnderCursor();
        if (cell && !cell.Unit) {
            hexGrid.AddUnit(
                Instantiate(HexUnit.unitPrefab), cell, Random.Range(0f, 360f)
            );
        }
    }

    // Destroy the unit at the current cell
    void DestroyUnit () {
        HexCell cell = GetCellUnderCursor();
        if (cell && cell.Unit) {
            hexGrid.RemoveUnit(cell.Unit);
        }
    }

    // Handle input for editing
    void HandleInput () {
        HexCell currentCell = GetCellUnderCursor();
        if (currentCell) {
            if (previousCell && previousCell != currentCell) {
                ValidateDrag(currentCell);
            }
            else {
                isDrag = false;
            }
            EditCells(currentCell);
            previousCell = currentCell;
        }
        else {
            previousCell = null;
        }
    }

    // Validate the drag direction
    void ValidateDrag (HexCell currentCell) {
        for (
            dragDirection = HexDirection.NE;
            dragDirection <= HexDirection.NW;
            dragDirection++
        ) {
            if (previousCell.GetNeighbor(dragDirection) == currentCell) {
                isDrag = true;
                return;
            }
        }
        isDrag = false;
    }

    // Edit the cells within the brush size
    void EditCells (HexCell center) {
        int centerX = center.coordinates.X;
        int centerZ = center.coordinates.Z;

        for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++) {
            for (int x = centerX - r; x <= centerX + brushSize; x++) {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
        for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++) {
            for (int x = centerX - brushSize; x <= centerX + r; x++) {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }

    // Edit a single cell
    void EditCell (HexCell cell) {
        if (cell) {
            if (activeTerrainTypeIndex >= 0) {
                cell.TerrainTypeIndex = activeTerrainTypeIndex;
            }
            if (applyElevation) {
                cell.Elevation = activeElevation;
            }
            if (applyWaterLevel) {
                cell.WaterLevel = activeWaterLevel;
            }
            if (applySpecialIndex) {
                cell.SpecialIndex = activeSpecialIndex;
            }
            if (applyUrbanLevel) {
                cell.UrbanLevel = activeUrbanLevel;
            }
            if (applyFarmLevel) {
                cell.FarmLevel = activeFarmLevel;
            }
            if (applyPlantLevel) {
                cell.PlantLevel = activePlantLevel;
            }
            if (riverMode == OptionalToggle.No) {
                cell.RemoveRiver();
            }
            if (roadMode == OptionalToggle.No) {
                cell.RemoveRoads();
            }
            if (walledMode != OptionalToggle.Ignore) {
                cell.Walled = walledMode == OptionalToggle.Yes;
            }
            if (isDrag) {
                HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
                if (otherCell) {
                    if (riverMode == OptionalToggle.Yes) {
                        otherCell.SetOutgoingRiver(dragDirection);
                    }
                    if (roadMode == OptionalToggle.Yes) {
                        otherCell.AddRoad(dragDirection);
                    }
                }
            }
        }
    }
}