using UnityEngine;

namespace DemonSlayer.Prototype
{
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class PrototypeClearBlueSkyVisual : MonoBehaviour
    {
        [SerializeField] private PrototypePlayer player;
        private static readonly int WheelState = Animator.StringToHash("Base Layer.ClearBlueSkyEffect");
        private Animator animator;
        private SpriteRenderer visual;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            visual = GetComponent<SpriteRenderer>();
            animator.speed = 0;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
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
            if (!player.isActiveAndEnabled || player.Health.IsDead || !player.IsClearBlueSky)
            {
                Hide();
                return;
            }
            animator.Play(WheelState, 0, player.ClearBlueSkyProgress);
            animator.Update(0);
            visual.flipX = player.Facing < 0;
            visual.enabled = visual.sprite != null;
            float spin = Mathf.InverseLerp(PrototypePlayer.SkyLaunch, PrototypePlayer.SkySpinEnd, player.ClearBlueSkyProgress);
            transform.localRotation = Quaternion.Euler(0, 0, -120 * player.Facing * spin);
        }
        private void Hide()
        {
            visual.enabled = false;
            visual.sprite = null;
            transform.localRotation = Quaternion.identity;
        }
    }
}
