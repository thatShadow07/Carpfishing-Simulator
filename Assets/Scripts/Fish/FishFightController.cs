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
    [SerializeField, Min(0.1f)] private float maximumRigDistance = 3f;
    [SerializeField, Min(0.1f)] private float minimumLandingDistance = 2f;
    [SerializeField, Min(0.1f)] private float maximumFishDistance = 30f;
    [Header("Fish Movement")]
    [SerializeField, Min(0.1f)] private float baseRunSpeed = 2.5f;
    [SerializeField, Min(0.1f)] private float burstSpeed = 5f;
    [SerializeField, Min(0.1f)] private float burstDurationMin = 1.5f;
    [SerializeField, Min(0.1f)] private float burstDurationMax = 4f;
    [SerializeField, Min(0f)] private float burstCooldownMin = 3f;
    [SerializeField, Min(0f)] private float burstCooldownMax = 8f;
    [SerializeField, Range(0f, 1f)] private float burstChance = 0.5f;
    [Header("Stamina")]
    [SerializeField, Min(0f)] private float staminaDrainPerSecond = 10f;
    [SerializeField, Min(0f)] private float staminaRecoveryPerSecond = 3f;
    [SerializeField, Range(0f, 1f)] private float tiredThreshold = 0.3f;
    [SerializeField, Range(0f, 1f)] private float exhaustedThreshold = 0.1f;
    [Header("Fight Behaviour")]
    [SerializeField, Range(0f, 1f)] private float directionChangeChance = 0.4f;
    [SerializeField, Range(0f, 1f)] private float obstacleSeeking = 0.3f;
    [SerializeField, Min(0.1f)] private float directionChangeIntervalMin = 1f;
    [SerializeField, Min(0.1f)] private float directionChangeIntervalMax = 3f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField, Min(0.1f)] private float obstacleCheckDistance = 5f;

    private Rigidbody fishBody;
    private FishSpeciesData data;
    private Sinker hookedSinker;
    private Vector3 runDirection;
    private float stamina;
    private float burstTimer;
    private float burstCooldown;
    private float directionTimer;
    private float lineTension;
    private bool isFighting;
    private bool reportedTired;
    private FishFightState state = FishFightState.Idle;
    private float fishWeight = 8f;
    private float fishStrength = 1f;
    private float maxStamina = 100f;

    public bool IsFighting => isFighting;
    public bool IsBursting => isFighting && burstTimer > 0f;
    public FishFightState State => state;
    public float Stamina => stamina;
    public float StaminaNormalized => maxStamina <= 0f ? 0f : stamina / maxStamina;
    public float LineTension => lineTension;
    public float LineTensionNormalized => Mathf.Clamp01(lineTension / lineBreakingStrain);
    public float DragSetting => dragSetting;
    public float DistanceFromPlayer => playerTransform == null ? 0f : Vector3.Distance(transform.position, playerTransform.position);

    private void Awake()
    {
        if (fishAI == null) fishAI = GetComponent<FishAI>();
        fishBody = GetComponent<Rigidbody>();
        if (fishBody == null) fishBody = gameObject.AddComponent<Rigidbody>();
        fishBody.useGravity = false;
        fishBody.isKinematic = false;
        fishBody.linearDamping = 2f;
        fishBody.angularDamping = 5f;
        ResolvePlayer();
    }

    private void ResolvePlayer()
    {
        if (playerTransform != null) return;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
        else if (Camera.main != null) playerTransform = Camera.main.transform;
    }

    public void BeginFight()
    {
        if (isFighting) return;
        ResolvePlayer();
        if (playerTransform == null) { Debug.LogWarning("FishFightController: não encontrei Player Transform nem objeto com tag Player."); return; }
        hookedSinker = Sinker.Current;
        if (hookedSinker == null || !hookedSinker.IsInWater) { Debug.LogWarning("FishFightController: não existe um Sinker válido na água."); return; }
        data = fishAI != null ? fishAI.SpeciesData : data;
        if (data != null)
        {
            fishWeight = data.WeightKg;
            fishStrength = data.Strength;
            maxStamina = data.MaxStamina;
            burstSpeed = data.BurstSpeed;
            burstChance = data.BurstChance;
            obstacleSeeking = data.ObstacleSeeking;
            directionChangeChance = data.DirectionChangeChance;
        }
        stamina = maxStamina;
        lineTension = 0f;
        burstTimer = 0f;
        burstCooldown = Random.Range(burstCooldownMin, burstCooldownMax);
        directionTimer = 0f;
        reportedTired = false;
        isFighting = true;
        state = FishFightState.Fighting;
        hookedSinker.AttachFish(transform);
        if (fishingLine != null) fishingLine.SetTarget(transform);
        if (fishAI != null) fishAI.OnFightStarted();
        ChooseRunDirection(true);
        Debug.Log($"FISGADA! Carpa {fishWeight:F1} kg entrou em combate.");
    }

    private void Update()
    {
        if (!isFighting) return;
        ResolvePlayer();
        UpdateFightMovement();
        UpdateLineAndTension();
        UpdateStamina();
        HandlePlayerInput();
        CheckFightEnd();
    }

    private void UpdateFightMovement()
    {
        directionTimer -= Time.deltaTime;
        burstCooldown -= Time.deltaTime;
        if (burstTimer > 0f) burstTimer -= Time.deltaTime;
        else if (burstCooldown <= 0f && Random.value <= burstChance * Time.deltaTime)
        {
            burstTimer = Random.Range(burstDurationMin, burstDurationMax);
            burstCooldown = Random.Range(burstCooldownMin, burstCooldownMax);
            ChooseRunDirection(true);
        }
        if (directionTimer <= 0f)
        {
            directionTimer = Random.Range(directionChangeIntervalMin, directionChangeIntervalMax);
            if (Random.value <= directionChangeChance) ChooseRunDirection(false);
        }
        float speed = (burstTimer > 0f ? burstSpeed : baseRunSpeed) * Mathf.Lerp(0.35f, 1f, StaminaNormalized);
        Vector3 movement = runDirection * speed * Time.deltaTime;
        transform.position += movement;
        if (runDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(runDirection, Vector3.up), 4f * Time.deltaTime);
    }

    private void ChooseRunDirection(bool preferAwayFromPlayer)
    {
        Vector3 direction;
        if (preferAwayFromPlayer && playerTransform != null)
        {
            direction = transform.position - playerTransform.position;
            direction.y = Random.Range(-0.15f, 0.15f);
            if (direction.sqrMagnitude < 0.01f) direction = Random.insideUnitSphere;
            direction.Normalize();
        }
        else
        {
            direction = Random.insideUnitSphere;
            direction.y *= 0.35f;
            if (direction.sqrMagnitude < 0.01f) direction = transform.forward;
            direction.Normalize();
        }
        if (hookedSinker != null && obstacleSeeking > 0f)
        {
            Vector3 toRig = hookedSinker.transform.position - transform.position;
            if (toRig.sqrMagnitude > 0.01f && Random.value < obstacleSeeking * 0.35f)
                direction = Vector3.Slerp(direction, -toRig.normalized, obstacleSeeking).normalized;
        }
        if (Physics.Raycast(transform.position, direction, obstacleCheckDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            direction = -direction;
        runDirection = direction;
    }

    private void UpdateLineAndTension()
    {
        if (hookedSinker == null) return;
        Vector3 rodPosition = playerTransform != null ? playerTransform.position : hookedSinker.transform.position;
        Vector3 toRod = rodPosition - transform.position;
        float distanceToRod = toRod.magnitude;
        float dragForce = dragSetting * lineBreakingStrain;
        float movementForce = Mathf.Max(0f, Vector3.Dot(runDirection, -toRod.normalized)) * fishStrength * (fishWeight * 0.12f);
        float desiredTension = movementForce + dragForce * Mathf.Clamp01(distanceToRod / maximumFishDistance);
        if (fishingLine != null) lineTension = fishingLine.CalculateTension(desiredTension);
        else lineTension = Mathf.MoveTowards(lineTension, desiredTension, 8f * Time.deltaTime);
        if (lineTension > 0f && fishBody != null)
            fishBody.AddForce(toRod.normalized * lineTension * Mathf.Lerp(0.25f, 1f, dragSetting), ForceMode.Force);
        if (lineTension >= lineBreakingStrain) { EndFight(FishFightResult.LineBroken); return; }
        if (hookedSinker.AttachedFish != transform) hookedSinker.AttachFish(transform);
    }

    private void UpdateStamina()
    {
        float tensionFactor = Mathf.Clamp01(lineTension / lineBreakingStrain);
        float drain = staminaDrainPerSecond * (0.25f + tensionFactor * 1.5f);
        if (IsBursting) drain *= 1.6f;
        stamina = Mathf.Clamp(stamina - drain * Time.deltaTime, 0f, maxStamina);
        if (!reportedTired && StaminaNormalized <= tiredThreshold)
        {
            reportedTired = true;
            state = FishFightState.Tired;
            if (fishAI != null) fishAI.OnFightTired();
            Debug.Log("A carpa está cansada.");
        }
    }

    private void HandlePlayerInput()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.upArrowKey.isPressed) dragSetting = Mathf.Clamp01(dragSetting + 0.2f * Time.deltaTime);
        if (Keyboard.current.downArrowKey.isPressed) dragSetting = Mathf.Clamp01(dragSetting - 0.2f * Time.deltaTime);
        if (Keyboard.current.rKey.isPressed && DistanceFromPlayer > minimumLandingDistance)
        {
            float reelAmount = reelSpeed * Time.deltaTime;
            Vector3 towardPlayer = (playerTransform.position - transform.position).normalized;
            transform.position += towardPlayer * reelAmount * Mathf.Lerp(0.2f, 1f, 1f - StaminaNormalized);
        }
    }

    private void CheckFightEnd()
    {
        if (playerTransform == null) return;
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance > maximumFishDistance) { EndFight(FishFightResult.Escaped); return; }
        if (stamina <= maxStamina * exhaustedThreshold && distance <= minimumLandingDistance)
            EndFight(FishFightResult.Landed);
    }

    // Public because FishingReel can end the fight when the reel system detects
    // a terminal condition. Keeping the single exit point avoids duplicated cleanup.
    public void EndFight(FishFightResult result)
    {
        if (!isFighting) return;
        isFighting = false;
        state = result == FishFightResult.Landed ? FishFightState.Landed : FishFightState.Lost;
        lineTension = 0f;
        if (hookedSinker != null) hookedSinker.DetachFish();
        if (fishingLine != null) fishingLine.Clear();
        if (fishAI != null)
        {
            if (result == FishFightResult.Landed) fishAI.OnFishLanded();
            else fishAI.OnFishLost();
        }
        Debug.Log($"FIGHT END: {result}");
    }
}
