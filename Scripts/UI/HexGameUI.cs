using UnityEngine;
using UnityEngine.EventSystems;

public class HexGameUI : MonoBehaviour {

    public HexGrid grid; // Reference to the HexGrid

    HexCell currentCell; // Currently selected HexCell

    HexUnit selectedUnit; // Currently selected HexUnit

    // Method to toggle edit mode
    public void SetEditMode (bool toggle) {
        enabled = !toggle; // Enable or disable the HexGameUI component
        grid.ShowUI(!toggle); // Show or hide the grid UI
        grid.ClearPath(); // Clear any existing path
        if (toggle) {
            Shader.EnableKeyword("HEX_MAP_EDIT_MODE"); // Enable edit mode shader
        }
        else {
            Shader.DisableKeyword("HEX_MAP_EDIT_MODE"); // Disable edit mode shader
        }
    }

    // Update method called once per frame
    void Update () {
        if (!EventSystem.current.IsPointerOverGameObject()) { // Check if the pointer is not over a UI element
            if (Input.GetMouseButtonDown(0)) { // Check for left mouse button click
                DoSelection(); // Handle selection
            }
            else if (selectedUnit) { // Check if a unit is selected
                if (Input.GetMouseButtonDown(1)) { // Check for right mouse button click
                    DoMove(); // Handle movement
                }
                else {
                    DoPathfinding(); // Handle pathfinding
                }
            }
        }
    }

    // Method to handle selection of a unit
    void DoSelection () {
        grid.ClearPath(); // Clear any existing path
        UpdateCurrentCell(); // Update the current cell based on mouse position
        if (currentCell) {
            selectedUnit = currentCell.Unit; // Select the unit in the current cell
        }
    }

    // Method to handle pathfinding
    void DoPathfinding () {
        if (UpdateCurrentCell()) { // Update the current cell and check if it changed
            if (currentCell && selectedUnit.IsValidDestination(currentCell)) { // Check if the current cell is a valid destination
                grid.FindPath(selectedUnit.Location, currentCell, selectedUnit); // Find path to the current cell
            }
            else {
                grid.ClearPath(); // Clear the path if the destination is not valid
            }
        }
    }

    // Method to handle movement of the selected unit
    void DoMove () {
        if (grid.HasPath) { // Check if there is a valid path
            selectedUnit.Travel(grid.GetPath()); // Move the unit along the path
            grid.ClearPath(); // Clear the path after movement
        }
    }

    // Method to update the current cell based on mouse position
    bool UpdateCurrentCell () {
        HexCell cell =
            grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition)); // Get the cell under the mouse pointer
        if (cell != currentCell) { // Check if the cell has changed
            currentCell = cell; // Update the current cell
            return true; // Return true if the cell has changed
        }
        return false; // Return false if the cell has not changed
    }
}