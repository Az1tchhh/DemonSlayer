using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DemonSlayer.Core
{
    public sealed class CoreSceneTransition : MonoBehaviour
    {
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField, Min(.01f)] private float fadeDuration = 1.25f;
        [Tooltip("Optional scene name or full asset path, enabled in the build scene list. Leave empty to end on black. Can point to a story scene or the next stage.")]
        [SerializeField] private string nextScene;
        [Tooltip("Input/gameplay controllers to suspend during the transition. Physics and scaled timers are paused automatically.")]
        [SerializeField] private Behaviour[] gameplayToSuspend = new Behaviour[0];
        private bool[] previouslyEnabled;
        private float previousTimeScale;
        public bool IsTransitioning { get; private set; }
        public bool IsComplete { get; private set; }

        private void Awake()
        {
            if (!fadeOverlay)
            {
                Debug.LogError("Scene transition requires a full-screen fade overlay.", this);
                enabled = false;
                return;
            }
            fadeOverlay.alpha = 0;
            fadeOverlay.blocksRaycasts = false;
        }

        // Also usable by a future story scene's Continue button or dialogue-finished event.
        public void BeginTransition()
        {
            if (!isActiveAndEnabled || !fadeOverlay || IsTransitioning || Time.timeScale <= 0) return;
            if (!string.IsNullOrWhiteSpace(nextScene) && !Application.CanStreamedLevelBeLoaded(nextScene))
            {
                Debug.LogError($"Transition scene '{nextScene}' is not available. Add it to the enabled build scenes.", this);
                return;
            }
            IsTransitioning = true;
            previousTimeScale = Time.timeScale;
            previouslyEnabled = new bool[gameplayToSuspend.Length];
            for (int i = 0; i < gameplayToSuspend.Length; i++)
            {
                var behaviour = gameplayToSuspend[i];
                if (!behaviour || behaviour == this) continue;
                previouslyEnabled[i] = behaviour.enabled;
                behaviour.enabled = false;
            }
            Time.timeScale = 0;
            fadeOverlay.blocksRaycasts = true;
            StartCoroutine(FadeOut());
        }

        private IEnumerator FadeOut()
        {
            float elapsed = 0;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeOverlay.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            fadeOverlay.alpha = 1;
            IsComplete = true;
            if (!string.IsNullOrWhiteSpace(nextScene))
                yield return SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Single);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (!IsTransitioning) return;
            // Release the pause when this scene unloads or the transition is cancelled.
            Time.timeScale = previousTimeScale;
            for (int i = 0; i < gameplayToSuspend.Length; i++)
                if (gameplayToSuspend[i] && gameplayToSuspend[i] != this)
                    gameplayToSuspend[i].enabled = previouslyEnabled[i];
            if (fadeOverlay)
            {
                fadeOverlay.alpha = 0;
                fadeOverlay.blocksRaycasts = false;
            }
            IsTransitioning = IsComplete = false;
        }
    }
}
