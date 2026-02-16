using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor.Animations;

/// <summary>
/// Creates a minimal scene for testing character model + animations with textures.
/// Menu: DeathByCart → Build Character Test Scene
/// 
/// PREREQUISITES: Run "DeathByCart → Setup Player Character" first to configure
/// FBX import settings (scale, materials, humanoid avatar).
/// </summary>
public class CharacterTestScene : Editor
{
    private const string ModelPath = "Assets/Models/Player/Animations/HumanCharacter.fbx";
    private const string WalkAnimPath = "Assets/Models/Player/Animations/HumanWalking.fbx";
    private const string TexturePath = "Assets/Models/Player/Animations/Human.png";
    private const string ProperControllerPath = "Assets/Animations/PlayerAnimator.controller";

    [MenuItem("DeathByCart/Build Character Test Scene")]
    public static void Build()
    {
        // ── New empty scene ──
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        scene.name = "CharacterTest";

        // ── Ground plane ──
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(5, 1, 5); // 50×50 meters

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material groundMat = new Material(litShader);
        groundMat.color = new Color(0.25f, 0.3f, 0.22f);
        ground.GetComponent<Renderer>().material = groundMat;
        ground.isStatic = true;

        // ── Better lighting ──
        Light sun = Object.FindAnyObjectByType<Light>();
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            sun.intensity = 1.5f;
            sun.color = new Color(1f, 0.95f, 0.85f);
        }

        // ── Load the character model ──
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (modelPrefab == null)
        {
            EditorUtility.DisplayDialog("Error",
                "HumanCharacter.fbx not found at:\n" + ModelPath +
                "\n\nPlease make sure the file exists.", "OK");
            return;
        }

        // Instantiate the character
        GameObject character = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
        character.name = "TestCharacter";
        character.transform.position = Vector3.zero;
        character.transform.rotation = Quaternion.identity;

        // ── Measure actual rendered bounds ──
        Bounds charBounds = MeasureBounds(character);
        float charHeight = charBounds.size.y;
        float charRadius = Mathf.Max(charBounds.extents.x, charBounds.extents.z);
        Vector3 charCenter = charBounds.center - character.transform.position;

        Debug.Log("═══════════════════════════════════════════");
        Debug.Log($"[CHAR TEST] CHARACTER HEIGHT: {charHeight:F3} units");
        Debug.Log($"[CHAR TEST] Bounds: min={charBounds.min} max={charBounds.max}");
        Debug.Log($"[CHAR TEST] Center: {charCenter}");
        Debug.Log($"[CHAR TEST] Transform scale: {character.transform.localScale}");

        // ── Size check and warning ──
        if (charHeight > 3f)
        {
            Debug.LogWarning($"[CHAR TEST] ⚠️ Character is {charHeight:F1}m tall! " +
                "This is likely a scale issue. Run 'DeathByCart → Setup Player Character' " +
                "to fix FBX import scale, then rebuild this scene.");

            // Apply emergency runtime scale fix
            float targetHeight = 1.75f;
            float scaleFactor = targetHeight / charHeight;
            character.transform.localScale = Vector3.one * scaleFactor;

            // Re-measure after scaling
            charBounds = MeasureBounds(character);
            charHeight = charBounds.size.y;
            charRadius = Mathf.Max(charBounds.extents.x, charBounds.extents.z);
            charCenter = charBounds.center - character.transform.position;

            Debug.Log($"[CHAR TEST] Applied emergency scale: {scaleFactor:F4}");
            Debug.Log($"[CHAR TEST] New height: {charHeight:F3}m");
        }
        else if (charHeight < 0.5f)
        {
            Debug.LogWarning($"[CHAR TEST] ⚠️ Character is only {charHeight:F3}m tall! " +
                "Model may be too small.");
        }
        else
        {
            Debug.Log($"[CHAR TEST] ✅ Character height looks correct: {charHeight:F2}m");
        }

        // ── Ensure character stands on the ground ──
        // Push feet above ground based on bounds
        float groundOffset = -charBounds.min.y;
        if (Mathf.Abs(groundOffset) > 0.01f)
        {
            character.transform.position = new Vector3(0, groundOffset, 0);
            Debug.Log($"[CHAR TEST] Adjusted Y by {groundOffset:F3} to stand on ground");
        }

