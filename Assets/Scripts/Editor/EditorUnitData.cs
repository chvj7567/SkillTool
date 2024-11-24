using UnityEditor;

[CustomEditor(typeof(UnitData))]
public class EditorUnitData : Editor
{
    private SerializedProperty eUnitProperty;
    private SerializedProperty unitNameProperty;
    private SerializedProperty eLevelProperty;
    private SerializedProperty maxHpProperty;
    private SerializedProperty hpRegenPerSecondProperty;
    private SerializedProperty maxMpProperty;
    private SerializedProperty mpRegenPerSecondProperty;
    private SerializedProperty attackPowerProperty;
    private SerializedProperty defensePowerProperty;
    private SerializedProperty moveSpeedProperty;
    private SerializedProperty rotateSpeedProperty;
    private SerializedProperty rangeProperty;
    private SerializedProperty rangeMultiProperty;
    private SerializedProperty viewAngleProperty;
    private SerializedProperty eSkill1Property;
    private SerializedProperty eSkill2Property;
    private SerializedProperty eSkill3Property;
    private SerializedProperty eSkill4Property;
    private SerializedProperty eItem1Property;

    private void OnEnable()
    {
        // SerializedProperty 가져오기
        eUnitProperty = serializedObject.FindProperty("eUnit");
        unitNameProperty = serializedObject.FindProperty("unitName");
        eLevelProperty = serializedObject.FindProperty("eLevel");
        maxHpProperty = serializedObject.FindProperty("maxHp");
        hpRegenPerSecondProperty = serializedObject.FindProperty("hpRegenPerSecond");
        maxMpProperty = serializedObject.FindProperty("maxMp");
        mpRegenPerSecondProperty = serializedObject.FindProperty("mpRegenPerSecond");
        attackPowerProperty = serializedObject.FindProperty("attackPower");
        defensePowerProperty = serializedObject.FindProperty("defensePower");
        moveSpeedProperty = serializedObject.FindProperty("moveSpeed");
        rotateSpeedProperty = serializedObject.FindProperty("rotateSpeed");
        rangeProperty = serializedObject.FindProperty("range");
        rangeMultiProperty = serializedObject.FindProperty("rangeMulti");
        viewAngleProperty = serializedObject.FindProperty("viewAngle");
        eSkill1Property = serializedObject.FindProperty("eSkill1");
        eSkill2Property = serializedObject.FindProperty("eSkill2");
        eSkill3Property = serializedObject.FindProperty("eSkill3");
        eSkill4Property = serializedObject.FindProperty("eSkill4");
        eItem1Property = serializedObject.FindProperty("eItem1");
    }

    public override void OnInspectorGUI()
    {
        // 기본 Inspector 표시
        DrawDefaultInspector();

        // SerializedObject 업데이트
        serializedObject.Update();

        // 유닛 정보 표시
        EditorGUILayout.Space(10f);
        EditorGUILayout.PropertyField(eUnitProperty);
        EditorGUILayout.PropertyField(unitNameProperty);
        EditorGUILayout.PropertyField(eLevelProperty);
        EditorGUILayout.PropertyField(maxHpProperty);
        EditorGUILayout.PropertyField(hpRegenPerSecondProperty);
        EditorGUILayout.PropertyField(maxMpProperty);
        EditorGUILayout.PropertyField(mpRegenPerSecondProperty);
        EditorGUILayout.PropertyField(attackPowerProperty);
        EditorGUILayout.PropertyField(defensePowerProperty);
        EditorGUILayout.PropertyField(moveSpeedProperty);
        EditorGUILayout.PropertyField(rotateSpeedProperty);
        EditorGUILayout.PropertyField(rangeProperty);
        EditorGUILayout.PropertyField(rangeMultiProperty);
        EditorGUILayout.PropertyField(viewAngleProperty);

        // 스킬 정보 표시
        EditorGUILayout.Space(10f);
        EditorGUILayout.PropertyField(eSkill1Property);
        EditorGUILayout.PropertyField(eSkill2Property);
        EditorGUILayout.PropertyField(eSkill3Property);
        EditorGUILayout.PropertyField(eSkill4Property);

        // 아이템 정보 표시
        EditorGUILayout.Space(10f);
        EditorGUILayout.PropertyField(eItem1Property);

        // 변경 사항 저장
        serializedObject.ApplyModifiedProperties();
    }
}