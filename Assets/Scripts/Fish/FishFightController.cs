using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(FishAI))]
public class FishFightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FishAI fishAI;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private FishingLine fishingLine;

    [Header("Fight Equipment")]
    [SerializeField, Min(0.1f)] private float lineBreakingStrain = 6f;
    [SerializeField, Range(0f, 1f)] private float dragSetting = 0.45f;
    [SerializeField, Min(0.1f)] private float reelSpeed = 2f;
    [SerializeField, Min(0.1f)] private float fishFollowSpeed = 1.2f;
    [SerializeField, Min(0.1f)] private float maximumRigDistance = 3f;
    [SerializeField, Min(0.1f)] private float minimumLandingDistance = 2f;
    [SerializeField, Min(0.1f)] private float maximumFishDistance = 30f;

    [Header("Line Physics")]
    [SerializeField, Min(0.1f)] private float baseLineTension = 0.4f;
    [SerializeField, Min(0.1f)] private float reelTensionPerSecond = 1.2f;
    [SerializeField, Min(0.1f)] private float tensionReleasePerSecond = 1.8f;
    [SerializeField, Min(0.1f)] private float slackLossThreshold = 0.08f;
    [SerializeField, Min(0.1f)] private float maximumSafeTension = 0.85f;

    [Header("Fish Runs")]
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

    [Header("Fish Behaviour")]
    [SerializeField, Range(0f, 1f)] private float obstacleFallbackChance = 0.25f;
    [SerializeField, Min(0.1f)] private float obstacleCheckDistance = 6f;
    [SerializeField] private LayerMask obstacleMask;

    private FishSpeciesData data;
    private Sinker hookedSinker;
    private Vector3 burstDirection;
    private float stamina;
    private float burstTimer;
    private float burstCooldown;
    private float lineTension;
    private bool isFighting;
    private FishFightState state = FishFightState.Idle;

    public bool IsFighting => isFighting;
    public bool IsBursting => isFighting && burstTimer > 0f;
    public FishFightState State => state;
    public float Stamina => stamina;
    public float StaminaNormalized => maxStamina <= 0f ? 0f : stamina / maxStamina;
    public float LineTension => lineTension;
    public float LineTensionNormalized => Mathf.Clamp01(lineTension / lineBreakingStrain);
    public float DragSetting => dragSetting;
    public float DistanceFromPlayer => playerTransform == null ? 0f : Vector3.Distance(transform.position, playerTransform.position);

    private float fishWeight = 8f;
    private float fishStrength = 1f;
    private float maxStamina = 100f;

    private void Awake()
    {
        if (fishAI == null) fishAI = GetComponent<FishAI>();
        if (fishAI != null) data = fishAI.SpeciesData;
    }

    public void BeginFight()
    {
        if (isFighting) return;
        if (playerTransform == null)
        {
            Debug.LogWarning("FishFightController: atribui o Player Transform no Inspector.");
            return;
        }

        hookedSinker = Sinker.Current;
        if (hookedSinker == null || !hookedSinker.IsInWater)
        {
            Debug.LogWarning("FishFightController: não existe um Sinker na água.");
            return;
        }

        data = fishAI != null ? fishAI.SpeciesData : data;
        if (data != null)
        {
            fishWeight = data.WeightKg;
            fishStrength = data.Strength;
            maxStamina = data.MaxStamina;
            burstSpeed = data.BurstSpeed;
        }

        stamina = maxStamina;
        lineTension = baseLineTension;
        burstTimer = 0f;
        burstCooldown = Random.Range(burstCooldownMin, burstCooldownMax);
        isFighting = true;
        state = FishFightState.Fighting;

        hookedSinker.AttachFish(transform);
        if (fishingLine != null) fishingLine.SetTarget(transform);
        if (fishAI != null) fishAI.OnFightStarted();

        Debug.Log($"FISGADA! Carpa {fishWeight:F1} kg entrou em combate.");
    }

    public void EndFight()
    {
        if (!isFighting && state == FishFightState.Landed) return;
        FinishFight(true, "A carpa foi recolhida com sucesso.");
    }

    private void Update()
    {
        if (!isFighting) return;

        if (playerTransform == null || hookedSinker == null)
        {
            FinishFight(false, "Combate interrompido: faltam referências.");
            return;
        }

        float dt = Time.deltaTime;
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance > maximumFishDistance)
        {
            FinishFight(false, "A carpa ganhou demasiada distância e escapou.");
            return;
        }

        HandlePlayerInput(dt);
        UpdateNaturalRecovery(dt);
        UpdateFatigueState();
        UpdateFishRun(dt);
        UpdateLanding(dt);
        CheckLineFailure();
    }

    private void HandlePlayerInput(float dt)
    {
        bool reeling = Keyboard.current != null && Keyboard.current.rKey.isPressed;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.isPressed)
                dragSetting = Mathf.Clamp01(dragSetting + 0.25f * dt);
            if (Keyboard.current.downArrowKey.isPressed)
                dragSetting = Mathf.Clamp01(dragSetting - 0.25f * dt);
        }

        if (reeling && burstTimer <= 0f)
        {
            PullRigAndFish(dt);
            lineTension += reelTensionPerSecond * dt * Mathf.Lerp(0.55f, 1.35f, dragSetting);
        }
        else
        {
            lineTension -= tensionReleasePerSecond * dt;
        }

        lineTension = Mathf.Clamp(lineTension, 0f, lineBreakingStrain * 1.2f);
    }

    private void UpdateNaturalRecovery(float dt)
    {
        if (burstTimer > 0f) return;

        float recovery = staminaRecoveryPerSecond;
        if (data != null) recovery *= data.StaminaRecoveryMultiplier;
        stamina = Mathf.Min(maxStamina, stamina + recovery * dt);
    }

    private void UpdateFatigueState()
    {
        if (stamina <= maxStamina * exhaustedThreshold)
        {
            if (state != FishFightState.Tired)
            {
                state = FishFightState.Tired;
                if (fishAI != null) fishAI.OnFightTired();
            }
        }
        else if (state == FishFightState.Tired && stamina >= maxStamina * tiredThreshold)
        {
            state = FishFightState.Fighting;
            if (fishAI != null) fishAI.OnFightStarted();
        }
    }

    private void UpdateFishRun(float dt)
    {
        if (burstTimer > 0f)
        {
            ExecuteBurst(dt);
            return;
        }

        burstCooldown -= dt;
        if (burstCooldown > 0f || state == FishFightState.Tired) return;
        if (stamina <= maxStamina * 0.2f) return;

        float chance = data != null ? data.BurstChance : 0.5f;
        float aggression = data != null ? data.Aggression : 0.5f;
        float intelligence = data != null ? data.Intelligence : 0.5f;
        chance *= Mathf.Lerp(0.55f, 1.5f, aggression);
        chance *= Mathf.Lerp(0.8f, 1.2f, intelligence);

        if (Random.value < chance * dt)
            StartBurst();
    }

    private void StartBurst()
    {
        state = FishFightState.Burst;
        burstTimer = Random.Range(burstDurationMin, burstDurationMax);
        burstCooldown = Random.Range(burstCooldownMin, burstCooldownMax);

        float strength = data != null ? data.Strength : fishStrength;
        float speed = burstSpeed * Random.Range(1f - burstSpeedVariation, 1f + burstSpeedVariation);
        speed *= Mathf.Max(0.5f, strength);

        burstDirection = ChooseRunDirection() * speed;
        Debug.Log("RUN! A carpa arrancou.");
    }

    private void ExecuteBurst(float dt)
    {
        float drain = staminaDrainPerSecond;
        if (data != null) drain *= data.StaminaDrainMultiplier;
        stamina = Mathf.Max(0f, stamina - drain * dt);

        float dragResistance = Mathf.Lerp(0.75f, 1.35f, dragSetting);
        lineTension += burstDirection.magnitude * 0.08f * dragResistance * dt;

        Vector3 movement = burstDirection * dt;
        transform.position += movement;

        if (movement.sqrMagnitude > 0.001f)
        {
            Quaternion rotation = Quaternion.LookRotation(movement.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 5f * dt);
        }

        burstTimer -= dt;
        if (burstTimer <= 0f || stamina <= 0f)
        {
            burstTimer = 0f;
            state = stamina <= maxStamina * exhaustedThreshold ? FishFightState.Tired : FishFightState.Fighting;
            lineTension *= 0.7f;
        }
    }

    private Vector3 ChooseRunDirection()
    {
        Vector3 away = transform.position - playerTransform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = -playerTransform.forward;
        away.Normalize();

        Vector3 obstacle = FindObstacleDirection();
        float obstacleSeeking = data != null ? data.ObstacleSeeking : obstacleFallbackChance;
        if (obstacle != Vector3.zero && Random.value < obstacleSeeking)
            return obstacle;

        float deepRunChance = data != null ? data.DeepRunChance : 0.5f;
        if (Random.value < deepRunChance)
        {
            Vector3 deep = away + Vector3.down * 0.8f;
            return deep.normalized;
        }

        float angle = Random.Range(-45f, 45f);
        return (Quaternion.Euler(0f, angle, 0f) * away).normalized;
    }

    private Vector3 FindObstacleDirection()
    {
        if (obstacleMask.value == 0) return Vector3.zero;

        Collider[] hits = Physics.OverlapSphere(transform.position, obstacleCheckDistance, obstacleMask);
        if (hits.Length == 0) return Vector3.zero;

        Collider closest = hits[0];
        float closestDistance = Vector3.Distance(transform.position, closest.transform.position);
        foreach (Collider hit in hits)
        {
            float distance = Vector3.Distance(transform.position, hit.transform.position);
            if (distance < closestDistance)
            {
                closest = hit;
                closestDistance = distance;
            }
        }

        Vector3 direction = closest.transform.position - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.zero;
    }

    private void PullRigAndFish(float dt)
    {
        Vector3 toPlayer = playerTransform.position - hookedSinker.transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.001f)
            hookedSinker.transform.position += toPlayer.normalized * reelSpeed * dt;

        Vector3 fishToRig = hookedSinker.transform.position - transform.position;
        fishToRig.y = 0f;
        float rigDistance = fishToRig.magnitude;

        if (rigDistance > maximumRigDistance)
        {
            float follow = fishFollowSpeed * Mathf.Lerp(0.7f, 1.3f, dragSetting);
            transform.position += fishToRig.normalized * follow * dt;
        }

        stamina = Mathf.Max(0f, stamina - (0.8f + fishWeight * 0.08f) * dt);
    }

    private void UpdateLanding(float dt)
    {
        if (state == FishFightState.Burst || state == FishFightState.Lost || state == FishFightState.Landed)
            return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        bool exhausted = stamina <= maxStamina * exhaustedThreshold;

        if (distance <= minimumLandingDistance && exhausted)
        {
            state = FishFightState.Landing;
            if (fishAI != null) fishAI.OnFightTired();
            FinishFight(true, "A carpa está cansada e pronta para o landing.");
        }
    }

    private void CheckLineFailure()
    {
        float strength = data != null ? data.Strength : fishStrength;
        float safeTension = lineBreakingStrain * Mathf.Lerp(0.65f, 1f, dragSetting);
        safeTension *= Mathf.Lerp(1f, 0.75f, Mathf.Clamp01(strength - 1f));

        if (lineTension > safeTension)
        {
            float overload = lineTension - safeTension;
            lineTension -= overload * Time.deltaTime * 0.5f;

            if (lineTension > lineBreakingStrain)
            {
                FinishFight(false, "A linha partiu sob demasiada tensão.");
            }
        }

        if (lineTension < slackLossThreshold)
        {
            lineTension = 0f;
            // Linha demasiado folgada: não partimos a linha, mas a carpa pode ganhar controlo.
            if (Random.value < Time.deltaTime * 0.15f && state == FishFightState.Fighting)
                state = FishFightState.Tired;
        }
    }

    private void FinishFight(bool landed, string reason)
    {
        if (!isFighting && state != FishFightState.Landed && state != FishFightState.Landing)
            return;

        isFighting = false;
        state = landed ? FishFightState.Landed : FishFightState.Lost;

        if (hookedSinker != null) hookedSinker.DetachFish();
        if (fishingLine != null) fishingLine.Clear();
        if (fishAI != null)
        {
            if (landed) fishAI.OnFishLanded();
            else fishAI.OnFishLost();
        }

        Debug.Log(reason);
    }
}
