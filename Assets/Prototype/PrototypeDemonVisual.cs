using UnityEngine;

namespace DemonSlayer.Prototype
{
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class PrototypeDemonVisual : MonoBehaviour
    {
        [SerializeField] private PrototypeDemon demon;
        [SerializeField, Min(.1f)] private float walkStrideLength = 1.2f;
        [SerializeField, Min(.01f)] private float hurtDuration = .18f;
        [SerializeField, Min(.01f)] private float deathDuration = .7f;
        private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
        private static readonly int WalkState = Animator.StringToHash("Base Layer.Walk");
        private static readonly int AttackState = Animator.StringToHash("Base Layer.Attack");
        private static readonly int HurtState = Animator.StringToHash("Base Layer.Hurt");
        private static readonly int DeathState = Animator.StringToHash("Base Layer.Death");
        private Animator animator;
        private SpriteRenderer visual;
        private Rigidbody2D body;
        private float idleTime, walkPhase;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            visual = GetComponent<SpriteRenderer>();
            body = demon.GetComponent<Rigidbody2D>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 0;
        }
        private void OnEnable() => demon.ResetPerformed += ResetVisual;
        private void OnDisable() => demon.ResetPerformed -= ResetVisual;
        private void LateUpdate()
        {
            int state = IdleState;
            float progress = 0;
            bool walking = demon.isActiveAndEnabled && Mathf.Abs(body.linearVelocity.x) > .1f;
            idleTime += Time.deltaTime;
            walkPhase = walking ? Mathf.Repeat(walkPhase + Mathf.Abs(body.linearVelocity.x) * Time.deltaTime / walkStrideLength, 1f) : 0;
            if (demon.Health.IsDead)
            {
                state = DeathState;
                progress = Mathf.Clamp01((Time.time - demon.Health.DeathTime) / deathDuration);
            }
            else if (demon.IsAttacking) { state = AttackState; progress = demon.AttackProgress; }
            else if (Time.time - demon.Health.LastDamageTime < hurtDuration)
            {
                state = HurtState;
                progress = (Time.time - demon.Health.LastDamageTime) / hurtDuration;
            }
            else if (walking) { state = WalkState; progress = walkPhase; }
            else progress = Mathf.Repeat(idleTime, 1f);
            animator.Play(state, 0, progress);
            animator.Update(0);
            visual.flipX = demon.Facing < 0;
        }
        private void ResetVisual()
        {
            idleTime = walkPhase = 0;
            animator.Rebind();
            animator.Play(IdleState, 0, 0);
            animator.Update(0);
            visual.flipX = demon.Facing < 0;
        }
    }
}
