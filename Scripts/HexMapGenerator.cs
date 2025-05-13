using System.Collections.Generic;
using UnityEngine;

// This class is responsible for generating a hexagonal map with various terrain features.
public class HexMapGenerator : MonoBehaviour {

    // Reference to the HexGrid that represents the map.
    public HexGrid grid;

    // Determines whether to use a fixed seed for random generation.
    public bool useFixedSeed;

    // Seed value for random generation.
    public int seed;

    // Probability of jittering terrain features.
    [Range(0f, 0.5f)]
    public float jitterProbability = 0.25f;

    // Minimum and maximum chunk sizes for terrain generation.
    [Range(20, 200)]
    public int chunkSizeMin = 30;
    [Range(20, 200)]
    public int chunkSizeMax = 100;

    // Probability of creating high-rise terrain features.
    [Range(0f, 1f)]
    public float highRiseProbability = 0.25f;

    // Probability of sinking terrain features.
    [Range(0f, 0.4f)]
    public float sinkProbability = 0.2f;

    // Percentage of land on the map.
    [Range(5, 95)]
    public int landPercentage = 50;

    // Water level elevation.
    [Range(1, 5)]
    public int waterLevel = 3;

    // Minimum and maximum elevation levels.
    [Range(-4, 0)]
    public int elevationMinimum = -2;
    [Range(6, 10)]
    public int elevationMaximum = 8;

    // Map border sizes.
    [Range(0, 10)]
    public int mapBorderX = 5;
    [Range(0, 10)]
    public int mapBorderZ = 5;

    // Region border size.
    [Range(0, 10)]
    public int regionBorder = 5;

    // Number of regions to divide the map into.
    [Range(1, 4)]
    public int regionCount = 1;

    // Percentage of erosion applied to the terrain.
    [Range(0, 100)]
    public int erosionPercentage = 50;

    // Initial moisture level for climate simulation.
    [Range(0f, 1f)]
    public float startingMoisture = 0.1f;

    // Factors for evaporation, precipitation, runoff, and seepage in climate simulation.
    [Range(0f, 1f)]
    public float evaporationFactor = 0.5f;
    [Range(0f, 1f)]
    public float precipitationFactor = 0.25f;
    [Range(0f, 1f)]
    public float runoffFactor = 0.25f;
    [Range(0f, 1f)]
    public float seepageFactor = 0.125f;

    // Wind direction and strength for climate simulation.
    public HexDirection windDirection = HexDirection.NW;
    [Range(1f, 10f)]
    public float windStrength = 4f;

    // Percentage of rivers on the map.
    [Range(0, 20)]
    public int riverPercentage = 10;

    // Probability of creating extra lakes.
    [Range(0f, 1f)]
    public float extraLakeProbability = 0.25f;

    // Temperature range for the map.
    [Range(0f, 1f)]
    public float lowTemperature = 0f;
    [Range(0f, 1f)]
    public float highTemperature = 1f;

    // Hemisphere mode for temperature distribution.
    public enum HemisphereMode {
        Both, North, South
    }
    public HemisphereMode hemisphere;

    // Jitter applied to temperature calculations.
    [Range(0f, 1f)]
    public float temperatureJitter = 0.1f;

    // Priority queue for terrain search operations.
    HexCellPriorityQueue searchFrontier;

    // Tracks the current search phase.
    int searchFrontierPhase;

    // Total number of cells and land cells on the map.
    int cellCount, landCells;

    // Channel used for temperature jitter.
    int temperatureJitterChannel;

    // Represents a region of the map.
    struct MapRegion {
        public int xMin, xMax, zMin, zMax;
    }

    // List of map regions.
    List<MapRegion> regions;

    // Climate data for each cell.
    struct ClimateData {
        public float clouds, moisture;
    }
    List<ClimateData> climate = new List<ClimateData>();
    List<ClimateData> nextClimate = new List<ClimateData>();

