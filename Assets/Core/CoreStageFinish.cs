using UnityEngine;

namespace DemonSlayer.Core
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CoreStageFinish : MonoBehaviour
    {
        [SerializeField] private CorePlayer player;
        [SerializeField] private CoreSceneTransition transition;

        private void Awake()
        {
            if (!player || !transition || !GetComponent<BoxCollider2D>().isTrigger)
            {
                Debug.LogError("Stage finish requires a player, scene transition and a trigger collider.", this);
                enabled = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActiveAndEnabled || player.Health.IsDead) return;
            if (other.GetComponentInParent<CorePlayer>() == player)
                transition.BeginTransition();
        }
    }
}
