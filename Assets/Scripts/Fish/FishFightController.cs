using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sistema de luta da carpa. O peixe usa força, peso, stamina e personalidade
/// para decidir quando arrancar, mudar de direção, procurar obstáculos e descansar.
/// O jogador aproxima o rig ao pescar, mas o peixe não é puxado instantaneamente.
/// </summary>
public class FishFightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FishAI fishAI;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private FishingLine fishingLine;

    [Header("Fallback Fish Stats")]
    [SerializeField, Min(0.1f)] private float fishWeight = 8f;
    [SerializeField, Min(0.1f)] private float fishStrength = 1f;
    [SerializeField, Min(1f)] private float maxStamina = 100f;

    [Header("Reeling")]
    [SerializeField, Min(0.1f)] private float reelSpeed = 2f;
    [SerializeField, Min(0.1f)] private float fishFollowSpeed = 1.2f;
    [SerializeField, Min(0.1f)] private float minimumLandingDistance = 2f;

    [Header("Fight Distance")]
    [SerializeField, Min(1f)] private float maximumFishDistance = 30f;
    [SerializeField, Min(0.1f)] private float minimumRigDistance = 0.5f;
    [SerializeField, Min(0.1f)] private float maximumRigDistance = 3f;

    [Header("Burst")]
    [SerializeField, Min(0.1f)] private float burstDurationMin = 1.5f;
    [SerializeField, Min(0.1f)] private float burstDurationMax = 4f;
    [SerializeField, Min(0f)] private float burstCooldownMin = 3f;
    [SerializeField, Min(0f)] private float burstCooldownMax = 8f;
    [SerializeField, Min(0.1f)] private float burstSpeed = 5f;
    [SerializeField, Range(0f, 1f)] private float burstSpeedVariation = 0.2f;

    [Header("Stamina")]
    [SerializeField, Min(0f)] private float staminaDrainPerSecond = 10f;
    [SerializeField, Min(0f)] private float staminaRecoveryPerSecond = 3f;
    [SerializeField, Range(0f, 1f)] private float exhaustedThreshold = 0.1f;
    [SerializeField, Range(0f, 1f)] private float tiredThreshold = 0.3f;

    [Header("Behaviour")]
    [SerializeField, Range(0f, 1f)] private float obstacleFallbackChance = 0.25f;
    [SerializeField] private float obstacleCheckDistance = 6f;
    [SerializeField] private LayerMask obstacleMask;

    private FishSpeciesData data;
    private FishAI.FishState state = FishAI.FishState.Roaming;

    private float stamina;
    private float burstTimer;
    private float burstCooldown;
    private Vector3 burstDirection;
    private bool isFighting;
    private Sinker hookedSinker;

    public bool IsFighting => isFighting;
    public bool IsBursting => state == FishAI.FishState.Fighting && burstTimer > 0f;
    public float Stamina => stamina;
    public float StaminaNormalized => maxStamina <= 0f ? 0f : stamina / maxStamina;
    public float DistanceFromPlayer => playerTransform == null ? 0f : Vector3.Distance(transform.position, playerTransform.position);

    private void Awake()
    {
        if (fishAI == null)
            fishAI = GetComponent<FishAI>();

        if (fishAI != null)
            data = fishAI.SpeciesData;
    }

    private void Update()
    {
        if (!isFighting)
            return;

        if (playerTransform == null || hookedSinker == null)
        {
            Debug.LogWarning("FishFightController: faltam Player Transform ou Sinker.");
            return;
        }

        UpdateFight(Time.deltaTime);
    }

    public void BeginFight()
    {
        if (isFighting)
            return;

        if (playerTransform == null)
        {
            Debug.LogWarning("FishFightController: atribui o Player Transform no Inspector.");
            return;
        }

        hookedSinker = Sinker.Current;
        if (hookedSinker == null || !hookedSinker.IsInWater)
        {
            Debug.LogWarning("FishFightController: não existe um Sinker na água para ligar à carpa.");
            return;
        }

        data = fishAI != null ? fishAI.SpeciesData : data;

        if (data != null)
        {
            fishWeight = data.WeightKg;
            fishStrength = data.Strength;
            maxStamina = data.MaxStamina;
        }

        stamina = maxStamina;
        burstTimer = 0f;
        burstCooldown = Random.Range(burstCooldownMin, burstCooldownMax);
        isFighting = true;
        state = FishAI.FishState.Fighting;

        hookedSinker.AttachFish(transform);

        if (fishingLine != null)
            fishingLine.SetTarget(hookedSinker.transform);

        if (fishAI != null)
            fishAI.OnFightStarted();

        Debug.Log($"🐟 COMBATE! Carpa {fishWeight:F1} kg / força {fishStrength:F2}");
    }

    private void UpdateFight(float deltaTime)
    {
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance > maximumFishDistance)
        {
            FinishFight(false, "A carpa ganhou distância suficiente e escapou.");
            return;
        }

        if (burstTimer > 0f)
        {
            UpdateBurst(deltaTime);
            return;
        }

        burstCooldown -= deltaTime;

        bool reeling = Keyboard.current != null && Keyboard.current.rKey.isPressed;

        if (reeling)
            PullRigAndFish(deltaTime);
        else
            RecoverStamina(deltaTime);

        if (stamina <= maxStamina * exhaustedThreshold)
        {
            state = FishAI.FishState.Tired;
            if (fishAI != null)
                fishAI.OnFightTired();
        }
        else if (state == FishAI.FishState.Tired && stamina >= maxStamina * tiredThreshold)
        {
            state = FishAI.FishState.Fighting;
        }

        if (burstCooldown <= 0f && state != FishAI.FishState.Tired && ShouldBurst())
            StartBurst();
    }

    private void RecoverStamina(float deltaTime)
    {
        float recoveryMultiplier = data != null ? data.StaminaRecoveryMultiplier : 1f;
        stamina = Mathf.Min(maxStamina, stamina + staminaRecoveryPerSecond * recoveryMultiplier * deltaTime);
    }

    private bool ShouldBurst()
    {
        if (stamina <= maxStamina * 0.25f)
            return false;

        float chance = data != null ? data.BurstChance : 0.5f;
        float aggression = data != null ? data.Aggression : 0.5f;

        chance *= Mathf.Lerp(0.55f, 1.5f, aggression);
        return Random.value < chance * Time.deltaTime;
    }

    private void StartBurst()
    {
        state = FishAI.FishState.Fighting;
        burstTimer = Random.Range(burstDurationMin, burstDurationMax);
        burstCooldown = Random.Range(burstCooldownMin, burstCooldownMax);

        float speedMultiplier = Random.Range(1f - burstSpeedVariation, 1f + burstSpeedVariation);
        burstDirection = ChooseBurstDirection();

        float speed = burstSpeed * speedMultiplier;
        if (data != null)
            speed *= Mathf.Max(0.5f, data.Strength);

        // Guarda a velocidade no vetor para manter a arrancada consistente.
        burstDirection *= speed;

        Debug.Log("💨 A carpa arrancou!");
    }

    private void UpdateBurst(float deltaTime)
    {
        float drainMultiplier = data != null ? data.StaminaDrainMultiplier : 1f;
        stamina = Mathf.Max(0f, stamina - staminaDrainPerSecond * drainMultiplier * deltaTime);

        Vector3 movement = burstDirection * deltaTime;
        movement.y = 0f;

        transform.position += movement;

        if (movement.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * deltaTime);
        }

        MoveRigWithFish(movement);
        burstTimer -= deltaTime;

        if (stamina <= 0f || burstTimer <= 0f)
        {
            burstTimer = 0f;
            state = FishAI.FishState.Fighting;

            if (stamina <= 0f && fishAI != null)
                fishAI.OnFightTired();
        }
    }

    private Vector3 ChooseBurstDirection()
    {
        Vector3 awayFromPlayer = transform.position - playerTransform.position;
        awayFromPlayer.y = 0f;

        if (awayFromPlayer.sqrMagnitude < 0.01f)
            awayFromPlayer = -playerTransform.forward;

        awayFromPlayer.Normalize();

        Vector3 obstacleDirection = FindObstacleDirection();
        float obstacleSeeking = data != null ? data.ObstacleSeeking : obstacleFallbackChance;

        if (obstacleDirection != Vector3.zero && Random.value < obstacleSeeking)
            return obstacleDirection;

        // Varia ligeiramente a direção para não parecer um NPC que foge sempre em linha reta.
        float angle = Random.Range(-35f, 35f);
        return Quaternion.Euler(0f, angle, 0f) * awayFromPlayer;
    }

    private Vector3 FindObstacleDirection()
    {
        if (obstacleMask.value == 0)
            return Vector3.zero;

        Collider[] hits = Physics.OverlapSphere(transform.position, obstacleCheckDistance, obstacleMask);
        if (hits.Length == 0)
            return Vector3.zero;

        Collider closest = hits[0];
        float closestDistance = Vector3.Distance(transform.position, closest.transform.position);

        foreach (Collider hit in hits)
        {
            float d = Vector3.Distance(transform.position, hit.transform.position);
            if (d < closestDistance)
            {
                closest = hit;
                closestDistance = d;
            }
        }

        Vector3 direction = closest.transform.position - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.zero;
    }

    private void PullRigAndFish(float deltaTime)
    {
        if (hookedSinker == null)
            return;

        Vector3 rigToPlayer = playerTransform.position - hookedSinker.transform.position;
        rigToPlayer.y = 0f;

        if (rigToPlayer.sqrMagnitude > 0.001f)
        {
            float reel = reelSpeed * deltaTime;
            hookedSinker.transform.position += rigToPlayer.normalized * reel;
        }

        Vector3 fishToRig = hookedSinker.transform.position - transform.position;
        fishToRig.y = 0f;

        float rigDistance = fishToRig.magnitude;

        if (rigDistance > maximumRigDistance)
        {
            float strength = data != null ? data.Strength : fishStrength;
            float resistance = Mathf.Clamp01(1f / Mathf.Max(0.5f, strength));
            float follow = fishFollowSpeed * Mathf.Lerp(0.25f, 1f, resistance) * deltaTime;
            transform.position += fishToRig.normalized * Mathf.Min(follow, rigDistance - maximumRigDistance);
        }

        // Puxar cansa o peixe, mas pouco a pouco. Peixe forte resiste mais.
        float drainMultiplier = data != null ? data.StaminaDrainMultiplier : 1f;
        stamina = Mathf.Max(0f, stamina - staminaDrainPerSecond * 0.12f * drainMultiplier * deltaTime);

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= minimumLandingDistance && stamina <= maxStamina * tiredThreshold)
        {
            FinishFight(true, "🎣 A carpa está cansada e chegou à margem.");
        }
    }

    private void MoveRigWithFish(Vector3 movement)
    {
        if (hookedSinker == null)
            return;

        hookedSinker.transform.position += movement;

        Vector3 fishToRig = hookedSinker.transform.position - transform.position;
        fishToRig.y = 0f;

        if (fishToRig.magnitude > maximumRigDistance)
            hookedSinker.transform.position = transform.position + fishToRig.normalized * maximumRigDistance;
    }

    private void FinishFight(bool landed, string message)
    {
        if (!isFighting)
            return;

        isFighting = false;
        burstTimer = 0f;

        if (hookedSinker != null)
            hookedSinker.DetachFish();

        if (fishingLine != null)
            fishingLine.Clear();

        if (landed)
        {
            state = FishAI.FishState.Landed;
            if (fishAI != null)
                fishAI.OnFishLanded();
        }
        else
        {
            state = FishAI.FishState.Lost;
            if (fishAI != null)
                fishAI.OnFishLost();
        }

        Debug.Log(message);
    }

    public void ForceEndFight()
    {
        FinishFight(false, "🎣 Combate terminado.");
    }
}
