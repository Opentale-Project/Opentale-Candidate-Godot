using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoxelEngine.Scripts;

public partial class World : Node3D
{
	[Export] public int RenderDistanceHorizontal = 12;
	[Export] public int RenderDistanceVertical = 6;
	public PackedScene chunk_scene;

	private System.Collections.Generic.Dictionary<Vector3I, Chunk> chunks = new();
	private Queue<Vector3I> loadQueue = new();
	public Player player;
	public NoiseManager noiseManager;
	
	private bool queueActive;
	private Vector3I oldKey;
	private Vector3I newKey = new Vector3I(-256,-256,-256);
	private int yOff = 0;

	public override void _Ready()
	{
		if (NoiseManager.Instance == null)
		{
			noiseManager = NoiseManager.Instance;
		}
		chunk_scene = GD.Load<PackedScene>("res://Chunk.tscn");

		player = GetNode<Player>("../Player");

		if (player == null)
			GD.Print("No player found!");

		Callable.From(AsyncChunkLoader).CallDeferred();
	}

	public override void _Process(double delta)
	{
		UpdateChunkQueue();
	}

	private void UnloadFarChunks(int px, int py, int pz)
	{
		var toRemove = new System.Collections.Generic.List<Vector3I>();

		foreach (var kv in chunks)
		{
			var key = kv.Key;
			int dx = key.X - px;
			int dy = key.Y - py;
			int dz = key.Z - pz;

			if (Math.Abs(dx) > RenderDistanceHorizontal || Math.Abs(dy) > RenderDistanceVertical || Math.Abs(dz) > RenderDistanceHorizontal) toRemove.Add(key);
		}

		foreach (var key in toRemove)
		{
			chunks[key].QueueFree();
			chunks.Remove(key);
		}
	}
	
	
	private void UpdateChunkQueue()
	{
		if (player == null) return;
		
		int playerChunkX = Mathf.FloorToInt(player.GlobalPosition.X / Chunk.ChunkSizeHorizontal);
		int playerChunkY = Mathf.FloorToInt(player.GlobalPosition.Y / Chunk.ChunkSizeVertical);
		int playerChunkZ = Mathf.FloorToInt(player.GlobalPosition.Z / Chunk.ChunkSizeHorizontal);
		
		// === Check if player chunk changed
		oldKey = newKey;
		newKey = new(playerChunkX, playerChunkY, playerChunkZ);
		if (oldKey == newKey) return;
		else loadQueue.Clear();
				
		//TODO: if displacement is by 1 chunk, only call new line of chunks, not the whole function
		
		queueChunks(playerChunkX, playerChunkY, playerChunkZ);
		UnloadFarChunks(playerChunkX, playerChunkY, playerChunkZ);
	}
	
	private void queueChunks(int cX, int cY, int cZ)
	{
		for (int yy = 0; yy < Chunk.NumChunksVertical; yy++)
		{
			queueC(cX, yy, cZ);
		}
		for (int h = 1; h <= RenderDistanceHorizontal; h++)
		{
			for (int yy = 0; yy < Chunk.NumChunksVertical; yy++)
			{
				for (int x = -h + 1; x <=  h; x++) { queueC(cX + x, yy, cZ + h); }
				for (int z =  h - 1; z >= -h; z--) { queueC(cX + h, yy, cZ + z); }
				for (int x =  h - 1; x >= -h; x--) { queueC(cX + x, yy, cZ - h); }
				for (int z = -h + 1; z <=  h; z++) { queueC(cX - h, yy, cZ + z); }
			}
		}
	}
	
	private void queueC(int x, int y, int z)
	{
		Vector3I key = new(x, y, z);
		if(!chunks.ContainsKey(key) && !loadQueue.Contains(key)) loadQueue.Enqueue(key);
		//else GD.Print("#WARN#: Chunk in queue checked for enqueue action, not added again.");
	}

	private async void AsyncChunkLoader()
	{
		while (true)
		{
			if (loadQueue.Count > 0)
			{
				Vector3I key = loadQueue.Dequeue();

				await LoadChunkAsync(key.X, key.Y, key.Z);

				//await Task.Delay(1);
			}

			await Task.Delay(1);
		}
	}

	private async Task LoadChunkAsync(int cx, int cy, int cz)
	{
		Vector3I key = new(cx, cy, cz);

		if (chunks.ContainsKey(key)) return;

		Chunk chunk = chunk_scene.Instantiate<Chunk>();

		chunk.worldX = cx;
		chunk.worldY = cy;
		chunk.worldZ = cz;

		chunk.Position = new Vector3(
			cx * Chunk.ChunkSizeHorizontal,
			cy * Chunk.ChunkSizeVertical,
			cz * Chunk.ChunkSizeHorizontal
		);
		
		AddChild(chunk);
		chunks[key] = chunk;

		await Task.Delay(1);
	}

	public bool WorldToBlockCoords(
		Vector3 worldPos,
		out Vector3I chunkCoord,
		out Vector3I blockPos
	) {
		// Convert world coords → integer block coords
		int bx = Mathf.RoundToInt(worldPos.X);
		int bY = Mathf.RoundToInt(worldPos.Y);
		int bz = Mathf.RoundToInt(worldPos.Z);

		// Which chunk?
		int cx = Mathf.FloorToInt((float)bx / Chunk.ChunkSizeHorizontal);
		int cy = Mathf.FloorToInt((float)bY / Chunk.ChunkSizeVertical  );
		int cz = Mathf.FloorToInt((float)bz / Chunk.ChunkSizeHorizontal);

		chunkCoord = new Vector3I(cx, cy, cz);

		// Chunk missing = fail
		if (!chunks.TryGetValue(chunkCoord, out Chunk chunk))
		{
			blockPos = default;
			return false;
		}

		// Local block coords inside chunk
		int localX = bx - cx * Chunk.ChunkSizeHorizontal;
		int localY = bY - cy * Chunk.ChunkSizeVertical  ;
		int localZ = bz - cz * Chunk.ChunkSizeHorizontal;

		// Bounds check (ALL out-of-range returns must set blockPos)
		if (localX < 0 || localX >= Chunk.ChunkSizeHorizontal ||
			localY < 0 || localY >= Chunk.ChunkSizeVertical   ||
			localZ < 0 || localZ >= Chunk.ChunkSizeHorizontal)
		{
			blockPos = default;
			return false;
		}

		// VALID → assign and return true
		blockPos = new Vector3I(localX, localY, localZ);
		return true;
	}

	public void BreakBlock(Vector3 worldPos)
	{
		if(!WorldToBlockCoords(worldPos, out var chunkKey, out var blockPos) ||
		(chunks[chunkKey].GetBlock(blockPos.X, blockPos.Y, blockPos.Z) == (byte)VoxelType.DepthRock)) return;
		
		chunks[chunkKey].SetBlock(blockPos.X, blockPos.Y, blockPos.Z, (byte)VoxelType.Air);
	}

	public void PlaceBlock(Vector3 worldPos, byte block)
	{
		if(!WorldToBlockCoords(worldPos, out var chunkKey, out var blockPos)) return;
		chunks[chunkKey].SetBlock(blockPos.X, blockPos.Y, blockPos.Z, block);
	}
}
