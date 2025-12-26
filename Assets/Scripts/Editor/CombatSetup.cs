using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class CombatSetup : EditorWindow
{
    // Attack types matching WeaponController.AttackType enum
    const int ATK_LIGHT_COMBO = 1;
    const int ATK_HEAVY_FLOURISH = 2;
    const int ATK_HEAVY_STAB = 3;
    const int ATK_HEAVY_COMBO = 4;
    const int ATK_FENCING = 5;
    const int ATK_LEAPING = 6;
    const int ATK_PARRY = 7;

    [MenuItem("Klyra/Setup Combat Animations")]
    public static void Setup()
    {
        // Find animator controller
        string[] guids = AssetDatabase.FindAssets("AC_Polygon_Masculine t:AnimatorController");
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "Could not find AC_Polygon_Masculine animator controller!", "OK");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

        if (controller == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not load animator controller!", "OK");
            return;
        }

        // Find all animations
        AnimationClip idleClip = FindAnimation("A_Idle_Base_Sword");
        AnimationClip idleSheathedClip = FindAnimation("A_Idle_Sheathed_Sword_Masc");
        AnimationClip drawClip = FindAnimation("A_Draw_Sword_Masc");
        AnimationClip sheatheClip = FindAnimation("A_Sheathe_Sword_Masc");

        // Light Combo
        AnimationClip lightA = FindAnimation("A_Attack_LightCombo01A_Sword");
        AnimationClip lightB = FindAnimation("A_Attack_LightCombo01B_Sword");
        AnimationClip lightC = FindAnimation("A_Attack_LightCombo01C_Sword");

        // Heavy attacks
        AnimationClip heavyFlourish = FindAnimation("A_Attack_HeavyFlourish01_Sword");
        AnimationClip heavyStab = FindAnimation("A_Attack_HeavyStab01_Sword");

        // Heavy Combo
        AnimationClip heavyA = FindAnimation("A_Attack_HeavyCombo01A_Sword");
        AnimationClip heavyB = FindAnimation("A_Attack_HeavyCombo01B_Sword");
        AnimationClip heavyC = FindAnimation("A_Attack_HeavyCombo01C_Sword");

        // Special attacks
        AnimationClip fencing = FindAnimation("A_Attack_LightFencing01_Sword");
        AnimationClip leaping = FindAnimation("A_Attack_LightLeaping01_Sword");

        // Block & Parry
        AnimationClip blockClip = FindAnimation("A_Block_Loop_Sword");
        AnimationClip parryClip = FindAnimation("A_Parry_F_PommelStrike_Sword");

        if (idleClip == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find sword animations!\n\nMake sure AnimationSwordCombat is imported.", "OK");
            return;
        }

        // Add parameters
        AddParam(controller, "IsArmed", AnimatorControllerParameterType.Bool);
        AddParam(controller, "Attack", AnimatorControllerParameterType.Trigger);
        AddParam(controller, "AttackType", AnimatorControllerParameterType.Int);
        AddParam(controller, "ComboStep", AnimatorControllerParameterType.Int);
        AddParam(controller, "IsBlocking", AnimatorControllerParameterType.Bool);
        AddParam(controller, "Draw", AnimatorControllerParameterType.Trigger);
        AddParam(controller, "Sheathe", AnimatorControllerParameterType.Trigger);

        // Create avatar mask
        AvatarMask mask = CreateMask();

        // Remove existing Combat layer
        for (int i = controller.layers.Length - 1; i >= 0; i--)
        {
            if (controller.layers[i].name == "Combat")
                controller.RemoveLayer(i);
        }

        // Create Combat layer
        AnimatorControllerLayer layer = new AnimatorControllerLayer
        {
            name = "Combat",
            defaultWeight = 0f,
            blendingMode = AnimatorLayerBlendingMode.Override,
            avatarMask = mask,
            stateMachine = new AnimatorStateMachine()
        };

        layer.stateMachine.name = "Combat";
        layer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(layer.stateMachine, controller);
        controller.AddLayer(layer);

        var sm = layer.stateMachine;

        // === CREATE STATES ===
        var empty = sm.AddState("Empty", new Vector3(0, 0, 0));
        sm.defaultState = empty;

        // Sheathed idle (optional - use if we have the animation)
        AnimatorState sheathedIdle = null;
        if (idleSheathedClip != null)
        {
            sheathedIdle = sm.AddState("SheathedIdle", new Vector3(0, 100, 0));
            sheathedIdle.motion = idleSheathedClip;
        }

        // Draw and Sheathe states
        AnimatorState draw = null;
        if (drawClip != null)
        {
            draw = sm.AddState("Draw", new Vector3(125, 0, 0));
            draw.motion = drawClip;
        }

        AnimatorState sheathe = null;
        if (sheatheClip != null)
        {
            sheathe = sm.AddState("Sheathe", new Vector3(125, 100, 0));
            sheathe.motion = sheatheClip;
        }

        var idle = sm.AddState("SwordIdle", new Vector3(250, 0, 0));
        idle.motion = idleClip;

        // Light Combo states
        AnimatorState light1 = null, light2 = null, light3 = null;
        if (lightA != null) { light1 = sm.AddState("LightCombo1", new Vector3(500, -150, 0)); light1.motion = lightA; }
        if (lightB != null) { light2 = sm.AddState("LightCombo2", new Vector3(500, -100, 0)); light2.motion = lightB; }
        if (lightC != null) { light3 = sm.AddState("LightCombo3", new Vector3(500, -50, 0)); light3.motion = lightC; }

        // Heavy single attacks
        AnimatorState flourish = null, stab = null;
        if (heavyFlourish != null) { flourish = sm.AddState("HeavyFlourish", new Vector3(500, 50, 0)); flourish.motion = heavyFlourish; }
        if (heavyStab != null) { stab = sm.AddState("HeavyStab", new Vector3(500, 100, 0)); stab.motion = heavyStab; }

        // Heavy Combo states
        AnimatorState hvy1 = null, hvy2 = null, hvy3 = null;
        if (heavyA != null) { hvy1 = sm.AddState("HeavyCombo1", new Vector3(500, 150, 0)); hvy1.motion = heavyA; }
        if (heavyB != null) { hvy2 = sm.AddState("HeavyCombo2", new Vector3(500, 200, 0)); hvy2.motion = heavyB; }
        if (heavyC != null) { hvy3 = sm.AddState("HeavyCombo3", new Vector3(500, 250, 0)); hvy3.motion = heavyC; }

        // Special attacks
        AnimatorState fenc = null, leap = null;
        if (fencing != null) { fenc = sm.AddState("Fencing", new Vector3(500, 300, 0)); fenc.motion = fencing; }
        if (leaping != null) { leap = sm.AddState("Leaping", new Vector3(500, 350, 0)); leap.motion = leaping; }

        // Block & Parry
        AnimatorState block = null, parry = null;
        if (blockClip != null) { block = sm.AddState("Block", new Vector3(250, 150, 0)); block.motion = blockClip; }
        if (parryClip != null) { parry = sm.AddState("Parry", new Vector3(500, 400, 0)); parry.motion = parryClip; }

        // === TRANSITIONS ===

        // === DRAW / SHEATHE SYSTEM ===
        if (draw != null)
        {
            // Empty -> Draw (on Draw trigger)
            var toDraw = empty.AddTransition(draw);
            toDraw.AddCondition(AnimatorConditionMode.If, 0, "Draw");
            toDraw.duration = 0.1f;
            toDraw.hasExitTime = false;

            // Draw -> SwordIdle (when draw animation completes)
            var drawToIdle = draw.AddTransition(idle);
            drawToIdle.hasExitTime = true;
            drawToIdle.exitTime = 0.9f;
            drawToIdle.duration = 0.1f;
        }
        else
        {
            // Fallback: direct transition if no draw animation
            var toIdle = empty.AddTransition(idle);
            toIdle.AddCondition(AnimatorConditionMode.If, 0, "IsArmed");
            toIdle.duration = 0.2f;
            toIdle.hasExitTime = false;
        }

        if (sheathe != null)
        {
            // SwordIdle -> Sheathe (on Sheathe trigger)
            var toSheathe = idle.AddTransition(sheathe);
            toSheathe.AddCondition(AnimatorConditionMode.If, 0, "Sheathe");
            toSheathe.duration = 0.1f;
            toSheathe.hasExitTime = false;

            // Sheathe -> Empty (when sheathe animation completes)
            var sheatheToEmpty = sheathe.AddTransition(empty);
            sheatheToEmpty.hasExitTime = true;
            sheatheToEmpty.exitTime = 0.9f;
            sheatheToEmpty.duration = 0.1f;
        }
        else
        {
            // Fallback: direct transition if no sheathe animation
            var toEmpty = idle.AddTransition(empty);
            toEmpty.AddCondition(AnimatorConditionMode.IfNot, 0, "IsArmed");
            toEmpty.duration = 0.2f;
            toEmpty.hasExitTime = false;
        }

        // === BLOCKING ===
        if (block != null)
        {
            var toBlock = idle.AddTransition(block);
            toBlock.AddCondition(AnimatorConditionMode.If, 0, "IsBlocking");
            toBlock.duration = 0.1f;
            toBlock.hasExitTime = false;

            var fromBlock = block.AddTransition(idle);
            fromBlock.AddCondition(AnimatorConditionMode.IfNot, 0, "IsBlocking");
            fromBlock.duration = 0.15f;
            fromBlock.hasExitTime = false;
        }

        // === LIGHT COMBO ===
        if (light1 != null) AddAttackTransition(idle, light1, ATK_LIGHT_COMBO, 1);
        if (light2 != null) AddAttackTransition(idle, light2, ATK_LIGHT_COMBO, 2);
        if (light3 != null) AddAttackTransition(idle, light3, ATK_LIGHT_COMBO, 3);

        // Chain transitions for light combo
        if (light1 != null && light2 != null) AddComboChain(light1, light2, ATK_LIGHT_COMBO, 2);
        if (light2 != null && light3 != null) AddComboChain(light2, light3, ATK_LIGHT_COMBO, 3);

        // Return to idle
        if (light1 != null) AddReturnToIdle(light1, idle);
        if (light2 != null) AddReturnToIdle(light2, idle);
        if (light3 != null) AddReturnToIdle(light3, idle);

        // === HEAVY FLOURISH ===
        if (flourish != null)
        {
            AddAttackTransition(idle, flourish, ATK_HEAVY_FLOURISH, 1);
            AddReturnToIdle(flourish, idle);
        }

        // === HEAVY STAB ===
        if (stab != null)
        {
            AddAttackTransition(idle, stab, ATK_HEAVY_STAB, 1);
            AddReturnToIdle(stab, idle);
        }

        // === HEAVY COMBO ===
        if (hvy1 != null) AddAttackTransition(idle, hvy1, ATK_HEAVY_COMBO, 1);
        if (hvy2 != null) AddAttackTransition(idle, hvy2, ATK_HEAVY_COMBO, 2);
        if (hvy3 != null) AddAttackTransition(idle, hvy3, ATK_HEAVY_COMBO, 3);

        if (hvy1 != null && hvy2 != null) AddComboChain(hvy1, hvy2, ATK_HEAVY_COMBO, 2);
        if (hvy2 != null && hvy3 != null) AddComboChain(hvy2, hvy3, ATK_HEAVY_COMBO, 3);

        if (hvy1 != null) AddReturnToIdle(hvy1, idle);
        if (hvy2 != null) AddReturnToIdle(hvy2, idle);
        if (hvy3 != null) AddReturnToIdle(hvy3, idle);

        // === FENCING (Sprint attack) ===
        if (fenc != null)
        {
            AddAttackTransition(idle, fenc, ATK_FENCING, 1);
            AddReturnToIdle(fenc, idle);
        }

        // === LEAPING (Jump attack) ===
        if (leap != null)
        {
            AddAttackTransition(idle, leap, ATK_LEAPING, 1);
            AddReturnToIdle(leap, idle);
        }

        // === PARRY COUNTER ===
        if (parry != null)
        {
            AddAttackTransition(idle, parry, ATK_PARRY, 1);
            if (block != null)
            {
                // Can also parry from block state
                AddAttackTransition(block, parry, ATK_PARRY, 1);
            }
            AddReturnToIdle(parry, idle);
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        // Count what we added
        int stateCount = 0;
        if (draw != null) stateCount++;
        if (sheathe != null) stateCount++;
        if (light1 != null) stateCount++;
        if (light2 != null) stateCount++;
        if (light3 != null) stateCount++;
        if (flourish != null) stateCount++;
        if (stab != null) stateCount++;
        if (hvy1 != null) stateCount++;
        if (hvy2 != null) stateCount++;
        if (hvy3 != null) stateCount++;
        if (fenc != null) stateCount++;
        if (leap != null) stateCount++;
        if (block != null) stateCount++;
        if (parry != null) stateCount++;

        EditorUtility.DisplayDialog("Combat Setup Complete",
            $"Added Combat layer with {stateCount} states:\n\n" +
            $"Draw: {(draw != null ? "Yes" : "No")}\n" +
            $"Sheathe: {(sheathe != null ? "Yes" : "No")}\n" +
            $"Light Combo: {(light1 != null ? "A" : "")}{(light2 != null ? "B" : "")}{(light3 != null ? "C" : "")}\n" +
            $"Heavy Combo: {(hvy1 != null ? "A" : "")}{(hvy2 != null ? "B" : "")}{(hvy3 != null ? "C" : "")}\n" +
            $"Heavy Flourish: {(flourish != null ? "Yes" : "No")}\n" +
            $"Heavy Stab: {(stab != null ? "Yes" : "No")}\n" +
            $"Fencing: {(fenc != null ? "Yes" : "No")}\n" +
            $"Leaping: {(leap != null ? "Yes" : "No")}\n" +
            $"Block: {(block != null ? "Yes" : "No")}\n" +
            $"Parry: {(parry != null ? "Yes" : "No")}\n\n" +
            "CONTROLS:\n" +
            "1 = Draw/Sheathe sword\n" +
            "LMB = Light combo (3 hits)\n" +
            "RMB tap = Heavy (Flourish/Stab)\n" +
            "RMB hold = Heavy Combo\n" +
            "Sprint+LMB = Fencing thrust\n" +
            "Jump+LMB = Leaping attack\n" +
            "LMB+RMB = Block\n" +
            "Quick block release = Parry\n" +
            "Light x3 → RMB = Finisher",
            "OK");
    }

    static void AddAttackTransition(AnimatorState from, AnimatorState to, int attackType, int comboStep)
    {
        var t = from.AddTransition(to);
        t.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        t.AddCondition(AnimatorConditionMode.Equals, attackType, "AttackType");
        t.AddCondition(AnimatorConditionMode.Equals, comboStep, "ComboStep");
        t.duration = 0.1f;
        t.hasExitTime = false;
    }

    static void AddComboChain(AnimatorState from, AnimatorState to, int attackType, int comboStep)
    {
        var t = from.AddTransition(to);
        t.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        t.AddCondition(AnimatorConditionMode.Equals, attackType, "AttackType");
        t.AddCondition(AnimatorConditionMode.Equals, comboStep, "ComboStep");
        t.hasExitTime = true;
        t.exitTime = 0.5f;
        t.duration = 0.1f;
    }

    static void AddReturnToIdle(AnimatorState from, AnimatorState idle)
    {
        var t = from.AddTransition(idle);
        t.hasExitTime = true;
        t.exitTime = 0.9f;
        t.duration = 0.1f;
    }

    static AnimationClip FindAnimation(string name)
    {
        // Search in multiple Synty animation folders
        string[] searchPaths = new[]
        {
            "Assets/Synty/AnimationSwordCombat",
            "Assets/Synty/AnimationBaseLocomotion",
            "Assets/Synty/PolygonSamuraiEmpire"
        };

        foreach (string searchPath in searchPaths)
        {
            if (!AssetDatabase.IsValidFolder(searchPath)) continue;

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { searchPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains(name))
                {
                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var asset in assets)
                    {
                        if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                            return clip;
                    }
                }
            }
        }
        return null;
    }

    static void AddParam(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in controller.parameters)
            if (p.name == name) return;
        controller.AddParameter(name, type);
    }

    static AvatarMask CreateMask()
    {
        string maskPath = "Assets/Scripts/UpperBodyMask.mask";
        if (File.Exists(maskPath))
            AssetDatabase.DeleteAsset(maskPath);

        AvatarMask mask = new AvatarMask();
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
        {
            var part = (AvatarMaskBodyPart)i;
            bool on = part == AvatarMaskBodyPart.Body ||
                     part == AvatarMaskBodyPart.Head ||
                     part == AvatarMaskBodyPart.LeftArm ||
                     part == AvatarMaskBodyPart.RightArm ||
                     part == AvatarMaskBodyPart.LeftFingers ||
                     part == AvatarMaskBodyPart.RightFingers;
            mask.SetHumanoidBodyPartActive(part, on);
        }

        AssetDatabase.CreateAsset(mask, maskPath);
        return mask;
    }
}
