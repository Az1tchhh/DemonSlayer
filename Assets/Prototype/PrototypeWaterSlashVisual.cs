using UnityEngine;

namespace DemonSlayer.Prototype
{
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class PrototypeWaterSlashVisual : MonoBehaviour
    {
        [SerializeField] private PrototypePlayer player;
        private static readonly int WaterState = Animator.StringToHash("Base Layer.WaterEffect");
        private Animator animator;
        private SpriteRenderer visual;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            visual = GetComponent<SpriteRenderer>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 0;
            Hide();
        }
        private void OnEnable() => player.ResetPerformed += Hide;
        private void OnDisable()
        {
            player.ResetPerformed -= Hide;
            if (visual != null) Hide();
        }
        private void LateUpdate()
        {
            if (!player.isActiveAndEnabled || player.Health.IsDead || !player.IsWaterSlashing)
            {
                Hide();
                return;
            }
            // Shared dimensions and attack time keep the art aligned with the hit box.
            transform.localPosition = new Vector3(player.Facing * (PrototypePlayer.AttackStartOffset + PrototypePlayer.WaterSlashReach / 2), 0, 0);
            animator.Play(WaterState, 0, player.WaterSlashProgress);
            animator.Update(0);
            visual.flipX = player.Facing < 0;
            visual.enabled = visual.sprite != null;
        }
        private void Hide()
        {
            visual.enabled = false;
            visual.sprite = null;
        }
    }
}
