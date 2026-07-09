using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CarMatchClone.Data;

namespace CarMatchClone.Editor
{
    public class LevelEditorWindow : EditorWindow
    {
        private const int   COLS    = 7;
        private const int   ROWS    = 8;
        private const int   TOTAL   = COLS * ROWS;
        private const float CELL_PX = 48f;

        // ── Grid state ────────────────────────────────────────────────
        private readonly CellType[]       _cellTypes   = new CellType[TOTAL];
        private readonly CarColor[]       _cellColors  = new CarColor[TOTAL];
        private readonly FacingDirection[] _cellFacings = new FacingDirection[TOTAL];
        private readonly int[]            _cellStocks  = new int[TOTAL];

        // ── Fırça state ───────────────────────────────────────────────
        private CellType        _brushType   = CellType.Wall;
        private CarColor        _brushColor  = CarColor.None;
        private FacingDirection _brushFacing = FacingDirection.Down;
        private int             _brushStock  = 1;

        // ── Asset & ayarlar ───────────────────────────────────────────
        private LevelData _target;
        private float     _cellSize = 1.5f;
        private bool      _isPainting;

        // ── Stiller ───────────────────────────────────────────────────
        private GUIStyle _coordLabel;
        private GUIStyle _overlayLabel;

        // Renk satırının aktif olduğu tipler
        private bool BrushNeedsColor =>
            _brushType == CellType.CarSlot     ||
            _brushType == CellType.LockedBox   ||
            _brushType == CellType.GarageSpawner;

        private bool BrushIsGarage => _brushType == CellType.GarageSpawner;

        // ── Menü öğesi ───────────────────────────────────────────────
        [MenuItem("Window/CarMatchClone/Level Editor")]
        public static void Open() => GetWindow<LevelEditorWindow>("Level Editor");

        // ── Lifecycle ────────────────────────────────────────────────
        private void OnEnable()
        {
            minSize = new Vector2(430f, 700f);
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
            if (picked != _target) LoadAsset(picked);
            GUILayout.Space(12);
            EditorGUILayout.LabelField("Cell Size:", GUILayout.Width(60));
            _cellSize = EditorGUILayout.FloatField(_cellSize, GUILayout.Width(46));
            if (GUILayout.Button("Temizle", GUILayout.Width(60))) ClearGrid();
            EditorGUILayout.EndHorizontal();
        }

        // ── Fırça toolbar'ı ──────────────────────────────────────────
        private void DrawBrushToolbar()
        {
            // --- Tip satırı 1: temel tipler ---
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Tip:", GUILayout.Width(32));
            TypeBtn(CellType.CarSlot, "CarSlot", new Color(0.78f, 0.78f, 0.78f));
            TypeBtn(CellType.Empty,   "Empty",   new Color(0.40f, 0.70f, 0.40f));
            TypeBtn(CellType.Wall,    "Wall",    new Color(0.32f, 0.32f, 0.32f));
            EditorGUILayout.EndHorizontal();

            // --- Tip satırı 2: engel tipleri ---
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(32));
            TypeBtn(CellType.LockedBox,     "LockedBox", new Color(0.78f, 0.42f, 0.08f));
            TypeBtn(CellType.GarageSpawner, "Garage",    new Color(0.50f, 0.12f, 0.70f));
            EditorGUILayout.EndHorizontal();

