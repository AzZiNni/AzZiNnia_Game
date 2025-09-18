using System;
using Unity.Mathematics;
using UnityEngine;

namespace Azurin.Core
{
    /// <summary>
    /// Central lightweight event hub. Static for simplicity; can be swapped to ScriptableObject channels later.
    /// </summary>
    public static class GameEvents
    {
        // Game state
        public static event Action<GameStateChanged> OnGameStateChanged;

        // Voxel/terrain
        public static event Action<float3, float, float> OnTerrainModified; // pos, radius, strength

        // Mode switches
        public static event Action<bool> OnBuildModeChanged;

        public static void Raise(GameStateChanged payload) => OnGameStateChanged?.Invoke(payload);

        public static void RaiseTerrainModified(float3 pos, float radius, float strength)
            => OnTerrainModified?.Invoke(pos, radius, strength);

        public static void RaiseBuildModeChanged(bool isBuild) => OnBuildModeChanged?.Invoke(isBuild);
    }

    public struct GameStateChanged
    {
        public enum State { Running, Paused }
        public State NewState;
    }
}


