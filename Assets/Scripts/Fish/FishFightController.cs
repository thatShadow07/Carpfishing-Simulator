using UnityEngine;

/// <summary>
/// Controla o comportamento físico básico da carpa durante o combate.
/// Nesta primeira versão trata de distância, stamina e arrancadas.
/// O carreto, tensão real da linha e obstáculos serão ligados depois.
/// </summary>
public class FishFightController : MonoBehaviour
{
    [Header("Fish Stats")]
    [SerializeField] private float fishWeight = 8f;
    [SerializeField] private float fishStrength = 1f;
    [SerializeField] private float maxStamina = 100f;

    [Header("Fight")]
    [SerializeField] private float initialDistance = 12f;
    [SerializeField] private float minimumDistance = 1.5f;
    [SerializeField] private float recoverySpeed = 0.8f;
    [SerializeField] private float burstDuration = 2.5f;
    [SerializeField] private float burstCooldown = 3.5f;
    [SerializeField] private float burstSpeed = 4f;
    [SerializeField] private float staminaDrainPerSecond = 10f;
    [SerializeField] private float staminaRecoveryPerSecond = 4f;

    private float stamina;
    private float distanceFromPlayer;
    private float burstTimer;
    private float cooldownTimer;
    private bool isFighting;
    private bool isBursting;

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
        if (!isFighting) return;

        UpdateFight(Time.deltaTime);
    }

    public void BeginFight()
    {
        isFighting = true;
        isBursting = true;
        stamina = maxStamina;
        distanceFromPlayer = Mathf.Max(initialDistance, minimumDistance);
        burstTimer = burstDuration;
        cooldownTimer = 0f;

        Debug.Log($"⚔️ FIGHTING! A carpa de {fishWeight:F1} kg começou a lutar.");
    }

    private void UpdateFight(float deltaTime)
    {
        if (isBursting)
        {
            burstTimer -= deltaTime;
            stamina = Mathf.Max(0f, stamina - staminaDrainPerSecond * fishStrength * deltaTime);
            distanceFromPlayer += burstSpeed * fishStrength * deltaTime;

            if (burstTimer <= 0f || stamina <= 0f)
            {
                isBursting = false;
                cooldownTimer = burstCooldown;
                Debug.Log("🐟 A carpa cansou da arrancada.");
            }

            return;
        }

        cooldownTimer -= deltaTime;
        stamina = Mathf.Min(maxStamina, stamina + staminaRecoveryPerSecond * deltaTime);

        distanceFromPlayer = Mathf.Max(
            minimumDistance,
            distanceFromPlayer - recoverySpeed * deltaTime);

        if (cooldownTimer <= 0f && stamina > maxStamina * 0.25f)
        {
            StartBurst();
        }
    }

    private void StartBurst()
    {
        isBursting = true;
        burstTimer = burstDuration;
        Debug.Log("💨 A carpa fez uma nova arrancada!");
    }

    public void EndFight()
    {
        isFighting = false;
        isBursting = false;
        Debug.Log("🎣 Combate terminado.");
    }
}
