using Unity.Profiling;

namespace Azurin.Core
{
    /// <summary>
    /// Centralized profiler markers to avoid GC and duplicates.
    /// </summary>
    public static class ProfilerMarkers
    {
        public static readonly ProfilerMarker Voxel_UpdateChunks = new ProfilerMarker("VoxelTerrain.UpdateChunks");
        public static readonly ProfilerMarker Chunk_GenerateMesh = new ProfilerMarker("TerrainChunkV2.GenerateMesh");
        public static readonly ProfilerMarker SaveSystem_SaveWorld = new ProfilerMarker("WorldSaveSystem.SaveWorld");
    }
}


