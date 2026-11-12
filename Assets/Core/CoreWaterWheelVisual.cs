using UnityEngine;

namespace DemonSlayer.Core
{
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "DemonSlayer.Prototype", sourceAssembly: "Assembly-CSharp", sourceClassName: "PrototypeWaterWheelVisual")]
    public sealed class CoreWaterWheelVisual : MonoBehaviour
    {
        [SerializeField] private CorePlayer player;
        private static readonly int WheelState = Animator.StringToHash("Base Layer.WaterWheelEffect");
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
            if (!player.isActiveAndEnabled || player.Health.IsDead || !player.IsWaterWheeling)
            {
                Hide();
                return;
            }
            animator.Play(WheelState, 0, player.WaterWheelProgress);
            animator.Update(0);
            visual.flipX = player.Facing < 0;
            visual.enabled = visual.sprite != null;
            float spin = Mathf.InverseLerp(CorePlayer.WheelLaunch, CorePlayer.WheelSpinEnd, player.WaterWheelProgress);
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
