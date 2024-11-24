using UnityEditor;

[CustomEditor(typeof(ItemData))]
public class EditorItemData : Editor
{
    private SerializedProperty eItemProperty;
    private SerializedProperty itemDescProperty;
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
    private SerializedProperty distanceProperty;
    private SerializedProperty coolTimeProperty;
    private SerializedProperty damageProperty;

    private void OnEnable()
    {
        // SerializedProperty 가져오기
        eItemProperty = serializedObject.FindProperty("eItem");
        itemDescProperty = serializedObject.FindProperty("itemDesc");
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
        distanceProperty = serializedObject.FindProperty("distance");
        coolTimeProperty = serializedObject.FindProperty("coolTime");
        damageProperty = serializedObject.FindProperty("damage");
    }

    public override void OnInspectorGUI()
    {
        // 기본 Inspector 표시
        DrawDefaultInspector();

        // SerializedObject 업데이트
        serializedObject.Update();

        // 아이템 속성 표시
        EditorGUILayout.Space(10f);
        EditorGUILayout.PropertyField(eItemProperty);
        EditorGUILayout.PropertyField(itemDescProperty);
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
        EditorGUILayout.PropertyField(distanceProperty);
        EditorGUILayout.PropertyField(coolTimeProperty);
        EditorGUILayout.PropertyField(damageProperty);

        // 변경 사항 저장
        serializedObject.ApplyModifiedProperties();
    }
}