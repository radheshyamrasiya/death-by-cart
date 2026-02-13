using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

/// <summary>
/// One-click scene builder. Go to: DeathByCart → Build Test Scene
/// Creates the ground, cart, camera, obstacles, and lighting.
/// </summary>
public class SceneBuilder : EditorWindow
{
    [MenuItem("DeathByCart/Build Test Scene")]
    public static void BuildTestScene()
    {
        // --- Create a new scene ---
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ==================== GROUND ====================
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(10f, 1f, 10f); // 100x100m

        // Ground material - dark grey-green supermarket floor
        Material groundMat = new Material(GetDefaultLitShader());
        groundMat.color = new Color(0.25f, 0.28f, 0.22f); // dull olive-grey
        ground.GetComponent<Renderer>().material = groundMat;

        // Ground physics material (zero bounce)
        PhysicsMaterial groundPhysMat = new PhysicsMaterial("GroundPhysics");
        groundPhysMat.bounciness = 0f;
        groundPhysMat.dynamicFriction = 0.6f;
        groundPhysMat.staticFriction = 0.6f;
        groundPhysMat.bounceCombine = PhysicsMaterialCombine.Minimum;
        ground.GetComponent<MeshCollider>().material = groundPhysMat;

        // Save material assets
        EnsureFolder("Assets/Materials");
        AssetDatabase.CreateAsset(groundMat, "Assets/Materials/GroundFloor.mat");
        AssetDatabase.CreateAsset(groundPhysMat, "Assets/Materials/GroundPhysics.physicMaterial");

        // ==================== WALLS (boundary) ====================
        CreateWall("WallNorth", new Vector3(0, 2.5f, 50), new Vector3(100, 5, 1));
        CreateWall("WallSouth", new Vector3(0, 2.5f, -50), new Vector3(100, 5, 1));
        CreateWall("WallEast",  new Vector3(50, 2.5f, 0),  new Vector3(1, 5, 100));
        CreateWall("WallWest",  new Vector3(-50, 2.5f, 0), new Vector3(1, 5, 100));

        // ==================== CART ====================
        GameObject cart = new GameObject("Cart");
        cart.transform.position = new Vector3(0, 0.4f, 0); // Sits on ground (half collider height)

        // Physics Material (zero bounce — prevents bouncing on ground)
        PhysicsMaterial cartPhysMat = new PhysicsMaterial("CartPhysics");
        cartPhysMat.bounciness = 0f;
        cartPhysMat.dynamicFriction = 0.6f;
        cartPhysMat.staticFriction = 0.6f;
        cartPhysMat.bounceCombine = PhysicsMaterialCombine.Minimum;
        cartPhysMat.frictionCombine = PhysicsMaterialCombine.Average;
        AssetDatabase.CreateAsset(cartPhysMat, "Assets/Materials/CartPhysics.physicMaterial");

        // Rigidbody
        Rigidbody rb = cart.AddComponent<Rigidbody>();
        rb.mass = 5f;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth visual movement
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // No clipping through ground
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Box Collider with physics material
        BoxCollider col = cart.AddComponent<BoxCollider>();
        col.size = new Vector3(1f, 0.8f, 2f);
        col.center = new Vector3(0f, 0.4f, 0f);
        col.material = cartPhysMat;

        // Cart scripts
        cart.AddComponent<CartController>();
        cart.AddComponent<CartInventory>();
        cart.AddComponent<CartDebugHUD>();

        // Cart Body (visual)
        GameObject cartBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cartBody.name = "CartBody";
        cartBody.transform.SetParent(cart.transform);
        cartBody.transform.localPosition = new Vector3(0, 0.4f, 0);
        cartBody.transform.localScale = new Vector3(1f, 0.8f, 2f);
        Object.DestroyImmediate(cartBody.GetComponent<BoxCollider>()); // parent has the collider

        Material cartMat = new Material(GetDefaultLitShader());
        cartMat.color = new Color(0.7f, 0.7f, 0.75f); // metallic grey
        cartBody.GetComponent<Renderer>().material = cartMat;
        AssetDatabase.CreateAsset(cartMat, "Assets/Materials/CartMetal.mat");

        // Front indicator (so you know which way is forward)
        GameObject frontIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frontIndicator.name = "FrontIndicator";
        frontIndicator.transform.SetParent(cart.transform);
        frontIndicator.transform.localPosition = new Vector3(0, 0.5f, 1.2f);
        frontIndicator.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        Object.DestroyImmediate(frontIndicator.GetComponent<BoxCollider>());

        Material frontMat = new Material(GetDefaultLitShader());
        frontMat.color = Color.red;
        frontIndicator.GetComponent<Renderer>().material = frontMat;
        AssetDatabase.CreateAsset(frontMat, "Assets/Materials/FrontIndicator.mat");

        // ==================== PLAYER (Starter Assets) ====================
        // Load the Starter Assets prefabs
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerCapsule.prefab");
        GameObject cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerFollowCamera.prefab");

        GameObject player;
        if (playerPrefab != null)
        {
            player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.name = "Player";
            player.transform.position = new Vector3(0, 0.1f, -3f);

            // Add our custom scripts on top of Starter Assets
            if (player.GetComponent<PlayerStateMachine>() == null)
                player.AddComponent<PlayerStateMachine>();
            if (player.GetComponent<CartInteraction>() == null)
                player.AddComponent<CartInteraction>();

            // Set ground layer for grounded check
            var tpc = player.GetComponent<StarterAssets.ThirdPersonController>();
            if (tpc != null)
            {
                tpc.GroundLayers = LayerMask.GetMask("Default");
            }

            Debug.Log("[SCENE] ✅ Starter Assets player spawned!");
        }
        else
        {
            // Fallback: create basic player if prefab not found
            Debug.LogWarning("[SCENE] ⚠️ Starter Assets PlayerCapsule.prefab not found! Creating basic player.");
            player = new GameObject("Player");
            player.transform.position = new Vector3(0, 0.1f, -3f);
            CharacterController playerCC = player.AddComponent<CharacterController>();
            playerCC.height = 2f;
            playerCC.radius = 0.4f;
            playerCC.center = new Vector3(0, 1f, 0);
            player.AddComponent<PlayerMotor>();
            player.AddComponent<PlayerStateMachine>();
            player.AddComponent<CartInteraction>();

            GameObject playerBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerBody.name = "PlayerBody";
            playerBody.transform.SetParent(player.transform);
            playerBody.transform.localPosition = new Vector3(0, 1f, 0);
            playerBody.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            Object.DestroyImmediate(playerBody.GetComponent<CapsuleCollider>());
        }

        // ==================== CAMERA (Cinemachine) ====================
        // Delete default Main Camera (Starter Assets camera prefab replaces it)
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Object.DestroyImmediate(mainCam.gameObject);
        }

