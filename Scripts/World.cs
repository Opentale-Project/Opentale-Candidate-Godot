using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoxelEngine.Scripts;

public partial class World : Node3D
{
	[Export] public int RenderDistance = 12;
	public PackedScene chunk_scene;

	private System.Collections.Generic.Dictionary<Vector2I, Chunk> chunks = new();
	private Queue<Vector2I> loadQueue = new();
	public Player player;
	public NoiseManager noiseManager;
	
	private bool queueActive;
	private Vector2I oldKey;
	private Vector2I newKey = new Vector2I(-256,-256);

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

	private void UnloadFarChunks(int px, int pz)
	{
		var toRemove = new System.Collections.Generic.List<Vector2I>();

		foreach (var kv in chunks)
		{
			var key = kv.Key;
			int dx = key.X - px;
			int dz = key.Y - pz;

			if (Math.Abs(dx) > RenderDistance || Math.Abs(dz) > RenderDistance) toRemove.Add(key);
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
		int playerChunkZ = Mathf.FloorToInt(player.GlobalPosition.Z / Chunk.ChunkSizeHorizontal);
		
		// === Check if player chunk changed
		oldKey = newKey;
		newKey = new(playerChunkX, playerChunkZ);
		if (oldKey == newKey) return;
		
		//TODO: if displacement is by 1 chunk, only call new line of chunks, not the whole function
		
		queueChunks(playerChunkX, playerChunkZ);
		UnloadFarChunks(playerChunkX, playerChunkZ);
	}
	
	private void queueChunks(int cX, int cZ)
	{
		queueChunk(cX, cZ);
		for (int iter = 1; iter <= RenderDistance; iter++)
		{
			for (int x = -iter + 1; x <=  iter; x++) { queueChunk(cX + x   , cZ + iter); }
			for (int z =  iter - 1; z >= -iter; z--) { queueChunk(cX + iter, cZ + z   ); }
			for (int x =  iter - 1; x >= -iter; x--) { queueChunk(cX + x   , cZ - iter); }
			for (int z = -iter + 1; z <=  iter; z++) { queueChunk(cX - iter, cZ + z   ); }
		}
		//queueActive = false;
	}
	
	private void queueChunk(int x, int z)
	{
		Vector2I key = new(x, z);
		if(!chunks.ContainsKey(key) && !loadQueue.Contains(key)) loadQueue.Enqueue(key);
		//else GD.Print("#WARN#: Chunk in queue checked for enqueue action, not added again.");
	}

	private async void AsyncChunkLoader()
	{
		while (true)
		{
			if (loadQueue.Count > 0)
			{
				Vector2I key = loadQueue.Dequeue();

				await LoadChunkAsync(key.X, key.Y);

				//await Task.Delay(1);
			}

			await Task.Delay(1);
		}
	}

	private async Task LoadChunkAsync(int cx, int cz)
	{
		Vector2I key = new(cx, cz);

		if (chunks.ContainsKey(key)) return;

		Chunk chunk = chunk_scene.Instantiate<Chunk>();

		chunk.worldX = cx;
		chunk.worldZ = cz;

		chunk.Position = new Vector3(
			cx * Chunk.ChunkSizeHorizontal,
			0,
			cz * Chunk.ChunkSizeHorizontal
		);
		
		AddChild(chunk);
		chunks[key] = chunk;

		await Task.Delay(1);
	}

	public bool WorldToBlockCoords(
		Vector3 worldPos,
		out Vector2I chunkCoord,
		out Vector3I blockPos
	) {
		// Convert world coords → integer block coords
		int bx = Mathf.RoundToInt(worldPos.X);
		int by = Mathf.RoundToInt(worldPos.Y);
		int bz = Mathf.RoundToInt(worldPos.Z);

		// Which chunk?
		int cx = Mathf.FloorToInt((float)bx / Chunk.ChunkSizeHorizontal);
		int cz = Mathf.FloorToInt((float)bz / Chunk.ChunkSizeHorizontal);

		chunkCoord = new Vector2I(cx, cz);

		// Chunk missing = fail
		if (!chunks.TryGetValue(chunkCoord, out Chunk chunk))
		{
			blockPos = default;
			return false;
		}

		// Local block coords inside chunk
		int localX = bx - cx * Chunk.ChunkSizeHorizontal;
		int localZ = bz - cz * Chunk.ChunkSizeHorizontal;

		// Bounds check (ALL out-of-range returns must set blockPos)
		if (localX < 0 || localX >= Chunk.ChunkSizeHorizontal ||
			by < 0      || by >= Chunk.ChunkSizeVertical   ||
			localZ < 0 || localZ >= Chunk.ChunkSizeHorizontal)
		{
			blockPos = default;
			return false;
		}

		// VALID → assign and return true
		blockPos = new Vector3I(localX, by, localZ);
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
