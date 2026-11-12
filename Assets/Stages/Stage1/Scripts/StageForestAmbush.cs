using DemonSlayer.Core;
using UnityEngine;

namespace DemonSlayer.Stages
{
    public sealed class StageForestAmbush : MonoBehaviour
    {
        [SerializeField] private CorePlayer player;
        [SerializeField] private CoreArena arena;
        [SerializeField] private Camera view;
        [SerializeField] private CoreDemon[] demons;
        [SerializeField] private float triggerX = 88;
        [SerializeField] private float spawnHeight = .1f;
        [SerializeField, Min(0)] private float screenMargin = 1.5f;
        [SerializeField, Min(.1f)] private float spacing = 2;
        [SerializeField] private Vector2 spawnBounds = new Vector2(-10, 142);
        private Rigidbody2D playerBody;
        private bool triggered;

        private void Awake()
        {
            if (!player || !arena || !view || demons == null || demons.Length != 3 ||
                !demons[0] || !demons[1] || !demons[2])
            {
                Debug.LogError("Forest ambush requires a player, arena, camera and exactly three demons.", this);
                enabled = false;
                return;
            }
            playerBody = player.GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            if (player) player.ResetPerformed += ResetEncounter;
        }

        private void OnDisable()
        {
            if (player) player.ResetPerformed -= ResetEncounter;
        }

        private void Update()
        {
            if (triggered || Time.timeScale <= 0 || player.Health.IsDead || playerBody.position.x < triggerX) return;
            triggered = true;
            int leftCount = Random.Range(1, 3);
            float halfWidth = view.orthographicSize * view.aspect;
            for (int i = 0; i < demons.Length; i++)
            {
                bool left = i < leftCount;
                int sideIndex = left ? i : i - leftCount;
                // Start beyond the visible edge and behind/ahead of Tanjiro even if he is moving quickly.
                float edge = left ? Mathf.Min(view.transform.position.x - halfWidth, playerBody.position.x - halfWidth)
                                  : Mathf.Max(view.transform.position.x + halfWidth, playerBody.position.x + halfWidth);
                float x = edge + (left ? -1 : 1) * (screenMargin + sideIndex * spacing);
                var position = new Vector2(Mathf.Clamp(x, spawnBounds.x, spawnBounds.y), spawnHeight);
                demons[i].transform.position = position;
                demons[i].gameObject.SetActive(true);
                demons[i].ResetAt(position);
            }
            Physics2D.SyncTransforms();
        }

        private void ResetEncounter()
        {
            if (demons == null) return;
            foreach (var demon in demons)
                if (demon) demon.gameObject.SetActive(false);
            // Retrying from the boss checkpoint skips the already traversed route.
            triggered = arena && arena.CheckpointActive;
        }
    }
}
