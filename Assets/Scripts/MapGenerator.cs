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
	public int randomFillPercent;

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
		randomFillPercent = 80;
		alwaysKeepEdgesAsWalls = false;
		GenerateMap();
	}

	void Update() {
		if (Input.GetMouseButtonDown(0)) {
			GenerateMap();
		}
	}

	void GenerateMap() {
		map = new int[width, height];

		// Stage 1: populate the grid cells
		PopulateMap();

		// Stage 2: apply cellular automata rules
		for (int i = 0; i < 8; i++)
		{
			SmoothMap();
		}

		// Stage 3: finalise the map
		ProcessMap();
		AddMapBorder();

		// Generate mesh
		MeshGenerator meshGen = GetComponent<MeshGenerator>();
		meshGen.GenerateMesh(map, 1);

		if (currentLab != null) {
			Destroy(currentLab);
		}

		if (laboratoryPrefab != null && islandCenters != null && islandCenters.Length > 0) {
			// pick an island
			Vector2 center = islandCenters[Random.Range(0, islandCenters.Length)];
			Vector3 worldPos = new Vector3(center.x - width / 2.0f + 0.5f, 0.5f, center.y - height / 2f + 2.5f);
			currentLab = Instantiate(laboratoryPrefab, worldPos, Quaternion.Euler(90f, 0.0f, 0.0f));
		}
		SpawnSharkie();
		SpawnZombies();
	}

	/*
	 * STAGE 1: Populate the map
	 */
	void PopulateMap()
    {
		RandomFillMap();
    }

	void RandomFillMap()
	{
		if (useRandomSeed)
		{
			seed = Time.time.ToString();
		}

		System.Random pseudoRandom = new System.Random(seed.GetHashCode());

		// randomize centers of large islands
		islandCenters = new Vector2[3];
		for (int i = 0; i < islandCenters.Length; i++) {
			float margin = Mathf.Min(width, height) * 0.2f;
			float x = (float)pseudoRandom.NextDouble() * (width - margin * 2) + margin;
			float y = (float)pseudoRandom.NextDouble() * (height - margin * 2) + margin;
			islandCenters[i] = new Vector2(x, y);
		}


		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				if (alwaysKeepEdgesAsWalls && (x == 0 || x == width - 1 || y == 0 || y == height - 1))
				{
					map[x, y] = 1;
				}
				else
				{
					Vector2 pos = new Vector2(x, y);

					// find closest island center
					float closestDistance = float.MaxValue;
					foreach (Vector2 center in islandCenters)
					{
						float dist = Vector2.Distance(pos, center);
						if (dist < closestDistance)
							closestDistance = dist;
					}

					// apply falloff
					float maxIslandRadius = Mathf.Min(width, height) * 0.35f; // controls island size
					if (closestDistance > maxIslandRadius)
					{
						map[x, y] = 0; // limit island size by reverting to water
						continue;
					}
					float t = closestDistance / maxIslandRadius;
					float falloff = Mathf.Clamp01(Mathf.Pow(t, 2f));

					int adjustedFill = Mathf.RoundToInt(randomFillPercent * (1 - falloff));
					map[x, y] = (pseudoRandom.Next(0, 100) < adjustedFill) ? 1 : 0;
				}
			}
		}
	}


	/*
	 * STAGE 2: Smooth map with CA
	 */
	void SmoothMap() {
		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
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
		for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
		{
			for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
			{
				if (IsInMapRange(neighbourX, neighbourY))
				{
					if (neighbourX != gridX || neighbourY != gridY)
					{
						wallCount += map[neighbourX, neighbourY];
					}
				}
				else
				{
					wallCount++;
				}
			}
		}

		return wallCount;
	}

	void FillWater() {
		int width = map.GetLength(0);
		int height = map.GetLength(1);
		Queue<Vector2Int> queue = new Queue<Vector2Int>();
		bool[,] visited = new bool[width, height];

		// Start flood from the outer edge
		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				if ((x == 0 || x == width - 1 || y == 0 || y == height - 1) && map[x, y] == 0)
				{
					queue.Enqueue(new Vector2Int(x, y));
					visited[x, y] = true;
				}
			}
		}

		while (queue.Count > 0)
		{
			Vector2Int tile = queue.Dequeue();
			map[tile.x, tile.y] = 2; // Convert to water

			foreach (Vector2Int dir in new[] {
				new Vector2Int(0,1), new Vector2Int(1,0),
				new Vector2Int(0,-1), new Vector2Int(-1,0)
			})
			{
				int nx = tile.x + dir.x;
				int ny = tile.y + dir.y;

				if (IsInMapRange(nx, ny) && !visited[nx, ny] && map[nx, ny] == 0)
				{
					queue.Enqueue(new Vector2Int(nx, ny));
					visited[nx, ny] = true;
				}
			}
		}
	}

	void RemoveSmallWallRegions(int threshold) {
		bool[,] visited = new bool[map.GetLength(0), map.GetLength(1)];

		for (int x = 0; x < map.GetLength(0); x++)
		{
			for (int y = 0; y < map.GetLength(1); y++)
			{
				if (!visited[x, y] && map[x, y] == 1)
				{
					List<Vector2Int> region = GetRegionTiles(x, y, 1);
					if (region.Count < threshold)
					{
						foreach (Vector2Int tile in region)
						{
							map[tile.x, tile.y] = 0; // remove tiny wall blob
						}
					}

					foreach (Vector2Int tile in region)
					{
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

		while (queue.Count > 0)
		{
			Vector2Int tile = queue.Dequeue();
			tiles.Add(tile);

			for (int x = -1; x <= 1; x++)
			{
				for (int y = -1; y <= 1; y++)
				{
					if ((x == 0 || y == 0) && x != y) // 4-way neighbors
					{
						int nx = tile.x + x;
						int ny = tile.y + y;

						if (IsInMapRange(nx, ny) && !visited[nx, ny] && map[nx, ny] == targetType)
						{
							visited[nx, ny] = true;
							queue.Enqueue(new Vector2Int(nx, ny));
						}
					}
				}
			}
		}

		return tiles;
	}

	bool IsInMapRange(int x, int y) {
		return x >= 0 && x < width && y >= 0 && y < height;
	}


	/*
	 * Stage 3: produce the finished map
	 */
	void ProcessMap() {
		RemoveSmallWallRegions(20);
		FillWater();
		AddMiniIslands(10, 5);

		// After filling water, cache water tile positions
		waterTilePositions.Clear();

		for (int x = 0; x < map.GetLength(0); x++)
		{
			for (int y = 0; y < map.GetLength(1); y++)
			{
				if (map[x, y] == 2) // water tile
				{
					waterTilePositions.Add(new Vector2Int(x, y));
				}
			}
		}
	}

	void AddMapBorder() {
		int borderSize = 1;
		int[,] borderedMap = new int[width + borderSize * 2, height + borderSize * 2];

		for (int x = 0; x < borderedMap.GetLength(0); x++)
		{
			for (int y = 0; y < borderedMap.GetLength(1); y++)
			{
				if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
				{
					borderedMap[x, y] = map[x - borderSize, y - borderSize];
				}
				else
				{
					borderedMap[x, y] = 3;
				}
			}
		}
		map = borderedMap;
	}

	void AddMiniIslands(int count = 10, int maxSize = 3) {
		System.Random rand = new System.Random(seed.GetHashCode());

		for (int i = 0; i < count; i++)
		{
			int centerX = rand.Next(3, width - 3);
			int centerY = rand.Next(3, height - 3);

			// Skip if center is already land
			if (map[centerX, centerY] != 2) continue;

			int radius = rand.Next(1, maxSize + 1);

			for (int dx = -radius; dx <= radius; dx++)
			{
				for (int dy = -radius; dy <= radius; dy++)
				{
					int x = centerX + dx;
					int y = centerY + dy;

					if (IsInMapRange(x, y) && Mathf.Sqrt(dx * dx + dy * dy) <= radius)
					{
						if (map[x, y] == 2) // only convert water to land
						{
							map[x, y] = 1;
						}
					}
				}
			}
		}
	}


	void SpawnSharkie() {
		if (sharkiePrefab == null)
		{
			Debug.LogWarning("Sharkie prefab not assigned!");
			return;
		}

		// Remove old shark
		if (currentSharkie != null)
		{
			Destroy(currentSharkie);
		}

		// Try to find a water tile
		for (int i = 0; i < 100; i++)
		{
			int x = Random.Range(0, map.GetLength(0));
			int y = Random.Range(0, map.GetLength(1));

			if (map[x, y] == 2)
			{
				float worldX = x - map.GetLength(0) / 2f + 0.5f;
				float worldY = y - map.GetLength(1) / 2f + 0.5f;
				Vector3 spawnPos = new Vector3(worldX, worldY, 0f);

				currentSharkie = Instantiate(sharkiePrefab, spawnPos, sharkiePrefab.transform.rotation);

				ZombShark sharkScript = currentSharkie.GetComponent<ZombShark>();
				if (sharkScript != null)
				{
					sharkScript.mapGenerator = this;
				}

				return;
			}
		}

		Debug.LogWarning("Could not find valid water tile to spawn Sharkie!");
	}

	void SpawnZombies() {
		// clear old zombies
		foreach (GameObject z in zombies) Destroy(z);
		zombies.Clear();

		foreach (Vector2 center in islandCenters)
		{
			Vector3 pos = new Vector3(center.x - width / 2f + 0.5f, 0.5f, center.y - height / 2f + 0.5f);

			/* if it's the island with the lab, offset the zombie slightly forward
			if (currentLab != null && Vector2.Distance(center, new Vector2(currentLab.transform.position.x + width / 2f - 0.5f, currentLab.transform.position.z + height / 2f - 0.5f)) < 3f)
			{
				pos.z += 1.5f; // offset forward
			}*/

			GameObject z = Instantiate(zombiePrefab, pos, Quaternion.Euler(90f, 0, 0));
			zombies.Add(z);
		}
	}

	public bool IsWater(Vector2 worldPos) {
		int mapWidth = map.GetLength(0);
		int mapHeight = map.GetLength(1);

		int x = Mathf.FloorToInt(worldPos.x + mapWidth / 2f);
		int y = Mathf.FloorToInt(worldPos.y + mapHeight / 2f);

		if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
		{
			return map[x, y] == 2;
		}

		return false;
	}

	public int GetTileType(Vector2 worldPos) {
		int x = Mathf.FloorToInt(worldPos.x + map.GetLength(0) / 2f);
		int y = Mathf.FloorToInt(worldPos.y + map.GetLength(1) / 2f);

		if (x >= 0 && x < map.GetLength(0) && y >= 0 && y < map.GetLength(1))
		{
			return map[x, y];
		}

		return -1; // Outside bounds
	}

}