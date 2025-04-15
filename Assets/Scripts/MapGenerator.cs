using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/*
 * Based on "Procedural Cave Generation" by Sebastian Lague
 * https://www.youtube.com/watch?v=v7yyZZjF1z4&list=PLFt_AvWsXl0eZgMK_DT5_biRkWXftAOf9
 */
public class MapGenerator : MonoBehaviour
{

	public int width;
	public int height;

	public string seed;
	public bool useRandomSeed;

	// [Range(0, 100)]
	public int fillPercent;

	public bool alwaysKeepEdgesAsWalls;

	int[,] map;

	public List<Vector2Int> waterTilePositions = new List<Vector2Int>();

	private GameObject currentSharkie;
	public GameObject sharkiePrefab;
	private GameObject currentLab;
	public GameObject laboratoryPrefab;
	private List<GameObject> zombies = new List<GameObject>();
	public GameObject zombiePrefab;
	public Vector2[] islandCenters;

	/*
	 * Generate the map on start, on mouse click
	 */
	void Start() {
		fillPercent = 80;
		GenerateMap();
	}

	void Update() {
		if (Input.GetMouseButtonDown(0)) {
			GenerateMap();
		}
	}

	void GenerateMap() {
		map = new int[width, height];

		// populate the grid cells
		RandomFillMap();

		// apply cellular automata rules
		for (int i = 0; i < 8; i++) {
			SmoothMap();
		}

		// finalize the map
		ProcessMap();
		AddMapBorder();

		// generate mesh
		MeshGenerator meshGen = GetComponent<MeshGenerator>();
		meshGen.GenerateMesh(map, 1);

		// demolish old lab for each new map
		if (currentLab != null) {
			Destroy(currentLab);
		}

		// pick an island and calculate respective world position for lab
		if (laboratoryPrefab != null && islandCenters != null && islandCenters.Length > 0) {
			Vector2 center = islandCenters[Random.Range(0, islandCenters.Length)];
			Vector3 worldPos = new Vector3(center.x - width / 2.0f + 0.5f, 0.5f, center.y - height / 2f + 5f);
			currentLab = Instantiate(laboratoryPrefab, worldPos, Quaternion.Euler(90f, 0.0f, 0.0f));
		}

		// add friends :)
		SpawnSharkie();
		SpawnZombies();
	}


	/*
	 * STAGE 1: Populate the map
	 */

