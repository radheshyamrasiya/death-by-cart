using UnityEngine;

/// <summary>
/// Handles RC car deployment and control.
/// Called by GadgetInventory when player uses an RC car gadget.
/// Attach to the Player.
/// </summary>
public class RCCarItem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float carScale = 0.4f;

    // State
    private bool isControlling;
    private RCCarController activeCar;

    // References
    private PlayerStateMachine stateMachine;
    private CameraController cameraController;
    private Transform playerTransform;

    // Public API
    public bool IsControlling => isControlling;

    private void Start()
    {
        stateMachine = GetComponent<PlayerStateMachine>();
        playerTransform = transform;

        // Find camera controller
        Camera cam = Camera.main;
        if (cam != null)
            cameraController = cam.GetComponent<CameraController>();
    }

    /// <summary>Deploy the RC car. Called by GadgetInventory.</summary>
    public void DeployRCCar()
    {
        if (isControlling) return;

        // Can't deploy while pushing cart
        var cartInteraction = GetComponent<CartInteraction>();
        if (cartInteraction != null && cartInteraction.IsAttached)
        {
            Debug.Log("[RC CAR] Let go of the cart first!");
            return;
        }

        isControlling = true;

        // Spawn RC car at player's feet
        GameObject carObj = CreateRCCarVisual();
        carObj.transform.position = playerTransform.position + playerTransform.forward * 1.5f;
        carObj.transform.rotation = playerTransform.rotation;

        activeCar = carObj.AddComponent<RCCarController>();
        activeCar.OnCarDestroyed += OnRCCarDestroyed;
        activeCar.Activate();

        // Freeze player
        if (stateMachine != null)
            stateMachine.TransitionTo(PlayerStateMachine.PlayerState.ControllingRC);

        // Disable player movement components so WASD only drives the car
        var tpc = GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null) tpc.enabled = false;
        var playerCC = GetComponent<CharacterController>();
        if (playerCC != null) playerCC.enabled = false;

        // Switch camera to RC car
        if (cameraController != null)
            cameraController.SetTarget(carObj.transform);

        Debug.Log("[RC CAR] Deployed! WASD to drive, V to cancel.");
    }

    /// <summary>Cancel RC car control early. Called by GadgetInventory.</summary>
    public void CancelRCCar()
    {
        if (!isControlling || activeCar == null) return;
        activeCar.CancelByPlayer();
    }

    private void OnRCCarDestroyed()
    {
        isControlling = false;
        activeCar = null;

        // Return camera to player
        if (cameraController != null)
            cameraController.SetTarget(playerTransform);

        // Re-enable player movement components
        var tpc = GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null) tpc.enabled = true;
        var playerCC = GetComponent<CharacterController>();
        if (playerCC != null) playerCC.enabled = true;

        // Unfreeze player
        if (stateMachine != null)
            stateMachine.TransitionTo(PlayerStateMachine.PlayerState.FreeRoam);

        Debug.Log("[RC CAR] Control returned to player.");
    }

    private GameObject CreateRCCarVisual()
    {
        // Main body — small box
        GameObject car = new GameObject("RCCar");

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(car.transform);
        body.transform.localScale = new Vector3(0.5f, 0.25f, 0.8f) * carScale;
        body.transform.localPosition = new Vector3(0, 0.15f, 0);

        // Remove collider from visual (CharacterController handles collision)
        Object.Destroy(body.GetComponent<Collider>());

        // Color it bright orange
        Renderer rend = body.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(1f, 0.5f, 0f); // Orange
            rend.material = mat;
        }

        // Antenna — thin pole
        GameObject antenna = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        antenna.name = "Antenna";
        antenna.transform.SetParent(car.transform);
        antenna.transform.localScale = new Vector3(0.03f, 0.3f, 0.03f) * carScale;
        antenna.transform.localPosition = new Vector3(-0.08f * carScale, 0.4f * carScale, -0.2f * carScale);
        Object.Destroy(antenna.GetComponent<Collider>());

        Renderer antRend = antenna.GetComponent<Renderer>();
        if (antRend != null)
        {
            Material antMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            antMat.color = Color.black;
            antRend.material = antMat;
        }

        // Wheels — 4 small cylinders
        float wheelOffset = 0.18f * carScale;
        float wheelForward = 0.25f * carScale;
        Vector3[] wheelPositions = {
            new Vector3(-wheelOffset, 0.06f, wheelForward),
            new Vector3(wheelOffset, 0.06f, wheelForward),
            new Vector3(-wheelOffset, 0.06f, -wheelForward),
            new Vector3(wheelOffset, 0.06f, -wheelForward)
        };

        foreach (var wPos in wheelPositions)
        {
            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "Wheel";
            wheel.transform.SetParent(car.transform);
            wheel.transform.localScale = new Vector3(0.08f, 0.04f, 0.08f) * carScale;
            wheel.transform.localPosition = wPos;
            wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
            Object.Destroy(wheel.GetComponent<Collider>());

            Renderer wRend = wheel.GetComponent<Renderer>();
            if (wRend != null)
            {
                Material wMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                wMat.color = new Color(0.2f, 0.2f, 0.2f);
                wRend.material = wMat;
            }
        }

        // Blinking light on top (small red sphere)
        GameObject light = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        light.name = "Light";
        light.transform.SetParent(car.transform);
        light.transform.localScale = Vector3.one * 0.06f * carScale;
        light.transform.localPosition = new Vector3(0, 0.32f * carScale, 0.15f * carScale);
        Object.Destroy(light.GetComponent<Collider>());

        Renderer lRend = light.GetComponent<Renderer>();
        if (lRend != null)
        {
            Material lMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            lMat.color = Color.red;
            lMat.EnableKeyword("_EMISSION");
            lMat.SetColor("_EmissionColor", Color.red * 3f);
            lRend.material = lMat;
        }

        return car;
    }
}
