using UnityEngine;
using UnityEngine.UI;

// Represents a chunk of the hexagonal grid in the game.
public class HexGridChunk : MonoBehaviour {

    // Meshes for different terrain features.
    public HexMesh terrain, rivers, roads, water, waterShore, estuaries;

    // Manages additional features like decorations or special objects.
    public HexFeatureManager features;

    // Array of hexagonal cells in this chunk.
    HexCell[] cells;

    // Canvas for rendering UI elements associated with this chunk.
    Canvas gridCanvas;

    // Static colors used for blending weights in the mesh.
    static Color weights1 = new Color(1f, 0f, 0f);
    static Color weights2 = new Color(0f, 1f, 0f);
    static Color weights3 = new Color(0f, 0f, 1f);

    // Called when the object is initialized.
    void Awake () {
        // Get the canvas component for UI rendering.
        gridCanvas = GetComponentInChildren<Canvas>();

        // Initialize the cells array based on chunk size.
        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
    }

    // Adds a cell to the chunk at the specified index.
    public void AddCell (int index, HexCell cell) {
        cells[index] = cell;
        cell.chunk = this;

        // Set the cell's transform to be a child of the chunk's transform.
        cell.transform.SetParent(transform, false);

        // Set the cell's UI rectangle to be a child of the grid canvas.
        cell.uiRect.SetParent(gridCanvas.transform, false);
    }

    // Marks the chunk as needing to be refreshed.
    public void Refresh () {
        enabled = true;
    }

    // Shows or hides the UI elements for this chunk.
    public void ShowUI (bool visible) {
        gridCanvas.gameObject.SetActive(visible);
    }

    // Called at the end of the frame to update the chunk's mesh.
    void LateUpdate () {
        Triangulate();
        enabled = false;
    }

    // Rebuilds the mesh for the chunk based on its cells.
    public void Triangulate () {
        // Clear all meshes before rebuilding.
        terrain.Clear();
        rivers.Clear();
        roads.Clear();
        water.Clear();
        waterShore.Clear();
        estuaries.Clear();
        features.Clear();

        // Triangulate each cell in the chunk.
        for (int i = 0; i < cells.Length; i++) {
            Triangulate(cells[i]);
        }

        // Apply the changes to the meshes.
        terrain.Apply();
        rivers.Apply();
        roads.Apply();
        water.Apply();
        waterShore.Apply();
        estuaries.Apply();
        features.Apply();
    }

    // Triangulates a single cell, adding its geometry to the meshes.
    void Triangulate (HexCell cell) {
        // Process each direction of the hexagonal cell.
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
            Triangulate(d, cell);
        }

