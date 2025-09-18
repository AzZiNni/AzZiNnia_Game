using System;
using UnityEngine;

namespace Azurin.Core
{
    /// <summary>
    /// Controls high-level game state and time scale.
    /// </summary>
    public class GameStateController : MonoBehaviour
    {
        public enum State { Running, Paused }
        public State CurrentState { get; private set; } = State.Running;

        public event Action<State> OnStateChanged;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        public void SetState(State newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            Time.timeScale = (newState == State.Paused) ? 0f : 1f;
            OnStateChanged?.Invoke(newState);
            GameEvents.Raise(new GameStateChanged { NewState = newState == State.Paused ? GameStateChanged.State.Paused : GameStateChanged.State.Running });
        }

        public void TogglePause()
        {
            SetState(CurrentState == State.Paused ? State.Running : State.Paused);
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet<GameStateController>(out var self) && ReferenceEquals(self, this))
            {
                ServiceLocator.Unregister<GameStateController>();
            }
        }
    }
}


