using UnityEngine;
using UnityEngine.UI;

public class SaveLoadItem : MonoBehaviour {

    // Reference to the SaveLoadMenu script
    public SaveLoadMenu menu;

    // Property for the map name
    public string MapName {
        // Getter for the map name
        get {
            return mapName;
        }
        // Setter for the map name
        set {
            mapName = value;
            // Update the text of the first child element with the new map name
            transform.GetChild(0).GetComponent<Text>().text = value;
        }
    }

    // Private field to store the map name
    string mapName;

    // Method to select the current item
    public void Select () {
        // Call the SelectItem method on the menu with the current map name
        menu.SelectItem(mapName);
    }
}