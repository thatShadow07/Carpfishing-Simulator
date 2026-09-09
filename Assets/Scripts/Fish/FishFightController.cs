using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla a luta da carpa através do rig real. A carpa só recupera
/// distância quando o pescador recolhe com R. O chumbo acompanha a carpa
/// durante as arrancadas e a linha continua ligada ao rig.
/// </summary>
public class FishFightController : MonoBehaviour
{
    [Header("Fish Stats")]
    [SerializeField] private float fishWeight = 8f;
    [SerializeField] private float fishStrength = 1f;
    [SerializeField] private float maxStamina = 100f;

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private FishingLine fishingLine;

    [Header("Fight")]
    [SerializeField] private float minimumDistance = 1.5f;
    [SerializeField] private float reelSpeed = 2f;
    [SerializeField] private float burstDuration = 2.5f;
    [SerializeField] private float burstCooldown = 3.5f;
    [SerializeField] private float burstSpeed = 4f;
    [SerializeField] private float staminaDrainPerSecond = 10f;
    [SerializeField] private float staminaRecoveryPerSecond = 4f;

    private float stamina;
    private float distanceFromPlayer;
    private float burstTimer;
    private float cooldownTimer;
    private Vector3 burstDirection;
    private bool isFighting;
    private bool isBursting;
    private Sinker hookedSinker;

    public float Stamina => stamina;
    public float DistanceFromPlayer => distanceFromPlayer;
    public bool IsFighting => isFighting;
    public bool IsBursting => isBursting;

    private void Awake()
    {
        stamina = maxStamina;
    }

    private void Update()
    {
        if (!isFighting || playerTransform == null || hookedSinker == null) return;

        UpdateFight(Time.deltaTime);
    }

    public void BeginFight()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("FishFightController: Player Transform não está atribuído.");
            return;
        }

        hookedSinker = Sinker.Current;
        if (hookedSinker == null || !hookedSinker.IsInWater)
        {
            Debug.LogWarning("FishFightController: não foi encontrado um rig na água para ligar à carpa.");
            return;
        }

        isFighting = true;
        isBursting = false;
        stamina = maxStamina;
        cooldownTimer = 0f;
        distanceFromPlayer = Vector3.Distance(transform.position, playerTransform.position);

        hookedSinker.AttachFish(transform);

        // A linha continua ligada ao rig. O rig agora acompanha a carpa.
        if (fishingLine != null)
        {
            fishingLine.SetTarget(hookedSinker.transform);
        }

        StartBurst();

        Debug.Log($"⚔️ FIGHTING! A carpa de {fishWeight:F1} kg começou a lutar a {distanceFromPlayer:F1} m do jogador.");
    }

    private void UpdateFight(float deltaTime)
    {
        if (isBursting)
        {
            burstTimer -= deltaTime;
            stamina = Mathf.Max(0f, stamina - staminaDrainPerSecond * fishStrength * deltaTime);

            MoveFishAndRig(burstDirection, burstSpeed * fishStrength * deltaTime);

            if (burstTimer <= 0f || stamina <= 0f)
            {
                isBursting = false;
                cooldownTimer = burstCooldown;
                Debug.Log("🐟 A carpa cansou da arrancada.");
            }

            UpdateDistance();
            return;
        }

        cooldownTimer -= deltaTime;
        stamina = Mathf.Min(maxStamina, stamina + staminaRecoveryPerSecond * deltaTime);
        UpdateDistance();

        // A carpa não vem para o pescador sozinha. O jogador tem de recolher.
        if (Keyboard.current != null && Keyboard.current.rKey.isPressed)
        {
            ReelFish(deltaTime);
        }

        if (cooldownTimer <= 0f && stamina > maxStamina * 0.25f)
        {
            StartBurst();
        }
    }

    private void StartBurst()
    {
        isBursting = true;
        burstTimer = burstDuration;

        Vector3 awayFromPlayer = transform.position - playerTransform.position;
        awayFromPlayer.y = 0f;

        if (awayFromPlayer.sqrMagnitude < 0.001f)
        {
            awayFromPlayer = -playerTransform.forward;
            awayFromPlayer.y = 0f;
        }

        burstDirection = awayFromPlayer.normalized;
        Debug.Log("💨 A carpa fez uma nova arrancada!");
    }

    private void ReelFish(float deltaTime)
    {
        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f;

        float currentDistance = direction.magnitude;
        if (currentDistance <= minimumDistance)
        {
            EndFight();
            return;
        }

        float movement = Mathf.Min(reelSpeed * deltaTime, currentDistance - minimumDistance);
        MoveFishAndRig(direction.normalized, movement);
        stamina = Mathf.Max(0f, stamina - staminaDrainPerSecond * 0.25f * deltaTime);
        UpdateDistance();
    }

    private void MoveFishAndRig(Vector3 direction, float distance)
    {
        Vector3 movement = direction.normalized * distance;
        movement.y = 0f;

        transform.position += movement;

        if (hookedSinker != null)
        {
            hookedSinker.transform.position += movement;
        }

        if (movement.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(movement.normalized, Vector3.up);
        }
    }

    private void UpdateDistance()
    {
        distanceFromPlayer = Vector3.Distance(transform.position, playerTransform.position);
    }

    public void EndFight()
    {
        isFighting = false;
        isBursting = false;

        if (hookedSinker != null)
        {
            hookedSinker.DetachFish();
        }

        if (fishingLine != null)
        {
            fishingLine.Clear();
        }

        Debug.Log("🎣 Combate terminado — peixe e rig libertados.");
    }
}