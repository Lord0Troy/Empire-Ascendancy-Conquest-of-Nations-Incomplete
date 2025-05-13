using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;

public class SaveLoadMenu : MonoBehaviour {

    // Constant representing the version of the map file
    const int mapFileVersion = 5;

    // UI elements for the menu label and action button label
    public Text menuLabel, actionButtonLabel;

    // Input field for the name input
    public InputField nameInput;

    // RectTransform for the list content
    public RectTransform listContent;

    // Prefab for the save/load item
    public SaveLoadItem itemPrefab;

    // Reference to the HexGrid
    public HexGrid hexGrid;

    // Boolean to determine if the menu is in save mode
    bool saveMode;

    // Method to open the save/load menu
    public void Open (bool saveMode) {
        this.saveMode = saveMode;
        if (saveMode) {
            // Set the menu label and action button label for save mode
            menuLabel.text = "Save Map";
            actionButtonLabel.text = "Save";
        }
        else {
            // Set the menu label and action button label for load mode
            menuLabel.text = "Load Map";
            actionButtonLabel.text = "Load";
        }
        // Fill the list with save/load items
        FillList();
        // Activate the menu game object
        gameObject.SetActive(true);
        // Lock the HexMapCamera
        HexMapCamera.Locked = true;
    }

    // Method to close the save/load menu
    public void Close () {
        // Deactivate the menu game object
        gameObject.SetActive(false);
        // Unlock the HexMapCamera
        HexMapCamera.Locked = false;
    }

    // Method to perform the save or load action
    public void Action () {
        // Get the selected path
        string path = GetSelectedPath();
        if (path == null) {
            return;
        }
        if (saveMode) {
            // Save the map to the selected path
            Save(path);
        }
        else {
            // Load the map from the selected path
            Load(path);
        }
        // Close the menu
        Close();
    }

    // Method to select an item from the list
    public void SelectItem (string name) {
        // Set the name input field to the selected item's name
        nameInput.text = name;
    }

    // Method to delete the selected item
    public void Delete () {
        // Get the selected path
        string path = GetSelectedPath();
        if (path == null) {
            return;
        }
        // Delete the file at the selected path
        File.Delete(path);
        // Refresh the list
        FillList();
    }

    // Method to fill the list with save/load items
    void FillList () {
        // Clear the existing list content
        for (int i = 0; i < listContent.childCount; i++) {
            Destroy(listContent.GetChild(i).gameObject);
        }
        // Get the list of save files
        string[] paths = GetSavePaths();
        // Instantiate a save/load item for each save file
        for (int i = 0; i < paths.Length; i++) {
            SaveLoadItem item = Instantiate(itemPrefab);
            item.menu = this;
            item.MapName = Path.GetFileNameWithoutExtension(paths[i]);
            item.transform.SetParent(listContent, false);
        }
    }

    // Method to get the selected path
    string GetSelectedPath () {
        string mapName = nameInput.text;
        if (mapName.Length == 0) {
            return null;
        }
        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }

    // Method to get the list of save paths
    string[] GetSavePaths () {
        return Directory.GetFiles(Application.persistentDataPath, "*.map");
    }

    // Method to save the map to the specified path
    void Save (string path) {
        using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create))) {
            writer.Write(mapFileVersion);
            hexGrid.Save(writer);
        }
    }

    // Method to load the map from the specified path
    void Load (string path) {
        if (!File.Exists(path)) {
            Debug.LogError("File does not exist: " + path);
            return;
        }
        using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
            int header = reader.ReadInt32();
            if (header <= mapFileVersion) {
                hexGrid.Load(reader, header);
            }
            else {
                Debug.LogWarning("Unknown map format: " + header);
            }
        }
    }
}