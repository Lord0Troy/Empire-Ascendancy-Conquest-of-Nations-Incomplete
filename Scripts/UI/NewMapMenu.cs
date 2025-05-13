using UnityEngine;

public class NewMapMenu : MonoBehaviour {

	public HexGrid hexGrid;

	public HexMapGenerator mapGenerator;

	bool generateMaps = true;

	bool wrapping = true;

// Toggles the wrapping setting for the map
public void ToggleWrapping(bool toggle) {
    wrapping = toggle;
}

// Opens the new map menu and locks the camera
public void Open() {
    gameObject.SetActive(true);
    HexMapCamera.Locked = true;
}

// Closes the new map menu and unlocks the camera
public void Close() {
    gameObject.SetActive(false);
    HexMapCamera.Locked = false;
}

// Creates a small map with predefined dimensions
public void CreateSmallMap() {
    CreateMap(20, 15);
}

// Creates a medium map with predefined dimensions
public void CreateMediumMap() {
    CreateMap(40, 30);
}

// Creates a large map with predefined dimensions
public void CreateLargeMap() {
    CreateMap(80, 60);
}

// Creates a map with specified dimensions
void CreateMap(int x, int z) {
    if (generateMaps) {
        // Generates the map using the map generator
        mapGenerator.GenerateMap(x, z, wrapping);
    } else {
        // Creates the map using the hex grid
        hexGrid.CreateMap(x, z, wrapping);
    }
    // Validates the camera position after map creation
    HexMapCamera.ValidatePosition();
    // Closes the new map menu
    Close();
}
}