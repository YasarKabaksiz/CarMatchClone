using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CarMatchClone.Data;

namespace CarMatchClone.Editor
{
    public class LevelEditorWindow : EditorWindow
    {
        private const int COLS = 7;
        private const int ROWS = 8;
        private const int TOTAL = COLS * ROWS;
        private const float CELL_PX = 48f;

        // 56-elemanlı iç state
        private readonly CellType[] _cellTypes  = new CellType[TOTAL];
        private readonly CarColor[]  _cellColors = new CarColor[TOTAL];

        // Aktif fırça
        private CellType _brushType  = CellType.Wall;
        private CarColor _brushColor = CarColor.None;

        // Yüklü asset ve ayarlar
        private LevelData _target;
        private float     _cellSize = 1.5f;
        private bool      _isPainting;

        // Lazy-init stiller (OnGUI dışında oluşturulamaz)
        private GUIStyle _coordLabel;

        // ── Menü öğesi ───────────────────────────────────────────────
        [MenuItem("Window/CarMatchClone/Level Editor")]
        public static void Open() => GetWindow<LevelEditorWindow>("Level Editor");

        // ── Lifecycle ────────────────────────────────────────────────
        private void OnEnable()
        {
            minSize = new Vector2(430f, 600f);
            ClearGrid();
            TryLoadFromSelection();
        }

        private void OnSelectionChange()
        {
            TryLoadFromSelection();
            Repaint();
        }

        private void TryLoadFromSelection()
        {
            if (Selection.activeObject is LevelData ld && ld != _target)
                LoadAsset(ld);
        }

        // ── Ana çizim ────────────────────────────────────────────────
        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.Space(6);
            DrawAssetField();
            EditorGUILayout.Space(4);
            DrawBrushToolbar();
            EditorGUILayout.Space(8);
            DrawGrid();
            EditorGUILayout.Space(10);
            DrawActionButtons();

            if (Event.current.type == EventType.MouseUp)
            {
                _isPainting = false;
                Repaint();
            }
        }

        // ── Asset seçici ─────────────────────────────────────────────
        private void DrawAssetField()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("LevelData:", GUILayout.Width(72));
            var picked = (LevelData)EditorGUILayout.ObjectField(
                _target, typeof(LevelData), allowSceneObjects: false);
            if (picked != _target)
                LoadAsset(picked);

            GUILayout.Space(12);
            EditorGUILayout.LabelField("Cell Size:", GUILayout.Width(60));
            _cellSize = EditorGUILayout.FloatField(_cellSize, GUILayout.Width(46));

            if (GUILayout.Button("Temizle", GUILayout.Width(60)))
                ClearGrid();

