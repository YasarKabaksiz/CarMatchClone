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
        private readonly CellType[]        _cellTypes        = new CellType[TOTAL];
        private readonly CarColor[]        _cellColors       = new CarColor[TOTAL];  // CarSlot + LockedBox
        private readonly FacingDirection[] _cellFacings      = new FacingDirection[TOTAL];
        private readonly List<CarColor>[]  _cellGarageColors = new List<CarColor>[TOTAL]; // GarageSpawner

        // ── Fırça state ───────────────────────────────────────────────
        private CellType        _brushType    = CellType.Wall;
        private CarColor        _brushColor   = CarColor.None;
        private FacingDirection _brushFacing  = FacingDirection.Down;
        private List<CarColor>  _brushGarageColors = new List<CarColor>();

        // ── Asset & ayarlar ───────────────────────────────────────────
        private LevelData _target;
        private float     _cellSize = 1.5f;
        private bool      _isPainting;

        // ── Stiller ───────────────────────────────────────────────────
        private GUIStyle _coordLabel;
        private GUIStyle _overlayLabel;

        // CarSlot + LockedBox için tek renk seçimi aktif; Garage kendi listesini kullanır
        private bool BrushNeedsColor =>
            _brushType == CellType.CarSlot   ||
            _brushType == CellType.LockedBox;

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

            // --- Renk satırı (CarSlot + LockedBox için; Garage devre dışı) ---
            string colorLabel = _brushType == CellType.LockedBox ? "Gizli:" : "Renk:";
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

            // --- Yön satırı (yalnızca GarageSpawner aktif) ---
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Yön:", GUILayout.Width(32));
            GUI.enabled = BrushIsGarage;
            _brushFacing = (FacingDirection)EditorGUILayout.EnumPopup(
                _brushFacing, GUILayout.Width(90));
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // --- Garage renk listesi ---
            if (BrushIsGarage)
                DrawGarageColorList();
        }

        private void DrawGarageColorList()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Başlık + stok sayısı (salt okunur, listeden türetilir)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"Spawn Renk Sırası   Stok: {_brushGarageColors.Count}  (otomatik)",
                EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            // Ekle / Sil düğmeleri
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Renk Ekle", GUILayout.Width(90)))
            {
                _brushGarageColors.Add(CarColor.Red);
                Repaint();
            }
            GUI.enabled = _brushGarageColors.Count > 0;
            if (GUILayout.Button("- Son Rengi Sil", GUILayout.Width(110)))
            {
                _brushGarageColors.RemoveAt(_brushGarageColors.Count - 1);
                Repaint();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            if (_brushGarageColors.Count == 0)
            {
                EditorGUILayout.HelpBox("En az 1 renk ekleyin.", MessageType.Warning);
            }
            else
            {
                for (int i = 0; i < _brushGarageColors.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"{i + 1}.", GUILayout.Width(22));
                    _brushGarageColors[i] = (CarColor)EditorGUILayout.EnumPopup(
                        _brushGarageColors[i], GUILayout.Width(90));
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.HelpBox(
                "Yön ve renk sırasını ayarladıktan SONRA hücreyi boyayın.\n" +
                "Değer değiştirince hücreyi tekrar boyamak gerekir.",
                MessageType.Info);
        }

        private void TypeBtn(CellType type, string label, Color col)
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = _brushType == type ? col * 1.6f : col * 0.85f;
            if (GUILayout.Button(label, GUILayout.Width(80)))
            {
                _brushType = type;
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
                    EditorGUI.DrawRect(r, new Color(0.78f, 0.42f, 0.08f));
                    EditorGUI.DrawRect(
                        new Rect(r.x + 10, r.y + 10, r.width - 20, r.height - 20),
                        ToDisplayColor(_cellColors[idx]));
                    GUI.Label(new Rect(r.x + 2, r.y + 1, 14, 14), "L", _overlayLabel);
                    break;

                case CellType.GarageSpawner:
                    var gColors = _cellGarageColors[idx];
                    EditorGUI.DrawRect(r, new Color(0.45f, 0.10f, 0.65f));
                    // İç kare: sıradaki ilk rengi gösterir
                    var firstColor = (gColors != null && gColors.Count > 0)
                        ? gColors[0] : CarColor.None;
                    EditorGUI.DrawRect(
                        new Rect(r.x + 10, r.y + 10, r.width - 20, r.height - 20),
                        ToDisplayColor(firstColor));
                    // Sol üst: yön oku + stok sayısı
                    int stockCount = gColors?.Count ?? 0;
                    GUI.Label(
                        new Rect(r.x + 2, r.y + 1, r.width - 4, 14),
                        $"{DirectionArrow(_cellFacings[idx])} {stockCount}",
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
            _target = asset;
            Selection.activeObject = asset;
            Debug.Log($"[LevelEditor] Yeni asset → {path}");
        }

        // ── Yardımcılar ──────────────────────────────────────────────
        private void ClearGrid()
        {
            for (int i = 0; i < TOTAL; i++)
            {
                _cellTypes[i]        = CellType.Empty;
                _cellColors[i]       = CarColor.None;
                _cellFacings[i]      = FacingDirection.Down;
                _cellGarageColors[i] = new List<CarColor>();
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
                int idx = y * COLS + x;
                _cellTypes[idx]   = e.type;
                _cellColors[idx]  = e.color;
                _cellFacings[idx] = e.facingDirection;
                _cellGarageColors[idx] = e.garageColors != null
                    ? new List<CarColor>(e.garageColors)
                    : new List<CarColor>();
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
                        position        = new Vector2Int(x, y),
                        type            = _cellTypes[idx],
                        color           = _cellColors[idx],
                        facingDirection = _cellFacings[idx],
                        garageColors    = _cellGarageColors[idx]?.ToArray()
                                          ?? System.Array.Empty<CarColor>()
                    });
                }
            asset.cells    = cells.ToArray();
            asset.cellSize = _cellSize;
        }

        private void ApplyBrush(int idx)
        {
            _cellTypes[idx]   = _brushType;
            _cellFacings[idx] = _brushFacing;

            switch (_brushType)
            {
                case CellType.CarSlot:
                case CellType.LockedBox:
                    _cellColors[idx]       = _brushColor;
                    _cellGarageColors[idx] = new List<CarColor>();
                    break;

                case CellType.GarageSpawner:
                    _cellColors[idx]       = CarColor.None;
                    _cellGarageColors[idx] = new List<CarColor>(_brushGarageColors);
                    break;

                default: // Empty, Wall
                    _cellColors[idx]       = CarColor.None;
                    _cellGarageColors[idx] = new List<CarColor>();
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
