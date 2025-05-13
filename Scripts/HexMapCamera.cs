using UnityEngine;

public class HexMapCamera : MonoBehaviour {

    // Minimum and maximum zoom levels for the camera stick
    public float stickMinZoom, stickMaxZoom;

    // Minimum and maximum zoom levels for the camera swivel
    public float swivelMinZoom, swivelMaxZoom;

    // Minimum and maximum movement speeds based on zoom level
    public float moveSpeedMinZoom, moveSpeedMaxZoom;

    // Speed of camera rotation
    public float rotationSpeed;

    // References to the swivel and stick transforms
    Transform swivel, stick;

    // Reference to the HexGrid
    public HexGrid grid;

    // Current zoom level (0 to 1)
    float zoom = 1f;

    // Current rotation angle of the camera
    float rotationAngle;

    // Singleton instance of the HexMapCamera
    static HexMapCamera instance;

    // Property to lock or unlock the camera
    public static bool Locked {
        set {
            instance.enabled = !value;
        }
    }

    // Validate the camera's position
    public static void ValidatePosition () {
        instance.AdjustPosition(0f, 0f);
    }

    // Initialize references to the swivel and stick transforms
    void Awake () {
        swivel = transform.GetChild(0);
        stick = swivel.GetChild(0);
    }

    // Set the singleton instance and validate the position when enabled
    void OnEnable () {
        instance = this;
        ValidatePosition();
    }

    // Update the camera's zoom, rotation, and position based on input
    void Update () {
        // Adjust zoom based on mouse scroll wheel input
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if (zoomDelta != 0f) {
            AdjustZoom(zoomDelta);
        }

        // Adjust rotation based on rotation input
        float rotationDelta = Input.GetAxis("Rotation");
        if (rotationDelta != 0f) {
            AdjustRotation(rotationDelta);
        }

        // Adjust position based on horizontal and vertical input
        float xDelta = Input.GetAxis("Horizontal");
        float zDelta = Input.GetAxis("Vertical");
        if (xDelta != 0f || zDelta != 0f) {
            AdjustPosition(xDelta, zDelta);
        }
    }

    // Adjust the camera's zoom level
    void AdjustZoom (float delta) {
        zoom = Mathf.Clamp01(zoom + delta);

        // Adjust the stick's position based on zoom level
        float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
        stick.localPosition = new Vector3(0f, 0f, distance);

        // Adjust the swivel's rotation based on zoom level
        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
        swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }

    // Adjust the camera's rotation angle
    void AdjustRotation (float delta) {
        rotationAngle += delta * rotationSpeed * Time.deltaTime;
        if (rotationAngle < 0f) {
            rotationAngle += 360f;
        }
        else if (rotationAngle >= 360f) {
            rotationAngle -= 360f;
        }
        transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
    }

    // Adjust the camera's position
    void AdjustPosition (float xDelta, float zDelta) {
        // Calculate the direction based on rotation and input
        Vector3 direction =
            transform.localRotation *
            new Vector3(xDelta, 0f, zDelta).normalized;
        float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
        float distance =
            Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) *
            damping * Time.deltaTime;

        // Update the position based on direction and distance
        Vector3 position = transform.localPosition;
        position += direction * distance;
        transform.localPosition =
            grid.wrapping ? WrapPosition(position) : ClampPosition(position);
    }

    // Clamp the camera's position within the grid boundaries
    Vector3 ClampPosition (Vector3 position) {
        float xMax = (grid.cellCountX - 0.5f) * HexMetrics.innerDiameter;
        position.x = Mathf.Clamp(position.x, 0f, xMax);

        float zMax = (grid.cellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
        position.z = Mathf.Clamp(position.z, 0f, zMax);

        return position;
    }

    // Wrap the camera's position around the grid boundaries
    Vector3 WrapPosition (Vector3 position) {
        float width = grid.cellCountX * HexMetrics.innerDiameter;
        while (position.x < 0f) {
            position.x += width;
        }
        while (position.x > width) {
            position.x -= width;
        }

        float zMax = (grid.cellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
        position.z = Mathf.Clamp(position.z, 0f, zMax);

        grid.CenterMap(position.x);
        return position;
    }
}