using UnityEngine;

namespace DemonSlayer.Core
{
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "DemonSlayer.Prototype", sourceAssembly: "Assembly-CSharp", sourceClassName: "PrototypeFlowingDanceVisual")]
    public sealed class CoreFlowingDanceVisual : MonoBehaviour
    {
        [SerializeField] private CorePlayer player;
        private static readonly int DanceState = Animator.StringToHash("Base Layer.FlowingDanceEffect");
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
            if (!player.isActiveAndEnabled || player.Health.IsDead || !player.IsFlowingDancing)
            {
                Hide();
                return;
            }
            transform.localPosition = new Vector3(player.Facing * CorePlayer.DanceOffset, 0, 0);
            animator.Play(DanceState, 0, player.FlowingDanceProgress);
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
