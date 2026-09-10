using UnityEngine;
using UnityEngine.InputSystem;

public class CastingSystem : MonoBehaviour
{
    private enum CastState { Idle, Aiming, InFlight }

    [Header("Referências")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Rigidbody sinkerPrefab;
    [SerializeField] private Transform castOrigin;
    [SerializeField] private FishingLine fishingLine;
    [SerializeField] private FishingReel fishingReel;

    [Header("Rig pendurado")]
    [SerializeField, Min(0.1f)] private float hangingLineLength = 0.75f;

    [Header("Força")]
    [SerializeField] private float minForce = 5f;
    [SerializeField] private float maxForce = 25f;
    [SerializeField] private float chargeTime = 1.5f;

    private CastState state = CastState.Idle;
    private float chargeTimer;
    private Rigidbody currentSinker;

    private void OnEnable()
    {
        state = CastState.Idle;
        chargeTimer = 0f;
        CreateHangingSinker();
    }

    private void Update()
    {
        if (playerCamera == null || Mouse.current == null) return;

        bool isAiming = Mouse.current.rightButton.isPressed;

        switch (state)
        {
            case CastState.Idle:
                if (isAiming && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    state = CastState.Aiming;
                    chargeTimer = 0f;
                }
                break;

            case CastState.Aiming:
                if (!isAiming)
                {
                    state = CastState.Idle;
                    chargeTimer = 0f;
                    break;
                }

                chargeTimer = Mathf.Min(chargeTimer + Time.deltaTime, chargeTime);

                if (Mouse.current.leftButton.wasReleasedThisFrame)
                    Cast();
                break;

            case CastState.InFlight:
                break;
        }
    }

    private void CreateHangingSinker()
    {
        if (currentSinker != null || sinkerPrefab == null || castOrigin == null)
            return;

        currentSinker = Instantiate(sinkerPrefab, castOrigin.position, castOrigin.rotation);
        // The rig is always a free Rigidbody held by FishingLine. This lets it
        // hang naturally from the rod before casting.
        currentSinker.isKinematic = false;
        currentSinker.linearVelocity = Vector3.zero;
        currentSinker.angularVelocity = Vector3.zero;

        if (fishingLine != null)
        {
            fishingLine.SetTarget(currentSinker.transform);
            fishingLine.SetLineLength(hangingLineLength);
        }

        if (fishingReel != null)
            fishingReel.SetSinker(currentSinker);
    }

    private void Cast()
    {
        CreateHangingSinker();
        if (currentSinker == null || playerCamera == null)
            return;

        // The cast releases the stored line before applying its impulse.
        if (fishingLine != null)
            fishingLine.ResetLineLength();

        float chargePercent = chargeTime > 0f ? chargeTimer / chargeTime : 0f;
        float force = Mathf.Lerp(minForce, maxForce, chargePercent);

        currentSinker.isKinematic = false;
        currentSinker.linearVelocity = Vector3.zero;
        currentSinker.angularVelocity = Vector3.zero;
        currentSinker.AddForce(playerCamera.transform.forward * force, ForceMode.VelocityChange);

        Debug.Log($"Lançamento a {chargePercent:P0} de força ({force:F1})");
        state = CastState.InFlight;
    }

    public void ResetCast()
    {
        state = CastState.Idle;
        chargeTimer = 0f;

        if (fishingLine != null)
            fishingLine.Clear();

        if (currentSinker != null)
        {
            Destroy(currentSinker.gameObject);
            currentSinker = null;
        }

        CreateHangingSinker();
    }

    private void OnDisable()
    {
        if (currentSinker != null)
        {
            Destroy(currentSinker.gameObject);
            currentSinker = null;
        }
    }

    public float GetChargePercent() => chargeTime > 0f ? chargeTimer / chargeTime : 0f;
}