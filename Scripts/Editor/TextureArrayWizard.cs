using UnityEditor;
using UnityEngine;

public class TextureArrayWizard : ScriptableWizard {

    // Array to hold the textures that will be combined into a texture array
    public Texture2D[] textures;

    // Adds a menu item to create the texture array wizard
    [MenuItem ("Assets/Create/Texture Array")]
    static void CreateWizard () {
        // Displays the wizard with the specified title and button text
        ScriptableWizard.DisplayWizard<TextureArrayWizard>(
            "Create Texture Array", "Create"
        );
    }

    // Called when the user clicks the "Create" button
    void OnWizardCreate () {
        // If no textures are provided, exit the method
        if (textures.Length == 0) {
            return;
        }
        // Opens a save file panel to specify the path to save the texture array asset
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Texture Array", "Texture Array", "asset", "Save Texture Array"
        );
        // If no path is specified, exit the method
        if (path.Length == 0) {
            return;
        }

        // Get the first texture to use its properties for the texture array
        Texture2D t = textures[0];
        // Create a new Texture2DArray with the same width, height, format, and mipmap count as the first texture
        Texture2DArray textureArray = new Texture2DArray(
            t.width, t.height, textures.Length, t.format, t.mipmapCount > 1
        );
        // Set the anisotropic level, filter mode, and wrap mode of the texture array to match the first texture
        textureArray.anisoLevel = t.anisoLevel;
        textureArray.filterMode = t.filterMode;
        textureArray.wrapMode = t.wrapMode;

        // Copy each texture and its mipmaps into the texture array
        for (int i = 0; i < textures.Length; i++) {
            for (int m = 0; m < t.mipmapCount; m++) {
                Graphics.CopyTexture(textures[i], 0, m, textureArray, i, m);
            }
        }

        // Save the texture array as an asset at the specified path
        AssetDatabase.CreateAsset(textureArray, path);
    }
}