        // ── Add CharacterController (auto-fit to bounds) ──
        // The CC must be positioned so that when it rests on the ground (Y=0),
        // transform.y = 0 (feet on floor). Formula: center.y = height/2 + skinWidth
        CharacterController cc = character.AddComponent<CharacterController>();
        cc.skinWidth = 0.08f;
        cc.height = Mathf.Max(charHeight * 0.95f, 0.5f);
        cc.radius = Mathf.Clamp(charRadius * 0.8f, 0.1f, cc.height / 4f);
        cc.center = new Vector3(0, cc.height / 2f + cc.skinWidth, 0);
        cc.minMoveDistance = 0;

        Debug.Log($"[CHAR TEST] CharacterController: height={cc.height:F2} radius={cc.radius:F2} center={cc.center}");

        // ── Apply texture/material ──
        ApplyTexture(character);

        // ── Setup Animator ──
        SetupAnimator(character);

        // ── Add movement controller ──
        character.AddComponent<SimpleCharacterMover>();

        // ── Camera setup (framed to character height) ──
        Camera cam = Camera.main;
        if (cam != null)
        {
            float camDist = charHeight * 2.5f; // Camera distance relative to character
            float camHeight = charHeight * 0.8f;
            cam.transform.position = new Vector3(0, camHeight + character.transform.position.y, camDist);
            cam.transform.LookAt(character.transform.position + Vector3.up * (charHeight * 0.5f));
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
        }

        // ── Log renderer info ──
        var renderers = character.GetComponentsInChildren<Renderer>();
        Debug.Log($"[CHAR TEST] Found {renderers.Length} renderers:");
        foreach (var r in renderers)
        {
            string meshName = r is SkinnedMeshRenderer smr ? smr.sharedMesh?.name ?? "null" : "MeshRenderer";
            string matName = r.sharedMaterial?.name ?? "NO MATERIAL";
            string texName = "none";
            if (r.sharedMaterial != null && r.sharedMaterial.mainTexture != null)
                texName = r.sharedMaterial.mainTexture.name;
            Debug.Log($"[CHAR TEST]   {r.gameObject.name}: mesh={meshName} material={matName} texture={texName}");
        }

