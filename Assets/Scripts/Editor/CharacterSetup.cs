using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Editor tool to set up ALL character animations from multiple packs
/// and create a comprehensive Animator Controller.
/// Menu: DeathByCart → Setup Player Character
///
/// Animation Sources:
///   - Action Adventure Pack (Assets/Models/ActionAdventurePack/)
///   - Locomotion Pack (Assets/Models/LocomotionPack/)
///   - Original animations (Assets/Models/Player/Animations/)
/// </summary>
public class CharacterSetup : Editor
{
    // ── Folder paths ──
    private const string AnimFolder = "Assets/Models/Player/Animations";
    private const string AAFolder   = "Assets/Models/ActionAdventurePack";
    private const string LocoFolder = "Assets/Models/LocomotionPack";
    private const string PlayerModelPath = AnimFolder + "/HumanCharacter.fbx";
    private const string AnimControllerPath = "Assets/Animations/PlayerAnimator.controller";

    // ── All animation FBX paths (across all packs) ──
    private static readonly string[] AnimPaths = new string[]
    {
        // Action Adventure Pack — core movement
        AAFolder + "/idle.fbx",
        AAFolder + "/walking.fbx",
        AAFolder + "/running.fbx",
        AAFolder + "/jumping up.fbx",
        AAFolder + "/falling idle.fbx",
        AAFolder + "/falling to roll.fbx",
        AAFolder + "/run to stop.fbx",

        // Action Adventure Pack — crouch
        AAFolder + "/crouched sneaking left.fbx",
        AAFolder + "/stand to cover.fbx",
        AAFolder + "/cover to stand.fbx",

        // Locomotion Pack — strafing
        LocoFolder + "/left strafe walking.fbx",
        LocoFolder + "/right strafe walking.fbx",

        // Original — crouch idle, backward movement, items
        AnimFolder + "/Character_01@Crouching Idle.fbx",
        AnimFolder + "/Character_01@Walking Backwards.fbx",
        AnimFolder + "/Character_01@Running Backward.fbx",
        AnimFolder + "/Character_01@Pick Up Item Running.fbx",
        AnimFolder + "/Character_01@Taking Item.fbx",
    };

    // Clips that should loop
    private static readonly string[] LoopingAnimNames = new string[]
    {
        "idle", "Idle",
        "walking", "Walking",
        "running", "Running",
        "falling idle",
        "Crouching Idle", "crouching idle",
        "crouched sneaking", "Crouched Walking",
        "Walking Backwards", "Running Backward",
        "strafe walking", "strafe",
    };

    [MenuItem("DeathByCart/Setup Player Character")]
    public static void SetupPlayerCharacter()
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("[CHARACTER SETUP] Starting full character setup...");

        // Step 1: Configure the main model (with skin)
        ConfigureModelImport(PlayerModelPath, isSkinned: true, shouldLoop: false);