        // Add features if the cell is not underwater and has no rivers or roads.
        if (!cell.IsUnderwater) {
            if (!cell.HasRiver && !cell.HasRoads) {
                features.AddFeature(cell, cell.Position);
            }

            // Add special features if the cell is marked as special.
            if (cell.IsSpecial) {
                features.AddSpecialFeature(cell, cell.Position);
            }
        }
    }

    // Triangulates a specific direction of a cell.
    void Triangulate (HexDirection direction, HexCell cell) {
        // Get the center position of the cell.
        Vector3 center = cell.Position;

        // Calculate the edge vertices for the current direction.
        EdgeVertices e = new EdgeVertices(
            center + HexMetrics.GetFirstSolidCorner(direction),
            center + HexMetrics.GetSecondSolidCorner(direction)
        );

        // Handle river-specific triangulation.
        if (cell.HasRiver) {
            if (cell.HasRiverThroughEdge(direction)) {
                e.v3.y = cell.StreamBedY;

                if (cell.HasRiverBeginOrEnd) {
                    TriangulateWithRiverBeginOrEnd(direction, cell, center, e);
                } else {
                    TriangulateWithRiver(direction, cell, center, e);
                }
            } else {
                TriangulateAdjacentToRiver(direction, cell, center, e);
            }
        } else {
            // Handle triangulation for cells without rivers.
            TriangulateWithoutRiver(direction, cell, center, e);

            // Add features if the cell is not underwater and has no roads in this direction.
            if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction)) {
                features.AddFeature(cell, (center + e.v1 + e.v5) * (1f / 3f));
            }
        }

        // Handle connections to neighboring cells.
        if (direction <= HexDirection.SE) {
            TriangulateConnection(direction, cell, e);
        }

        // Handle underwater cells.
        if (cell.IsUnderwater) {
            TriangulateWater(direction, cell, center);
        }
    }

    // Triangulates water for a cell in a specific direction.
    void TriangulateWater(HexDirection direction, HexCell cell, Vector3 center) {
        // Set the center's y-coordinate to the water surface level.
        center.y = cell.WaterSurfaceY;

        // Get the neighboring cell in the specified direction.
        HexCell neighbor = cell.GetNeighbor(direction);

        // If the neighbor is not underwater, triangulate the water shore.
        if (neighbor != null && !neighbor.IsUnderwater) {
            TriangulateWaterShore(direction, cell, neighbor, center);
        }
        // Otherwise, triangulate open water.
        else {
            TriangulateOpenWater(direction, cell, neighbor, center);
        }
    }

    // Triangulates open water for a cell in a specific direction.
    void TriangulateOpenWater(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center) {
        // Calculate the first and second water corners for the cell.
        Vector3 c1 = center + HexMetrics.GetFirstWaterCorner(direction);
        Vector3 c2 = center + HexMetrics.GetSecondWaterCorner(direction);

        // Add a triangle for the water surface.
        water.AddTriangle(center, c1, c2);

        // Set the cell indices for the triangle.
        Vector3 indices;
        indices.x = indices.y = indices.z = cell.Index;
        water.AddTriangleCellData(indices, weights1);

        // If the direction is valid and the neighbor exists, add a water bridge.
        if (direction <= HexDirection.SE && neighbor != null) {
            Vector3 bridge = HexMetrics.GetWaterBridge(direction);
            Vector3 e1 = c1 + bridge;
            Vector3 e2 = c2 + bridge;

            // Add a quad for the water bridge.
            water.AddQuad(c1, c2, e1, e2);
            indices.y = neighbor.Index;
            water.AddQuadCellData(indices, weights1, weights2);

            // Handle the next neighbor for additional water connections.
            if (direction <= HexDirection.E) {
                HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
                if (nextNeighbor == null || !nextNeighbor.IsUnderwater) {
                    return;
                }
                water.AddTriangle(c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next()));
                indices.z = nextNeighbor.Index;
                water.AddTriangleCellData(indices, weights1, weights2, weights3);
            }
        }
    }

    // Triangulates the water shore between a cell and its neighbor.
    void TriangulateWaterShore(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center) {
        // Calculate the edge vertices for the water shore.
        EdgeVertices e1 = new EdgeVertices(
            center + HexMetrics.GetFirstWaterCorner(direction),
            center + HexMetrics.GetSecondWaterCorner(direction)
        );

        // Add triangles for the water shore.
        water.AddTriangle(center, e1.v1, e1.v2);
        water.AddTriangle(center, e1.v2, e1.v3);
        water.AddTriangle(center, e1.v3, e1.v4);
        water.AddTriangle(center, e1.v4, e1.v5);

        // Set the cell indices for the water shore triangles.
        Vector3 indices;
        indices.x = indices.z = cell.Index;
        indices.y = neighbor.Index;
        water.AddTriangleCellData(indices, weights1);
        water.AddTriangleCellData(indices, weights1);
        water.AddTriangleCellData(indices, weights1);
        water.AddTriangleCellData(indices, weights1);

        // Adjust the neighbor's position for wrapping.
        Vector3 center2 = neighbor.Position;
        if (neighbor.ColumnIndex < cell.ColumnIndex - 1) {
            center2.x += HexMetrics.wrapSize * HexMetrics.innerDiameter;
        } else if (neighbor.ColumnIndex > cell.ColumnIndex + 1) {
            center2.x -= HexMetrics.wrapSize * HexMetrics.innerDiameter;
        }
        center2.y = center.y;

        // Calculate the edge vertices for the neighbor.
        EdgeVertices e2 = new EdgeVertices(
            center2 + HexMetrics.GetSecondSolidCorner(direction.Opposite()),
            center2 + HexMetrics.GetFirstSolidCorner(direction.Opposite())
        );

        // If the cell has a river through the edge, triangulate the estuary.
        if (cell.HasRiverThroughEdge(direction)) {
            TriangulateEstuary(e1, e2, cell.HasIncomingRiver && cell.IncomingRiver == direction, indices);
        }
        // Otherwise, add quads for the water shore.
        else {
            waterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            waterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            waterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            waterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadCellData(indices, weights1, weights2);
            waterShore.AddQuadCellData(indices, weights1, weights2);
            waterShore.AddQuadCellData(indices, weights1, weights2);
            waterShore.AddQuadCellData(indices, weights1, weights2);
        }

        // Handle the next neighbor for additional shore connections.
        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (nextNeighbor != null) {
            Vector3 center3 = nextNeighbor.Position;
            if (nextNeighbor.ColumnIndex < cell.ColumnIndex - 1) {
                center3.x += HexMetrics.wrapSize * HexMetrics.innerDiameter;
            } else if (nextNeighbor.ColumnIndex > cell.ColumnIndex + 1) {
                center3.x -= HexMetrics.wrapSize * HexMetrics.innerDiameter;
            }
            Vector3 v3 = center3 + (nextNeighbor.IsUnderwater ?
                HexMetrics.GetFirstWaterCorner(direction.Previous()) :
                HexMetrics.GetFirstSolidCorner(direction.Previous()));
            v3.y = center.y;

            // Add a triangle for the connection to the next neighbor.
            waterShore.AddTriangle(e1.v5, e2.v5, v3);
            waterShore.AddTriangleUV(
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f)
            );
            indices.z = nextNeighbor.Index;
            waterShore.AddTriangleCellData(indices, weights1, weights2, weights3);
        }
    }
    // Triangulates an estuary where a river meets the ocean.
    void TriangulateEstuary(
        EdgeVertices e1, EdgeVertices e2, bool incomingRiver, Vector3 indices
    ) {
        // Add triangles for the water shore at the estuary.
        waterShore.AddTriangle(e2.v1, e1.v2, e1.v1);
        waterShore.AddTriangle(e2.v5, e1.v5, e1.v4);
        waterShore.AddTriangleUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f)
        );
        waterShore.AddTriangleUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f)
        );
        waterShore.AddTriangleCellData(indices, weights2, weights1, weights1);
        waterShore.AddTriangleCellData(indices, weights2, weights1, weights1);

        // Add quads and triangles for the estuary geometry.
        estuaries.AddQuad(e2.v1, e1.v2, e2.v2, e1.v3);
        estuaries.AddTriangle(e1.v3, e2.v2, e2.v4);
        estuaries.AddQuad(e1.v3, e1.v4, e2.v4, e2.v5);

        // Add UV mapping for the estuary.
        estuaries.AddQuadUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 0f)
        );
        estuaries.AddTriangleUV(
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f)
        );
        estuaries.AddQuadUV(
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 1f)
        );

        // Add cell data for the estuary.
        estuaries.AddQuadCellData(
            indices, weights2, weights1, weights2, weights1
        );
        estuaries.AddTriangleCellData(indices, weights1, weights2, weights2);
        estuaries.AddQuadCellData(indices, weights1, weights2);

        // Add UV2 mapping for the estuary based on river direction.
        if (incomingRiver) {
            estuaries.AddQuadUV2(
                new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f),
                new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f)
            );
            estuaries.AddTriangleUV2(
                new Vector2(0.5f, 1.1f),
                new Vector2(1f, 0.8f),
                new Vector2(0f, 0.8f)
            );
            estuaries.AddQuadUV2(
                new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f),
                new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f)
            );
        } else {
            estuaries.AddQuadUV2(
                new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f),
                new Vector2(0f, 0f), new Vector2(0.5f, -0.3f)
            );
            estuaries.AddTriangleUV2(
                new Vector2(0.5f, -0.3f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f)
            );
            estuaries.AddQuadUV2(
                new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f),
                new Vector2(1f, 0f), new Vector2(1.5f, -0.2f)
            );
        }
    }

    // Triangulates a cell without a river.
    void TriangulateWithoutRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
    ) {
        // Create a fan of triangles from the center to the edge vertices.
        TriangulateEdgeFan(center, e, cell.Index);

        // If the cell has roads, triangulate them.
        if (cell.HasRoads) {
            Vector2 interpolators = GetRoadInterpolators(direction, cell);
            TriangulateRoad(
                center,
                Vector3.Lerp(center, e.v1, interpolators.x),
                Vector3.Lerp(center, e.v5, interpolators.y),
                e, cell.HasRoadThroughEdge(direction), cell.Index
            );
        }
    }

    // Determines the interpolation values for road placement.
    Vector2 GetRoadInterpolators(HexDirection direction, HexCell cell) {
        Vector2 interpolators;
        if (cell.HasRoadThroughEdge(direction)) {
            interpolators.x = interpolators.y = 0.5f;
        } else {
            interpolators.x =
                cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
            interpolators.y =
                cell.HasRoadThroughEdge(direction.Next()) ? 0.5f : 0.25f;
        }
        return interpolators;
    }

    // Triangulates a cell adjacent to a river.
    void TriangulateAdjacentToRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
    ) {
        // If the cell has roads, triangulate them adjacent to the river.
        if (cell.HasRoads) {
            TriangulateRoadAdjacentToRiver(direction, cell, center, e);
        }

        // Adjust the center position based on river connections.
        if (cell.HasRiverThroughEdge(direction.Next())) {
            if (cell.HasRiverThroughEdge(direction.Previous())) {
                center += HexMetrics.GetSolidEdgeMiddle(direction) *
                    (HexMetrics.innerToOuter * 0.5f);
            } else if (cell.HasRiverThroughEdge(direction.Previous2())) {
                center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
            }
        } else if (
            cell.HasRiverThroughEdge(direction.Previous()) &&
            cell.HasRiverThroughEdge(direction.Next2())
        ) {
            center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
        }

        // Create a middle edge for the river-adjacent cell.
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f)
        );

        // Add a strip of triangles between the middle and outer edges.
        TriangulateEdgeStrip(
            m, weights1, cell.Index,
            e, weights1, cell.Index
        );

        // Create a fan of triangles from the center to the middle edge.
        TriangulateEdgeFan(center, m, cell.Index);

        // Add features if the cell is not underwater and has no road in this direction.
        if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction)) {
            features.AddFeature(cell, (center + e.v1 + e.v5) * (1f / 3f));
        }
    }

    // Triangulates a road adjacent to a river.
    void TriangulateRoadAdjacentToRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
    ) {
        // Determine if the cell has a road through the current edge.
        bool hasRoadThroughEdge = cell.HasRoadThroughEdge(direction);
    
        // Check if the previous and next edges have rivers.
        bool previousHasRiver = cell.HasRiverThroughEdge(direction.Previous());
        bool nextHasRiver = cell.HasRiverThroughEdge(direction.Next());
    
        // Get the interpolation values for road placement.
        Vector2 interpolators = GetRoadInterpolators(direction, cell);
    
        // Initialize the road center at the cell's center.
        Vector3 roadCenter = center;
    
        // Adjust the road center if the cell has a river beginning or ending.
        if (cell.HasRiverBeginOrEnd) {
            roadCenter += HexMetrics.GetSolidEdgeMiddle(
                cell.RiverBeginOrEndDirection.Opposite()
            ) * (1f / 3f);
        }
        // Handle cases where the incoming and outgoing rivers are opposite.
        else if (cell.IncomingRiver == cell.OutgoingRiver.Opposite()) {
            Vector3 corner;
            if (previousHasRiver) {
                if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Next())) {
                    return; // No road to triangulate.
                }
                corner = HexMetrics.GetSecondSolidCorner(direction);
            } else {
                if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Previous())) {
                    return; // No road to triangulate.
                }
                corner = HexMetrics.GetFirstSolidCorner(direction);
            }
            roadCenter += corner * 0.5f;
    
            // Add a bridge if the road crosses the river.
            if (cell.IncomingRiver == direction.Next() && (
                cell.HasRoadThroughEdge(direction.Next2()) ||
                cell.HasRoadThroughEdge(direction.Opposite())
            )) {
                features.AddBridge(roadCenter, center - corner * 0.5f);
            }
            center += corner * 0.25f;
        }
        // Adjust the road center for rivers that turn left or right.
        else if (cell.IncomingRiver == cell.OutgoingRiver.Previous()) {
            roadCenter -= HexMetrics.GetSecondCorner(cell.IncomingRiver) * 0.2f;
        } else if (cell.IncomingRiver == cell.OutgoingRiver.Next()) {
            roadCenter -= HexMetrics.GetFirstCorner(cell.IncomingRiver) * 0.2f;
        }
        // Handle cases where both previous and next edges have rivers.
        else if (previousHasRiver && nextHasRiver) {
            if (!hasRoadThroughEdge) {
                return; // No road to triangulate.
            }
            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(direction) * HexMetrics.innerToOuter;
            roadCenter += offset * 0.7f;
            center += offset * 0.5f;
        }
        // Handle cases where only one edge has a river.
        else {
            HexDirection middle;
            if (previousHasRiver) {
                middle = direction.Next();
            } else if (nextHasRiver) {
                middle = direction.Previous();
            } else {
                middle = direction;
            }
    
            // Check if there are roads to triangulate.
            if (!cell.HasRoadThroughEdge(middle) &&
                !cell.HasRoadThroughEdge(middle.Previous()) &&
                !cell.HasRoadThroughEdge(middle.Next())) {
                return; // No road to triangulate.
            }
    
            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(middle);
            roadCenter += offset * 0.25f;
    
            // Add a bridge if the road crosses the river.
            if (direction == middle && cell.HasRoadThroughEdge(direction.Opposite())) {
                features.AddBridge(
                    roadCenter,
                    center - offset * (HexMetrics.innerToOuter * 0.7f)
                );
            }
        }
    
        // Calculate the left and right road edges.
        Vector3 mL = Vector3.Lerp(roadCenter, e.v1, interpolators.x);
        Vector3 mR = Vector3.Lerp(roadCenter, e.v5, interpolators.y);
    
        // Triangulate the road geometry.
        TriangulateRoad(roadCenter, mL, mR, e, hasRoadThroughEdge, cell.Index);
    
        // Add road edges if the previous or next edges have rivers.
        if (previousHasRiver) {
            TriangulateRoadEdge(roadCenter, center, mL, cell.Index);
        }
        if (nextHasRiver) {
            TriangulateRoadEdge(roadCenter, mR, center, cell.Index);
        }
    }
    
    // Triangulates a river at its beginning or end.
    void TriangulateWithRiverBeginOrEnd(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
    ) {
        // Create middle edge vertices for the river.
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f)
        );
        m.v3.y = e.v3.y;
    
        // Add a strip of triangles between the middle and outer edges.
        TriangulateEdgeStrip(
            m, weights1, cell.Index,
            e, weights1, cell.Index
        );
    
        // Create a fan of triangles from the center to the middle edge.
        TriangulateEdgeFan(center, m, cell.Index);
    
        // If the cell is not underwater, add river geometry.
        if (!cell.IsUnderwater) {
            bool reversed = cell.HasIncomingRiver;
            Vector3 indices;
            indices.x = indices.y = indices.z = cell.Index;
    
            // Add a quad for the river surface.
            TriangulateRiverQuad(
                m.v2, m.v4, e.v2, e.v4,
                cell.RiverSurfaceY, 0.6f, reversed, indices
            );
    
            // Adjust the y-coordinates for the river surface.
            center.y = m.v2.y = m.v4.y = cell.RiverSurfaceY;
    
            // Add a triangle for the river's end.
            rivers.AddTriangle(center, m.v2, m.v4);
    
            // Add UV mapping for the river triangle.
            if (reversed) {
                rivers.AddTriangleUV(
                    new Vector2(0.5f, 0.4f),
                    new Vector2(1f, 0.2f), new Vector2(0f, 0.2f)
                );
            } else {
                rivers.AddTriangleUV(
                    new Vector2(0.5f, 0.4f),
                    new Vector2(0f, 0.6f), new Vector2(1f, 0.6f)
                );
            }
    
            // Add cell data for the river triangle.
            rivers.AddTriangleCellData(indices, weights1);
        }
    }

    // Triangulates a river flowing through a cell.
    void TriangulateWithRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
    ) {
        Vector3 centerL, centerR;
    
        // Determine the left and right centers based on river connections.
        if (cell.HasRiverThroughEdge(direction.Opposite())) {
            centerL = center +
                HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
            centerR = center +
                HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
        }
        else if (cell.HasRiverThroughEdge(direction.Next())) {
            centerL = center;
            centerR = Vector3.Lerp(center, e.v5, 2f / 3f);
        }
        else if (cell.HasRiverThroughEdge(direction.Previous())) {
            centerL = Vector3.Lerp(center, e.v1, 2f / 3f);
            centerR = center;
        }
        else if (cell.HasRiverThroughEdge(direction.Next2())) {
            centerL = center;
            centerR = center +
                HexMetrics.GetSolidEdgeMiddle(direction.Next()) *
                (0.5f * HexMetrics.innerToOuter);
        }
        else {
            centerL = center +
                HexMetrics.GetSolidEdgeMiddle(direction.Previous()) *
                (0.5f * HexMetrics.innerToOuter);
            centerR = center;
        }
    
        // Calculate the center point between the left and right centers.
        center = Vector3.Lerp(centerL, centerR, 0.5f);
    
        // Create middle edge vertices for the river.
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(centerL, e.v1, 0.5f),
            Vector3.Lerp(centerR, e.v5, 0.5f),
            1f / 6f
        );
        m.v3.y = center.y = e.v3.y;
    
        // Add a strip of triangles between the middle and outer edges.
        TriangulateEdgeStrip(
            m, weights1, cell.Index,
            e, weights1, cell.Index
        );
    
        // Add triangles and quads to form the terrain around the river.
        terrain.AddTriangle(centerL, m.v1, m.v2);
        terrain.AddQuad(centerL, center, m.v2, m.v3);
        terrain.AddQuad(center, centerR, m.v3, m.v4);
        terrain.AddTriangle(centerR, m.v4, m.v5);
    
        // Add cell data for the terrain.
        Vector3 indices;
        indices.x = indices.y = indices.z = cell.Index;
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddQuadCellData(indices, weights1);
        terrain.AddQuadCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);
    
        // Add river geometry if the cell is not underwater.
        if (!cell.IsUnderwater) {
            bool reversed = cell.IncomingRiver == direction;
    
            // Add quads for the river surface.
            TriangulateRiverQuad(
                centerL, centerR, m.v2, m.v4,
                cell.RiverSurfaceY, 0.4f, reversed, indices
            );
            TriangulateRiverQuad(
                m.v2, m.v4, e.v2, e.v4,
                cell.RiverSurfaceY, 0.6f, reversed, indices
            );
        }
    }
    
    // Triangulates the connection between a cell and its neighbor.
    void TriangulateConnection(
        HexDirection direction, HexCell cell, EdgeVertices e1
    ) {
        // Get the neighboring cell in the specified direction.
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null) {
            return; // No neighbor, no connection to triangulate.
        }
    
        // Calculate the bridge vector between the cell and its neighbor.
        Vector3 bridge = HexMetrics.GetBridge(direction);
        bridge.y = neighbor.Position.y - cell.Position.y;
    
        // Create edge vertices for the neighbor.
        EdgeVertices e2 = new EdgeVertices(
            e1.v1 + bridge,
            e1.v5 + bridge
        );
    
        // Check if the connection involves a river or a road.
        bool hasRiver = cell.HasRiverThroughEdge(direction);
        bool hasRoad = cell.HasRoadThroughEdge(direction);
    
        // Handle river connections.
        if (hasRiver) {
            e2.v3.y = neighbor.StreamBedY;
            Vector3 indices;
            indices.x = indices.z = cell.Index;
            indices.y = neighbor.Index;
    
            // If neither cell is underwater, triangulate the river connection.
            if (!cell.IsUnderwater) {
                if (!neighbor.IsUnderwater) {
                    TriangulateRiverQuad(
                        e1.v2, e1.v4, e2.v2, e2.v4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
                        cell.HasIncomingRiver && cell.IncomingRiver == direction,
                        indices
                    );
                }
                // If the neighbor is underwater, triangulate a waterfall.
                else if (cell.Elevation > neighbor.WaterLevel) {
                    TriangulateWaterfallInWater(
                        e1.v2, e1.v4, e2.v2, e2.v4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY,
                        neighbor.WaterSurfaceY, indices
                    );
                }
            }
            // If the cell is underwater but the neighbor is not, triangulate a waterfall.
            else if (
                !neighbor.IsUnderwater &&
                neighbor.Elevation > cell.WaterLevel
            ) {
                TriangulateWaterfallInWater(
                    e2.v4, e2.v2, e1.v4, e1.v2,
                    neighbor.RiverSurfaceY, cell.RiverSurfaceY,
                    cell.WaterSurfaceY, indices
                );
            }
        }

		// Handles the triangulation of edges based on their type (slope or flat).
		if (cell.GetEdgeType(direction) == HexEdgeType.Slope) {
			// If the edge is a slope, triangulate terraces between the two cells.
			TriangulateEdgeTerraces(e1, cell, e2, neighbor, hasRoad);
		} else {
			// If the edge is flat, triangulate a simple edge strip.
			TriangulateEdgeStrip(
				e1, weights1, cell.Index,
				e2, weights2, neighbor.Index, hasRoad
			);
		}
		
		// Adds walls between the two cells if necessary.
		features.AddWall(e1, cell, e2, neighbor, hasRiver, hasRoad);
		
		// Handles the triangulation of corners between three cells.
		HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
		if (direction <= HexDirection.E && nextNeighbor != null) {
			// Calculate the position of the third vertex for the corner.
			Vector3 v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
			v5.y = nextNeighbor.Position.y;
		
			// Determine the elevation relationships between the cells.
			if (cell.Elevation <= neighbor.Elevation) {
				if (cell.Elevation <= nextNeighbor.Elevation) {
					// Triangulate the corner with the current cell as the lowest.
					TriangulateCorner(
						e1.v5, cell, e2.v5, neighbor, v5, nextNeighbor
					);
				} else {
					// Triangulate the corner with the next neighbor as the lowest.
					TriangulateCorner(
						v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor
					);
				}
			} else if (neighbor.Elevation <= nextNeighbor.Elevation) {
				// Triangulate the corner with the neighbor as the lowest.
				TriangulateCorner(
					e2.v5, neighbor, v5, nextNeighbor, e1.v5, cell
				);
			} else {
				// Triangulate the corner with the next neighbor as the lowest.
				TriangulateCorner(
					v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor
				);
			}
		}
	}
    // Triangulates a waterfall in water between two elevations.
    void TriangulateWaterfallInWater(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y1, float y2, float waterY, Vector3 indices
    ) {
        // Adjust the y-coordinates of the vertices to match the elevations.
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;
    
        // Perturb the vertices for a natural look.
        v1 = HexMetrics.Perturb(v1);
        v2 = HexMetrics.Perturb(v2);
        v3 = HexMetrics.Perturb(v3);
        v4 = HexMetrics.Perturb(v4);
    
        // Interpolate the vertices to create the waterfall effect.
        float t = (waterY - y2) / (y1 - y2);
        v3 = Vector3.Lerp(v3, v1, t);
        v4 = Vector3.Lerp(v4, v2, t);
    
        // Add the waterfall geometry to the river mesh.
        rivers.AddQuadUnperturbed(v1, v2, v3, v4);
        rivers.AddQuadUV(0f, 1f, 0.8f, 1f);
        rivers.AddQuadCellData(indices, weights1, weights2);
    }
    
    // Triangulates a corner between three cells.
    void TriangulateCorner(
        Vector3 bottom, HexCell bottomCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        // Determine the edge types between the cells.
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);
    
        // Handle different combinations of edge types.
        if (leftEdgeType == HexEdgeType.Slope) {
            if (rightEdgeType == HexEdgeType.Slope) {
                // Both edges are slopes, triangulate terraces.
                TriangulateCornerTerraces(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            } else if (rightEdgeType == HexEdgeType.Flat) {
                // Left edge is a slope, right edge is flat.
                TriangulateCornerTerraces(
                    left, leftCell, right, rightCell, bottom, bottomCell
                );
            } else {
                // Left edge is a slope, right edge is a cliff.
                TriangulateCornerTerracesCliff(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }
        } else if (rightEdgeType == HexEdgeType.Slope) {
            if (leftEdgeType == HexEdgeType.Flat) {
                // Right edge is a slope, left edge is flat.
                TriangulateCornerTerraces(
                    right, rightCell, bottom, bottomCell, left, leftCell
                );
            } else {
                // Right edge is a slope, left edge is a cliff.
                TriangulateCornerCliffTerraces(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }
        } else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            if (leftCell.Elevation < rightCell.Elevation) {
                // Left cell is lower, triangulate cliff-terraces.
                TriangulateCornerCliffTerraces(
                    right, rightCell, bottom, bottomCell, left, leftCell
                );
            } else {
                // Right cell is lower, triangulate terraces-cliff.
                TriangulateCornerTerracesCliff(
                    left, leftCell, right, rightCell, bottom, bottomCell
                );
            }
        } else {
            // All edges are flat, add a simple triangle.
            terrain.AddTriangle(bottom, left, right);
            Vector3 indices;
            indices.x = bottomCell.Index;
            indices.y = leftCell.Index;
            indices.z = rightCell.Index;
            terrain.AddTriangleCellData(indices, weights1, weights2, weights3);
        }
    
        // Add walls between the cells if necessary.
        features.AddWall(bottom, bottomCell, left, leftCell, right, rightCell);
    }
    
    // Triangulates terraces along an edge between two cells.
    void TriangulateEdgeTerraces(
        EdgeVertices begin, HexCell beginCell,
        EdgeVertices end, HexCell endCell,
        bool hasRoad
    ) {
        // Interpolate the first terrace step.
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color w2 = HexMetrics.TerraceLerp(weights1, weights2, 1);
        float i1 = beginCell.Index;
        float i2 = endCell.Index;
    
        // Triangulate the first terrace strip.
        TriangulateEdgeStrip(begin, weights1, i1, e2, w2, i2, hasRoad);
    
        // Interpolate and triangulate the remaining terrace steps.
        for (int i = 2; i < HexMetrics.terraceSteps; i++) {
            EdgeVertices e1 = e2;
            Color w1 = w2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            w2 = HexMetrics.TerraceLerp(weights1, weights2, i);
            TriangulateEdgeStrip(e1, w1, i1, e2, w2, i2, hasRoad);
        }
    
        // Triangulate the final terrace strip.
        TriangulateEdgeStrip(e2, w2, i1, end, weights2, i2, hasRoad);
    }

    // Triangulates terraces at a corner between three cells.
    void TriangulateCornerTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        // Interpolate the first terrace step between the three cells.
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color w3 = HexMetrics.TerraceLerp(weights1, weights2, 1);
        Color w4 = HexMetrics.TerraceLerp(weights1, weights3, 1);
        Vector3 indices;
        indices.x = beginCell.Index;
        indices.y = leftCell.Index;
        indices.z = rightCell.Index;
    
        // Add the first triangle for the terrace.
        terrain.AddTriangle(begin, v3, v4);
        terrain.AddTriangleCellData(indices, weights1, w3, w4);
    
        // Interpolate and add quads for the remaining terrace steps.
        for (int i = 2; i < HexMetrics.terraceSteps; i++) {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color w1 = w3;
            Color w2 = w4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            w3 = HexMetrics.TerraceLerp(weights1, weights2, i);
            w4 = HexMetrics.TerraceLerp(weights1, weights3, i);
            terrain.AddQuad(v1, v2, v3, v4);
            terrain.AddQuadCellData(indices, w1, w2, w3, w4);
        }
    
        // Add the final quad connecting the terraces to the left and right cells.
        terrain.AddQuad(v3, v4, left, right);
        terrain.AddQuadCellData(indices, w3, w4, weights2, weights3);
    }
    
    // Triangulates a corner with terraces and a cliff between three cells.
    void TriangulateCornerTerracesCliff(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        // Calculate the boundary point where the cliff meets the terraces.
        float b = 1f / (rightCell.Elevation - beginCell.Elevation);
        if (b < 0) {
            b = -b;
        }
        Vector3 boundary = Vector3.Lerp(
            HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b
        );
        Color boundaryWeights = Color.Lerp(weights1, weights3, b);
        Vector3 indices;
        indices.x = beginCell.Index;
        indices.y = leftCell.Index;
        indices.z = rightCell.Index;
    
        // Add a boundary triangle for the cliff.
        TriangulateBoundaryTriangle(
            begin, weights1, left, weights2, boundary, boundaryWeights, indices
        );
    
        // Check if the edge between the left and right cells is a slope.
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            // Add a boundary triangle for the slope.
            TriangulateBoundaryTriangle(
                left, weights2, right, weights3,
                boundary, boundaryWeights, indices
            );
        } else {
            // Add a simple triangle for the flat edge.
            terrain.AddTriangleUnperturbed(
                HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary
            );
            terrain.AddTriangleCellData(
                indices, weights2, weights3, boundaryWeights
            );
        }
    }
    
    // Triangulates a corner with a cliff and terraces between three cells.
    void TriangulateCornerCliffTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        // Calculate the boundary point where the cliff meets the terraces.
        float b = 1f / (leftCell.Elevation - beginCell.Elevation);
        if (b < 0) {
            b = -b;
        }
        Vector3 boundary = Vector3.Lerp(
            HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b
        );
        Color boundaryWeights = Color.Lerp(weights1, weights2, b);
        Vector3 indices;
        indices.x = beginCell.Index;
        indices.y = leftCell.Index;
        indices.z = rightCell.Index;
    
        // Add a boundary triangle for the cliff.
        TriangulateBoundaryTriangle(
            right, weights3, begin, weights1, boundary, boundaryWeights, indices
        );
    
        // Check if the edge between the left and right cells is a slope.
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            // Add a boundary triangle for the slope.
            TriangulateBoundaryTriangle(
                left, weights2, right, weights3,
                boundary, boundaryWeights, indices
            );
        } else {
            // Add a simple triangle for the flat edge.
            terrain.AddTriangleUnperturbed(
                HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary
            );
            terrain.AddTriangleCellData(
                indices, weights2, weights3, boundaryWeights
            );
        }
    }

    // Triangulates a boundary triangle between two cells.
    void TriangulateBoundaryTriangle(
        Vector3 begin, Color beginWeights,
        Vector3 left, Color leftWeights,
        Vector3 boundary, Color boundaryWeights, Vector3 indices
    ) {
        // Interpolate the first terrace step between the two cells.
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color w2 = HexMetrics.TerraceLerp(beginWeights, leftWeights, 1);
    
        // Add the first triangle for the boundary.
        terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
        terrain.AddTriangleCellData(indices, beginWeights, w2, boundaryWeights);
    
        // Interpolate and add triangles for the remaining terrace steps.
        for (int i = 2; i < HexMetrics.terraceSteps; i++) {
            Vector3 v1 = v2;
            Color w1 = w2;
            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            w2 = HexMetrics.TerraceLerp(beginWeights, leftWeights, i);
            terrain.AddTriangleUnperturbed(v1, v2, boundary);
            terrain.AddTriangleCellData(indices, w1, w2, boundaryWeights);
        }
    
        // Add the final triangle connecting the terraces to the left cell.
        terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
        terrain.AddTriangleCellData(indices, w2, leftWeights, boundaryWeights);
    }
    
    // Triangulates a fan of triangles from the center to the edge vertices.
    void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, float index) {
        // Add triangles for each edge of the fan.
        terrain.AddTriangle(center, edge.v1, edge.v2);
        terrain.AddTriangle(center, edge.v2, edge.v3);
        terrain.AddTriangle(center, edge.v3, edge.v4);
        terrain.AddTriangle(center, edge.v4, edge.v5);
    
        // Add cell data for the triangles.
        Vector3 indices;
        indices.x = indices.y = indices.z = index;
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);
    }
    
    // Triangulates a strip of quads between two edges.
    void TriangulateEdgeStrip(
        EdgeVertices e1, Color w1, float index1,
        EdgeVertices e2, Color w2, float index2,
        bool hasRoad = false
    ) {
        // Add quads for each segment of the strip.
        terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
    
        // Add cell data for the quads.
        Vector3 indices;
        indices.x = indices.z = index1;
        indices.y = index2;
        terrain.AddQuadCellData(indices, w1, w2);
        terrain.AddQuadCellData(indices, w1, w2);
        terrain.AddQuadCellData(indices, w1, w2);
        terrain.AddQuadCellData(indices, w1, w2);
    
        // If there is a road, triangulate the road segment.
        if (hasRoad) {
            TriangulateRoadSegment(
                e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4, w1, w2, indices
            );
        }
    }
    
    // Triangulates a quad for a river segment.
    void TriangulateRiverQuad(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y, float v, bool reversed, Vector3 indices
    ) {
        // Overload to handle a single elevation for the river.
        TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed, indices);
    }
    
    // Triangulates a quad for a river segment with varying elevations.
    void TriangulateRiverQuad(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y1, float y2, float v, bool reversed, Vector3 indices
    ) {
        // Set the y-coordinates of the vertices to match the elevations.
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;
    
        // Add the quad to the river mesh.
        rivers.AddQuad(v1, v2, v3, v4);
    
        // Add UV mapping for the river quad.
        if (reversed) {
            rivers.AddQuadUV(1f, 0f, 0.8f - v, 0.6f - v);
        } else {
            rivers.AddQuadUV(0f, 1f, v, v + 0.2f);
        }
    
        // Add cell data for the river quad.
        rivers.AddQuadCellData(indices, weights1, weights2);
    }
    
    // Triangulates a road segment within a cell.
    void TriangulateRoad(
        Vector3 center, Vector3 mL, Vector3 mR,
        EdgeVertices e, bool hasRoadThroughCellEdge, float index
    ) {
        if (hasRoadThroughCellEdge) {
            // Add a road segment with a center point.
            Vector3 indices;
            indices.x = indices.y = indices.z = index;
            Vector3 mC = Vector3.Lerp(mL, mR, 0.5f);
            TriangulateRoadSegment(
                mL, mC, mR, e.v2, e.v3, e.v4,
                weights1, weights1, indices
            );
    
            // Add triangles for the road center.
            roads.AddTriangle(center, mL, mC);
            roads.AddTriangle(center, mC, mR);
    
            // Add UV mapping for the road triangles.
            roads.AddTriangleUV(
                new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f)
            );
            roads.AddTriangleUV(
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f)
            );
    
            // Add cell data for the road triangles.
            roads.AddTriangleCellData(indices, weights1);
            roads.AddTriangleCellData(indices, weights1);
        } else {
            // Add a simple road edge if no road passes through the cell edge.
            TriangulateRoadEdge(center, mL, mR, index);
        }
    }
    
    // Triangulates a road edge between two points.
    void TriangulateRoadEdge(
        Vector3 center, Vector3 mL, Vector3 mR, float index
    ) {
        // Add a triangle for the road edge.
        roads.AddTriangle(center, mL, mR);
    
        // Add UV mapping for the road edge.
        roads.AddTriangleUV(
            new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f)
        );
    
        // Add cell data for the road edge.
        Vector3 indices;
        indices.x = indices.y = indices.z = index;
        roads.AddTriangleCellData(indices, weights1);
    }
    
    // Triangulates a road segment between two edges.
    void TriangulateRoadSegment(
        Vector3 v1, Vector3 v2, Vector3 v3,
        Vector3 v4, Vector3 v5, Vector3 v6,
        Color w1, Color w2, Vector3 indices
    ) {
        // Add quads for the road segment.
        roads.AddQuad(v1, v2, v4, v5);
        roads.AddQuad(v2, v3, v5, v6);
    
        // Add UV mapping for the road quads.
        roads.AddQuadUV(0f, 1f, 0f, 0f);
        roads.AddQuadUV(1f, 0f, 0f, 0f);
    
        // Add cell data for the road quads.
        roads.AddQuadCellData(indices, w1, w2);
        roads.AddQuadCellData(indices, w1, w2);
    }
}
