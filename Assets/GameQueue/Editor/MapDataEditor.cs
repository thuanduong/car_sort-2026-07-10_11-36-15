#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class MapDataEditor : EditorWindow
{
    private MapData currentMapData;
    private int levelToLoad = 1;
    private Vector2 scrollPosition;
    private string levelDataPath = "Assets/Resources/Levels";

    private Dictionary<int, Texture2D> itemPreviews = new Dictionary<int, Texture2D>();
    private int selectedBrushType = 0; // "Cọ vẽ" đang được chọn
    private bool previewsLoaded = false;

    private const float PALETTE_BUTTON_SIZE = 50f;
    private const float GRID_BUTTON_SIZE = 45f;

    private static Color[] typeColors = new Color[]
            {
                Color.white,
                Color.red, Color.green, Color.blue, Color.yellow,
                Color.pink, Color.orange, Color.purple
            };

    [MenuItem("Tools/Game/Map Data Editor")]
    public static void ShowWindow()
    {
        GetWindow<MapDataEditor>("Map Data Editor");
    }

    void OnEnable()
    {
        levelDataPath = EditorPrefs.GetString("MapDataEditor_LevelPath", "Assets/Resources/Levels");
        LoadItemPreviews();
    }

    void OnDisable()
    {
        EditorPrefs.SetString("MapDataEditor_LevelPath", levelDataPath);
    }

    void OnGUI()
    {
        if (!previewsLoaded)
        {
            EditorGUILayout.HelpBox("Đang tải tài nguyên... Nếu cửa sổ không hiện, vui lòng mở lại.", MessageType.Info);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
        levelDataPath = EditorGUILayout.TextField("Level Data Path", levelDataPath);
        EditorGUILayout.HelpBox("Đường dẫn đến thư mục chứa file JSON của level. Ví dụ: Assets/Resources/Levels", MessageType.None);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Level Management", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        levelToLoad = EditorGUILayout.IntField("Level to Load/Create", levelToLoad);
        if (GUILayout.Button("Load Level"))
        {
            LoadMapData(levelToLoad);
        }
        if (GUILayout.Button("Create New Level"))
        {
            CreateNewMapData(levelToLoad);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (currentMapData == null)
        {
            EditorGUILayout.HelpBox("Load a level or create a new one to begin editing.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.LabelField($"Editing Level: {currentMapData.MapLevel}", EditorStyles.boldLabel);

        currentMapData.MaxMove = EditorGUILayout.IntField("Max Moves", currentMapData.MaxMove);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Grid Configuration", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        int newNumQueue = EditorGUILayout.IntField("Number of Queues", currentMapData.NumQueue);
        int newNumPerRow = EditorGUILayout.IntField("Items Per Queue", currentMapData.NumPerRow);

        if (EditorGUI.EndChangeCheck())
        {
            ResizeMap(newNumQueue, newNumPerRow);
        }

        EditorGUILayout.Space();

        // --- Bảng chọn "cọ vẽ" (Item Palette) ---
        DrawItemPalette();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Dummy Item (Click to change)", GUILayout.Width(EditorGUIUtility.labelWidth - 4));
        Texture2D dummyTexture = itemPreviews.ContainsKey(currentMapData.DummyType) ? itemPreviews[currentMapData.DummyType] : itemPreviews[0];
        if (GUILayout.Button(new GUIContent(dummyTexture), GUILayout.Width(GRID_BUTTON_SIZE), GUILayout.Height(GRID_BUTTON_SIZE)))
        {
            if (currentMapData.DummyType != selectedBrushType)
            {
                Undo.RecordObject(this, "Change Dummy Type");
                currentMapData.DummyType = selectedBrushType;
                EditorUtility.SetDirty(this);
            }
        }
        EditorGUILayout.LabelField($"Type: {currentMapData.DummyType}", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
        DrawVisualMapGrid();

        EditorGUILayout.Space();

        if (GUILayout.Button("Save Level", GUILayout.Height(40)))
        {
            SaveMapData();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawItemPalette()
    {
        EditorGUILayout.LabelField("Item Palette (Chọn loại xe để vẽ)", EditorStyles.boldLabel);
        using (var scrollView = new EditorGUILayout.ScrollViewScope(Vector2.zero, GUILayout.Height(PALETTE_BUTTON_SIZE + 20)))
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            var sortedPreviews = itemPreviews.OrderBy(kvp => kvp.Key);

            foreach (var preview in sortedPreviews)
            {
                int type = preview.Key;
                Texture2D texture = preview.Value;
                GUI.backgroundColor = (selectedBrushType == type) ? Color.cyan : Color.white;

                if (GUILayout.Button(new GUIContent(texture), GUILayout.Width(PALETTE_BUTTON_SIZE), GUILayout.Height(PALETTE_BUTTON_SIZE)))
                {
                    selectedBrushType = type;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawVisualMapGrid()
    {
        EditorGUILayout.LabelField("Item Layout", EditorStyles.boldLabel);
        if (currentMapData.Map == null || currentMapData.NumQueue <= 0) return;

        EditorGUILayout.BeginVertical(GUI.skin.box);
        int ll = currentMapData.NumPerRow;
        for (int j = ll - 1; j >= 0; j--) // j là hàng (row)
        {
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < currentMapData.NumQueue; i++) // i là cột (column/queue)
            {
                int index = i * currentMapData.NumPerRow + j;
                if (index >= currentMapData.Map.Count) continue;

                int currentTypeInCell = currentMapData.Map[index];
                Texture2D cellTexture = itemPreviews.ContainsKey(currentTypeInCell) ? itemPreviews[currentTypeInCell] : itemPreviews[0];

                if (GUILayout.Button(new GUIContent(cellTexture), GUILayout.Width(GRID_BUTTON_SIZE), GUILayout.Height(GRID_BUTTON_SIZE)))
                {
                    if (currentMapData.Map[index] != selectedBrushType)
                    {
                        Undo.RecordObject(this, "Paint Map Cell"); 
                        currentMapData.Map[index] = selectedBrushType;
                        EditorUtility.SetDirty(this); 
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void CreateNewMapData(int level)
    {
        currentMapData = new MapData
        {
            MapLevel = level,
            NumQueue = 4,
            NumPerRow = 4,
            MaxMove = 100,
            DummyType = 0,
            Map = new List<int>(new int[16]) // 4x4
        };
        levelToLoad = level;
        Debug.Log($"Created a new template for Level {level}.");
    }

    private void LoadMapData(int level)
    {
        string path = Path.Combine(levelDataPath, $"level_{level}.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            currentMapData = JsonUtility.FromJson<MapData>(json);
            levelToLoad = level;
            Debug.Log($"Successfully loaded Level {level} from {path}");
        }
        else
        {
            Debug.LogWarning($"Level file not found at {path}. A new template will be created if you save.");
            CreateNewMapData(level);
        }
    }

    private void SaveMapData()
    {
        if (currentMapData == null)
        {
            EditorUtility.DisplayDialog("Error", "No map data to save. Please load or create a level first.", "OK");
            return;
        }

        string directoryPath = levelDataPath;
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string filePath = Path.Combine(directoryPath, $"level_{currentMapData.MapLevel}.json");
        string json = JsonUtility.ToJson(currentMapData, true);

        File.WriteAllText(filePath, json);

        Debug.Log($"Level {currentMapData.MapLevel} saved successfully to {filePath}");
        AssetDatabase.Refresh(); 
    }

    private void ResizeMap(int newNumQueue, int newNumPerRow)
    {
        if (newNumQueue <= 0 || newNumPerRow <= 0) return;
        int newSize = newNumQueue * newNumPerRow;
        List<int> newMap = new List<int>(new int[newSize]);

        for (int i = 0; i < newNumQueue; i++)
        {
            for (int j = 0; j < newNumPerRow; j++)
            {
                int newIndex = i * newNumPerRow + j;
                if (i < currentMapData.NumQueue && j < currentMapData.NumPerRow && currentMapData.Map != null)
                {
                    int oldIndex = i * currentMapData.NumPerRow + j;
                    newMap[newIndex] = currentMapData.Map[oldIndex];
                }
            }
        }

        currentMapData.NumQueue = newNumQueue;
        currentMapData.NumPerRow = newNumPerRow;
        currentMapData.Map = newMap;
    }

    private void LoadItemPreviews()
    {
        itemPreviews.Clear();
        previewsLoaded = false;

        try
        {
            Func<Color, Texture2D> createColorTexture = (color) =>
            {
                int size = (int)PALETTE_BUTTON_SIZE;
                Texture2D tex = new Texture2D(size, size);
                Color[] pixels = new Color[size * size];
                for (int p = 0; p < pixels.Length; p++)
                {
                    pixels[p] = color;
                }
                tex.SetPixels(pixels);
                tex.Apply();
                return tex;
            };
            itemPreviews[-1] = createColorTexture(Color.black);

            // 1. Tạo texture cho ô trống (Type 0) là màu trắng
            itemPreviews[0] = createColorTexture(new Color(0.9f, 0.9f, 0.9f)); // Dùng màu xám nhạt cho ô trống

            // 2. Tạo texture màu cho tối đa 20 loại item khác nhau
            for (int i = 0; i < typeColors.Length; i++)
            {
                Color color = typeColors[i];
                itemPreviews[i] = createColorTexture(color);
            }
        }
        catch (Exception e) { Debug.LogError($"Lỗi khi tải item previews: {e.Message}"); }
        finally { previewsLoaded = true; }
    }
}

#endif