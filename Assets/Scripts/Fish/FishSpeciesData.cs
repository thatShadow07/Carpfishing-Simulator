using UnityEngine;

[CreateAssetMenu(fileName = "FishSpeciesData", menuName = "Carpfishing/Fish Species Data")]
public class FishSpeciesData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string speciesName = "Common Carp";

    [Header("Physical")]
    [SerializeField, Min(0.1f)] private float weightKg = 8f;
    [SerializeField, Min(0.1f)] private float bodyStrength = 1f;
    [SerializeField, Min(0.1f)] private float swimmingSpeed = 1.5f;
    [SerializeField, Min(0.1f)] private float burstSpeed = 5f;

    [Header("Endurance")]
    [SerializeField, Min(1f)] private float maxStamina = 100f;
    [SerializeField, Min(0f)] private float staminaDrainMultiplier = 1f;
    [SerializeField, Min(0f)] private float staminaRecoveryMultiplier = 1f;

    [Header("Behaviour")]
    [SerializeField, Range(0f, 1f)] private float caution = 0.5f;
    [SerializeField, Range(0f, 1f)] private float aggression = 0.5f;
    [SerializeField, Range(0f, 1f)] private float intelligence = 0.5f;
    [SerializeField, Range(0f, 1f)] private float obstacleSeeking = 0.3f;
    [SerializeField, Range(0f, 1f)] private float marginResistance = 0.5f;
    [SerializeField, Range(0f, 1f)] private float burstChance = 0.5f;
    [SerializeField, Range(0f, 1f)] private float baitConfidence = 0.5f;

    [Header("Fight Behaviour")]
    [SerializeField, Range(0f, 1f)] private float headShakeChance = 0.35f;
    [SerializeField, Range(0f, 1f)] private float directionChangeChance = 0.4f;
    [SerializeField, Range(0f, 1f)] private float deepRunChance = 0.5f;
    [SerializeField, Min(0.1f)] private float fightAggressionMultiplier = 1f;

    public string SpeciesName => speciesName;
    public float WeightKg => weightKg;
    public float Strength => bodyStrength;
    public float SwimmingSpeed => swimmingSpeed;
    public float BurstSpeed => burstSpeed;
    public float MaxStamina => maxStamina;
    public float StaminaDrainMultiplier => staminaDrainMultiplier;
    public float StaminaRecoveryMultiplier => staminaRecoveryMultiplier;
    public float Caution => caution;
    public float Aggression => aggression;
    public float Intelligence => intelligence;
    public float ObstacleSeeking => obstacleSeeking;
    public float MarginResistance => marginResistance;
    public float BurstChance => burstChance;
    public float BaitConfidence => baitConfidence;
    public float HeadShakeChance => headShakeChance;
    public float DirectionChangeChance => directionChangeChance;
    public float DeepRunChance => deepRunChance;
    public float FightAggressionMultiplier => fightAggressionMultiplier;
}