            // --- Renk satırı ---
            // Label'ı seçili tipe göre değiştir: ne için renk seçildiği belli olsun
            string colorLabel = _brushType == CellType.LockedBox     ? "Gizli:"
                              : _brushType == CellType.GarageSpawner ? "Spawn:"
                              : "Renk:";

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(colorLabel, GUILayout.Width(44));
            GUI.enabled = BrushNeedsColor;
            ColorBtn(CarColor.Red,    new Color(0.90f, 0.20f, 0.20f));
            ColorBtn(CarColor.Blue,   new Color(0.20f, 0.40f, 0.90f));
            ColorBtn(CarColor.Green,  new Color(0.20f, 0.78f, 0.30f));
            ColorBtn(CarColor.Yellow, new Color(0.92f, 0.80f, 0.10f));
            ColorBtn(CarColor.Purple, new Color(0.60f, 0.20f, 0.82f));
            ColorBtn(CarColor.Orange, new Color(0.92f, 0.52f, 0.12f));
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // --- Yön & Stok satırı (yalnızca GarageSpawner aktif) ---
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Yön:", GUILayout.Width(32));
            GUI.enabled = BrushIsGarage;
            _brushFacing = (FacingDirection)EditorGUILayout.EnumPopup(
                _brushFacing, GUILayout.Width(90));
            GUILayout.Space(12);
            // Stok etiketi: mevcut fırça değerini göster, sıfır ise uyarı rengi
            var prevC = GUI.contentColor;
            GUI.contentColor = (BrushIsGarage && _brushStock == 0) ? new Color(1f, 0.4f, 0.4f) : Color.white;
            GUILayout.Label($"Stok [{_brushStock}]:", GUILayout.Width(58));
            GUI.contentColor = prevC;
            _brushStock = Mathf.Max(0, EditorGUILayout.IntField(_brushStock, GUILayout.Width(40)));
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Uyarı: Garage seçiliyken parametreleri boya ÖNCE ayarla
            if (BrushIsGarage)
            {
                EditorGUILayout.HelpBox(
                    "Yön ve Stok değerlerini ayarladıktan SONRA hücreyi boyayın.\n" +
                    "Değer değiştirince hücreyi tekrar boyamak gerekir.",
                    MessageType.Info);
            }
        }