    // List of flow directions for rivers.
    List<HexDirection> flowDirections = new List<HexDirection>();

    // Represents a biome with terrain and plant types.
    struct Biome {
        public int terrain, plant;

        public Biome (int terrain, int plant) {
            this.terrain = terrain;
            this.plant = plant;
        }
    }

    // Temperature and moisture bands for biome determination.
    static float[] temperatureBands = { 0.1f, 0.3f, 0.6f };
    static float[] moistureBands = { 0.12f, 0.28f, 0.85f };

    // Array of predefined biomes.
    static Biome[] biomes = {
        new Biome(0, 0), new Biome(4, 0), new Biome(4, 0), new Biome(4, 0),
        new Biome(0, 0), new Biome(2, 0), new Biome(2, 1), new Biome(2, 2),
        new Biome(0, 0), new Biome(1, 0), new Biome(1, 1), new Biome(1, 2),
        new Biome(0, 0), new Biome(1, 1), new Biome(1, 2), new Biome(1, 3)
    };

    // Main method to generate the map.
    public void GenerateMap (int x, int z, bool wrapping) {
        // Save the original random state.
        Random.State originalRandomState = Random.state;

        // Initialize the random seed.
        if (!useFixedSeed) {
            seed = Random.Range(0, int.MaxValue);
            seed ^= (int)System.DateTime.Now.Ticks;
            seed ^= (int)Time.unscaledTime;
            seed &= int.MaxValue;
        }
        Random.InitState(seed);

        // Initialize the grid and cells.
        cellCount = x * z;
        grid.CreateMap(x, z, wrapping);
        if (searchFrontier == null) {
            searchFrontier = new HexCellPriorityQueue();
        }
        for (int i = 0; i < cellCount; i++) {
            grid.GetCell(i).WaterLevel = waterLevel;
        }

        // Generate map features.
        CreateRegions();
        CreateLand();
        ErodeLand();
        CreateClimate();
        CreateRivers();
        SetTerrainType();

        // Reset search phases for all cells.
        for (int i = 0; i < cellCount; i++) {
            grid.GetCell(i).SearchPhase = 0;
        }

        // Restore the original random state.
        Random.state = originalRandomState;
    }
// Creates regions on the map based on the specified region count.
void CreateRegions () {
    // Initialize or clear the regions list.
    if (regions == null) {
        regions = new List<MapRegion>();
    } else {
        regions.Clear();
    }

    // Determine the border size for the X-axis based on whether the map wraps.
    int borderX = grid.wrapping ? regionBorder : mapBorderX;
    MapRegion region;

    // Divide the map into regions based on the region count.
    switch (regionCount) {
        default:
            // Single region covering the entire map.
            if (grid.wrapping) {
                borderX = 0;
            }
            region.xMin = borderX;
            region.xMax = grid.cellCountX - borderX;
            region.zMin = mapBorderZ;
            region.zMax = grid.cellCountZ - mapBorderZ;
            regions.Add(region);
            break;

        case 2:
            // Split the map into two regions, either horizontally or vertically.
            if (Random.value < 0.5f) {
                // Vertical split.
                region.xMin = borderX;
                region.xMax = grid.cellCountX / 2 - regionBorder;
                region.zMin = mapBorderZ;
                region.zMax = grid.cellCountZ - mapBorderZ;
                regions.Add(region);

                region.xMin = grid.cellCountX / 2 + regionBorder;
                region.xMax = grid.cellCountX - borderX;
                regions.Add(region);
            } else {
                // Horizontal split.
                if (grid.wrapping) {
                    borderX = 0;
                }
                region.xMin = borderX;
                region.xMax = grid.cellCountX - borderX;
                region.zMin = mapBorderZ;
                region.zMax = grid.cellCountZ / 2 - regionBorder;
                regions.Add(region);

                region.zMin = grid.cellCountZ / 2 + regionBorder;
                region.zMax = grid.cellCountZ - mapBorderZ;
                regions.Add(region);
            }
            break;

        case 3:
            // Split the map into three vertical regions.
            region.xMin = borderX;
            region.xMax = grid.cellCountX / 3 - regionBorder;
            region.zMin = mapBorderZ;
            region.zMax = grid.cellCountZ - mapBorderZ;
            regions.Add(region);

            region.xMin = grid.cellCountX / 3 + regionBorder;
            region.xMax = grid.cellCountX * 2 / 3 - regionBorder;
            regions.Add(region);

            region.xMin = grid.cellCountX * 2 / 3 + regionBorder;
            region.xMax = grid.cellCountX - borderX;
            regions.Add(region);
            break;

        case 4:
            // Split the map into four quadrants.
            region.xMin = borderX;
            region.xMax = grid.cellCountX / 2 - regionBorder;
            region.zMin = mapBorderZ;
            region.zMax = grid.cellCountZ / 2 - regionBorder;
            regions.Add(region);

            region.xMin = grid.cellCountX / 2 + regionBorder;
            region.xMax = grid.cellCountX - borderX;
            regions.Add(region);

            region.zMin = grid.cellCountZ / 2 + regionBorder;
            region.zMax = grid.cellCountZ - mapBorderZ;
            regions.Add(region);

            region.xMin = borderX;
            region.xMax = grid.cellCountX / 2 - regionBorder;
            regions.Add(region);
            break;
    }
}

// Generates land on the map by raising or sinking terrain.
void CreateLand () {
    // Calculate the total land budget based on the land percentage.
    int landBudget = Mathf.RoundToInt(cellCount * landPercentage * 0.01f);
    landCells = landBudget;

    // Attempt to create land within a maximum number of iterations.
    for (int guard = 0; guard < 10000; guard++) {
        // Randomly decide whether to sink or raise terrain.
        bool sink = Random.value < sinkProbability;

        // Process each region to modify terrain.
        for (int i = 0; i < regions.Count; i++) {
            MapRegion region = regions[i];
            int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax - 1);

            if (sink) {
                // Sink terrain in the region.
                landBudget = SinkTerrain(chunkSize, landBudget, region);
            } else {
                // Raise terrain in the region.
                landBudget = RaiseTerrain(chunkSize, landBudget, region);
                if (landBudget == 0) {
                    return; // Stop if the land budget is exhausted.
                }
            }
        }
    }

