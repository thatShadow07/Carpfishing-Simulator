using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla a luta da carpa através do rig real.
/// A carpa faz arrancadas, fica cansada e só perde terreno quando o pescador
/// recolhe. O rig é puxado pelo carreto e a carpa oferece resistência, em vez
/// de ser movida instantaneamente com o chumbo.
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
    [SerializeField] private float burstDuration = 2.5f;
    [SerializeField] private float burstCooldown = 5f;
    [SerializeField] private float burstSpeed = 4f;
    [SerializeField] private float staminaDrainPerSecond = 12f;
    [SerializeField] private float staminaRecoveryPerSecond = 2.5f;
    [SerializeField] private float exhaustedRecoveryDelay = 3f;

    private float stamina;
    private float distanceFromPlayer;
    private float burstTimer;
    private float cooldownTimer;
    private float exhaustedTimer;
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
        exhaustedTimer = 0f;
        distanceFromPlayer = Vector3.Distance(transform.position, playerTransform.position);

        hookedSinker.AttachFish(transform);

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

                if (stamina <= 0f)
                {
                    exhaustedTimer = exhaustedRecoveryDelay;
                    Debug.Log("🐟 A carpa ficou completamente cansada e precisa de recuperar.");
                }
                else
                {
                    Debug.Log("🐟 A carpa cansou da arrancada.");
                }
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
            stamina = Mathf.Min(maxStamina, stamina + staminaRecoveryPerSecond * deltaTime);
        }

        if (Keyboard.current != null && Keyboard.current.rKey.isPressed)
        {
            ReelRig(deltaTime);
        }

        UpdateDistance();

        // Só pode fazer uma nova arrancada depois de recuperar alguma stamina
        // e de terminar o período de descanso.
        if (cooldownTimer <= 0f && exhaustedTimer <= 0f && stamina > maxStamina * 0.6f)
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

    private void ReelRig(float deltaTime)
    {
        // Primeiro puxamos o rig/chumbo. O pescador não move diretamente a carpa.
        Vector3 rigToPlayer = playerTransform.position - hookedSinker.transform.position;
        rigToPlayer.y = 0f;

        float rigDistance = rigToPlayer.magnitude;
        if (rigDistance > 0.001f)
        {
            float movement = Mathf.Min(reelSpeed * deltaTime, rigDistance);
            hookedSinker.transform.position += rigToPlayer.normalized * movement;
        }

        // A carpa oferece resistência e só acompanha o rig gradualmente.
        Vector3 fishToRig = hookedSinker.transform.position - transform.position;
        fishToRig.y = 0f;

        float fishRigDistance = fishToRig.magnitude;
        if (fishRigDistance > 0.001f)
        {
            float resistanceFactor = Mathf.Lerp(0.35f, 1f, 1f - stamina / maxStamina);
            float followSpeed = fishResistanceSpeed * resistanceFactor;

            if (fishRigDistance > maxRigFishDistance)
            {
                followSpeed *= 1.5f;
            }

            float fishMovement = Mathf.Min(followSpeed * deltaTime, fishRigDistance);
            transform.position += fishToRig.normalized * fishMovement;
        }

        // Recolher também cansa a carpa ligeiramente.
        stamina = Mathf.Max(0f, stamina - staminaDrainPerSecond * 0.08f * deltaTime);

        if (Vector3.Distance(transform.position, playerTransform.position) <= minimumDistance)
        {
            EndFight();
        }
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