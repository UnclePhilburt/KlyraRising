using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class BossAnimatorSetup : MonoBehaviour
{
    [MenuItem("Klyra/Create Boss Animator Controller")]
    public static void CreateBossAnimator()
    {
        // Create the animator controller
        string path = "Assets/BossAnimator.controller";
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        // Add parameters
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);

        // Get the root state machine
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        // Find animations
        AnimationClip idleClip = FindAnimationClip("A_Idle_Ready_RootMotion_Sword", "A_Idle_Ready_Sword", "A_Idle");
        AnimationClip attackClip = FindAnimationClip("A_Attack_HeavyFlourish01_Sword", "A_Attack_HeavyCombo01A_Sword");

        // Create Idle state
        AnimatorState idleState = rootStateMachine.AddState("Idle", new Vector3(300, 50, 0));
        if (idleClip != null)
        {
            idleState.motion = idleClip;
            Debug.Log($"[BossAnimator] Set Idle animation: {idleClip.name}");
        }
        else
        {
            Debug.LogWarning("[BossAnimator] No idle animation found - assign manually");
        }
        rootStateMachine.defaultState = idleState;

        // Create Attack state
        AnimatorState attackState = rootStateMachine.AddState("Attack", new Vector3(550, 50, 0));
        if (attackClip != null)
        {
            attackState.motion = attackClip;
            Debug.Log($"[BossAnimator] Set Attack animation: {attackClip.name}");
        }
        else
        {
            Debug.LogWarning("[BossAnimator] No attack animation found - assign manually");
        }

        // Create transition from Any State to Attack
        AnimatorStateTransition anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        anyToAttack.duration = 0.1f;
        anyToAttack.hasExitTime = false;

        // Create transition from Attack back to Idle
        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.9f;
        attackToIdle.duration = 0.2f;

        // Save
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        // Select it
        Selection.activeObject = controller;
        EditorGUIUtility.PingObject(controller);

        Debug.Log("[BossAnimator] Created BossAnimator.controller at Assets/");
        Debug.Log("[BossAnimator] Assign this to your Boss's Animator component");
    }

    static AnimationClip FindAnimationClip(params string[] possibleNames)
    {
        foreach (string name in possibleNames)
        {
            // Search for FBX files with this name
            string[] guids = AssetDatabase.FindAssets(name + " t:animationclip");
            if (guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                if (clip != null) return clip;
            }

            // Also search for embedded animations in FBX
            string[] fbxGuids = AssetDatabase.FindAssets(name);
            foreach (string guid in fbxGuids)
            {
                string fbxPath = AssetDatabase.GUIDToAssetPath(guid);
                if (fbxPath.EndsWith(".fbx"))
                {
                    Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                    foreach (Object asset in assets)
                    {
                        if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                        {
                            return clip;
                        }
                    }
                }
            }
        }
        return null;
    }
}