        // ── Save scene ──
        string scenePath = "Assets/Scenes/CharacterTest.unity";
        EnsureFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);

        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("[CHAR TEST] ✅ Character Test Scene built!");
        Debug.Log("[CHAR TEST] Press Play to see the character.");
        Debug.Log("[CHAR TEST] WASD = Move | Shift = Run | Mouse = Look | ESC = Unlock cursor");
        Debug.Log("═══════════════════════════════════════════");

        EditorUtility.DisplayDialog("Done!",
            $"Character Test Scene created!\n\n" +
            $"Character height: {charHeight:F2}m\n" +
            $"Renderers found: {renderers.Length}\n\n" +
            "Press Play to see your character.\n" +
            "WASD = Move | Shift = Run | Mouse = Look", "OK");
    }

    /// <summary>
    /// Measure the total world-space bounds of all renderers in the character.
    /// </summary>
    static Bounds MeasureBounds(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("[CHAR TEST] No renderers found — can't measure bounds!");
            return new Bounds(obj.transform.position, Vector3.one);
        }

        Bounds total = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            total.Encapsulate(renderers[i].bounds);
        return total;
    }

    /// <summary>
    /// Try to apply the Human.png texture to the character's materials.
    /// If the FBX was imported with material import disabled, we create a material manually.
    /// </summary>
    static void ApplyTexture(GameObject character)
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        if (tex == null)
        {
            Debug.LogWarning("[CHAR TEST] ⚠️ Human.png texture not found at " + TexturePath);
            return;
        }

        var renderers = character.GetComponentsInChildren<Renderer>();
        bool anyFixed = false;

        foreach (var r in renderers)
        {
            if (r.sharedMaterial == null || r.sharedMaterial.mainTexture == null)
            {
                // Create a proper material with the texture
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material mat = new Material(shader);
                mat.mainTexture = tex;
                mat.name = "CharacterSkin";

                // Save the material as an asset so it persists
                EnsureFolder("Assets", "Materials");
                string matPath = "Assets/Materials/CharacterSkin.mat";
                
                // Check if already exists
                Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (existing != null)
                {
                    existing.mainTexture = tex;
                    r.sharedMaterial = existing;
                }
                else
                {
                    AssetDatabase.CreateAsset(mat, matPath);
                    r.sharedMaterial = mat;
                }

                anyFixed = true;
                Debug.Log($"[CHAR TEST] ✅ Applied texture to {r.gameObject.name}");
            }
            else
            {
                Debug.Log($"[CHAR TEST] ✅ {r.gameObject.name} already has texture: {r.sharedMaterial.mainTexture.name}");
            }
        }

        if (anyFixed)
            AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Setup the Animator — prefer the proper PlayerAnimator.controller,
    /// fall back to creating a basic one if not found.
    /// </summary>
    static void SetupAnimator(GameObject character)
    {
        // Find the Animator
        Animator anim = character.GetComponent<Animator>();
        if (anim == null)
            anim = character.GetComponentInChildren<Animator>();
        if (anim == null)
        {
            anim = character.AddComponent<Animator>();
            Debug.LogWarning("[CHAR TEST] ⚠️ No Animator found — added one manually.");
        }

        anim.applyRootMotion = false;

        // Log avatar info
        Debug.Log($"[CHAR TEST] Animator: avatar={anim.avatar?.name ?? "NONE"} isHuman={anim.isHuman}");

        // Try the proper controller first (created by CharacterSetup)
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ProperControllerPath);
        if (controller != null)
        {
            anim.runtimeAnimatorController = controller;
            Debug.Log("[CHAR TEST] ✅ Using PlayerAnimator.controller (Idle/Walk/Run/PushCart)");
            return;
        }

        Debug.LogWarning("[CHAR TEST] ⚠️ PlayerAnimator.controller not found. Creating a basic one.");
        Debug.LogWarning("[CHAR TEST] Run 'DeathByCart → Setup Player Character' for the full controller.");

        // Fallback: create a simple controller with walk animation
        AnimationClip walkClip = null;
        Object[] walkAssets = AssetDatabase.LoadAllAssetsAtPath(WalkAnimPath);
        foreach (var asset in walkAssets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                walkClip = clip;
                Debug.Log($"[CHAR TEST] Found walk clip: {clip.name} ({clip.length}s)");
                break;
            }
        }

        if (walkClip == null)
        {
            Debug.LogWarning("[CHAR TEST] ⚠️ No walk animation found — character will T-pose.");
            return;
        }

        EnsureFolder("Assets", "Animations");
        string fallbackPath = "Assets/Animations/CharTestAnimator.controller";
        AnimatorController fallback = AnimatorController.CreateAnimatorControllerAtPath(fallbackPath);
        fallback.AddParameter("Speed", AnimatorControllerParameterType.Float);

        var rootSM = fallback.layers[0].stateMachine;

        // Idle (frozen walk frame)
        AnimatorState idleState = rootSM.AddState("Idle", new Vector3(250, 0, 0));
        idleState.motion = walkClip;
        idleState.speed = 0f;
        rootSM.defaultState = idleState;

        // Walk
        AnimatorState walkState = rootSM.AddState("Walk", new Vector3(250, 80, 0));
        walkState.motion = walkClip;
        walkState.speed = 1f;

        // Idle → Walk
        var toWalk = idleState.AddTransition(walkState);
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        toWalk.hasExitTime = false;
        toWalk.duration = 0.15f;

        // Walk → Idle
        var toIdle = walkState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        toIdle.hasExitTime = false;
        toIdle.duration = 0.15f;

        EditorUtility.SetDirty(fallback);
        AssetDatabase.SaveAssets();

        anim.runtimeAnimatorController = fallback;
        Debug.Log("[CHAR TEST] ✅ Basic Idle/Walk controller created and assigned.");
    }

    static void EnsureFolder(string parent, string folder)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + folder))
            AssetDatabase.CreateFolder(parent, folder);
    }
}
