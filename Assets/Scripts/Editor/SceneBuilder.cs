using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.AI;
using Unity.AI.Navigation;

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

        // NavMesh surface for zombie pathfinding — auto-bake!
        NavMeshSurface navSurface = ground.AddComponent<NavMeshSurface>();
        navSurface.collectObjects = CollectObjects.All;
        navSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        navSurface.BuildNavMesh();
        Debug.Log("[SCENE] ✅ NavMesh baked automatically on Ground!");

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

        // Grid inventory system (RE4-style)
        cart.AddComponent<GridInventory>();
        cart.AddComponent<InventoryUI>();
        cart.AddComponent<InventoryManager>();
        cart.AddComponent<ItemDataLibrary>();
        cart.AddComponent<ItemSpawner>();

        // Cart Body (visual — use Cart.fbx if available, fallback to primitives)
        GameObject cartModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Cart/Cart.fbx");
        if (cartModel != null)
        {
            GameObject cartVisual = (GameObject)PrefabUtility.InstantiatePrefab(cartModel);
            cartVisual.name = "CartModel";
            cartVisual.transform.SetParent(cart.transform);
            cartVisual.transform.localPosition = new Vector3(0, 0.3f, 0);
            cartVisual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            cartVisual.transform.localScale = new Vector3(45.7f, 45.7f, 45.7f);
            // Remove any colliders from the FBX — parent has the physics collider
            foreach (var extraCol in cartVisual.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(extraCol);
            }
            Debug.Log("[SCENE] ✅ Cart.fbx model loaded!");
        }
        else
        {
            Debug.LogWarning("[SCENE] ⚠️ Cart.fbx not found at Assets/Cart/Cart.fbx — using primitive cube.");
            GameObject cartBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cartBody.name = "CartBody";
            cartBody.transform.SetParent(cart.transform);
            cartBody.transform.localPosition = new Vector3(0, 0.4f, 0);
            cartBody.transform.localScale = new Vector3(1f, 0.8f, 2f);
            Object.DestroyImmediate(cartBody.GetComponent<BoxCollider>());

            Material cartMat = new Material(GetDefaultLitShader());
            cartMat.color = new Color(0.7f, 0.7f, 0.75f);
            cartBody.GetComponent<Renderer>().material = cartMat;
            AssetDatabase.CreateAsset(cartMat, "Assets/Materials/CartMetal.mat");

            // Front indicator
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
        }

        // ==================== PLAYER (Starter Assets) ====================
        // Load the Starter Assets prefabs
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerCapsule.prefab");

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
            if (player.GetComponent<StaminaSystem>() == null)
                player.AddComponent<StaminaSystem>();
            if (player.GetComponent<StaminaBarUI>() == null)
                player.AddComponent<StaminaBarUI>();
            if (player.GetComponent<PlayerStaminaBridge>() == null)
                player.AddComponent<PlayerStaminaBridge>();
            if (player.GetComponent<ItemPickup>() == null)
                player.AddComponent<ItemPickup>();
            if (player.GetComponent<HealthSystem>() == null)
                player.AddComponent<HealthSystem>();
            if (player.GetComponent<HealthBarUI>() == null)
                player.AddComponent<HealthBarUI>();
            if (player.GetComponent<PlayerCrouch>() == null)
                player.AddComponent<PlayerCrouch>();
            if (player.GetComponent<RCCarItem>() == null)
                player.AddComponent<RCCarItem>();
            if (player.GetComponent<GadgetInventory>() == null)
                player.AddComponent<GadgetInventory>();

            // Set ground layer for grounded check
            var tpc = player.GetComponent<StarterAssets.ThirdPersonController>();
            if (tpc != null)
            {
                tpc.GroundLayers = LayerMask.GetMask("Default");
                tpc.LockCameraPosition = true;
                tpc.MoveSpeed = 10f;    // Match cart non-sprint max
                tpc.SprintSpeed = 20f;  // Match cart sprint max
            }

            Debug.Log("[SCENE] ✅ Starter Assets player spawned with stamina + health!");
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
            player.AddComponent<StaminaSystem>();
            player.AddComponent<StaminaBarUI>();
            player.AddComponent<PlayerStaminaBridge>();
            player.AddComponent<ItemPickup>();
            player.AddComponent<HealthSystem>();
            player.AddComponent<HealthBarUI>();
            player.AddComponent<PlayerCrouch>();
            player.AddComponent<RCCarItem>();
            player.AddComponent<GadgetInventory>();

            GameObject playerBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerBody.name = "PlayerBody";
            playerBody.transform.SetParent(player.transform);
            playerBody.transform.localPosition = new Vector3(0, 1f, 0);
            playerBody.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            Object.DestroyImmediate(playerBody.GetComponent<CapsuleCollider>());
        }

        // ==================== CAMERA ====================
        // Keep the default Main Camera and add our CameraController.
        // The ThirdPersonController needs a "MainCamera"-tagged camera to exist
        // for camera-relative movement to work.
        Camera existingCam = Camera.main;
        if (existingCam != null)
        {
            // Ensure MainCamera tag
            existingCam.gameObject.tag = "MainCamera";

            // Add our camera controller targeting the player
            if (existingCam.gameObject.GetComponent<CameraController>() == null)
            {
                CameraController camCtrl = existingCam.gameObject.AddComponent<CameraController>();
                SerializedObject so = new SerializedObject(camCtrl);
                SerializedProperty targetProp = so.FindProperty("target");
                targetProp.objectReferenceValue = player.transform;
                so.ApplyModifiedProperties();
            }
            Debug.Log("[SCENE] ✅ Main Camera configured with CameraController targeting player.");
        }
        else
        {
            Debug.LogWarning("[SCENE] ⚠️ No Main Camera found in scene!");
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

        // ==================== RC CAR PICKUP (WHITE CUBE) ====================
        {
            GameObject rcPickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rcPickup.name = "RCCar_Pickup";
            rcPickup.transform.position = new Vector3(4f, 0.5f, 4f);
            rcPickup.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            // White material
            Renderer rcRend = rcPickup.GetComponent<Renderer>();
            Material rcMat = new Material(GetDefaultLitShader());
            rcMat.color = Color.white;
            rcRend.material = rcMat;

            // Create ItemData at runtime for the RC car
            ItemData rcItemData = ScriptableObject.CreateInstance<ItemData>();
            rcItemData.itemName = "RC Car";
            rcItemData.width = 1;
            rcItemData.height = 1;
            rcItemData.weight = 0.5f;
            rcItemData.type = ItemData.ItemType.Gadget;
            rcItemData.itemColor = Color.white;
            rcItemData.description = "Remote-controlled distraction car. Press V to deploy.";
            EnsureFolder("Assets/Items");
            AssetDatabase.CreateAsset(rcItemData, "Assets/Items/RCCar.asset");

            // Add WorldItem
            WorldItem rcWorld = rcPickup.AddComponent<WorldItem>();
            rcWorld.itemData = rcItemData;

            Debug.Log("[SCENE] ✅ RC Car pickup (white cube) spawned at (4, 0.5, 4)");
        }

        // ==================== FART BOMB PICKUPS (GREEN CUBES) ====================
        {
            // Create shared ItemData for Fart Bombs
            ItemData fartBombData = ScriptableObject.CreateInstance<ItemData>();
            fartBombData.itemName = "Fart Bomb";
            fartBombData.width = 1;
            fartBombData.height = 1;
            fartBombData.weight = 0.3f;
            fartBombData.type = ItemData.ItemType.Gadget;
            fartBombData.itemColor = new Color(0.2f, 0.8f, 0.1f);
            fartBombData.description = "Remote fart bomb. Place it, then detonate from anywhere!";
            EnsureFolder("Assets/Items");
            AssetDatabase.CreateAsset(fartBombData, "Assets/Items/FartBomb.asset");

            // Light green cube
            GameObject fb1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fb1.name = "FartBomb_Pickup_1";
            fb1.transform.position = new Vector3(-5f, 0.5f, 6f);
            fb1.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Renderer fb1Rend = fb1.GetComponent<Renderer>();
            Material fb1Mat = new Material(GetDefaultLitShader());
            fb1Mat.color = new Color(0.4f, 0.9f, 0.3f); // Light green
            fb1Rend.material = fb1Mat;
            WorldItem fb1World = fb1.AddComponent<WorldItem>();
            fb1World.itemData = fartBombData;

            // Dark green cube
            GameObject fb2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fb2.name = "FartBomb_Pickup_2";
            fb2.transform.position = new Vector3(8f, 0.5f, -4f);
            fb2.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Renderer fb2Rend = fb2.GetComponent<Renderer>();
            Material fb2Mat = new Material(GetDefaultLitShader());
            fb2Mat.color = new Color(0.1f, 0.45f, 0.05f); // Dark green
            fb2Rend.material = fb2Mat;
            WorldItem fb2World = fb2.AddComponent<WorldItem>();
            fb2World.itemData = fartBombData;

            Debug.Log("[SCENE] ✅ Fart Bomb pickups spawned (light green + dark green)");
        }

        // ==================== STUN MINE PICKUP (BLUE CUBE) ====================
        {
            ItemData stunMineData = ScriptableObject.CreateInstance<ItemData>();
            stunMineData.itemName = "Stun Mine";
            stunMineData.width = 1;
            stunMineData.height = 1;
            stunMineData.weight = 0.4f;
            stunMineData.type = ItemData.ItemType.Gadget;
            stunMineData.itemColor = new Color(0.3f, 0.5f, 1f);
            stunMineData.description = "Proximity stun mine. Stuns nearby zombies for 2 seconds.";
            EnsureFolder("Assets/Items");
            AssetDatabase.CreateAsset(stunMineData, "Assets/Items/StunMine.asset");

            GameObject smPickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
            smPickup.name = "StunMine_Pickup";
            smPickup.transform.position = new Vector3(6f, 0.5f, 10f);
            smPickup.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Renderer smRend = smPickup.GetComponent<Renderer>();
            Material smMat = new Material(GetDefaultLitShader());
            smMat.color = new Color(0.2f, 0.4f, 0.9f); // Blue
            smRend.material = smMat;
            WorldItem smWorld = smPickup.AddComponent<WorldItem>();
            smWorld.itemData = stunMineData;

            Debug.Log("[SCENE] ✅ Stun Mine pickup (blue cube) spawned at (6, 0.5, 10)");
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

        // ==================== NOISE + ZOMBIES ====================
        // Add NoiseSystem and ZombieSpawner to cart
        if (cart.GetComponent<NoiseSystem>() == null)
            cart.AddComponent<NoiseSystem>();
        if (cart.GetComponent<ZombieSpawner>() == null)
            cart.AddComponent<ZombieSpawner>();

        Debug.Log("[SCENE] ✅ NoiseSystem + ZombieSpawner added to cart!");
        EditorUtility.DisplayDialog("Death By Cart", 
            "Test scene built successfully!\n\n" +
            "Controls:\n" +
            "  WASD - Move (player or cart)\n" +
            "  E - Grab / Release cart\n" +
            "  F - Pick up / Deposit items\n" +
            "  G - Drop carried item\n" +
            "  Tab - Open inventory\n" +
            "  Shift - Sprint\n" +
            "  Ctrl - Sneak (on cart)\n\n" +
            "Zombies will chase you — sneak to survive!\n" +
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
