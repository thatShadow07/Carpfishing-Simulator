using UnityEngine;
using UnityEngine.InputSystem;

<<<<<<< Updated upstream
/// <summary>
/// Sistema de luta da carpa. O peixe usa força, peso, stamina e personalidade
/// para decidir quando arrancar, mudar de direção, procurar obstáculos e descansar.
/// O jogador aproxima o rig ao pescar, mas o peixe não é puxado instantaneamente.
/// </summary>
=======
>>>>>>> Stashed changes
public class FishFightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FishAI fishAI;
    [SerializeField] private Transform playerTransform;

    [Header("Line")]
    [SerializeField] private FishingLine fishingLine;

<<<<<<< Updated upstream
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
=======
    [Header("Fight")]
    [SerializeField] private float reelSpeed = 2f;
    [SerializeField] private float fishFollowSpeed = 1.5f;
    [SerializeField] private float maximumFishDistance = 25f;
    [SerializeField] private float landingDistance = 2f;

    [Header("Burst")]
    [SerializeField] private float burstDurationMin = 1.5f;
    [SerializeField] private float burstDurationMax = 4f;
    [SerializeField] private float burstCooldownMin = 2f;
    [SerializeField] private float burstCooldownMax = 7f;

    [Header("Stamina")]
    [SerializeField] private float staminaDrainPerSecond = 10f;
    [SerializeField] private float staminaRecoveryPerSecond = 3f;

    [Header("Obstacle")]
    [SerializeField] private float obstacleCheckDistance = 5f;
    [SerializeField] private LayerMask obstacleMask;

    private FishSpeciesData data;

    private FishFightState state =
        FishFightState.Idle;

    private float stamina;
    private float maxStamina;

    private float burstTimer;
    private float burstCooldown;

    private Vector3 movementDirection;

    private bool isFighting;

    public bool IsFighting => isFighting;
    public FishFightState State => state;

    public float StaminaNormalized
    {
        get
        {
            if (maxStamina <= 0f)
                return 0f;

            return stamina / maxStamina;
        }
    }

    private void Awake()
    {
        if (fishAI == null)
            fishAI = GetComponent<FishAI>();

        if (fishAI != null)
            data = fishAI.SpeciesData;
>>>>>>> Stashed changes
    }

    public void BeginFight()
    {
        if (isFighting)
            return;

        if (playerTransform == null)
        {
<<<<<<< Updated upstream
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
=======
            Debug.LogWarning(
                "FishFightController: Player Transform não atribuído."
            );

            return;
        }

        data = fishAI != null
            ? fishAI.SpeciesData
            : data;

        maxStamina =
            data != null
                ? data.MaxStamina
                : 100f;

        stamina = maxStamina;

        burstCooldown = 0f;
        burstTimer = 0f;

        isFighting = true;
>>>>>>> Stashed changes

        ChangeState(FishFightState.Fighting);

        if (fishAI != null)
            fishAI.OnFishFightStarted();

        if (Sinker.Current != null)
        {
            Sinker.Current.AttachFish(
                transform
            );
        }

        if (fishingLine != null)
<<<<<<< Updated upstream
            fishingLine.SetTarget(hookedSinker.transform);

        if (fishAI != null)
            fishAI.OnFightStarted();

        Debug.Log($"🐟 COMBATE! Carpa {fishWeight:F1} kg / força {fishStrength:F2}");
=======
        {
            fishingLine.SetTarget(
                transform
            );
        }
>>>>>>> Stashed changes
    }

    private void Update()
    {
<<<<<<< Updated upstream
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
=======
        if (!isFighting)
            return;

        if (playerTransform == null)
            return;

        switch (state)
        {
            case FishFightState.Fighting:
                UpdateNormalFight();
                break;

            case FishFightState.Burst:
                UpdateBurst();
                break;

            case FishFightState.Tired:
                UpdateTired();
                break;
        }
    }

    private void UpdateNormalFight()
    {
        bool playerReeling =
            Keyboard.current != null &&
            Keyboard.current.rKey.isPressed;

        float distance =
            Vector3.Distance(
                transform.position,
                playerTransform.position
            );

        if (distance >= maximumFishDistance)
        {
            TryEscape();
            return;
        }

        RecoverStamina(playerReeling);

        if (playerReeling)
        {
            PullFishTowardsPlayer();
        }

        burstCooldown -= Time.deltaTime;

        if (burstCooldown <= 0f)
        {
            if (ShouldBurst())
            {
                StartBurst();
                return;
            }
        }

        if (stamina <= maxStamina * 0.15f)
        {
            ChangeState(FishFightState.Tired);
        }
    }

    private void RecoverStamina(bool playerReeling)
    {
        if (playerReeling)
            return;

        float multiplier =
            data != null
                ? data.StaminaRecoveryMultiplier
                : 1f;

        stamina +=
            staminaRecoveryPerSecond *
            multiplier *
            Time.deltaTime;

        stamina =
            Mathf.Clamp(
                stamina,
                0f,
                maxStamina
            );
>>>>>>> Stashed changes
    }

    private bool ShouldBurst()
    {
<<<<<<< Updated upstream
        if (stamina <= maxStamina * 0.25f)
            return false;

        float chance = data != null ? data.BurstChance : 0.5f;
        float aggression = data != null ? data.Aggression : 0.5f;

        chance *= Mathf.Lerp(0.55f, 1.5f, aggression);
        return Random.value < chance * Time.deltaTime;
=======
        if (stamina < maxStamina * 0.25f)
            return false;

        float chance =
            data != null
                ? data.BurstChance
                : 0.5f;

        float aggression =
            data != null
                ? data.Aggression
                : 0.5f;

        chance *=
            Mathf.Lerp(
                0.5f,
                1.5f,
                aggression
            );

        return Random.value <
               chance * Time.deltaTime;
>>>>>>> Stashed changes
    }

    private void StartBurst()
    {
<<<<<<< Updated upstream
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
=======
        ChangeState(FishFightState.Burst);

        burstTimer =
            Random.Range(
                burstDurationMin,
                burstDurationMax
            );

        movementDirection =
            CalculateBurstDirection();

        if (data != null)
        {
            movementDirection =
                ChooseTacticalDirection();
        }
    }

    private void UpdateBurst()
    {
        float strength =
            data != null
                ? data.Strength
                : 1f;

        float speed =
            data != null
                ? data.BurstSpeed
                : 5f;

        float drain =
            staminaDrainPerSecond *
            strength;

        stamina -=
            drain *
            Time.deltaTime;

        transform.position +=
            movementDirection *
            speed *
            Time.deltaTime;

        RotateTowardsMovement();

        burstTimer -=
            Time.deltaTime;

        if (stamina <= 0f)
        {
            stamina = 0f;
            ChangeState(FishFightState.Tired);
            return;
        }

        if (burstTimer <= 0f)
        {
            burstCooldown =
                Random.Range(
                    burstCooldownMin,
                    burstCooldownMax
                );

            ChangeState(FishFightState.Fighting);
        }
    }

    private void UpdateTired()
    {
        stamina +=
            staminaRecoveryPerSecond *
            Time.deltaTime;

        if (stamina >= maxStamina * 0.35f)
        {
            ChangeState(FishFightState.Fighting);
        }

        PullFishTowardsPlayer();
    }

    private void PullFishTowardsPlayer()
    {
        Vector3 direction =
            playerTransform.position -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();

        float distance =
            Vector3.Distance(
                transform.position,
                playerTransform.position
            );

        float resistance =
            data != null
                ? data.Strength
                : 1f;

        float movement =
            reelSpeed *
            Time.deltaTime /
            resistance;

        movement =
            Mathf.Clamp(
                movement,
                0f,
                distance
            );

        transform.position +=
            direction *
            movement;

        if (fishAI != null)
            transform.LookAt(
                new Vector3(
                    playerTransform.position.x,
                    transform.position.y,
                    playerTransform.position.z
                )
            );

        stamina -=
            staminaDrainPerSecond *
            0.15f *
            Time.deltaTime;

        if (distance <= landingDistance)
        {
            FinishFight(
                FishFightResult.Landed
            );
        }
    }

    private Vector3 CalculateBurstDirection()
    {
        Vector3 away =
            transform.position -
            playerTransform.position;

        away.y = 0f;

        if (away.sqrMagnitude < 0.001f)
            away = -playerTransform.forward;

        return away.normalized;
    }

    private Vector3 ChooseTacticalDirection()
    {
        Vector3 away =
            CalculateBurstDirection();

        Vector3 towardsObstacle =
            FindNearestObstacleDirection();

        float obstacleSeeking =
            data != null
                ? data.ObstacleSeeking
                : 0.3f;

        if (towardsObstacle != Vector3.zero &&
            Random.value < obstacleSeeking)
        {
            return towardsObstacle;
        }

        return away;
    }

    private Vector3 FindNearestObstacleDirection()
    {
        if (Physics.Raycast(
                transform.position,
                transform.forward,
                obstacleCheckDistance,
                obstacleMask))
        {
            return transform.right.normalized;
        }

        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                obstacleCheckDistance,
                obstacleMask
            );

        if (hits.Length == 0)
            return Vector3.zero;

        Collider closest =
            hits[0];

        float closestDistance =
            Vector3.Distance(
                transform.position,
                closest.transform.position
            );

        foreach (Collider hit in hits)
        {
            float distance =
                Vector3.Distance(
                    transform.position,
                    hit.transform.position
                );

            if (distance < closestDistance)
            {
                closest = hit;
                closestDistance = distance;
            }
        }

        Vector3 direction =
            closest.transform.position -
            transform.position;

        direction.y = 0f;

        return direction.normalized;
    }

    private void RotateTowardsMovement()
    {
        if (movementDirection.sqrMagnitude < 0.001f)
            return;

        Quaternion rotation =
            Quaternion.LookRotation(
                movementDirection,
                Vector3.up
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                rotation,
                5f * Time.deltaTime
            );
    }

    private void TryEscape()
    {
        FinishFight(
            FishFightResult.Escaped
        );

        if (fishAI != null)
            fishAI.OnFishEscaped();
    }

    public void FinishFight(
        FishFightResult result
    )
>>>>>>> Stashed changes
    {
        if (!isFighting)
            return;

        isFighting = false;
<<<<<<< Updated upstream
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
=======

        switch (result)
        {
            case FishFightResult.Landed:
                ChangeState(
                    FishFightState.Landed
                );

                if (fishAI != null)
                    fishAI.OnFishLanded();

                break;

            case FishFightResult.Escaped:
            case FishFightResult.LineBroken:
            case FishFightResult.HookPulled:
            case FishFightResult.ReachedObstacle:

                ChangeState(
                    FishFightState.Lost
                );

                break;
        }

        if (Sinker.Current != null)
            Sinker.Current.DetachFish();

        if (fishingLine != null)
            fishingLine.Clear();
    }

    private void ChangeState(
        FishFightState newState
    )
    {
        state = newState;
>>>>>>> Stashed changes
    }

    public void ForceEndFight()
    {
        FinishFight(false, "🎣 Combate terminado.");
    }
}