        if (cameraPrefab != null)
        {
            GameObject followCam = (GameObject)PrefabUtility.InstantiatePrefab(cameraPrefab);
            followCam.name = "PlayerFollowCamera";
            Debug.Log("[SCENE] ✅ Cinemachine follow camera spawned!");
        }
        else
        {
            Debug.LogWarning("[SCENE] ⚠️ PlayerFollowCamera.prefab not found! No camera created.");
        }

        // ==================== OBSTACLE CUBES ====================
        Material obstacleMat = new Material(GetDefaultLitShader());
        obstacleMat.color = new Color(0.85f, 0.55f, 0.2f); // orange-brown cardboard
        AssetDatabase.CreateAsset(obstacleMat, "Assets/Materials/ObstacleBox.mat");

        // Scatter obstacles at various positions
        Vector3[] obstaclePositions = new Vector3[]
        {
            new Vector3(5, 0.5f, 8),
            new Vector3(-4, 0.5f, 12),
            new Vector3(10, 0.5f, 3),
            new Vector3(-8, 1f, -5),
            new Vector3(3, 0.5f, -10),
            new Vector3(-12, 0.5f, 6),
            new Vector3(7, 1f, -7),
            new Vector3(-3, 0.5f, 15),
            new Vector3(15, 0.5f, -3),
            new Vector3(0, 0.5f, 20),
        };

        Vector3[] obstacleSizes = new Vector3[]
        {
            new Vector3(1, 1, 1),
            new Vector3(2, 1, 1),
            new Vector3(1, 2, 1),
            new Vector3(1.5f, 1, 1.5f),
            new Vector3(1, 1, 2),
            new Vector3(2, 1.5f, 1),
            new Vector3(1, 1, 1),
            new Vector3(1.5f, 1, 1),
            new Vector3(1, 1.5f, 1.5f),
            new Vector3(2, 1, 2),
        };

        for (int i = 0; i < obstaclePositions.Length; i++)
        {
            GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obs.name = $"Obstacle_{i + 1}";
            obs.transform.position = obstaclePositions[i];
            obs.transform.localScale = obstacleSizes[i];

            Rigidbody obsRb = obs.AddComponent<Rigidbody>();
            obsRb.mass = 1f;
            obsRb.useGravity = true;

            obs.GetComponent<Renderer>().material = obstacleMat;
        }

        // ==================== LIGHTING ====================
        // The default scene already has a directional light, let's boost it
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.intensity = 1.5f;
                light.color = new Color(1f, 0.95f, 0.85f); // warm
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
            }
        }

        // ==================== SAVE ====================
        EnsureFolder("Assets/Scenes");
        EditorSceneManager.SaveScene(newScene, "Assets/Scenes/TestGround.unity");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("✅ Test Scene built! Hit Play to test.");
        EditorUtility.DisplayDialog("Death By Cart", 
            "Test scene built successfully!\n\n" +
            "Controls:\n" +
            "  WASD - Move (player or cart)\n" +
            "  E - Grab / Release cart\n" +
            "  Shift - Sprint\n" +
            "  Ctrl - Sneak (on cart)\n" +
            "  C - Cycle camera mode\n" +
            "  U - Add item to cart\n" +
            "  I - Remove item from cart\n\n" +
            "Walk to cart, press E to grab it!\n" +
            "Hit Play to test!", "Let's Go! 🛒");
    }

    private static void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.3f);
        // Walls are static — no Rigidbody
    }

    private static Shader GetDefaultLitShader()
    {
        // Try URP Lit shader first, fall back to Standard
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        return shader;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