    // Log a warning if the land budget could not be fully used.
    if (landBudget > 0) {
        Debug.LogWarning("Failed to use up " + landBudget + " land budget.");
        landCells -= landBudget;
    }
}

// Raises terrain in a specific region.
int RaiseTerrain (int chunkSize, int budget, MapRegion region) {
    searchFrontierPhase += 1;

    // Get a random starting cell within the region.
    HexCell firstCell = GetRandomCell(region);
    firstCell.SearchPhase = searchFrontierPhase;
    firstCell.Distance = 0;
    firstCell.SearchHeuristic = 0;
    searchFrontier.Enqueue(firstCell);
    HexCoordinates center = firstCell.coordinates;

    // Determine the elevation increase (1 or 2 levels).
    int rise = Random.value < highRiseProbability ? 2 : 1;
    int size = 0;

    // Expand the terrain modification until the chunk size is reached.
    while (size < chunkSize && searchFrontier.Count > 0) {
        HexCell current = searchFrontier.Dequeue();
        int originalElevation = current.Elevation;
        int newElevation = originalElevation + rise;

        // Skip if the new elevation exceeds the maximum allowed.
        if (newElevation > elevationMaximum) {
            continue;
        }

        current.Elevation = newElevation;

        // Reduce the budget if the cell transitions from water to land.
        if (originalElevation < waterLevel && newElevation >= waterLevel && --budget == 0) {
            break;
        }

        size += 1;

        // Add neighboring cells to the search frontier.
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
            HexCell neighbor = current.GetNeighbor(d);
            if (neighbor && neighbor.SearchPhase < searchFrontierPhase) {
                neighbor.SearchPhase = searchFrontierPhase;
                neighbor.Distance = neighbor.coordinates.DistanceTo(center);
                neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;
                searchFrontier.Enqueue(neighbor);
            }
        }
    }

    searchFrontier.Clear();
    return budget;
}

