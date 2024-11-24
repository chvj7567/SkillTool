using NUnit.Framework.Internal;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkillData))]
public class EditorSkillData : Editor
{
    private SerializedProperty eSkillProperty;
    private SerializedProperty skillDescProperty;
    private SerializedProperty isTargetingProperty;
    private SerializedProperty isChannelingProperty;
    private SerializedProperty distanceProperty;
    private SerializedProperty coolTimeProperty;
    private SerializedProperty eSkillCostProperty;
    private SerializedProperty costProperty;
    private SerializedProperty liEffectDataProperty;

    private bool[] showEffectDataArray;

    private void OnEnable()
    {
        // SerializedProperty 가져오기
        eSkillProperty = serializedObject.FindProperty("eSkill");
        skillDescProperty = serializedObject.FindProperty("skillDesc");
        isTargetingProperty = serializedObject.FindProperty("isTargeting");
        isChannelingProperty = serializedObject.FindProperty("isChanneling");
        distanceProperty = serializedObject.FindProperty("distance");
        coolTimeProperty = serializedObject.FindProperty("coolTime");
        eSkillCostProperty = serializedObject.FindProperty("eSkillCost");
        costProperty = serializedObject.FindProperty("cost");
        liEffectDataProperty = serializedObject.FindProperty("liEffectData");

        // 이펙트 데이터 펼침 상태 배열 초기화
        showEffectDataArray = new bool[10];
    }

