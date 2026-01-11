namespace VoxelEngine.Scripts;
using Godot;

public partial class NoiseManager : Node
{
	public static NoiseManager Instance { get; private set; }
	
	// Noise instances - make them private and expose through properties
	private FastNoiseLite _noise;
	private FastNoiseLite _biomeNoise;
	private FastNoiseLite _mountainNoise;
	private FastNoiseLite _hillNoise;
	private FastNoiseLite _detailNoise;
	private FastNoiseLite _caveNoise;
	private FastNoiseLite _caveBranchNoise;
	private FastNoiseLite _undergroundRoomNoise;
	private FastNoiseLite _surfaceOpeningNoise;
	
	// Properties for easy access
	public FastNoiseLite Noise => _noise;
	public FastNoiseLite BiomeNoise => _biomeNoise;
	public FastNoiseLite MountainNoise => _mountainNoise;
	public FastNoiseLite HillNoise => _hillNoise;
	public FastNoiseLite DetailNoise => _detailNoise;
	public FastNoiseLite CaveNoise => _caveNoise;
	public FastNoiseLite CaveBranchNoise => _caveBranchNoise;
	public FastNoiseLite UndergroundRoomNoise => _undergroundRoomNoise;
	public FastNoiseLite SurfaceOpeningNoise => _surfaceOpeningNoise;
	public BiomeEnum BiomeType;
	
	// Noise configuration settings
	public float Frequency { get; set; } = 0.01f;
	public int Octaves { get; set; } = 3;
	public float Persistence { get; set; } = 0.5f;
	public float Lacunarity { get; set; } = 2.0f;

	public enum BiomeEnum
	{
		GrassLands,
		Mountains,
		Forest,
		Ocean,
	}
	
	public override void _Ready()
	{
		Instance = this;
		
		// Initialize all noise instances
		InitializeNoise();
		
		// Set default parameters for all noises
		ConfigureAllNoises();
	}
	
	private void InitializeNoise()
	{
		_noise = new FastNoiseLite();
		_biomeNoise = new FastNoiseLite();
		_mountainNoise = new FastNoiseLite();
		_hillNoise = new FastNoiseLite();
		_detailNoise = new FastNoiseLite();
		_caveNoise = new FastNoiseLite();
		_caveBranchNoise = new FastNoiseLite();
		_undergroundRoomNoise = new FastNoiseLite();
		_surfaceOpeningNoise = new FastNoiseLite();
	}
	
	private void ConfigureAllNoises()
	{
		// Set common parameters for all noises
		SetNoiseParameters(_noise, 0.01f);
		SetNoiseParameters(_biomeNoise, 0.005f);
		SetNoiseParameters(_mountainNoise, 0.003f);
		SetNoiseParameters(_hillNoise, 0.008f);
		SetNoiseParameters(_detailNoise, 0.02f);
		SetNoiseParameters(_caveNoise, 0.015f);
		SetNoiseParameters(_caveBranchNoise, 0.03f);
		SetNoiseParameters(_undergroundRoomNoise, 0.002f);
		SetNoiseParameters(_surfaceOpeningNoise, 0.006f);
		
		// Configure specific noise types
		ConfigureBiomeNoise();
		ConfigureCaveNoises();
	}
	
	private void SetNoiseParameters(FastNoiseLite noise, float frequency)
	{
		noise.SetNoiseType(FastNoiseLite.NoiseTypeEnum.Simplex);
		noise.SetFrequency(frequency);
		noise.SetFractalOctaves(Octaves);
		noise.SetFractalLacunarity(Lacunarity);
	}
	
	private void ConfigureBiomeNoise()
	{
		_biomeNoise.SetNoiseType(FastNoiseLite.NoiseTypeEnum.Simplex);
		_biomeNoise.SetFrequency(0.005f);
		_biomeNoise.SetFractalOctaves(4);
		_biomeNoise.SetFractalLacunarity(1.8f);
	}
	
	private void ConfigureCaveNoises()
	{
		_caveNoise.SetNoiseType(FastNoiseLite.NoiseTypeEnum.Simplex);
		_caveNoise.SetFrequency(0.015f);
		_caveNoise.SetFractalOctaves(3);
		
		_caveBranchNoise.SetNoiseType(FastNoiseLite.NoiseTypeEnum.Simplex);
		_caveBranchNoise.SetFrequency(0.03f);
		_caveBranchNoise.SetFractalOctaves(2);
	}
	
	// Method to update noise settings at runtime
	public void UpdateNoiseSettings()
	{
		ConfigureAllNoises();
	}
	
	// Method to set seed for all noises
	public void SetSeed(int seed)
	{
		_noise.SetSeed(seed);
		_biomeNoise.SetSeed(seed + 1);
		_mountainNoise.SetSeed(seed + 2);
		_hillNoise.SetSeed(seed + 3);
		_detailNoise.SetSeed(seed + 4);
		_caveNoise.SetSeed(seed + 5);
		_caveBranchNoise.SetSeed(seed + 6);
		_undergroundRoomNoise.SetSeed(seed + 7);
		_surfaceOpeningNoise.SetSeed(seed + 8);
	}
}