// Sinks terrain in a specific region.
int SinkTerrain (int chunkSize, int budget, MapRegion region) {
    searchFrontierPhase += 1;

    // Get a random starting cell within the region.
    HexCell firstCell = GetRandomCell(region);
    firstCell.SearchPhase = searchFrontierPhase;
    firstCell.Distance = 0;
    firstCell.SearchHeuristic = 0;
    searchFrontier.Enqueue(firstCell);
    HexCoordinates center = firstCell.coordinates;

    // Determine the elevation decrease (1 or 2 levels).
    int sink = Random.value < highRiseProbability ? 2 : 1;
    int size = 0;

    // Expand the terrain modification until the chunk size is reached.
    while (size < chunkSize && searchFrontier.Count > 0) {
        HexCell current = searchFrontier.Dequeue();
        int originalElevation = current.Elevation;
        int newElevation = current.Elevation - sink;

        // Skip if the new elevation is below the minimum allowed.
        if (newElevation < elevationMinimum) {
            continue;
        }

        current.Elevation = newElevation;

        // Increase the budget if the cell transitions from land to water.
        if (originalElevation >= waterLevel && newElevation < waterLevel) {
            budget += 1;
        }

        size += 1;

        // Add neighboring cells to the search frontier.
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
            HexCell neighbor = current.GetNeighbor(d);
            if (neighbor && neighbor.SearchPhase < searchFrontierPhase) {
                neighbor.SearchPhase = searchFrontierPhase;
                neighbor.Distance = neighbor.coordinates.DistanceTo(center);
                neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;
                searchFrontier.Enqueue(neighbor);
            }
        }
    }

    searchFrontier.Clear();
    return budget;
}

	// Applies erosion to the terrain to smooth it out.
	void ErodeLand () {
		// Collect all erodible cells on the map.
		List<HexCell> erodibleCells = ListPool<HexCell>.Get();
		for (int i = 0; i < cellCount; i++) {
			HexCell cell = grid.GetCell(i);
			if (IsErodible(cell)) {
				erodibleCells.Add(cell); // Add cells that can be eroded to the list.
			}
		}

		// Calculate the target number of erodible cells based on the erosion percentage.
		int targetErodibleCount = (int)(erodibleCells.Count * (100 - erosionPercentage) * 0.01f);

		// Perform erosion until the target number of erodible cells is reached.
		while (erodibleCells.Count > targetErodibleCount) {
			int index = Random.Range(0, erodibleCells.Count); // Select a random erodible cell.
			HexCell cell = erodibleCells[index];
			HexCell targetCell = GetErosionTarget(cell); // Find a target cell for erosion.

			// Lower the elevation of the current cell and raise the elevation of the target cell.
			cell.Elevation -= 1;
			targetCell.Elevation += 1;

			// Remove the current cell from the erodible list if it is no longer erodible.
			if (!IsErodible(cell)) {
				erodibleCells[index] = erodibleCells[erodibleCells.Count - 1];
				erodibleCells.RemoveAt(erodibleCells.Count - 1);
			}

			// Check neighboring cells and add them to the erodible list if they become erodible.
			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbor = cell.GetNeighbor(d);
				if (neighbor && neighbor.Elevation == cell.Elevation + 2 && !erodibleCells.Contains(neighbor)) {
					erodibleCells.Add(neighbor);
				}
			}

			// Add the target cell to the erodible list if it becomes erodible.
			if (IsErodible(targetCell) && !erodibleCells.Contains(targetCell)) {
				erodibleCells.Add(targetCell);
			}

			// Remove neighboring cells of the target cell that are no longer erodible.
			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbor = targetCell.GetNeighbor(d);
				if (neighbor && neighbor != cell && neighbor.Elevation == targetCell.Elevation + 1 && !IsErodible(neighbor)) {
					erodibleCells.Remove(neighbor);
				}
			}
		}

		// Return the erodible cells list to the pool for reuse.
		ListPool<HexCell>.Add(erodibleCells);
	}

	// Determines if a cell is erodible by checking if it has a neighbor with a lower elevation.
	bool IsErodible (HexCell cell) {
		int erodibleElevation = cell.Elevation - 2; // Minimum elevation difference for erosion.
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
			HexCell neighbor = cell.GetNeighbor(d);
			if (neighbor && neighbor.Elevation <= erodibleElevation) {
				return true; // The cell is erodible if a neighbor has a sufficiently lower elevation.
			}
		}
		return false;
	}

	// Finds a target cell for erosion by selecting a neighbor with a lower elevation.
	HexCell GetErosionTarget (HexCell cell) {
		List<HexCell> candidates = ListPool<HexCell>.Get(); // Pool for candidate cells.
		int erodibleElevation = cell.Elevation - 2; // Minimum elevation difference for erosion.
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
			HexCell neighbor = cell.GetNeighbor(d);
			if (neighbor && neighbor.Elevation <= erodibleElevation) {
				candidates.Add(neighbor); // Add neighbors with lower elevation to the candidates list.
			}
		}
		HexCell target = candidates[Random.Range(0, candidates.Count)]; // Randomly select a target cell.
		ListPool<HexCell>.Add(candidates); // Return the candidates list to the pool.
		return target;
	}

	// Simulates climate changes on the map.
	void CreateClimate () {
		climate.Clear(); // Clear the current climate data.
		nextClimate.Clear(); // Clear the next cycle's climate data.

		// Initialize climate data for all cells.
		ClimateData initialData = new ClimateData();
		initialData.moisture = startingMoisture; // Set the starting moisture level.
		ClimateData clearData = new ClimateData(); // Empty data for the next cycle.
		for (int i = 0; i < cellCount; i++) {
			climate.Add(initialData);
			nextClimate.Add(clearData);
		}

		// Simulate climate evolution over multiple cycles.
		for (int cycle = 0; cycle < 40; cycle++) {
			for (int i = 0; i < cellCount; i++) {
				EvolveClimate(i); // Update the climate for each cell.
			}
			// Swap the current and next climate data for the next cycle.
			List<ClimateData> swap = climate;
			climate = nextClimate;
			nextClimate = swap;
		}
	}

	// Updates the climate for a specific cell.
	void EvolveClimate (int cellIndex) {
		HexCell cell = grid.GetCell(cellIndex);
		ClimateData cellClimate = climate[cellIndex];

		// Handle evaporation and cloud formation for underwater cells.
		if (cell.IsUnderwater) {
			cellClimate.moisture = 1f; // Maximum moisture for underwater cells.
			cellClimate.clouds += evaporationFactor; // Add clouds based on evaporation.
		} else {
			// Evaporation for land cells.
			float evaporation = cellClimate.moisture * evaporationFactor;
			cellClimate.moisture -= evaporation;
			cellClimate.clouds += evaporation;
		}

		// Handle precipitation.
		float precipitation = cellClimate.clouds * precipitationFactor;
		cellClimate.clouds -= precipitation;
		cellClimate.moisture += precipitation;

		// Limit cloud density based on elevation.
		float cloudMaximum = 1f - cell.ViewElevation / (elevationMaximum + 1f);
		if (cellClimate.clouds > cloudMaximum) {
			cellClimate.moisture += cellClimate.clouds - cloudMaximum;
			cellClimate.clouds = cloudMaximum;
		}

		// Disperse clouds and moisture to neighboring cells.
		HexDirection mainDispersalDirection = windDirection.Opposite(); // Main wind direction.
		float cloudDispersal = cellClimate.clouds * (1f / (5f + windStrength));
		float runoff = cellClimate.moisture * runoffFactor * (1f / 6f);
		float seepage = cellClimate.moisture * seepageFactor * (1f / 6f);
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
			HexCell neighbor = cell.GetNeighbor(d);
			if (!neighbor) {
				continue; // Skip if there is no neighbor in this direction.
			}
			ClimateData neighborClimate = nextClimate[neighbor.Index];
			if (d == mainDispersalDirection) {
				neighborClimate.clouds += cloudDispersal * windStrength; // Stronger dispersal in the main wind direction.
			} else {
				neighborClimate.clouds += cloudDispersal; // Regular dispersal in other directions.
			}

			// Handle runoff and seepage based on elevation differences.
			int elevationDelta = neighbor.ViewElevation - cell.ViewElevation;
			if (elevationDelta < 0) {
				cellClimate.moisture -= runoff;
				neighborClimate.moisture += runoff;
			} else if (elevationDelta == 0) {
				cellClimate.moisture -= seepage;
				neighborClimate.moisture += seepage;
			}

			nextClimate[neighbor.Index] = neighborClimate; // Update the neighbor's climate data.
		}

		// Update the current cell's climate data for the next cycle.
		ClimateData nextCellClimate = nextClimate[cellIndex];
		nextCellClimate.moisture += cellClimate.moisture;
		if (nextCellClimate.moisture > 1f) {
			nextCellClimate.moisture = 1f; // Cap the moisture level at 1.
		}
		nextClimate[cellIndex] = nextCellClimate;
		climate[cellIndex] = new ClimateData(); // Reset the current cell's climate data.
	}
	// Creates rivers on the map based on climate and terrain.
	void CreateRivers () {
		// List to store potential river origin cells.
		List<HexCell> riverOrigins = ListPool<HexCell>.Get();
		for (int i = 0; i < cellCount; i++) {
			HexCell cell = grid.GetCell(i);
			if (cell.IsUnderwater) {
				continue; // Skip underwater cells.
			}
			ClimateData data = climate[i];
			// Calculate the weight of the cell based on moisture and elevation.
			float weight =
				data.moisture * (cell.Elevation - waterLevel) /
				(elevationMaximum - waterLevel);
			// Add the cell multiple times based on its weight to increase its chances of being selected.
			if (weight > 0.75f) {
				riverOrigins.Add(cell);
				riverOrigins.Add(cell);
			}
			if (weight > 0.5f) {
				riverOrigins.Add(cell);
			}
			if (weight > 0.25f) {
				riverOrigins.Add(cell);
			}
		}

		// Calculate the river budget based on the percentage of land cells.
		int riverBudget = Mathf.RoundToInt(landCells * riverPercentage * 0.01f);
		while (riverBudget > 0 && riverOrigins.Count > 0) {
			// Randomly select a river origin from the list.
			int index = Random.Range(0, riverOrigins.Count);
			int lastIndex = riverOrigins.Count - 1;
			HexCell origin = riverOrigins[index];
			riverOrigins[index] = riverOrigins[lastIndex];
			riverOrigins.RemoveAt(lastIndex);

			// Check if the origin is valid for a river.
			if (!origin.HasRiver) {
				bool isValidOrigin = true;
				for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
					HexCell neighbor = origin.GetNeighbor(d);
					if (neighbor && (neighbor.HasRiver || neighbor.IsUnderwater)) {
						isValidOrigin = false;
						break;
					}
				}
				// Create a river if the origin is valid.
				if (isValidOrigin) {
					riverBudget -= CreateRiver(origin);
				}
			}
		}

		// Log a warning if the river budget could not be fully used.
		if (riverBudget > 0) {
			Debug.LogWarning("Failed to use up river budget.");
		}

		// Return the river origins list to the pool for reuse.
		ListPool<HexCell>.Add(riverOrigins);
	}

	// Creates a river starting from a specific cell.
	int CreateRiver (HexCell origin) {
		int length = 1; // Track the length of the river.
		HexCell cell = origin;
		HexDirection direction = HexDirection.NE; // Initial direction for the river.
		while (!cell.IsUnderwater) {
			int minNeighborElevation = int.MaxValue;
			flowDirections.Clear(); // Clear the list of possible flow directions.
			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbor = cell.GetNeighbor(d);
				if (!neighbor) {
					continue; // Skip if there is no neighbor in this direction.
				}

				// Track the minimum elevation of neighboring cells.
				if (neighbor.Elevation < minNeighborElevation) {
					minNeighborElevation = neighbor.Elevation;
				}

				// Skip neighbors that already have rivers or are the origin.
				if (neighbor == origin || neighbor.HasIncomingRiver) {
					continue;
				}

				int delta = neighbor.Elevation - cell.Elevation;
				if (delta > 0) {
					continue; // Skip neighbors at a higher elevation.
				}

				// If the neighbor already has an outgoing river, connect to it and end the river.
				if (neighbor.HasOutgoingRiver) {
					cell.SetOutgoingRiver(d);
					return length;
				}

				// Add the direction to the flow directions list based on elevation difference.
				if (delta < 0) {
					flowDirections.Add(d);
					flowDirections.Add(d);
					flowDirections.Add(d);
				}
				if (
					length == 1 ||
					(d != direction.Next2() && d != direction.Previous2())
				) {
					flowDirections.Add(d);
				}
				flowDirections.Add(d);
			}

			// If no valid flow directions are found, end the river.
			if (flowDirections.Count == 0) {
				if (length == 1) {
					return 0; // If the river is only 1 cell long, discard it.
				}

				// Create a lake if the river cannot flow further.
				if (minNeighborElevation >= cell.Elevation) {
					cell.WaterLevel = minNeighborElevation;
					if (minNeighborElevation == cell.Elevation) {
						cell.Elevation = minNeighborElevation - 1;
					}
				}
				break;
			}

			// Randomly select a direction for the river to flow.
			direction = flowDirections[Random.Range(0, flowDirections.Count)];
			cell.SetOutgoingRiver(direction);
			length += 1;

			// Create a lake if the river reaches a flat area and meets the probability for extra lakes.
			if (
				minNeighborElevation >= cell.Elevation &&
				Random.value < extraLakeProbability
			) {
				cell.WaterLevel = cell.Elevation;
				cell.Elevation -= 1;
			}

			// Move to the next cell in the selected direction.
			cell = cell.GetNeighbor(direction);
		}
		return length; // Return the length of the river.
	}

	// Sets the terrain type for each cell based on temperature and moisture.
	void SetTerrainType () {
		temperatureJitterChannel = Random.Range(0, 4); // Randomly select a channel for temperature jitter.
		int rockDesertElevation =
			elevationMaximum - (elevationMaximum - waterLevel) / 2; // Elevation threshold for rock deserts.

		for (int i = 0; i < cellCount; i++) {
			HexCell cell = grid.GetCell(i);
			float temperature = DetermineTemperature(cell); // Determine the temperature of the cell.
			float moisture = climate[i].moisture; // Get the moisture level of the cell.
			if (!cell.IsUnderwater) {
				// Determine the temperature and moisture bands for the cell.
				int t = 0;
				for (; t < temperatureBands.Length; t++) {
					if (temperature < temperatureBands[t]) {
						break;
					}
				}
				int m = 0;
				for (; m < moistureBands.Length; m++) {
					if (moisture < moistureBands[m]) {
						break;
					}
				}
				Biome cellBiome = biomes[t * 4 + m]; // Select the biome based on temperature and moisture.

				// Adjust the terrain type for specific conditions.
				if (cellBiome.terrain == 0) {
					if (cell.Elevation >= rockDesertElevation) {
						cellBiome.terrain = 3; // Set to rock desert if elevation is high enough.
					}
				}
				else if (cell.Elevation == elevationMaximum) {
					cellBiome.terrain = 4; // Set to mountain if at maximum elevation.
				}

				// Adjust plant levels for specific conditions.
				if (cellBiome.terrain == 4) {
					cellBiome.plant = 0; // No plants in mountains.
				}
				else if (cellBiome.plant < 3 && cell.HasRiver) {
					cellBiome.plant += 1; // Increase plant level if the cell has a river.
				}

				// Assign the terrain type and plant level to the cell.
				cell.TerrainTypeIndex = cellBiome.terrain;
				cell.PlantLevel = cellBiome.plant;
			}
			else {
				// Determine terrain type for underwater cells.
				int terrain;
				if (cell.Elevation == waterLevel - 1) {
					int cliffs = 0, slopes = 0;
					for (
						HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++
					) {
						HexCell neighbor = cell.GetNeighbor(d);
						if (!neighbor) {
							continue;
						}
						int delta = neighbor.Elevation - cell.WaterLevel;
						if (delta == 0) {
							slopes += 1;
						}
						else if (delta > 0) {
							cliffs += 1;
						}
					}

					// Determine terrain type based on surrounding cliffs and slopes.
					if (cliffs + slopes > 3) {
						terrain = 1; // Coastal terrain.
					}
					else if (cliffs > 0) {
						terrain = 3; // Rocky terrain.
					}
					else if (slopes > 0) {
						terrain = 0; // Sloped terrain.
					}
					else {
						terrain = 1; // Default to coastal terrain.
					}
				}
				else if (cell.Elevation >= waterLevel) {
					terrain = 1; // Coastal terrain.
				}
				else if (cell.Elevation < 0) {
					terrain = 3; // Deep water or rocky terrain.
				}
				else {
					terrain = 2; // Shallow water.
				}

				// Adjust terrain type for cold underwater areas.
				if (terrain == 1 && temperature < temperatureBands[0]) {
					terrain = 2; // Set to icy terrain.
				}
				cell.TerrainTypeIndex = terrain; // Assign the terrain type to the cell.
			}
		}
	}

	// Determines the temperature of a specific cell based on its latitude, elevation, and noise.
	float DetermineTemperature (HexCell cell) {
		// Calculate the latitude of the cell as a fraction of the map's height.
		float latitude = (float)cell.coordinates.Z / grid.cellCountZ;

		// Adjust latitude based on the selected hemisphere mode.
		if (hemisphere == HemisphereMode.Both) {
			latitude *= 2f; // Scale latitude to range from 0 to 2.
			if (latitude > 1f) {
				latitude = 2f - latitude; // Mirror latitude for the southern hemisphere.
			}
		}
		else if (hemisphere == HemisphereMode.North) {
			latitude = 1f - latitude; // Invert latitude for the northern hemisphere.
		}

		// Interpolate temperature based on the latitude and the defined temperature range.
		float temperature =
			Mathf.LerpUnclamped(lowTemperature, highTemperature, latitude);

		// Adjust temperature based on the cell's elevation relative to the water level.
		temperature *= 1f - (cell.ViewElevation - waterLevel) /
			(elevationMaximum - waterLevel + 1f);

		// Add jitter to the temperature using noise sampling for variation.
		float jitter =
			HexMetrics.SampleNoise(cell.Position * 0.1f)[temperatureJitterChannel];

		// Apply jitter to the temperature, scaled by the temperature jitter factor.
		temperature += (jitter * 2f - 1f) * temperatureJitter;

		return temperature; // Return the calculated temperature.
	}

	// Selects a random cell within a specified map region.
	HexCell GetRandomCell (MapRegion region) {
		return grid.GetCell(
			Random.Range(region.xMin, region.xMax), // Random X-coordinate within the region.
			Random.Range(region.zMin, region.zMax)  // Random Z-coordinate within the region.
		);
	}
}