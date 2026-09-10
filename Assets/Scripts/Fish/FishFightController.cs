using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(FishAI), typeof(Rigidbody))]
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
    [SerializeField, Min(0.05f)] private float maximumReelRecoverySpeed = 0.75f;
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
    [SerializeField, Min(0.01f)] private float baseSwimForce = 14f;
    [SerializeField, Min(0.01f)] private float waterDragCoefficient = 1.2f;

    [Header("Stamina")]
    [SerializeField, Min(0f)] private float staminaDrainPerSecond = 10f;
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
    private bool isReeling;
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
    public float LineTensionNormalized => Mathf.Clamp01(lineTension / GetBreakingStrain());
    public float DragSetting => dragSetting;
    public float DistanceFromPlayer => playerTransform == null ? 0f : Vector3.Distance(transform.position, playerTransform.position);

    private void Awake()
    {
        if (fishAI == null) fishAI = GetComponent<FishAI>();
        fishBody = GetComponent<Rigidbody>();
        fishBody.useGravity = false;
        fishBody.isKinematic = true;
        ResolvePlayer();
    }

    private void Update()
    {
        if (!isFighting || Keyboard.current == null)
            return;

        isReeling = Keyboard.current.rKey.isPressed;
        if (Keyboard.current.upArrowKey.isPressed)
            dragSetting = Mathf.Clamp01(dragSetting + 0.2f * Time.deltaTime);
        if (Keyboard.current.downArrowKey.isPressed)
            dragSetting = Mathf.Clamp01(dragSetting - 0.2f * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (!isFighting)
            return;

        ResolvePlayer();
        if (playerTransform == null || fishingLine == null || fishingLine.IsBroken)
        {
            EndFight(FishFightResult.LineBroken);
            return;
        }

        UpdateFightMovement();
        UpdateLineAndTension();
        UpdateStamina();
        CheckFightEnd();
    }

    private void ResolvePlayer()
    {
        if (playerTransform != null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
        else if (Camera.main != null) playerTransform = Camera.main.transform;
    }

    public void BeginFight()
    {
        if (isFighting)
            return;

        ResolvePlayer();
        if (playerTransform == null || fishingLine == null)
        {
            Debug.LogWarning("FishFightController: faltam referências ao jogador ou à linha.");
            return;
        }

        hookedSinker = Sinker.Current;
        if (hookedSinker == null || !hookedSinker.IsInWater)
        {
            Debug.LogWarning("FishFightController: não existe rig válido na água.");
            return;
        }

        LoadSpeciesData();
        stamina = maxStamina;
        lineTension = 0f;
        burstTimer = 0f;
        burstCooldown = Random.Range(burstCooldownMin, burstCooldownMax);
        directionTimer = 0f;
        reportedTired = false;
        isFighting = true;
        state = FishFightState.Fighting;

        fishBody.mass = Mathf.Max(0.1f, fishWeight);
        fishBody.useGravity = false;
        fishBody.isKinematic = false;
        fishBody.linearVelocity = Vector3.zero;
        fishBody.angularVelocity = Vector3.zero;

        // The main line always remains connected to the physical rig. The fish
        // is connected to that rig by Sinker.AttachFish, preserving the chain
        // instead of replacing the rig with the fish as the line target.
        hookedSinker.AttachFish(transform);
        if (fishingLine.Target != hookedSinker.transform)
            fishingLine.SetTarget(hookedSinker.transform);

        fishAI?.OnFightStarted();
        ChooseRunDirection(true);

        Debug.Log($"FISGADA! {fishWeight:F1} kg entrou em combate.");
    }

    private void LoadSpeciesData()
    {
        data = fishAI != null ? fishAI.SpeciesData : null;
        if (data == null)
            return;

        fishWeight = data.WeightKg;
        fishStrength = data.Strength;
        maxStamina = data.MaxStamina;
        burstSpeed = data.BurstSpeed;
        burstChance = data.BurstChance;
        obstacleSeeking = data.ObstacleSeeking;
        directionChangeChance = data.DirectionChangeChance;
    }

    private void UpdateFightMovement()
    {
        UpdateFightIntent();

        float staminaFactor = Mathf.Clamp01(StaminaNormalized);
        float burstMultiplier = IsBursting ? 1.8f : 1f;
        float desiredSpeed = (IsBursting ? burstSpeed : baseRunSpeed) * Mathf.Lerp(0.35f, 1f, staminaFactor);
        float speedFactor = desiredSpeed <= 0f ? 0f : Mathf.Clamp01(fishBody.linearVelocity.magnitude / desiredSpeed);
        float fishForce = baseSwimForce * fishStrength * staminaFactor * burstMultiplier * (1f - speedFactor * 0.65f);

        fishBody.AddForce(runDirection * fishForce, ForceMode.Force);

        Vector3 velocity = fishBody.linearVelocity;
        if (velocity.sqrMagnitude > 0.0001f)
            fishBody.AddForce(-velocity.normalized * velocity.sqrMagnitude * waterDragCoefficient, ForceMode.Force);

        if (runDirection.sqrMagnitude > 0.001f)
        {
            Quaternion rotation = Quaternion.LookRotation(runDirection, Vector3.up);
            fishBody.MoveRotation(Quaternion.Slerp(fishBody.rotation, rotation, 4f * Time.fixedDeltaTime));
        }
    }

    private void UpdateFightIntent()
    {
        directionTimer -= Time.fixedDeltaTime;
        burstCooldown -= Time.fixedDeltaTime;

        if (burstTimer > 0f)
            burstTimer -= Time.fixedDeltaTime;
        else if (burstCooldown <= 0f && Random.value <= burstChance * Time.fixedDeltaTime)
        {
            burstTimer = Random.Range(burstDurationMin, burstDurationMax);
            burstCooldown = Random.Range(burstCooldownMin, burstCooldownMax);
            ChooseRunDirection(true);
        }

        if (directionTimer <= 0f)
        {
            directionTimer = Random.Range(directionChangeIntervalMin, directionChangeIntervalMax);
            if (Random.value <= directionChangeChance)
                ChooseRunDirection(false);
        }
    }

    private void ChooseRunDirection(bool preferAwayFromPlayer)
    {
        Vector3 direction = preferAwayFromPlayer && playerTransform != null
            ? transform.position - playerTransform.position
            : Random.insideUnitSphere;

        direction.y = Random.Range(-0.15f, 0.15f);
        if (direction.sqrMagnitude < 0.01f)
            direction = transform.forward;

        direction.Normalize();

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
        if (isReeling)
            fishingLine.ReelIn(Mathf.Min(reelSpeed, maximumReelRecoverySpeed) * Time.fixedDeltaTime);

        lineTension = fishingLine.Tension;
    }

    private void UpdateStamina()
    {
        float tensionFactor = Mathf.Clamp01(lineTension / GetBreakingStrain());
        float movementCost = fishBody.linearVelocity.magnitude / Mathf.Max(0.1f, burstSpeed);
        float drain = staminaDrainPerSecond * (0.15f + tensionFactor + movementCost * 0.4f);
        if (IsBursting)
            drain *= 1.5f;

        stamina = Mathf.Max(0f, stamina - drain * Time.fixedDeltaTime);

        if (!reportedTired && StaminaNormalized <= tiredThreshold)
        {
            reportedTired = true;
            state = FishFightState.Tired;
            fishAI?.OnFightTired();
            Debug.Log("A carpa está cansada.");
        }
    }

    private void CheckFightEnd()
    {
        float distance = DistanceFromPlayer;
        if (distance > maximumFishDistance)
        {
            EndFight(FishFightResult.Escaped);
            return;
        }

        if (stamina <= maxStamina * exhaustedThreshold && distance <= minimumLandingDistance)
            EndFight(FishFightResult.Landed);
    }

    private float GetBreakingStrain()
    {
        return fishingLine != null ? fishingLine.BreakingStrain : lineBreakingStrain;
    }

    public void EndFight(FishFightResult result)
    {
        if (!isFighting)
            return;

        isFighting = false;
        isReeling = false;
        state = result == FishFightResult.Landed ? FishFightState.Landed : FishFightState.Lost;
        lineTension = 0f;

        fishBody.linearVelocity = Vector3.zero;
        fishBody.angularVelocity = Vector3.zero;
        fishBody.isKinematic = true;

        if (hookedSinker != null)
            hookedSinker.DetachFish();

        fishingLine?.Clear();

        if (fishAI != null)
        {
            if (result == FishFightResult.Landed) fishAI.OnFishLanded();
            else fishAI.OnFishLost();
        }

        Debug.Log($"FIGHT END: {result}");
    }
}
