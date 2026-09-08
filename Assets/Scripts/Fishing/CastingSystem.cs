using UnityEngine;
using UnityEngine.InputSystem;

public class CastingSystem : MonoBehaviour
{
    private enum CastState { Idle, Aiming, InFlight }

    [Header("Referências")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Rigidbody sinkerPrefab;
    [SerializeField] private Transform castOrigin;

    [Header("Força")]
    [SerializeField] private float minForce = 5f;
    [SerializeField] private float maxForce = 25f;
    [SerializeField] private float chargeTime = 1.5f;

    private CastState state = CastState.Idle;
    private float chargeTimer;

    private void OnEnable()
    {
        state = CastState.Idle;
        chargeTimer = 0f;
    }

    private void Update()
    {
        if (playerCamera == null || Mouse.current == null) return;

        bool isAiming = Mouse.current.rightButton.isPressed;

        switch (state)
        {
            case CastState.Idle:
                // Só começa a carregar se o botão direito estiver premido
                if (isAiming && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    state = CastState.Aiming;
                    chargeTimer = 0f;
                }
                break;

            case CastState.Aiming:
                // Se soltares o botão direito a meio, cancela sem lançar
                if (!isAiming)
                {
                    state = CastState.Idle;
                    chargeTimer = 0f;
                    break;
                }

                chargeTimer = Mathf.Min(chargeTimer + Time.deltaTime, chargeTime);

                if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    Cast();
                }
                break;

            case CastState.InFlight:
                break;
        }
    }

    private void Cast()
    {
        float chargePercent = chargeTimer / chargeTime;
        float force = Mathf.Lerp(minForce, maxForce, chargePercent);

        Vector3 origin = castOrigin != null ? castOrigin.position : transform.position;
        Vector3 direction = playerCamera.transform.forward;

        Rigidbody sinker = Instantiate(sinkerPrefab, origin, Quaternion.identity);
        sinker.AddForce(direction * force, ForceMode.VelocityChange);

        Debug.Log($"Lançamento a {chargePercent:P0} de força ({force:F1})");
        state = CastState.InFlight;
    }

    // O futuro ReelSystem chama isto quando o rig for recolhido de volta
    public void ResetCast()
    {
        state = CastState.Idle;
        chargeTimer = 0f;
    }

    public float GetChargePercent() => chargeTime > 0f ? chargeTimer / chargeTime : 0f;
}