	void RandomFillMap() {

		// use current time for random seeds
		if (useRandomSeed) {
			seed = Time.time.ToString();
		}

		// using seeded randomization for determinism if I want it
		System.Random random = new System.Random(seed.GetHashCode());

		// randomize centers of large islands
		islandCenters = new Vector2[3];
		for (int i = 0; i < islandCenters.Length; i++) {
			float margin = Mathf.Min(width, height) * 0.2f;
			// keeps centers between margin and height or width - margin
			float x = (float)random.NextDouble() * (width - margin * 2) + margin;
			float y = (float)random.NextDouble() * (height - margin * 2) + margin;
			islandCenters[i] = new Vector2(x, y);
		}

		// iterate through grid
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				if (alwaysKeepEdgesAsWalls && (x == 0 || x == width - 1 || y == 0 || y == height - 1)) {
					map[x, y] = 1;
				}
				else {
					Vector2 pos = new Vector2(x, y);

					// find closest island center
					float closestDistance = float.MaxValue;
					foreach (Vector2 center in islandCenters) {
						float dist = Vector2.Distance(pos, center);
						if (dist < closestDistance) {
							closestDistance = dist;
						}	
					}

					// apply falloff
					float maxIslandRadius = Mathf.Min(width, height) * 0.35f; // controls island size
					if (closestDistance > maxIslandRadius) {
						map[x, y] = 0; // limit island size by reverting to water
						continue;
					}

					// close to 1 --> high falloff aka more density close to center
					float t = closestDistance / maxIslandRadius;
					float falloff = Mathf.Clamp01(Mathf.Pow(t, 2f));

					// calculate inverse percentage and fill based on probability
					int adjustedFill = Mathf.RoundToInt(fillPercent * (1 - falloff));
					map[x, y] = (random.Next(0, 100) < adjustedFill) ? 1 : 0;
				}
			}
		}
	}


	/*
	 * STAGE 2: Smooth map with CA
	 */
	void SmoothMap() {
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				int neighbourWallTiles = GetSurroundingWallCount(x, y);

				if (neighbourWallTiles > 4)
					map[x, y] = 1;
				else if (neighbourWallTiles < 4)
					map[x, y] = 0;

			}
		}
	}


	int GetSurroundingWallCount(int gridX, int gridY) {
		int wallCount = 0;
		for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++) {
			for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++) {
				if (IsInMapRange(neighbourX, neighbourY)) {
					if (neighbourX != gridX || neighbourY != gridY) {
						wallCount += map[neighbourX, neighbourY];
					}
				}
				else {
					wallCount++;
				}
			}
		}
		return wallCount;
	}

	bool IsInMapRange(int x, int y) {
		return x >= 0 && x < width && y >= 0 && y < height;
	}


	/*
	 * Stage 3: produce the finished map
	 */
	void ProcessMap() {

		// clean up some little floaters in the corners
		RemoveSmallWallRegions(5);

		// create zone for Sharkie to spawn in
		FillWater();
		waterTilePositions.Clear();
		for (int x = 0; x < map.GetLength(0); x++) {
			for (int y = 0; y < map.GetLength(1); y++) {
				if (map[x, y] == 2) {
					waterTilePositions.Add(new Vector2Int(x, y));
				}
			}
		}

		// beautification of the archipelago
		AddMiniIslands(10, 5);
	}

	void AddMiniIslands(int count, int maxSize) {
		System.Random rand = new System.Random(seed.GetHashCode());

		// generating some extra tiny islands after CA so they aren't just lumps
		for (int i = 0; i < count; i++) {
			int centerX = rand.Next(3, width - 3);
			int centerY = rand.Next(3, height - 3);

			// skip if center is already land
			if (map[centerX, centerY] != 2) continue;

			int radius = rand.Next(1, maxSize + 1);

			// build around center to randomized radius
			for (int dx = -radius; dx <= radius; dx++) {
				for (int dy = -radius; dy <= radius; dy++) {
					int x = centerX + dx;
					int y = centerY + dy;

					if (IsInMapRange(x, y) && Mathf.Sqrt(dx * dx + dy * dy) <= radius) {
						if (map[x, y] == 2) {
							// only convert water to land
							map[x, y] = 1;
						}
					}
				}
			}
		}
	}

	void FillWater() {
		int width = map.GetLength(0);
		int height = map.GetLength(1);
		Queue<Vector2Int> queue = new Queue<Vector2Int>();
		bool[,] visited = new bool[width, height];

		// use bfs to flood water from the corner (avoids Sharkie ending up in a pond)
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				if ((x == 0 || x == width - 1 || y == 0 || y == height - 1) && map[x, y] == 0) {
					queue.Enqueue(new Vector2Int(x, y));
					visited[x, y] = true;
				}
			}
		}

		// use queue to explore surrounding tiles and mark as water
		while (queue.Count > 0) {
			Vector2Int tile = queue.Dequeue();
			map[tile.x, tile.y] = 2;

			foreach (Vector2Int dir in new[] {
				new Vector2Int(0,1), new Vector2Int(1,0),
				new Vector2Int(0,-1), new Vector2Int(-1,0)
			}) {
				int nx = tile.x + dir.x;
				int ny = tile.y + dir.y;

				if (IsInMapRange(nx, ny) && !visited[nx, ny] && map[nx, ny] == 0) {
					queue.Enqueue(new Vector2Int(nx, ny));
					visited[nx, ny] = true;
				}
			}
		}
	}

	void RemoveSmallWallRegions(int threshold) {
		bool[,] visited = new bool[map.GetLength(0), map.GetLength(1)];

		// find weird noisy chunks of wall across the grid
		for (int x = 0; x < map.GetLength(0); x++) {
			for (int y = 0; y < map.GetLength(1); y++) {
				if (!visited[x, y] && map[x, y] == 1) {
					List<Vector2Int> region = GetRegionTiles(x, y, 1);

					// remove tiny wall blob if smaller than threshold
					if (region.Count < threshold) {
						foreach (Vector2Int tile in region) {
							map[tile.x, tile.y] = 0;
						}
					}

					foreach (Vector2Int tile in region) {
						visited[tile.x, tile.y] = true;
					}
				}
			}
		}
	}

	List<Vector2Int> GetRegionTiles(int startX, int startY, int targetType) {
		List<Vector2Int> tiles = new List<Vector2Int>();
		Queue<Vector2Int> queue = new Queue<Vector2Int>();
		bool[,] visited = new bool[map.GetLength(0), map.GetLength(1)];

		queue.Enqueue(new Vector2Int(startX, startY));
		visited[startX, startY] = true;

		// basically repeat the bfs flood to find connected tiles of a given type
		while (queue.Count > 0) {
			Vector2Int tile = queue.Dequeue();
			tiles.Add(tile);

			for (int x = -1; x <= 1; x++) {
				for (int y = -1; y <= 1; y++) {
					if ((x == 0 || y == 0) && x != y) {
						int nx = tile.x + x;
						int ny = tile.y + y;

						if (IsInMapRange(nx, ny) && !visited[nx, ny] && map[nx, ny] == targetType) {
							visited[nx, ny] = true;
							queue.Enqueue(new Vector2Int(nx, ny));
						}
					}
				}
			}
		}
		return tiles;
	}

	public int GetTileType(Vector2 worldPos) {
		// convert back from center origin to corner origin
		int x = Mathf.FloorToInt(worldPos.x + map.GetLength(0) / 2f);
		int y = Mathf.FloorToInt(worldPos.y + map.GetLength(1) / 2f);

		if (x >= 0 && x < map.GetLength(0) && y >= 0 && y < map.GetLength(1)) {
			return map[x, y];
		}

		return -1; // oob
	}

	void AddMapBorder() {
		int borderSize = 1;
		int[,] borderedMap = new int[width + borderSize * 2, height + borderSize * 2];

		// add borders so Sharkie has hard boundary
		for (int x = 0; x < borderedMap.GetLength(0); x++) {
			for (int y = 0; y < borderedMap.GetLength(1); y++) {
				if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize) {
					borderedMap[x, y] = map[x - borderSize, y - borderSize];
				}
				else {
					borderedMap[x, y] = 3;
				}
			}
		}
		map = borderedMap;
	}

	void SpawnSharkie() {
		if (sharkiePrefab == null) {
			Debug.LogWarning("Sharkie prefab not assigned!");
			return;
		}

		// remove last shark
		if (currentSharkie != null) {
			Destroy(currentSharkie);
		}

		// find a water tile (should not need 100 tries lol)
		for (int i = 0; i < 100; i++) {
			int x = Random.Range(0, width);
			int y = Random.Range(0, height);

			// if water, spawn Sharkie & assign mapGenerator 
			if (map[x, y] == 2) {
				float worldX = x - map.GetLength(0) / 2f + 0.5f;
				float worldY = y - map.GetLength(1) / 2f + 0.5f;
				Vector3 spawnPos = new Vector3(worldX, 0f, worldY);

				currentSharkie = Instantiate(sharkiePrefab, spawnPos, sharkiePrefab.transform.rotation);

				ZombShark sharkScript = currentSharkie.GetComponent<ZombShark>();
				if (sharkScript != null) {
					sharkScript.mapGenerator = this;
				}
				return;
			}
		}
		Debug.LogWarning("no water found for Sharkie :(");
	}

	void SpawnZombies() {
		// clear old zombies
		foreach (GameObject z in zombies) Destroy(z);
		zombies.Clear();

		// add a zombie to each main centerpoint
		foreach (Vector2 center in islandCenters) {
			Vector3 pos = new Vector3(center.x - width / 2f + 0.5f, 0.5f, center.y - height / 2f + 0.5f);

			GameObject z = Instantiate(zombiePrefab, pos, Quaternion.Euler(90f, 0, 0));
			zombies.Add(z);
		}
	}
}