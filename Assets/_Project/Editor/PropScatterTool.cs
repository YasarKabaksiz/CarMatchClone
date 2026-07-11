using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarMatchClone.Editor
{
    public class PropScatterTool : EditorWindow
    {
        private GameObject _propPrefab;

        private float _xMin     = -5f;
        private float _xMax     =  5f;
        private float _zMin     = -5f;
        private float _zMax     =  5f;
        private float _groundY  =  0f;

        private int   _count    = 20;
        private float _scaleMin = 0.8f;
        private float _scaleMax = 1.2f;

        private string _parentName = "ScatteredProps";

        [MenuItem("Window/CarMatchClone/Prop Scatter Tool")]
        public static void Open() => GetWindow<PropScatterTool>("Prop Scatter");

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Prop Scatter Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // --- Prefab ---
            _propPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Prop Prefab", _propPrefab, typeof(GameObject), allowSceneObjects: false);

            EditorGUILayout.Space(8);

            // --- Alan ---
            EditorGUILayout.LabelField("Dağılım Alanı (XZ)", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("X", GUILayout.Width(12));
            _xMin = EditorGUILayout.FloatField(_xMin, GUILayout.Width(60));
            GUILayout.Label("→", GUILayout.Width(16));
            _xMax = EditorGUILayout.FloatField(_xMax, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Z", GUILayout.Width(12));
            _zMin = EditorGUILayout.FloatField(_zMin, GUILayout.Width(60));
            GUILayout.Label("→", GUILayout.Width(16));
            _zMax = EditorGUILayout.FloatField(_zMax, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            _groundY = EditorGUILayout.FloatField("Y (zemin)", _groundY);

            EditorGUILayout.Space(8);

            // --- Scale ---
            EditorGUILayout.LabelField("Scale Varyasyonu", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Min", GUILayout.Width(28));
            _scaleMin = EditorGUILayout.FloatField(_scaleMin, GUILayout.Width(60));
            GUILayout.Label("Max", GUILayout.Width(28));
            _scaleMax = EditorGUILayout.FloatField(_scaleMax, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // --- Adet + parent adı ---
            _count      = Mathf.Max(1, EditorGUILayout.IntField("Adet", _count));
            _parentName = EditorGUILayout.TextField("Parent Adı", _parentName);

            EditorGUILayout.Space(12);

            // --- Doğrulama + Scatter butonu ---
            bool valid = _propPrefab != null
                      && _xMax > _xMin
                      && _zMax > _zMin
                      && _scaleMax >= _scaleMin;

            GUI.enabled = valid;
            if (GUILayout.Button("Scatter", GUILayout.Height(32)))
                DoScatter();
            GUI.enabled = true;

            // Hata/uyarı mesajları
            if (_propPrefab == null)
                EditorGUILayout.HelpBox("Bir Prop Prefab seçin.", MessageType.Warning);
            else if (_xMax <= _xMin || _zMax <= _zMin)
                EditorGUILayout.HelpBox("Alan boyutları geçersiz (max > min olmalı).", MessageType.Error);
            else if (_scaleMax < _scaleMin)
                EditorGUILayout.HelpBox("Scale Max, Scale Min'den küçük olamaz.", MessageType.Error);
        }

        private void DoScatter()
        {
            // Tüm oluşturma işlemlerini tek bir Undo grubu altında topla —
            // Ctrl+Z ile parent + tüm child'lar tek seferde geri alınır.
            Undo.SetCurrentGroupName("Prop Scatter");
            int undoGroup = Undo.GetCurrentGroup();

            var parent = new GameObject(_parentName);
            Undo.RegisterCreatedObjectUndo(parent, "Prop Scatter");

            for (int i = 0; i < _count; i++)
            {
                float x     = Random.Range(_xMin, _xMax);
                float z     = Random.Range(_zMin, _zMax);
                float rotY  = Random.Range(0f, 360f);
                float scale = Random.Range(_scaleMin, _scaleMax);

                // InstantiatePrefab kullanıyoruz: prefab bağlantısı korunur,
                // Instantiate gibi bağlantıyı koparmaz.
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(_propPrefab);
                Undo.RegisterCreatedObjectUndo(instance, "Prop Scatter");

                instance.transform.SetPositionAndRotation(
                    new Vector3(x, _groundY, z),
                    Quaternion.Euler(0f, rotY, 0f));
                instance.transform.localScale = Vector3.one * scale;
                instance.transform.SetParent(parent.transform, worldPositionStays: true);
            }

            Undo.CollapseUndoOperations(undoGroup);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = parent;

            Debug.Log($"[PropScatterTool] {_count} × '{_propPrefab.name}' → '{_parentName}' altına yerleştirildi.");
        }
    }
}
