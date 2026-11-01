using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DemonSlayer.Prototype
{
    public sealed class PrototypeArena : MonoBehaviour
    {
        [SerializeField] private PrototypePlayer player;
        [SerializeField] private PrototypeDemon demon;
        [SerializeField] private Transform checkpoint;
        [SerializeField] private SpriteRenderer checkpointVisual;
        [SerializeField] private TMP_Text status;
        [SerializeField] private TMP_Text message;
        [SerializeField] private string enemyName = "DEMON";
        [SerializeField] private PrototypeZenitsuAssist zenitsu;
        [SerializeField] private PrototypeCompanionSelection companions;
        [SerializeField] private PrototypeGiyuProtection giyu;
        private InputAction retry;
        private Vector2 spawn, enemySpawn;
        public bool CheckpointActive { get; private set; }
        private void Awake()
        {
            spawn = player.transform.position;
            enemySpawn = demon.transform.position;
            retry = new InputAction("Retry", InputActionType.Button, "<Keyboard>/r");
        }
        private void OnEnable() => retry.Enable();
        private void OnDisable() => retry.Disable();
        private void OnDestroy() => retry.Dispose();
        private void Update()
        {
            if (retry.WasPressedThisFrame()) Retry();
            if (!player.Health.IsDead && !CheckpointActive &&
                Vector2.Distance(player.transform.position, checkpoint.position) < 1.3f)
            {
                CheckpointActive = true;
                spawn = checkpoint.position;
                checkpointVisual.color = new Color(.3f, 1, .65f);
            }
            string kata = player.KataRemaining > 0 ? $"{player.KataRemaining:0.0}s" : "READY";
            string wheel = player.WheelRemaining > 0 ? $"{player.WheelRemaining:0.0}s" : "READY";
            string dance = player.DanceRemaining > 0 ? $"{player.DanceRemaining:0.0}s" : "READY";
            string fire = player.HinokamiRemaining > 0 ? $"{player.HinokamiRemaining:0.0}s" : "READY";
            string sky = player.SkyRemaining > 0 ? $"{player.SkyRemaining:0.0}s" : "READY";
            string dash = player.IsDashing ? "ACTIVE" : player.DashRemaining > 0 ? $"{player.DashRemaining:0.0}s" : "READY";
            string dashHint = player.DashEnabled ? $"     SHIFT DASH  {dash}" : "";
            string block = player.IsBlocking ? "GUARDING" : "HOLD";
            string assist = !zenitsu ? "" : $"     L ZENITSU  {(zenitsu.IsActive ? "ACTIVE" : zenitsu.Remaining > 0 ? $"{zenitsu.Remaining:0.0}s" : "READY")}";
            if (companions)
                assist = $"     L {companions.DisplayName}  {(companions.HasActiveAssist ? "ACTIVE" : companions.Remaining > 0 ? $"{companions.Remaining:0.0}s" : "READY")}";
            string selectionHint = companions ? $"     [1/2/3 {(companions.SelectionLocked ? "LOCKED" : "SELECT")}]" : "";
            string protection = !giyu || !giyu.isActiveAndEnabled ? "" : $"   GIYU AUTO {(giyu.IsActive ? "SHIELD" : giyu.Remaining > 0 ? $"{giyu.Remaining:0.0}s" : "READY")}";
            status.text = $"TANJIRO  {player.Health.Current}/{player.Health.Maximum}     {enemyName}  {demon.Health.Current}/{demon.Health.Maximum}     CHECKPOINT  {(CheckpointActive ? "ACTIVE" : "Ahead")}{selectionHint}\nQ SURFACE SLASH {kata}   E WATER WHEEL {wheel}   F FLOWING DANCE {dance}{protection}\nG  HINOKAMI: DANCE  {fire}     H  CLEAR BLUE SKY  {sky}{dashHint}     K BLOCK  {block}{assist}";
            message.text = player.Health.IsDead ? "YOU FELL\nPress R to retry from your checkpoint" :
                demon.Health.IsDead ? $"{enemyName} DEFEATED\nPress R to replay the encounter" :
                CheckpointActive ? $"Checkpoint active — defeat {enemyName.ToLowerInvariant()} ahead" : "Cross the low step and reach the green checkpoint";
        }
        public void Retry()
        {
            player.ResetAt(spawn);
            demon.ResetAt(enemySpawn);
            Physics2D.SyncTransforms();
        }
    }
}
