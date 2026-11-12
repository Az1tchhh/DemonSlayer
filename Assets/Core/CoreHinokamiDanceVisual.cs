using UnityEngine;

namespace DemonSlayer.Core
{
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "DemonSlayer.Prototype", sourceAssembly: "Assembly-CSharp", sourceClassName: "PrototypeHinokamiDanceVisual")]
    public sealed class CoreHinokamiDanceVisual : MonoBehaviour
    {
        [SerializeField] private CorePlayer player;
        private static readonly int FireState = Animator.StringToHash("Base Layer.HinokamiEffect");
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
            if (!player.isActiveAndEnabled || player.Health.IsDead || !player.IsHinokamiDancing)
            {
                Hide();
                return;
            }
            // Shared dimensions and attack time keep the art aligned with the hit box.
            transform.localPosition = new Vector3(player.Facing * (CorePlayer.AttackStartOffset + CorePlayer.HinokamiReach / 2), CorePlayer.HinokamiHeightOffset, 0);
            animator.Play(FireState, 0, player.HinokamiDanceProgress);
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