            EditorGUILayout.EndHorizontal();
        }

        // ── Fırça toolbar'ı ──────────────────────────────────────────
        private void DrawBrushToolbar()
        {
            // CellType satırı
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Tip:", GUILayout.Width(32));
            TypeBtn(CellType.CarSlot, "CarSlot", new Color(0.78f, 0.78f, 0.78f));
            TypeBtn(CellType.Empty,   "Empty",   new Color(0.40f, 0.70f, 0.40f));
            TypeBtn(CellType.Wall,    "Wall",    new Color(0.32f, 0.32f, 0.32f));
            EditorGUILayout.EndHorizontal();

            // Color satırı (yalnızca CarSlot seçiliyken etkin)
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Renk:", GUILayout.Width(32));
            GUI.enabled = _brushType == CellType.CarSlot;
            ColorBtn(CarColor.Red,    new Color(0.90f, 0.20f, 0.20f));
            ColorBtn(CarColor.Blue,   new Color(0.20f, 0.40f, 0.90f));
            ColorBtn(CarColor.Green,  new Color(0.20f, 0.78f, 0.30f));
            ColorBtn(CarColor.Yellow, new Color(0.92f, 0.80f, 0.10f));
            ColorBtn(CarColor.Purple, new Color(0.60f, 0.20f, 0.82f));
            ColorBtn(CarColor.Orange, new Color(0.92f, 0.52f, 0.12f));
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void TypeBtn(CellType type, string label, Color col)
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = _brushType == type ? col * 1.6f : col * 0.9f;
            if (GUILayout.Button(label, GUILayout.Width(68)))
            {
                _brushType = type;
                if (type != CellType.CarSlot) _brushColor = CarColor.None;
            }
            GUI.backgroundColor = prev;
        }

        private void ColorBtn(CarColor color, Color displayCol)
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = _brushColor == color ? displayCol * 1.5f : displayCol;
            if (GUILayout.Button(color.ToString(), GUILayout.Width(58)))
                _brushColor = color;
            GUI.backgroundColor = prev;
        }

        // ── Grid (8×7) ───────────────────────────────────────────────
        private void DrawGrid()
        {
            float w = COLS * CELL_PX;
            float h = ROWS * CELL_PX;
            Rect area = GUILayoutUtility.GetRect(w, h, GUILayout.Width(w), GUILayout.Height(h));

            Event e = Event.current;

            // y=6 üstte, y=0 altta gösterilir (exit'e en yakın sıra aşağıda)
            for (int y = ROWS - 1; y >= 0; y--)
            {
                for (int x = 0; x < COLS; x++)
                {
                    int idx = y * COLS + x;
                    var cell = new Rect(
                        area.x + x * CELL_PX + 1,
                        area.y + (ROWS - 1 - y) * CELL_PX + 1,
                        CELL_PX - 2,
                        CELL_PX - 2);

                    EditorGUI.DrawRect(cell, CellColor(idx));
                    GUI.Label(new Rect(cell.x + 2, cell.yMax - 13, cell.width, 13),
                        $"{x},{y}", _coordLabel);

                    bool over = cell.Contains(e.mousePosition);
                    if (over && (e.type == EventType.MouseDown ||
                                (_isPainting && e.type == EventType.MouseDrag)))
                    {
                        if (e.type == EventType.MouseDown) _isPainting = true;
                        ApplyBrush(idx);
                        GUI.changed = true;
                        Repaint();
                        e.Use();
                    }
                }
            }
        }

        // ── Save / Save As ───────────────────────────────────────────
        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = _target != null;
            if (GUILayout.Button("Save", GUILayout.Height(28)))
            {
                WriteToAsset(_target);
                EditorUtility.SetDirty(_target);
                AssetDatabase.SaveAssets();
                Debug.Log($"[LevelEditor] Kaydedildi → {AssetDatabase.GetAssetPath(_target)}");
            }
            GUI.enabled = true;

            if (GUILayout.Button("Save As…", GUILayout.Height(28)))
                SaveAs();

            EditorGUILayout.EndHorizontal();
        }

        private void SaveAs()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Level Kaydet", "Level_New", "asset",
                "Dosya adını girin", "Assets/_Project/Levels");
            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<LevelData>();
            WriteToAsset(asset);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            _target = asset;
            Selection.activeObject = asset;
            Debug.Log($"[LevelEditor] Yeni asset → {path}");
        }

        // ── Yardımcılar ──────────────────────────────────────────────
        private void ClearGrid()
        {
            for (int i = 0; i < TOTAL; i++)
            {
                _cellTypes[i]  = CellType.Empty;
                _cellColors[i] = CarColor.None;
            }
            Repaint();
        }

        private void LoadAsset(LevelData asset)
        {
            _target = asset;
            ClearGrid();
            if (asset == null) return;

            _cellSize = asset.cellSize;

            if (asset.cells != null)
                foreach (var e in asset.cells)
                {
                    int x = e.position.x, y = e.position.y;
                    if (x < 0 || x >= COLS || y < 0 || y >= ROWS) continue;
                    int idx = y * COLS + x;
                    _cellTypes[idx]  = e.type;
                    _cellColors[idx] = e.color;
                }

        }

        private void WriteToAsset(LevelData asset)
        {
            var cells = new List<LevelData.CellEntry>(TOTAL);
            for (int y = 0; y < ROWS; y++)
                for (int x = 0; x < COLS; x++)
                {
                    int idx = y * COLS + x;
                    cells.Add(new LevelData.CellEntry
                    {
                        position = new Vector2Int(x, y),
                        type     = _cellTypes[idx],
                        color    = _cellColors[idx]
                    });
                }
            asset.cells    = cells.ToArray();
            asset.cellSize = _cellSize;
        }

        private void ApplyBrush(int idx)
        {
            _cellTypes[idx]  = _brushType;
            _cellColors[idx] = _brushType == CellType.CarSlot ? _brushColor : CarColor.None;
        }

        private Color CellColor(int idx) => _cellTypes[idx] switch
        {
            CellType.CarSlot => ToDisplayColor(_cellColors[idx]),
            CellType.Empty   => new Color(0.40f, 0.70f, 0.40f),
            CellType.Wall    => new Color(0.25f, 0.25f, 0.25f),
            _                => Color.magenta
        };

        private static Color ToDisplayColor(CarColor c) => c switch
        {
            Data.CarColor.Red    => new Color(0.90f, 0.22f, 0.22f),
            Data.CarColor.Blue   => new Color(0.22f, 0.42f, 0.90f),
            Data.CarColor.Green  => new Color(0.22f, 0.78f, 0.32f),
            Data.CarColor.Yellow => new Color(0.92f, 0.80f, 0.10f),
            Data.CarColor.Purple => new Color(0.62f, 0.22f, 0.82f),
            Data.CarColor.Orange => new Color(0.92f, 0.52f, 0.12f),
            _                    => new Color(0.72f, 0.72f, 0.72f)
        };

        private void InitStyles()
        {
            if (_coordLabel != null) return;
            _coordLabel = new GUIStyle(EditorStyles.label)
            {
                fontSize  = 7,
                alignment = TextAnchor.LowerLeft,
                normal    = { textColor = new Color(0f, 0f, 0f, 0.45f) }
            };
        }
    }
}
