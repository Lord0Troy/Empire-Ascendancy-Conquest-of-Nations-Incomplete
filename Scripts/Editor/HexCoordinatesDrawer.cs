using UnityEngine;
using UnityEditor;

// Custom property drawer for HexCoordinates type
[CustomPropertyDrawer(typeof(HexCoordinates))]
public class HexCoordinatesDrawer : PropertyDrawer {

    // Override the OnGUI method to define how HexCoordinates should be drawn in the inspector
    public override void OnGUI (
        Rect position, SerializedProperty property, GUIContent label
    ) {
        // Create a new HexCoordinates instance using the x and z values from the property
        HexCoordinates coordinates = new HexCoordinates(
            property.FindPropertyRelative("x").intValue,
            property.FindPropertyRelative("z").intValue
        );

        // Draw the property label
        position = EditorGUI.PrefixLabel(position, label);
        // Display the coordinates as a label
        GUI.Label(position, coordinates.ToString());
    }
}