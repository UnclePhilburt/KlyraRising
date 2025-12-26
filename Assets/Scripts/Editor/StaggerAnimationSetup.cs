#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

public class StaggerAnimationSetup : EditorWindow
{
    [MenuItem("Klyra/Setup Stagger Animation")]
    public static void SetupStaggerAnimation()
    {
        // Find the animator controller
        string controllerPath = "Assets/Synty/AnimationBaseLocomotion/Animations/Polygon/AC_Polygon_Masculine.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

        if (controller == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find animator controller at:\n" + controllerPath, "OK");
            return;
        }

        // Find the Flinch animation
        string flinchPath = "Assets/Synty/AnimationBaseLocomotion/Animations/Polygon/Flinch.fbx";
        AnimationClip flinchClip = null;

        // Load all clips from the FBX
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(flinchPath);
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                flinchClip = clip;
                break;
            }
        }

        if (flinchClip == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find Flinch animation at:\n" + flinchPath, "OK");
            return;
        }

        // Get the base layer
        AnimatorControllerLayer baseLayer = controller.layers[0];
        AnimatorStateMachine stateMachine = baseLayer.stateMachine;

        // Check if HitReact parameter already exists
        var existingParam = controller.parameters.FirstOrDefault(p => p.name == "HitReact");
        if (existingParam == null)
        {
            controller.AddParameter("HitReact", AnimatorControllerParameterType.Trigger);
            Debug.Log("[StaggerSetup] Added HitReact trigger parameter");
        }
        else if (existingParam.type != AnimatorControllerParameterType.Trigger)
        {
            // Remove and re-add as trigger
            controller.RemoveParameter(existingParam);
            controller.AddParameter("HitReact", AnimatorControllerParameterType.Trigger);
            Debug.Log("[StaggerSetup] Fixed HitReact parameter type to Trigger");
        }
        else
        {
            Debug.Log("[StaggerSetup] HitReact trigger already exists");
        }

        // Check if Stagger state already exists
        AnimatorState staggerState = stateMachine.states
            .Select(s => s.state)
            .FirstOrDefault(s => s.name == "Stagger");

        if (staggerState == null)
        {
            // Create the Stagger state
            staggerState = stateMachine.AddState("Stagger", new Vector3(400, 200, 0));
            staggerState.motion = flinchClip;
            Debug.Log("[StaggerSetup] Created Stagger state");
        }
        else
        {
            // Update existing state
            staggerState.motion = flinchClip;
            Debug.Log("[StaggerSetup] Updated existing Stagger state");
        }

        // Remove existing transitions to Stagger and recreate them
        var existingTransitions = stateMachine.anyStateTransitions
            .Where(t => t.destinationState == staggerState)
            .ToArray();

        foreach (var t in existingTransitions)
        {
            stateMachine.RemoveAnyStateTransition(t);
            Debug.Log("[StaggerSetup] Removed old transition to Stagger");
        }

        // Create fresh transition from Any State to Stagger
        AnimatorStateTransition toStagger = stateMachine.AddAnyStateTransition(staggerState);
        toStagger.AddCondition(AnimatorConditionMode.If, 0, "HitReact");
        toStagger.hasExitTime = false;
        toStagger.duration = 0.05f;
        toStagger.canTransitionToSelf = true; // Allow retriggering while in stagger
        Debug.Log("[StaggerSetup] Created transition from Any State to Stagger");

        // Find a state to return to (prefer Idle or Locomotion)
        AnimatorState returnState = stateMachine.states
            .Select(s => s.state)
            .FirstOrDefault(s => s.name.Contains("Idle") || s.name.Contains("Locomotion") || s.name.Contains("Blend"));

        if (returnState == null && stateMachine.states.Length > 0)
        {
            // Just use the default state
            returnState = stateMachine.defaultState;
        }

        if (returnState != null && returnState != staggerState)
        {
            // Remove existing transitions from Stagger
            var oldTransitions = staggerState.transitions.ToArray();
            foreach (var t in oldTransitions)
            {
                staggerState.RemoveTransition(t);
            }

            // Create fresh transition back to idle/locomotion
            AnimatorStateTransition toReturn = staggerState.AddTransition(returnState);
            toReturn.hasExitTime = true;
            toReturn.exitTime = 0.9f;
            toReturn.duration = 0.2f;
            Debug.Log($"[StaggerSetup] Created transition from Stagger to {returnState.name}");
        }

        // Mark as dirty and save
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Success",
            "Stagger animation setup complete!\n\n" +
            "- Added HitReact trigger\n" +
            "- Created Stagger state with Flinch animation\n" +
            "- Created transitions\n\n" +
            "Enemies will now play the flinch animation when staggered.",
            "OK");
    }
}
#endif
