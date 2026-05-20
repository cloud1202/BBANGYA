using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
[CustomEditor(typeof(AssetReferenceBase<Enum, UnityEngine.Object>), true)]
public class AssetReferenceEditor : Editor 
{
    private SerializedProperty property; 
    private ReorderableList reorderableList;
    private string referenceName;

    // 에디터 창이 활성화될 때 또는 선택된 오브젝트가 변경될 때 호출됩니다.
    protected void OnEnable()
    {
        property = serializedObject.FindProperty("assetDatas");
        referenceName = serializedObject.FindProperty("Name").stringValue;
        reorderableList = new ReorderableList(serializedObject, property,
            draggable: true,
            displayHeader: true,
            displayAddButton: true,
            displayRemoveButton: true);

        reorderableList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, $"{referenceName}", EditorStyles.boldLabel);
        };

        // 리스트의 각 요소를 그리는 콜백입니다.
        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            // 현재 SoundEntry 요소에 대한 SerializedProperty를 가져옵니다.
            SerializedProperty element = property.GetArrayElementAtIndex(index);
            SerializedProperty idProperty = element.FindPropertyRelative("id");
            SerializedProperty clipRefProperty = element.FindPropertyRelative("data");
            SerializedProperty labelProperty = element.FindPropertyRelative("containLabel");

            float enumWidth = 200f;
            float objectFieldWidth = 150f;
            float labelFieldWidth = rect.width - objectFieldWidth - enumWidth - 10f;

            Rect idRect = new Rect(rect.x, rect.y, enumWidth, EditorGUIUtility.singleLineHeight);
            Rect refRect = new Rect(rect.x + enumWidth, rect.y, objectFieldWidth, EditorGUIUtility.singleLineHeight);
            Rect labelRect = new Rect(rect.x + enumWidth + objectFieldWidth + 10f, rect.y, labelFieldWidth, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(idRect, idProperty, GUIContent.none);

            EditorGUI.PropertyField(refRect, clipRefProperty, GUIContent.none);

            ContainLabel value = (ContainLabel)labelProperty.intValue;

            value = (ContainLabel)EditorGUI.EnumFlagsField(labelRect, value);

            labelProperty.intValue = (int)value;
        };

        // 각 요소의 높이를 설정합니다.
        reorderableList.elementHeightCallback = (int index) =>
        {
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        };

        // 요소 추가 버튼을 눌렀을 때 호출되는 콜백입니다.
        reorderableList.onAddCallback = (ReorderableList list) =>
        {
            int newIndex = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = newIndex;
            SerializedProperty newElement = list.serializedProperty.GetArrayElementAtIndex(newIndex);

            newElement.FindPropertyRelative("id").enumValueIndex = newIndex;
            SerializedProperty clipRefProperty = newElement.FindPropertyRelative("data");
            clipRefProperty.FindPropertyRelative("m_AssetGUID").stringValue = "";
        };

        ChangeProperties();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();

        reorderableList.DoLayoutList();

        EditorGUILayout.Space();

        if (property.isArray)
        {
            HashSet<int> seenIds = new HashSet<int>();
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                SerializedProperty idProperty = element.FindPropertyRelative("id");
                int currentId = idProperty.enumValueIndex;

                if (seenIds.Contains(currentId))
                {
                    EditorGUILayout.HelpBox($"중복된 ID Index '{currentId}'가 발견되었습니다. 각 ID는 고유해야 합니다.", MessageType.Warning);
                }
                else
                {
                    seenIds.Add(currentId);
                }
            }
        }

        if (EditorGUI.EndChangeCheck())
        {
            ChangeProperties();
        }
    }

    private void ChangeProperties()
    {
        UpdateLabelsAllAssetReference();
        serializedObject.ApplyModifiedProperties();
    }

    private void UpdateLabelsAllAssetReference()
    {
        for (int i = 0; i < property.arraySize; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            SerializedProperty dataProperty = element.FindPropertyRelative("data");
        }
    }
}
#endif

public class AssetReferenceBase<E, T> : ScriptableObject
    where E : Enum
    where T : UnityEngine.Object
{
    [Serializable]
    public class AssetResource : IAssetResource
    {
        public E id;
        public AssetReferenceT<T> data;
        public ContainLabel containLabel;

        public AssetReference assetRef => data;
    }
    public List<AssetResource> assetDatas;
}