        private void TypeBtn(CellType type, string label, Color col)
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = _brushType == type ? col * 1.6f : col * 0.85f;
            if (GUILayout.Button(label, GUILayout.Width(80)))
            {
                _brushType = type;
                // Renk sadece renk gerektiren tipler için korunur;
                // Wall ve Empty seçilince sıfırlanır
                if (type == CellType.Wall || type == CellType.Empty)
                    _brushColor = CarColor.None;
            }
            GUI.backgroundColor = prev;
        }

        private void ColorBtn(CarColor color, Color displayCol)
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = _brushColor == color ? displayCol * 1.5f : displayCol;
            if (GUILayout.Button(color.ToString(), GUILayout.Width(52)))
                _brushColor = color;
            GUI.backgroundColor = prev;
        }

        // ── Grid (8×7) ───────────────────────────────────────────────
        private void DrawGrid()
        {
            float w    = COLS * CELL_PX;
            float h    = ROWS * CELL_PX;
            Rect  area = GUILayoutUtility.GetRect(w, h, GUILayout.Width(w), GUILayout.Height(h));
            Event e    = Event.current;

            for (int y = ROWS - 1; y >= 0; y--)
            {
                for (int x = 0; x < COLS; x++)
                {
                    int  idx      = y * COLS + x;
                    var  cellRect = new Rect(
                        area.x + x * CELL_PX + 1,
                        area.y + (ROWS - 1 - y) * CELL_PX + 1,
                        CELL_PX - 2,
                        CELL_PX - 2);

                    DrawCell(cellRect, idx);

                    // Koordinat etiketi (sol alt)
                    GUI.Label(
                        new Rect(cellRect.x + 2, cellRect.yMax - 13, cellRect.width, 13),
                        $"{x},{y}", _coordLabel);

                    bool over = cellRect.Contains(e.mousePosition);
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

        private void DrawCell(Rect r, int idx)
        {
            switch (_cellTypes[idx])
            {
                case CellType.CarSlot:
                    EditorGUI.DrawRect(r, ToDisplayColor(_cellColors[idx]));
                    break;

                case CellType.Empty:
                    EditorGUI.DrawRect(r, new Color(0.40f, 0.70f, 0.40f));
                    break;

                case CellType.Wall:
                    EditorGUI.DrawRect(r, new Color(0.25f, 0.25f, 0.25f));
                    break;

                case CellType.LockedBox:
                    // Dış çerçeve: koyu amber → "kilitli" hücre rengi
                    EditorGUI.DrawRect(r, new Color(0.78f, 0.42f, 0.08f));
                    // İç kare: gizli araç rengi
                    EditorGUI.DrawRect(
                        new Rect(r.x + 10, r.y + 10, r.width - 20, r.height - 20),
                        ToDisplayColor(_cellColors[idx]));
                    // "L" etiketi sol üst
                    GUI.Label(new Rect(r.x + 2, r.y + 1, 14, 14), "L", _overlayLabel);
                    break;

                case CellType.GarageSpawner:
                    // Dış çerçeve: koyu mor → garaj rengi
                    EditorGUI.DrawRect(r, new Color(0.45f, 0.10f, 0.65f));
                    // İç kare: spawn araç rengi
                    EditorGUI.DrawRect(
                        new Rect(r.x + 10, r.y + 10, r.width - 20, r.height - 20),
                        ToDisplayColor(_cellColors[idx]));
                    // Yön oku + stok sayısı sol üst ("↓ 2" formatında)
                    GUI.Label(
                        new Rect(r.x + 2, r.y + 1, r.width - 4, 14),
                        $"{DirectionArrow(_cellFacings[idx])} {_cellStocks[idx]}",
                        _overlayLabel);
                    break;

                default:
                    EditorGUI.DrawRect(r, Color.magenta);
                    break;
            }
        }

        private static string DirectionArrow(FacingDirection d)
        {
            switch (d)
            {
                case FacingDirection.Up:    return "↑";
                case FacingDirection.Down:  return "↓";
                case FacingDirection.Left:  return "←";
                case FacingDirection.Right: return "→";
                default:                   return "?";
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
            _target   = asset;
            Selection.activeObject = asset;
            Debug.Log($"[LevelEditor] Yeni asset → {path}");
        }

        // ── Yardımcılar ──────────────────────────────────────────────
        private void ClearGrid()
        {
            for (int i = 0; i < TOTAL; i++)
            {
                _cellTypes[i]   = CellType.Empty;
                _cellColors[i]  = CarColor.None;
                _cellFacings[i] = FacingDirection.Down;
                _cellStocks[i]  = 0;
            }
            Repaint();
        }

        private void LoadAsset(LevelData asset)
        {
            _target = asset;
            ClearGrid();
            if (asset == null || asset.cells == null) return;

            _cellSize = asset.cellSize;
            foreach (var e in asset.cells)
            {
                int x = e.position.x, y = e.position.y;
                if (x < 0 || x >= COLS || y < 0 || y >= ROWS) continue;
                int idx         = y * COLS + x;
                _cellTypes[idx]   = e.type;
                _cellColors[idx]  = e.color;
                _cellFacings[idx] = e.facingDirection;
                _cellStocks[idx]  = e.garageStockCount;
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
                        position         = new Vector2Int(x, y),
                        type             = _cellTypes[idx],
                        color            = _cellColors[idx],
                        facingDirection  = _cellFacings[idx],
                        garageStockCount = _cellStocks[idx]
                    });
                }
            asset.cells    = cells.ToArray();
            asset.cellSize = _cellSize;
        }

        private void ApplyBrush(int idx)
        {
            _cellTypes[idx] = _brushType;
            switch (_brushType)
            {
                case CellType.CarSlot:
                case CellType.LockedBox:
                    _cellColors[idx]  = _brushColor;
                    _cellFacings[idx] = FacingDirection.Down;
                    _cellStocks[idx]  = 0;
                    break;
                case CellType.GarageSpawner:
                    _cellColors[idx]  = _brushColor;
                    _cellFacings[idx] = _brushFacing;
                    _cellStocks[idx]  = _brushStock;
                    break;
                default: // Empty, Wall
                    _cellColors[idx]  = CarColor.None;
                    _cellFacings[idx] = FacingDirection.Down;
                    _cellStocks[idx]  = 0;
                    break;
            }
        }

        private static Color ToDisplayColor(CarColor c)
        {
            switch (c)
            {
                case CarColor.Red:    return new Color(0.90f, 0.22f, 0.22f);
                case CarColor.Blue:   return new Color(0.22f, 0.42f, 0.90f);
                case CarColor.Green:  return new Color(0.22f, 0.78f, 0.32f);
                case CarColor.Yellow: return new Color(0.92f, 0.80f, 0.10f);
                case CarColor.Purple: return new Color(0.62f, 0.22f, 0.82f);
                case CarColor.Orange: return new Color(0.92f, 0.52f, 0.12f);
                default:              return new Color(0.72f, 0.72f, 0.72f);
            }
        }

        private void InitStyles()
        {
            if (_coordLabel != null) return;
            _coordLabel = new GUIStyle(EditorStyles.label)
            {
                fontSize  = 7,
                alignment = TextAnchor.LowerLeft,
                normal    = { textColor = new Color(0f, 0f, 0f, 0.45f) }
            };
            _overlayLabel = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 11,
                alignment = TextAnchor.UpperLeft,
                normal    = { textColor = Color.white }
            };
        }
    }
}
