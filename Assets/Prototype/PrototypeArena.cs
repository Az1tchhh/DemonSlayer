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
            status.text = $"TANJIRO  {player.Health.Current}/{player.Health.Maximum}     DEMON  {demon.Health.Current}/{demon.Health.Maximum}     CHECKPOINT  {(CheckpointActive ? "ACTIVE" : "Ahead")}\nQ  SURFACE SLASH  {kata}     E  WATER WHEEL  {wheel}     F  FLOWING DANCE  {dance}\nG  HINOKAMI: DANCE  {fire}     H  CLEAR BLUE SKY  {sky}";
            message.text = player.Health.IsDead ? "YOU FELL\nPress R to retry from your checkpoint" :
                demon.Health.IsDead ? "DEMON DEFEATED\nPress R to replay the encounter" :
                CheckpointActive ? "Checkpoint active — defeat the demon ahead" : "Cross the low step and reach the green checkpoint";
        }
        public void Retry()
        {
            player.ResetAt(spawn);
            demon.ResetAt(enemySpawn);
            Physics2D.SyncTransforms();
        }
    }
}