    public override void OnInspectorGUI()
    {
        // 기본 Inspector 표시
        DrawDefaultInspector();

        // SerializedObject 업데이트
        serializedObject.Update();

        // 스킬 속성 표시
        EditorGUILayout.Space(10f);
        EditorGUILayout.PropertyField(eSkillProperty);
        EditorGUILayout.PropertyField(skillDescProperty);
        EditorGUILayout.PropertyField(isTargetingProperty);
        EditorGUILayout.PropertyField(isChannelingProperty);
        EditorGUILayout.PropertyField(distanceProperty);
        EditorGUILayout.PropertyField(coolTimeProperty);
        EditorGUILayout.PropertyField(eSkillCostProperty);
        EditorGUILayout.PropertyField(costProperty);

        // 이펙트 정보 리스트 표시
        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("이펙트 정보 리스트", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        liEffectDataProperty.arraySize = EditorGUILayout.IntSlider("이펙트 수", liEffectDataProperty.arraySize, 0, 10);

        for (int i = 0; i < liEffectDataProperty.arraySize; i++)
        {
            SerializedProperty effectDataProperty = liEffectDataProperty.GetArrayElementAtIndex(i);
            SerializedProperty EParticleProperty = effectDataProperty.FindPropertyRelative("EParticle");
            SerializedProperty effectAngleProperty = effectDataProperty.FindPropertyRelative("effectAngle");
            SerializedProperty createCasterPositionProperty = effectDataProperty.FindPropertyRelative("createCasterPosition");
            SerializedProperty startDelayProperty = effectDataProperty.FindPropertyRelative("startDelay");
            SerializedProperty createOnEmptyProperty = effectDataProperty.FindPropertyRelative("createOnEmpty");
            SerializedProperty attachProperty = effectDataProperty.FindPropertyRelative("attach");
            SerializedProperty duplicationProperty = effectDataProperty.FindPropertyRelative("duplication");
            SerializedProperty onDecalProperty = effectDataProperty.FindPropertyRelative("onDecal");
            SerializedProperty eStatModifyTypeProperty = effectDataProperty.FindPropertyRelative("eStatModifyType");
            SerializedProperty eDamageType1Property = effectDataProperty.FindPropertyRelative("eDamageType1");
            SerializedProperty eDamageType2Property = effectDataProperty.FindPropertyRelative("eDamageType2");
            SerializedProperty damageProperty = effectDataProperty.FindPropertyRelative("damage");
            SerializedProperty eTargetProperty = effectDataProperty.FindPropertyRelative("eTarget");
            SerializedProperty eTargetMaskProperty = effectDataProperty.FindPropertyRelative("eTargetMask");
            SerializedProperty moveToPosProperty = effectDataProperty.FindPropertyRelative("moveToPos");
            SerializedProperty moveSpeedProperty = effectDataProperty.FindPropertyRelative("moveSpeed");
            SerializedProperty offsetToTargetProperty = effectDataProperty.FindPropertyRelative("offsetToTarget");
            SerializedProperty eCollisionProperty = effectDataProperty.FindPropertyRelative("eCollision");
            SerializedProperty collisionAngleProperty = effectDataProperty.FindPropertyRelative("collisionAngle");
            SerializedProperty sphereRadiusProperty = effectDataProperty.FindPropertyRelative("sphereRadius");
            SerializedProperty boxHalfProperty = effectDataProperty.FindPropertyRelative("boxHalf");
            SerializedProperty triggerStartDelayProperty = effectDataProperty.FindPropertyRelative("triggerStartDelay");
            SerializedProperty triggerStayTimeProperty = effectDataProperty.FindPropertyRelative("triggerStayTime");
            SerializedProperty triggerEnterProperty = effectDataProperty.FindPropertyRelative("triggerEnter");
            SerializedProperty triggerExitProperty = effectDataProperty.FindPropertyRelative("triggerExit");
            SerializedProperty stayTickTimeProperty = effectDataProperty.FindPropertyRelative("stayTickTime");

            // 이펙트 데이터 펼침 상태 저장 및 버튼 표시
            showEffectDataArray[i] = EditorGUILayout.Foldout(showEffectDataArray[i], "Effect Data " + i, true);

            if (showEffectDataArray[i])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(EParticleProperty);
                EditorGUILayout.PropertyField(effectAngleProperty);
                EditorGUILayout.PropertyField(createCasterPositionProperty);
                EditorGUILayout.PropertyField(startDelayProperty);
                EditorGUILayout.PropertyField(createOnEmptyProperty);
                EditorGUILayout.PropertyField(attachProperty);
                EditorGUILayout.PropertyField(duplicationProperty);
                EditorGUILayout.PropertyField(onDecalProperty);

                EditorGUILayout.PropertyField(eStatModifyTypeProperty);
                EditorGUILayout.PropertyField(eDamageType1Property);
                EditorGUILayout.PropertyField(eDamageType2Property);
                EditorGUILayout.PropertyField(damageProperty);

                EditorGUILayout.PropertyField(eTargetProperty);
                EditorGUILayout.PropertyField(eTargetMaskProperty);

                EditorGUILayout.PropertyField(moveToPosProperty);
                if (moveToPosProperty.boolValue)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(moveSpeedProperty);
                    EditorGUILayout.PropertyField(offsetToTargetProperty);
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                EditorGUILayout.PropertyField(eCollisionProperty);
                if (eCollisionProperty.enumValueIndex == 1)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(sphereRadiusProperty);
                    EditorGUILayout.PropertyField(collisionAngleProperty);
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                    }
                }
                else if (eCollisionProperty.enumValueIndex == 2)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(boxHalfProperty);
                    EditorGUILayout.PropertyField(collisionAngleProperty);
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                EditorGUILayout.PropertyField(triggerStartDelayProperty);
                EditorGUILayout.PropertyField(triggerStayTimeProperty);
                EditorGUILayout.PropertyField(triggerEnterProperty);
                EditorGUILayout.PropertyField(triggerExitProperty);
                EditorGUILayout.PropertyField(stayTickTimeProperty);

                EditorGUILayout.Space(5f);
                EditorGUI.indentLevel--;
            }
        }

        EditorGUI.indentLevel--;

        // 변경 사항 저장
        serializedObject.ApplyModifiedProperties();
    }
}
