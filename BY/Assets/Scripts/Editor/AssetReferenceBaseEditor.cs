using UnityEngine;

using UnityEditor;
using UnityEditorInternal;
using UnityEditor.AddressableAssets;
using System.Linq;
using System.Text;
[CustomEditor(typeof(AssetReferenceBase<System.Enum,UnityEngine.Object>), true)]
public class AssetReferenceBaseEditor : Editor
{
    private SerializedProperty soundsProperty;
    private ReorderableList reorderableList;

    // 에디터 창이 활성화될 때 또는 선택된 오브젝트가 변경될 때 호출됩니다.
    protected void OnEnable()
    {
        soundsProperty = serializedObject.FindProperty("assetDatas");

        reorderableList = new ReorderableList(serializedObject, soundsProperty,
            draggable: true,
            displayHeader: true,
            displayAddButton: true,
            displayRemoveButton: true);

        reorderableList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Resource Mapping", EditorStyles.boldLabel);
        };

        // 리스트의 각 요소를 그리는 콜백입니다.
        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            // 현재 SoundEntry 요소에 대한 SerializedProperty를 가져옵니다.
            SerializedProperty element = soundsProperty.GetArrayElementAtIndex(index);
            SerializedProperty idProperty = element.FindPropertyRelative("id");
            SerializedProperty clipRefProperty = element.FindPropertyRelative("data");
            SerializedProperty labelsForStringProperty = element.FindPropertyRelative("labelForString");

            float enumWidth = 120f;
            float labelsWidth = 180f;
            float objectFieldWidth = rect.width - enumWidth - labelsWidth - 10f;

            Rect idRect = new Rect(rect.x, rect.y, enumWidth, EditorGUIUtility.singleLineHeight);
            Rect labelsRect = new Rect(rect.x + enumWidth + 5f, rect.y, labelsWidth, EditorGUIUtility.singleLineHeight);
            Rect clipRefRect = new Rect(rect.x + enumWidth + labelsWidth + 10f, rect.y, objectFieldWidth, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(idRect, idProperty, GUIContent.none);

            EditorGUI.PropertyField(clipRefRect, clipRefProperty, GUIContent.none);

            EditorGUI.PropertyField(labelsRect, labelsForStringProperty, GUIContent.none);
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

        //if (soundsProperty.isArray)
        //{
        //    HashSet<E> seenIds = new HashSet<E>();
        //    for (int i = 0; i < soundsProperty.arraySize; i++)
        //    {
        //        SerializedProperty element = soundsProperty.GetArrayElementAtIndex(i);
        //        SerializedProperty idProperty = element.FindPropertyRelative("id");
        //        E currentId = (E)idProperty.enumValueIndex;

        //        if (seenIds.Contains(currentId))
        //        {
        //            EditorGUILayout.HelpBox($"중복된 SOUND_DATA '{currentId}'가 발견되었습니다. 각 SOUND_DATA는 고유해야 합니다.", MessageType.Warning);
        //        }
        //        else
        //        {
        //            seenIds.Add(currentId);
        //        }
        //    }
        //}

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
        for (int i = 0; i < soundsProperty.arraySize; i++)
        {
            SerializedProperty element = soundsProperty.GetArrayElementAtIndex(i);
            SerializedProperty dataProperty = element.FindPropertyRelative("data");
            SerializedProperty labelsProperty = element.FindPropertyRelative("labels");
            SerializedProperty labelForStringProperty = element.FindPropertyRelative("labelForString");

            UpdateLabelsFromAssetReference(dataProperty, labelsProperty, labelForStringProperty);
        }
    }

    private void UpdateLabelsFromAssetReference(SerializedProperty dataProp, SerializedProperty labelsProp, SerializedProperty labelStringProp)
    {
        string guid = dataProp.FindPropertyRelative("m_AssetGUID").stringValue;
        if (string.IsNullOrEmpty(guid))
        {
            labelsProp.ClearArray();
            labelStringProp.stringValue = string.Empty;
            return;
        }

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var entry = settings.FindAssetEntry(guid);
        if (entry == null)
        {
            labelsProp.ClearArray();
            labelStringProp.stringValue = string.Empty;
            return;
        }

        labelsProp.ClearArray();

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < entry.labels.Count; i++)
        {
            labelsProp.InsertArrayElementAtIndex(i);
            SerializedProperty labelProp = labelsProp.GetArrayElementAtIndex(i);
            labelProp.FindPropertyRelative("m_LabelString").stringValue = entry.labels.ElementAt(i);
            sb.Append(entry.labels.ElementAt(i));
            sb.Append(", ");
        }
        if (entry.labels.Count > 0)
            sb.Length -= 2;
        labelStringProp.stringValue = sb.ToString();
    }
}
