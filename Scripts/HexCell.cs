using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class HexCell : MonoBehaviour {

    // Coordinates of the hex cell
    public HexCoordinates coordinates;

    // UI RectTransform for the hex cell
    public RectTransform uiRect;

    // Reference to the chunk this cell belongs to
    public HexGridChunk chunk;

    // Index of the hex cell
    public int Index { get; set; }

    // Column index of the hex cell
    public int ColumnIndex { get; set; }

    // Elevation of the hex cell
    public int Elevation {
        get {
            return elevation;
        }
        set {
            if (elevation == value) {
                return;
            }
            int originalViewElevation = ViewElevation;
            elevation = value;
            if (ViewElevation != originalViewElevation) {
                ShaderData.ViewElevationChanged();
            }
            RefreshPosition();
            ValidateRivers();

            for (int i = 0; i < roads.Length; i++) {
                if (roads[i] && GetElevationDifference((HexDirection)i) > 1) {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }

    // Water level of the hex cell
    public int WaterLevel {
        get {
            return waterLevel;
        }
        set {
            if (waterLevel == value) {
                return;
            }
            int originalViewElevation = ViewElevation;
            waterLevel = value;
            if (ViewElevation != originalViewElevation) {
                ShaderData.ViewElevationChanged();
            }
            ValidateRivers();
            Refresh();
        }
    }

    // View elevation of the hex cell
    public int ViewElevation {
        get {
            return elevation >= waterLevel ? elevation : waterLevel;
        }
    }

    // Check if the hex cell is underwater
    public bool IsUnderwater {
        get {
            return waterLevel > elevation;
        }
    }

    // Check if the hex cell has an incoming river
    public bool HasIncomingRiver {
        get {
            return hasIncomingRiver;
        }
    }

    // Check if the hex cell has an outgoing river
    public bool HasOutgoingRiver {
        get {
            return hasOutgoingRiver;
        }
    }

    // Check if the hex cell has a river
    public bool HasRiver {
        get {
            return hasIncomingRiver || hasOutgoingRiver;
        }
    }

    // Check if the hex cell has a river beginning or ending
    public bool HasRiverBeginOrEnd {
        get {
            return hasIncomingRiver != hasOutgoingRiver;
        }
    }

    // Get the direction of the river beginning or ending
    public HexDirection RiverBeginOrEndDirection {
        get {
            return hasIncomingRiver ? incomingRiver : outgoingRiver;
        }
    }

    // Check if the hex cell has roads
    public bool HasRoads {
        get {
            for (int i = 0; i < roads.Length; i++) {
                if (roads[i]) {
                    return true;
                }
            }
            return false;
        }
    }

    // Get the direction of the incoming river
    public HexDirection IncomingRiver {
        get {
            return incomingRiver;
        }
    }

    // Get the direction of the outgoing river
    public HexDirection OutgoingRiver {
        get {
            return outgoingRiver;
        }
    }

    // Get the position of the hex cell
    public Vector3 Position {
        get {
            return transform.localPosition;
        }
    }

    // Get the Y position of the stream bed
    public float StreamBedY {
        get {
            return
                (elevation + HexMetrics.streamBedElevationOffset) *
                HexMetrics.elevationStep;
        }
    }

    // Get the Y position of the river surface
    public float RiverSurfaceY {
        get {
            return
                (elevation + HexMetrics.waterElevationOffset) *
                HexMetrics.elevationStep;
        }
    }

    // Get the Y position of the water surface
    public float WaterSurfaceY {
        get {
            return
                (waterLevel + HexMetrics.waterElevationOffset) *
                HexMetrics.elevationStep;
        }
    }

    // Urban level of the hex cell
    public int UrbanLevel {
        get {
            return urbanLevel;
        }
        set {
            if (urbanLevel != value) {
                urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    // Farm level of the hex cell
    public int FarmLevel {
        get {
            return farmLevel;
        }
        set {
            if (farmLevel != value) {
                farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    // Plant level of the hex cell
    public int PlantLevel {
        get {
            return plantLevel;
        }
        set {
            if (plantLevel != value) {
                plantLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    // Special index of the hex cell
    public int SpecialIndex {
        get {
            return specialIndex;
        }
        set {
            if (specialIndex != value && !HasRiver) {
                specialIndex = value;
                RemoveRoads();
                RefreshSelfOnly();
            }
        }
    }

    // Check if the hex cell is special
    public bool IsSpecial {
        get {
            return specialIndex > 0;
        }
    }

    // Check if the hex cell is walled
    public bool Walled {
        get {
            return walled;
        }
        set {
            if (walled != value) {
                walled = value;
                Refresh();
            }
        }
    }

    // Terrain type index of the hex cell
    public int TerrainTypeIndex {
        get {
            return terrainTypeIndex;
        }
        set {
            if (terrainTypeIndex != value) {
                terrainTypeIndex = value;
                ShaderData.RefreshTerrain(this);
            }
        }
    }

    // Check if the hex cell is visible
    public bool IsVisible {
        get {
            return visibility > 0 && Explorable;
        }
    }

    // Check if the hex cell is explored
    public bool IsExplored {
        get {
            return explored && Explorable;
        }
        private set {
            explored = value;
        }
    }

    // Check if the hex cell is explorable
    public bool Explorable { get; set; }

    // Distance of the hex cell
    public int Distance {
        get {
            return distance;
        }
        set {
            distance = value;
        }
    }

    // Unit on the hex cell
    public HexUnit Unit { get; set; }

    // Path from another hex cell
    public HexCell PathFrom { get; set; }

    // Search heuristic for pathfinding
    public int SearchHeuristic { get; set; }

    // Search priority for pathfinding
    public int SearchPriority {
        get {
            return distance + SearchHeuristic;
        }
    }

    // Search phase for pathfinding
    public int SearchPhase { get; set; }

    // Next hex cell with the same priority in pathfinding
    public HexCell NextWithSamePriority { get; set; }

    // Shader data for the hex cell
    public HexCellShaderData ShaderData { get; set; }

    // Terrain type index of the hex cell
    int terrainTypeIndex;

    // Elevation of the hex cell
    int elevation = int.MinValue;
    int waterLevel;

    // Urban, farm, and plant levels of the hex cell
    int urbanLevel, farmLevel, plantLevel;

    // Special index of the hex cell
    int specialIndex;

    // Distance of the hex cell
    int distance;

    // Visibility of the hex cell
    int visibility;

    // Check if the hex cell is explored
    bool explored;

    // Check if the hex cell is walled
    bool walled;

    // Check if the hex cell has incoming and outgoing rivers
    bool hasIncomingRiver, hasOutgoingRiver;
    HexDirection incomingRiver, outgoingRiver;

    // Neighbors of the hex cell
    [SerializeField]
    HexCell[] neighbors;

    // Roads of the hex cell
    [SerializeField]
    bool[] roads;

    // Increase the visibility of the hex cell
    public void IncreaseVisibility () {
        visibility += 1;
        if (visibility == 1) {
            IsExplored = true;
            ShaderData.RefreshVisibility(this);
        }
    }

    // Decrease the visibility of the hex cell
    public void DecreaseVisibility () {
        visibility -= 1;
        if (visibility == 0) {
            ShaderData.RefreshVisibility(this);
        }
    }

    // Reset the visibility of the hex cell
    public void ResetVisibility () {
        if (visibility > 0) {
            visibility = 0;
            ShaderData.RefreshVisibility(this);
        }
    }

    // Get the neighbor of the hex cell in a specific direction
    public HexCell GetNeighbor (HexDirection direction) {
        return neighbors[(int)direction];
    }

    // Set the neighbor of the hex cell in a specific direction
    public void SetNeighbor (HexDirection direction, HexCell cell) {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    // Get the edge type of the hex cell in a specific direction
    public HexEdgeType GetEdgeType (HexDirection direction) {
        return HexMetrics.GetEdgeType(
            elevation, neighbors[(int)direction].elevation
        );
    }

    // Get the edge type of the hex cell with another cell
    public HexEdgeType GetEdgeType (HexCell otherCell) {
        return HexMetrics.GetEdgeType(
            elevation, otherCell.elevation
        );
    }

    // Check if the hex cell has a river through a specific edge
    public bool HasRiverThroughEdge (HexDirection direction) {
        return
            hasIncomingRiver && incomingRiver == direction ||
            hasOutgoingRiver && outgoingRiver == direction;
    }

    // Remove the incoming river from the hex cell
    public void RemoveIncomingRiver () {
        if (!hasIncomingRiver) {
            return;
        }
        hasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(incomingRiver);
        neighbor.hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    // Remove the outgoing river from the hex cell
    public void RemoveOutgoingRiver () {
        if (!hasOutgoingRiver) {
            return;
        }
        hasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    // Remove both incoming and outgoing rivers from the hex cell
    public void RemoveRiver () {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    // Set the outgoing river of the hex cell in a specific direction
    public void SetOutgoingRiver (HexDirection direction) {
        if (hasOutgoingRiver && outgoingRiver == direction) {
            return;
        }

        HexCell neighbor = GetNeighbor(direction);
        if (!IsValidRiverDestination(neighbor)) {
            return;
        }

        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == direction) {
            RemoveIncomingRiver();
        }
        hasOutgoingRiver = true;
        outgoingRiver = direction;
        specialIndex = 0;

        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();
        neighbor.specialIndex = 0;

        SetRoad((int)direction, false);
    }

    // Check if the hex cell has a road through a specific edge
    public bool HasRoadThroughEdge (HexDirection direction) {
        return roads[(int)direction];
    }

    // Add a road to the hex cell in a specific direction
    public void AddRoad (HexDirection direction) {
        if (
            !roads[(int)direction] && !HasRiverThroughEdge(direction) &&
            !IsSpecial && !GetNeighbor(direction).IsSpecial &&
            GetElevationDifference(direction) <= 1
        ) {
            SetRoad((int)direction, true);
        }
    }

    // Remove all roads from the hex cell
    public void RemoveRoads () {
        for (int i = 0; i < neighbors.Length; i++) {
            if (roads[i]) {
                SetRoad(i, false);
            }
        }
    }

    // Get the elevation difference between the hex cell and a neighbor in a specific direction
    public int GetElevationDifference (HexDirection direction) {
        int difference = elevation - GetNeighbor(direction).elevation;
        return difference >= 0 ? difference : -difference;
    }

    // Check if a neighbor is a valid river destination
    bool IsValidRiverDestination (HexCell neighbor) {
        return neighbor && (
            elevation >= neighbor.elevation || waterLevel == neighbor.elevation
        );
    }

    // Validate the rivers of the hex cell
    void ValidateRivers () {
        if (
            hasOutgoingRiver &&
            !IsValidRiverDestination(GetNeighbor(outgoingRiver))
        ) {
            RemoveOutgoingRiver();
        }
        if (
            hasIncomingRiver &&
            !GetNeighbor(incomingRiver).IsValidRiverDestination(this)
        ) {
            RemoveIncomingRiver();
        }
    }

    // Set the road state of the hex cell in a specific direction
    void SetRoad (int index, bool state) {
        roads[index] = state;
        neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
        neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    // Refresh the position of the hex cell
    void RefreshPosition () {
        Vector3 position = transform.localPosition;
        position.y = elevation * HexMetrics.elevationStep;
        position.y +=
            (HexMetrics.SampleNoise(position).y * 2f - 1f) *
            HexMetrics.elevationPerturbStrength;
        transform.localPosition = position;

        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = -position.y;
        uiRect.localPosition = uiPosition;
    }

    // Refresh the hex cell and its neighbors
    void Refresh () {
        if (chunk) {
            chunk.Refresh();
            for (int i = 0; i < neighbors.Length; i++) {
                HexCell neighbor = neighbors[i];
                if (neighbor != null && neighbor.chunk != chunk) {
                    neighbor.chunk.Refresh();
                }
            }
            if (Unit) {
                Unit.ValidateLocation();
            }
        }
    }

    // Refresh only the hex cell
    void RefreshSelfOnly () {
        chunk.Refresh();
        if (Unit) {
            Unit.ValidateLocation();
        }
    }

    // Save the hex cell data to a binary writer
    public void Save (BinaryWriter writer) {
        writer.Write((byte)terrainTypeIndex);
        writer.Write((byte)(elevation + 127));
        writer.Write((byte)waterLevel);
        writer.Write((byte)urbanLevel);
        writer.Write((byte)farmLevel);
        writer.Write((byte)plantLevel);
        writer.Write((byte)specialIndex);
        writer.Write(walled);

        if (hasIncomingRiver) {
            writer.Write((byte)(incomingRiver + 128));
        }
        else {
            writer.Write((byte)0);
        }

        if (hasOutgoingRiver) {
            writer.Write((byte)(outgoingRiver + 128));
        }
        else {
            writer.Write((byte)0);
        }

        int roadFlags = 0;
        for (int i = 0; i < roads.Length; i++) {
            if (roads[i]) {
                roadFlags |= 1 << i;
            }
        }
        writer.Write((byte)roadFlags);
        writer.Write(IsExplored);
    }

    // Load the hex cell data from a binary reader
    public void Load (BinaryReader reader, int header) {
        terrainTypeIndex = reader.ReadByte();
        ShaderData.RefreshTerrain(this);
        elevation = reader.ReadByte();
        if (header >= 4) {
            elevation -= 127;
        }
        RefreshPosition();
        waterLevel = reader.ReadByte();
        urbanLevel = reader.ReadByte();
        farmLevel = reader.ReadByte();
        plantLevel = reader.ReadByte();
        specialIndex = reader.ReadByte();
        walled = reader.ReadBoolean();

        byte riverData = reader.ReadByte();
        if (riverData >= 128) {
            hasIncomingRiver = true;
            incomingRiver = (HexDirection)(riverData - 128);
        }
        else {
            hasIncomingRiver = false;
        }

        riverData = reader.ReadByte();
        if (riverData >= 128) {
            hasOutgoingRiver = true;
            outgoingRiver = (HexDirection)(riverData - 128);
        }
        else {
            hasOutgoingRiver = false;
        }

        int roadFlags = reader.ReadByte();
        for (int i = 0; i < roads.Length; i++) {
            roads[i] = (roadFlags & (1 << i)) != 0;
        }

        IsExplored = header >= 3 ? reader.ReadBoolean() : false;
        ShaderData.RefreshVisibility(this);
    }

    // Set the label of the hex cell
    public void SetLabel (string text) {
        UnityEngine.UI.Text label = uiRect.GetComponent<Text>();
        label.text = text;
    }

    // Disable the highlight of the hex cell
    public void DisableHighlight () {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.enabled = false;
    }

    // Enable the highlight of the hex cell with a specific color
    public void EnableHighlight (Color color) {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.color = color;
        highlight.enabled = true;
    }

    // Set the map data of the hex cell
    public void SetMapData (float data) {
        ShaderData.SetMapData(this, data);
    }
}