        // Step 2: Configure all animation FBX files
        int configured = 0;
        foreach (string path in AnimPaths)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) == null)
            {
                Debug.LogWarning($"[CHARACTER SETUP] ⚠️ Not found: {path}");
                continue;
            }

            bool shouldLoop = false;
            foreach (string loopName in LoopingAnimNames)
            {
                if (path.ToLower().Contains(loopName.ToLower()))
                {
                    shouldLoop = true;
                    break;
                }
            }

            ConfigureModelImport(path, isSkinned: false, shouldLoop: shouldLoop);
            configured++;
        }

        Debug.Log($"[CHARACTER SETUP] ✅ Configured {configured} animation files");

        // Step 3: Create the full Animator Controller
        CreateAnimatorController();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("═══════════════════════════════════════════");
        Debug.Log($"[CHARACTER SETUP] ✅ ALL DONE! {configured} animations + Animator Controller ready.");
        Debug.Log("[CHARACTER SETUP] Now run DeathByCart → Build Character Test Scene");
        Debug.Log("═══════════════════════════════════════════");

        EditorUtility.DisplayDialog("Character Setup Complete!",
            $"Configured {configured} animation files\n" +
            "Animator Controller created at:\n" +
            AnimControllerPath + "\n\n" +
            "Now run: DeathByCart → Build Character Test Scene", "OK");
    }

    /// <summary>Configure a Mixamo FBX for humanoid animation.</summary>
    static void ConfigureModelImport(string assetPath, bool isSkinned, bool shouldLoop)
    {
        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[CHARACTER SETUP] ⚠️ Could not find: {assetPath}");
            return;
        }

        // Scale fix: Mixamo exports in cm, Unity uses meters
        importer.globalScale = 0.01f;
        importer.useFileScale = false;

        // Materials (only for main model)
        if (isSkinned)
        {
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        }

        // Humanoid rig
        importer.animationType = ModelImporterAnimationType.Human;

        if (isSkinned)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        }
        else
        {
            // Animation files copy avatar from the main model
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            var avatars = AssetDatabase.LoadAllAssetsAtPath(PlayerModelPath);
            foreach (var asset in avatars)
            {
                if (asset is Avatar avatar)
                {
                    importer.sourceAvatar = avatar;
                    break;
                }
            }
        }

        // Clip loop & root motion settings
        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;

        if (clips != null && clips.Length > 0)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].loopTime = shouldLoop;
                clips[i].loopPose = shouldLoop;
                clips[i].lockRootRotation = true;
                clips[i].lockRootHeightY = true;
                clips[i].lockRootPositionXZ = true;
            }
            importer.clipAnimations = clips;
        }

        importer.SaveAndReimport();
        string loopLabel = shouldLoop ? "🔄 LOOP" : "▶️ ONCE";
        Debug.Log($"[CHARACTER SETUP] ✅ {System.IO.Path.GetFileName(assetPath)} [{loopLabel}]");
    }

    /// <summary>Load the first AnimationClip from an FBX file.</summary>
    static AnimationClip LoadClip(string fbxPath)
    {
        if (string.IsNullOrEmpty(fbxPath)) return null;
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        if (assets == null) return null;

        foreach (var asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        }
        return null;
    }

    /// <summary>Helper to create a transition between two animator states.</summary>
    static AnimatorStateTransition MakeTransition(AnimatorState from, AnimatorState to,
        AnimatorConditionMode mode, float threshold, string param, float duration = 0.15f)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.AddCondition(mode, threshold, param);
        t.hasExitTime = false;
        t.duration = duration;
        t.canTransitionToSelf = false;
        return t;
    }

    /// <summary>Helper: make a transition with TWO conditions.</summary>
    static AnimatorStateTransition MakeTransition2(AnimatorState from, AnimatorState to,
        AnimatorConditionMode mode1, float threshold1, string param1,
        AnimatorConditionMode mode2, float threshold2, string param2,
        float duration = 0.15f)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.AddCondition(mode1, threshold1, param1);
        t.AddCondition(mode2, threshold2, param2);
        t.hasExitTime = false;
        t.duration = duration;
        t.canTransitionToSelf = false;
        return t;
    }

    /// <summary>Create the full Animator Controller with all states and transitions.</summary>
    static void CreateAnimatorController()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(AnimControllerPath);

        // ══════════════════════════════════════
        //  PARAMETERS
        // ══════════════════════════════════════
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Bool);
        controller.AddParameter("FreeFall", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsCrouching", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsPushing", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsHiding", AnimatorControllerParameterType.Bool);
        controller.AddParameter("PickUp", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("TakeItem", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        // ══════════════════════════════════════
        //  LOAD ALL CLIPS
        // ══════════════════════════════════════

        // Action Adventure Pack
        AnimationClip idleClip       = LoadClip(AAFolder + "/idle.fbx");
        AnimationClip walkClip       = LoadClip(AAFolder + "/walking.fbx");
        AnimationClip runClip        = LoadClip(AAFolder + "/running.fbx");
        AnimationClip jumpClip       = LoadClip(AAFolder + "/jumping up.fbx");
        AnimationClip fallClip       = LoadClip(AAFolder + "/falling idle.fbx");
        AnimationClip landClip       = LoadClip(AAFolder + "/falling to roll.fbx");
        AnimationClip crouchWalkClip = LoadClip(AAFolder + "/crouched sneaking left.fbx");
        AnimationClip pushClip       = LoadClip(AAFolder + "/walking.fbx"); // Push = slow walk

        // Locomotion Pack
        AnimationClip strafeLeftClip  = LoadClip(LocoFolder + "/left strafe walking.fbx");
        AnimationClip strafeRightClip = LoadClip(LocoFolder + "/right strafe walking.fbx");

        // Original collection
        AnimationClip crouchIdleClip = LoadClip(AnimFolder + "/Character_01@Crouching Idle.fbx");
        AnimationClip walkBackClip   = LoadClip(AnimFolder + "/Character_01@Walking Backwards.fbx");
        AnimationClip runBackClip    = LoadClip(AnimFolder + "/Character_01@Running Backward.fbx");
        AnimationClip pickUpClip     = LoadClip(AnimFolder + "/Character_01@Pick Up Item Running.fbx");
        AnimationClip takeItemClip   = LoadClip(AnimFolder + "/Character_01@Taking Item.fbx");

        // Log what we found
        string[] clipNames = { "Idle", "Walk", "Run", "Jump", "Fall", "Land",
            "CrouchIdle", "CrouchWalk", "StrafeL", "StrafeR",
            "WalkBack", "RunBack", "PickUp", "TakeItem", "Push" };
        AnimationClip[] clips = { idleClip, walkClip, runClip, jumpClip, fallClip, landClip,
            crouchIdleClip, crouchWalkClip, strafeLeftClip, strafeRightClip,
            walkBackClip, runBackClip, pickUpClip, takeItemClip, pushClip };
        for (int i = 0; i < clipNames.Length; i++)
        {
            string status = clips[i] != null ? clips[i].name : "MISSING";
            Debug.Log($"[ANIM] {clipNames[i]}: {status}");
        }

        // ══════════════════════════════════════
        //  STATES
        // ══════════════════════════════════════
        float col1 = 250f, col2 = 500f, col3 = 750f;

        // -- Ground movement --
        AnimatorState idle = sm.AddState("Idle", new Vector3(col1, 0, 0));
        idle.motion = idleClip;
        sm.defaultState = idle;

        AnimatorState walk = sm.AddState("Walk", new Vector3(col1, 80, 0));
        walk.motion = walkClip;

        AnimatorState run = sm.AddState("Run", new Vector3(col1, 160, 0));
        run.motion = runClip;

        // -- Crouch --
        AnimatorState crouchIdle = sm.AddState("CrouchIdle", new Vector3(col2, 0, 0));
        crouchIdle.motion = crouchIdleClip ?? idleClip;

        AnimatorState crouchWalk = sm.AddState("CrouchWalk", new Vector3(col2, 80, 0));
        crouchWalk.motion = crouchWalkClip ?? walkClip;
        if (crouchWalkClip == null && walkClip != null) crouchWalk.speed = 0.5f;

        // -- Jump / Fall / Land --
        AnimatorState jumpUp = sm.AddState("JumpUp", new Vector3(col3, 0, 0));
        jumpUp.motion = jumpClip;

        AnimatorState falling = sm.AddState("Falling", new Vector3(col3, 80, 0));
        falling.motion = fallClip;

        AnimatorState landing = sm.AddState("Landing", new Vector3(col3, 160, 0));
        landing.motion = landClip;

        // -- Push Cart --
        AnimatorState push = sm.AddState("PushCart", new Vector3(col1, 320, 0));
        push.motion = pushClip ?? walkClip;
        push.speed = 0.7f;

        // -- Item Interactions --
        AnimatorState pickUp = sm.AddState("PickUpItem", new Vector3(col2, 240, 0));
        pickUp.motion = pickUpClip;

        AnimatorState takeItem = sm.AddState("TakingItem", new Vector3(col2, 320, 0));
        takeItem.motion = takeItemClip;

        // -- Backward movement (future use) --
        AnimatorState walkBack = sm.AddState("WalkBackward", new Vector3(col1, 240, 0));
        walkBack.motion = walkBackClip ?? walkClip;

        // -- Strafing (future use) --
        AnimatorState strafeL = sm.AddState("StrafeLeft", new Vector3(col3, 240, 0));
        strafeL.motion = strafeLeftClip ?? walkClip;

        AnimatorState strafeR = sm.AddState("StrafeRight", new Vector3(col3, 320, 0));
        strafeR.motion = strafeRightClip ?? walkClip;

        // ══════════════════════════════════════
        //  TRANSITIONS — Ground Movement
        // ══════════════════════════════════════

        // Idle ↔ Walk (Speed 0.1)
        MakeTransition(idle, walk, AnimatorConditionMode.Greater, 0.1f, "Speed");
        MakeTransition(walk, idle, AnimatorConditionMode.Less, 0.1f, "Speed");

        // Walk ↔ Run (Speed 5)
        MakeTransition(walk, run, AnimatorConditionMode.Greater, 5f, "Speed");
        MakeTransition(run, walk, AnimatorConditionMode.Less, 4.5f, "Speed");

        // Run → Idle (sudden stop)
        MakeTransition(run, idle, AnimatorConditionMode.Less, 0.1f, "Speed", 0.2f);

        // ══════════════════════════════════════
        //  TRANSITIONS — Crouch
        // ══════════════════════════════════════

        // Enter crouch
        MakeTransition(idle, crouchIdle, AnimatorConditionMode.If, 0, "IsCrouching");
        MakeTransition(walk, crouchWalk, AnimatorConditionMode.If, 0, "IsCrouching");
        MakeTransition(run, crouchWalk, AnimatorConditionMode.If, 0, "IsCrouching");

        // CrouchIdle ↔ CrouchWalk
        MakeTransition(crouchIdle, crouchWalk, AnimatorConditionMode.Greater, 0.1f, "Speed");
        MakeTransition(crouchWalk, crouchIdle, AnimatorConditionMode.Less, 0.1f, "Speed");

        // Exit crouch
        MakeTransition(crouchIdle, idle, AnimatorConditionMode.IfNot, 0, "IsCrouching");
        MakeTransition(crouchWalk, walk, AnimatorConditionMode.IfNot, 0, "IsCrouching");

        // ══════════════════════════════════════
        //  TRANSITIONS — Jump / Fall / Land
        // ══════════════════════════════════════

        // Any grounded → JumpUp
        var anyToJump = sm.AddAnyStateTransition(jumpUp);
        anyToJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
        anyToJump.hasExitTime = false;
        anyToJump.duration = 0.1f;

        // JumpUp → Falling (after jump apex, when no longer "Jump")
        MakeTransition(jumpUp, falling, AnimatorConditionMode.IfNot, 0, "Jump", 0.15f);

        // Any → Falling (when not grounded and not jumping — e.g. walked off a ledge)
        var anyToFall = sm.AddAnyStateTransition(falling);
        anyToFall.AddCondition(AnimatorConditionMode.If, 0, "FreeFall");
        anyToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
        anyToFall.hasExitTime = false;
        anyToFall.duration = 0.15f;

        // Falling → Landing (when grounded again)
        MakeTransition(falling, landing, AnimatorConditionMode.If, 0, "Grounded", 0.1f);

        // Landing → Idle (after clip)
        var landToIdle = landing.AddTransition(idle);
        landToIdle.hasExitTime = true;
        landToIdle.exitTime = 0.85f;
        landToIdle.duration = 0.15f;

        // Landing → Walk (if moving when landing)
        MakeTransition2(landing, walk,
            AnimatorConditionMode.Greater, 0.1f, "Speed",
            AnimatorConditionMode.If, 0, "Grounded",
            0.2f);

        // ══════════════════════════════════════
        //  TRANSITIONS — Push Cart
        // ══════════════════════════════════════

        var anyToPush = sm.AddAnyStateTransition(push);
        anyToPush.AddCondition(AnimatorConditionMode.If, 0, "IsPushing");
        anyToPush.hasExitTime = false;
        anyToPush.duration = 0.2f;

        MakeTransition(push, idle, AnimatorConditionMode.IfNot, 0, "IsPushing", 0.2f);

        // ══════════════════════════════════════
        //  TRANSITIONS — Item Interactions
        // ══════════════════════════════════════

        if (pickUpClip != null)
        {
            var anyToPickUp = sm.AddAnyStateTransition(pickUp);
            anyToPickUp.AddCondition(AnimatorConditionMode.If, 0, "PickUp");
            anyToPickUp.hasExitTime = false;
            anyToPickUp.duration = 0.1f;

            var pickUpToIdle = pickUp.AddTransition(idle);
            pickUpToIdle.hasExitTime = true;
            pickUpToIdle.exitTime = 0.9f;
            pickUpToIdle.duration = 0.15f;
        }

        if (takeItemClip != null)
        {
            var anyToTake = sm.AddAnyStateTransition(takeItem);
            anyToTake.AddCondition(AnimatorConditionMode.If, 0, "TakeItem");
            anyToTake.hasExitTime = false;
            anyToTake.duration = 0.1f;

            var takeToIdle = takeItem.AddTransition(idle);
            takeToIdle.hasExitTime = true;
            takeToIdle.exitTime = 0.9f;
            takeToIdle.duration = 0.15f;
        }

        // ══════════════════════════════════════
        //  DONE
        // ══════════════════════════════════════

        EditorUtility.SetDirty(controller);
        Debug.Log("[CHARACTER SETUP] ✅ Animator Controller created: " +
            "Idle/Walk/Run/CrouchIdle/CrouchWalk/JumpUp/Falling/Landing/" +
            "PushCart/PickUpItem/TakingItem/WalkBackward/StrafeLeft/StrafeRight");
    }
}
