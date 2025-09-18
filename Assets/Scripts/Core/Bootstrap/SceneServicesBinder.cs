using UnityEngine;
using Azurin.Core;
using Azurin.Player;
using Voxel;
using UI;

/// <summary>
/// Binds scene instances to the ServiceLocator to avoid runtime Find* calls.
/// Assign references via inspector.
/// </summary>
public class SceneServicesBinder : MonoBehaviour
{
    [Header("Core Services")]
    public GameStateController gameStateController;
    public SaveLoadService saveLoadService;
    public GameplayStatsService gameplayStatsService;
    public DebugHUD debugHUD;

    [Header("Gameplay Components")]
    public InputHandler inputHandler;
    public VoxelTerrain voxelTerrain;

    private void Awake()
    {
        if (gameStateController != null) ServiceLocator.Register(gameStateController);
        if (saveLoadService != null) ServiceLocator.Register(saveLoadService);
        if (gameplayStatsService != null) ServiceLocator.Register(gameplayStatsService);
        if (debugHUD != null) ServiceLocator.Register(debugHUD);
        if (inputHandler != null) ServiceLocator.Register(inputHandler);
        if (voxelTerrain != null) ServiceLocator.Register(voxelTerrain);
    }
}


