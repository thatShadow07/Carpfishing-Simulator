using UnityEngine;

/// <summary>
/// Controla o movimento da carpa durante o combate.
/// Nesta fase liga o combate à posição real do jogador e faz a carpa
/// executar arrancadas e recuperar distância. A tensão da linha e o carreto
/// serão ligados numa etapa seguinte.
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
    private Vector3 burstDirection;
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

        if (playerTransform == null)
        {
            Debug.LogWarning("FishFightController: Player Transform não está atribuído.");
            return;
        }

        UpdateFight(Time.deltaTime);
    }

    public void BeginFight()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("FishFightController: não foi possível começar o combate porque Player Transform não está atribuído.");
            return;
        }

        isFighting = true;
        isBursting = false;
        stamina = maxStamina;
        distanceFromPlayer = Vector3.Distance(transform.position, playerTransform.position);
        distanceFromPlayer = Mathf.Max(distanceFromPlayer, minimumDistance);
        cooldownTimer = 0f;

        if (fishingLine != null)
        {
            fishingLine.SetTarget(transform);
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

            MoveFish(burstDirection, burstSpeed * fishStrength * deltaTime);

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

        // Quando a carpa descansa, o pescador consegue recuperar terreno.
        MoveTowardsPlayer(recoverySpeed * deltaTime);
        UpdateDistance();

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

    private void MoveFish(Vector3 direction, float distance)
    {
        Vector3 movement = direction.normalized * distance;
        movement.y = 0f;
        transform.position += movement;

        if (movement.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(movement.normalized, Vector3.up);
        }
    }

    private void MoveTowardsPlayer(float distance)
    {
        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        float currentDistance = direction.magnitude;
        float movement = Mathf.Min(distance, Mathf.Max(0f, currentDistance - minimumDistance));

        MoveFish(direction, movement);
    }

    private void UpdateDistance()
    {
        distanceFromPlayer = Vector3.Distance(transform.position, playerTransform.position);
    }

    public void EndFight()
    {
        isFighting = false;
        isBursting = false;

        if (fishingLine != null)
        {
            fishingLine.Clear();
        }

        Debug.Log("🎣 Combate terminado.");
    }
}
