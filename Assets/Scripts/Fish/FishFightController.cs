using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla a luta da carpa através do rig real. Tem duas camadas de
/// cansaço: stamina de curto prazo (recupera quando o jogador não está a
/// puxar) e fadiga acumulada ao longo de toda a luta (nunca recupera, só
/// piora - reduz o teto da stamina e a intensidade das arrancadas com o
/// tempo). Perto da margem, mesmo cansada, pode tentar uma última investida.
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
    [SerializeField] private float fishResistanceSpeed = 0.8f;
    [SerializeField] private float maxRigFishDistance = 2f;

    [Header("Arrancadas (Bursts)")]
    [SerializeField] private float burstDurationMin = 1.2f;
    [SerializeField] private float burstDurationMax = 3f;
    [SerializeField] private float burstCooldownMin = 3f;
    [SerializeField] private float burstCooldownMax = 8f;
    [SerializeField] private float burstSpeed = 4f;
    [SerializeField, Range(0f, 1f)] private float burstSpeedVariance = 0.2f;
    [SerializeField, Range(0f, 2f)] private float burstTriggerChancePerSecond = 0.5f;
    [SerializeField, Range(0f, 1f)] private float minStaminaPercentForBurst = 0.6f;

    [Header("Stamina (curto prazo)")]
    [SerializeField] private float staminaDrainPerSecond = 12f;
    [SerializeField] private float staminaRecoveryPerSecond = 2.5f;
    [SerializeField] private float exhaustedRecoveryDelay = 3f;
    [SerializeField, Range(0f, 1f)] private float resistanceStrength = 0.7f;
    [SerializeField, Range(0f, 1f)] private float reelingRecoveryMultiplier = 0.1f; // quase não recupera enquanto puxas

    [Header("Fadiga acumulada (toda a luta)")]
    [SerializeField] private float overallFatigueGainPerStaminaPoint = 0.006f;
    [SerializeField, Range(0f, 1f)] private float maxOverallFatigue = 0.85f;
    [SerializeField, Range(0f, 1f)] private float overallFatigueStaminaCeilingImpact = 0.7f;
    [SerializeField, Range(0f, 1f)] private float overallFatigueRecoveryPenalty = 0.6f;
    [SerializeField, Range(0f, 1f)] private float overallFatigueBurstPenalty = 0.5f;

    [Header("Última investida perto da margem")]
    [SerializeField] private float secondWindDistance = 4f;
    [SerializeField, Range(0f, 2f)] private float secondWindChanceBoost = 1f;

    private float stamina;
    private float overallFatigue;
    private float toughnessFactor = 1f;
    private float distanceFromPlayer;
    private float burstTimer;
    private float cooldownTimer;
    private float exhaustedTimer;
    private float currentBurstSpeedMultiplier;
    private Vector3 burstDirection;
    private bool isFighting;
    private bool isBursting;
    private Sinker hookedSinker;

    public float Stamina => stamina;
    public float DistanceFromPlayer => distanceFromPlayer;
    public bool IsFighting => isFighting;
    public bool IsBursting => isBursting;

    private float EffectiveMaxStamina => maxStamina * (1f - overallFatigue * overallFatigueStaminaCeilingImpact);

    private void Awake() => stamina = maxStamina;

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
        overallFatigue = 0f;
        toughnessFactor = Mathf.Max(0.3f, (fishWeight / 8f) * fishStrength);
        cooldownTimer = 0f;
        exhaustedTimer = 0f;
        distanceFromPlayer = Vector3.Distance(transform.position, playerTransform.position);

        hookedSinker.AttachFish(transform);

        if (fishingLine != null)
            fishingLine.SetTarget(hookedSinker.transform);

        StartBurst();
        Debug.Log($"⚔️ FIGHTING! A carpa de {fishWeight:F1} kg começou a lutar a {distanceFromPlayer:F1} m do jogador.");
    }

    private void UpdateFight(float deltaTime)
    {
        bool isReeling = Keyboard.current != null && Keyboard.current.rKey.isPressed;

        if (isBursting)
        {
            burstTimer -= deltaTime;
            SpendStamina(staminaDrainPerSecond * fishStrength * deltaTime);
            MoveFishAndRig(burstDirection, burstSpeed * fishStrength * currentBurstSpeedMultiplier * deltaTime);

            if (burstTimer <= 0f || stamina <= 0f)
            {
                isBursting = false;
                cooldownTimer = Random.Range(burstCooldownMin, burstCooldownMax);

                if (stamina <= 0f)
                {
                    exhaustedTimer = exhaustedRecoveryDelay;
                    Debug.Log("🐟 A carpa ficou completamente cansada e precisa de recuperar.");
                }
                else Debug.Log("🐟 A carpa cansou da arrancada.");
            }

            UpdateDistance();
            return;
        }

        cooldownTimer -= deltaTime;

        if (exhaustedTimer > 0f)
        {
            exhaustedTimer -= deltaTime;
        }
        else
        {
            float recoveryMultiplier = Mathf.Lerp(1f, 1f - overallFatigueRecoveryPenalty, overallFatigue);

            // Enquanto o jogador mantém a linha em tensão (a puxar), a carpa
            // quase não recupera. Só descansa a sério quando a pressão alivia.
            if (isReeling) recoveryMultiplier *= reelingRecoveryMultiplier;

            stamina = Mathf.Min(EffectiveMaxStamina, stamina + staminaRecoveryPerSecond * recoveryMultiplier * deltaTime);
        }

        if (isReeling) ReelRig(deltaTime);

        UpdateDistance();

        bool nearMargin = distanceFromPlayer <= secondWindDistance;
        float staminaThreshold = EffectiveMaxStamina * (nearMargin ? minStaminaPercentForBurst * 0.5f : minStaminaPercentForBurst);
        bool readyToBurst = cooldownTimer <= 0f && exhaustedTimer <= 0f && stamina > staminaThreshold;

        float triggerChance = burstTriggerChancePerSecond * (nearMargin ? (1f + secondWindChanceBoost) : 1f);

        if (readyToBurst && Random.value < triggerChance * deltaTime)
        {
            StartBurst();
        }
    }

    private void SpendStamina(float amount)
    {
        stamina = Mathf.Max(0f, stamina - amount);
        overallFatigue = Mathf.Min(maxOverallFatigue, overallFatigue + (amount * overallFatigueGainPerStaminaPoint) / toughnessFactor);
    }

    private void StartBurst()
    {
        isBursting = true;

        float fatigueScale = Mathf.Lerp(1f, 1f - overallFatigueBurstPenalty, overallFatigue);
        burstTimer = Random.Range(burstDurationMin, burstDurationMax) * fatigueScale;
        currentBurstSpeedMultiplier = Random.Range(1f - burstSpeedVariance, 1f + burstSpeedVariance) * fatigueScale;

        Vector3 awayFromPlayer = transform.position - playerTransform.position;
        awayFromPlayer.y = 0f;

        if (awayFromPlayer.sqrMagnitude < 0.001f)
        {
            awayFromPlayer = -playerTransform.forward;
            awayFromPlayer.y = 0f;
        }

        burstDirection = awayFromPlayer.normalized;
        Debug.Log($"💨 A carpa fez uma nova arrancada! ({burstTimer:F1}s, fadiga acumulada: {overallFatigue:P0})");
    }

    private void ReelRig(float deltaTime)
    {
        Vector3 fishToRig = hookedSinker.transform.position - transform.position;
        fishToRig.y = 0f;
        float fishRigDistanceBefore = fishToRig.magnitude;

        float fatigue = 1f - stamina / maxStamina;
        float resistance = Mathf.Lerp(1f, 0.25f, fatigue) * fishStrength;

        Vector3 rigToPlayer = playerTransform.position - hookedSinker.transform.position;
        rigToPlayer.y = 0f;
        float rigDistance = rigToPlayer.magnitude;

        if (rigDistance > 0.001f)
        {
            float requestedMovement = Mathf.Min(reelSpeed * deltaTime, rigDistance);

            float tension = Mathf.Clamp01(fishRigDistanceBefore / maxRigFishDistance);
            float resistanceFactor = Mathf.Lerp(1f, 1f - resistanceStrength * resistance, tension);
            float actualMovement = requestedMovement * resistanceFactor;

            hookedSinker.transform.position += rigToPlayer.normalized * actualMovement;
        }

        Vector3 updatedFishToRig = hookedSinker.transform.position - transform.position;
        updatedFishToRig.y = 0f;
        float fishRigDistance = updatedFishToRig.magnitude;

        if (fishRigDistance > 0.001f)
        {
            float followFactor = Mathf.Lerp(0.25f, 1f, fatigue);
            float followSpeed = fishResistanceSpeed * (1f + followFactor);
            float fishMovement = Mathf.Min(followSpeed * deltaTime, fishRigDistance);
            transform.position += updatedFishToRig.normalized * fishMovement;
        }

        Vector3 finalFishToRig = hookedSinker.transform.position - transform.position;
        finalFishToRig.y = 0f;
        float finalDistance = finalFishToRig.magnitude;

        if (finalDistance > maxRigFishDistance)
        {
            Vector3 correction = finalFishToRig.normalized * (finalDistance - maxRigFishDistance);
            float fishShare = Mathf.Clamp01(0.35f + fatigue * 0.35f);

            transform.position += correction * fishShare;
            hookedSinker.transform.position -= correction * (1f - fishShare);
        }

        SpendStamina(staminaDrainPerSecond * 0.08f * deltaTime);

        if (Vector3.Distance(transform.position, playerTransform.position) <= minimumDistance)
            EndFight();
    }

    private void MoveFishAndRig(Vector3 direction, float distance)
    {
        Vector3 movement = direction.normalized * distance;
        movement.y = 0f;

        transform.position += movement;
        if (hookedSinker != null) hookedSinker.transform.position += movement;

        if (movement.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(movement.normalized, Vector3.up);
    }

    private void UpdateDistance()
    {
        distanceFromPlayer = Vector3.Distance(transform.position, playerTransform.position);
    }

    public void EndFight()
    {
        isFighting = false;
        isBursting = false;

        if (hookedSinker != null) hookedSinker.DetachFish();
        if (fishingLine != null) fishingLine.Clear();

        Debug.Log("🎣 Combate terminado — peixe e rig libertados.");
    }
}