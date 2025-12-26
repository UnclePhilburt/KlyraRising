#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EnemyBehavior))]
public class EnemyBehaviorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EnemyBehavior behavior = (EnemyBehavior)target;
        serializedObject.Update();

        // Always show behavior type dropdown
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_behaviorType"));

        EditorGUILayout.Space(10);

        // Get current behavior type
        BehaviorType type = (BehaviorType)serializedObject.FindProperty("_behaviorType").enumValueIndex;

        // Show relevant settings based on type
        switch (type)
        {
            case BehaviorType.Idle:
                EditorGUILayout.LabelField("Idle Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_facePlayerWhenNear"));
                if (serializedObject.FindProperty("_facePlayerWhenNear").boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_facePlayerDistance"));
                    EditorGUI.indentLevel--;
                }
                break;

            case BehaviorType.Patrol:
                EditorGUILayout.LabelField("Patrol Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_patrolPoints"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_patrolSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_patrolWaitTime"));
                break;

            case BehaviorType.Chase:
                EditorGUILayout.LabelField("Chase Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_detectionRange"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_chaseSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_chaseTimeout"));

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Attack Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_attackRange"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_attackDamage"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_attackCooldown"));
                break;

            case BehaviorType.Wander:
                EditorGUILayout.LabelField("Wander Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_wanderRadius"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_wanderSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_wanderWaitTime"));
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
