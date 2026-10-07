using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Автоматически собирает сцены Menu и Application по шагам лабораторной.
// Запускается сам при первом открытии проекта, либо вручную: Tools -> Lab -> Build Menu Scene
[InitializeOnLoad]
public static class MenuSceneBuilder
{
    const string SpriteDir = "Assets/GameMenu";
    const string MenuPath = "Assets/Scenes/Menu.unity";
    const string GamePath = "Assets/Scenes/Application.unity";

    static MenuSceneBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(MenuPath)) Build();
        };
    }

    [MenuItem("Tools/Lab/Build Menu Scene")]
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Scenes");
        SetupSprites();
        BuildGameScene();
        BuildMenuScene();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuPath, true), // index 0
            new EditorBuildSettingsScene(GamePath, true)  // index 1
        };
        EditorSceneManager.OpenScene(MenuPath);
        Debug.Log("Сцены Menu и Application собраны и добавлены в Build Settings.");
    }

    // ---------- Импорт PNG как Sprite ----------
    static void SetupSprites()
    {
        AssetDatabase.Refresh();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteDir }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            if (ti.textureType == TextureImporterType.Sprite) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }
    }

    static Sprite S(string name) =>
        AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDir}/{name}.png");

    // ---------- Общие хелперы ----------
    static GameObject CreateCanvas()
    {
        var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                                typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = 5;
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();                 // шаг 4
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        return go;
    }

    static GameObject CreateImage(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        go.GetComponent<Image>().sprite = sprite;
        return go;
    }

    static Button CreateButton(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 pos)
    {
        var go = CreateImage(name, parent, sprite, size, pos);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        return btn;
    }

    static GameObject CreateBackground(Transform canvas, Sprite sprite)
    {
        var bg = CreateImage("Background", canvas, sprite, Vector2.zero, Vector2.zero);
        var rt = (RectTransform)bg.transform;          // растянуть по рамке Canvas
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return bg;
    }

    // ---------- Сцена Menu (шаги 1–31) ----------
    static void BuildMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var canvas = CreateCanvas().transform;
        CreateBackground(canvas, S("background"));

        // MainMenu — пустой объект-группа с кнопками (шаги 11–13)
        var mainMenu = new GameObject("MainMenu", typeof(RectTransform));
        mainMenu.layer = 5;
        mainMenu.transform.SetParent(canvas, false);
        ((RectTransform)mainMenu.transform).sizeDelta = new Vector2(527, 617);

        var startBtn  = CreateButton("StartButton",  mainMenu.transform, S("start"),  new Vector2(450, 150), new Vector2(0, 200));
        var optionBtn = CreateButton("OptionButton", mainMenu.transform, S("option"), new Vector2(450, 150), new Vector2(0, 0));
        var quitBtn   = CreateButton("QuitButton",   mainMenu.transform, S("exit"),   new Vector2(450, 150), new Vector2(0, -200));

        // OptionsMenu + BackButton (шаги 15–20)
        var optionsMenu = CreateImage("OptionsMenu", canvas, S("optionsmenu"), new Vector2(650, 775), Vector2.zero);
        var backBtn = CreateButton("BackButton", optionsMenu.transform, S("backbutton"), new Vector2(80, 80), new Vector2(260, 320));

        // Скрипт MainMenu на объекте MainMenu (шаг 26)
        var script = mainMenu.AddComponent<MainMenu>();

        // OptionButton: MainMenu.SetActive(false), OptionsMenu.SetActive(true) (шаги 22–24)
        UnityEventTools.AddBoolPersistentListener(optionBtn.onClick, new UnityAction<bool>(mainMenu.SetActive), false);
        UnityEventTools.AddBoolPersistentListener(optionBtn.onClick, new UnityAction<bool>(optionsMenu.SetActive), true);

        // BackButton: наоборот (шаг 25)
        UnityEventTools.AddBoolPersistentListener(backBtn.onClick, new UnityAction<bool>(optionsMenu.SetActive), false);
        UnityEventTools.AddBoolPersistentListener(backBtn.onClick, new UnityAction<bool>(mainMenu.SetActive), true);

        // Start и Exit (шаги 30–31)
        UnityEventTools.AddPersistentListener(startBtn.onClick, new UnityAction(script.LoadLevel));
        UnityEventTools.AddPersistentListener(quitBtn.onClick, new UnityAction(script.ExitGame));

        // При запуске видно главное меню, окно опций скрыто (шаг 21)
        optionsMenu.SetActive(false);
        mainMenu.SetActive(true);

        EditorSceneManager.SaveScene(scene, MenuPath);
    }

    // ---------- Сцена Application (открывается по START) ----------
    static void BuildGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var canvas = CreateCanvas().transform;
        CreateBackground(canvas, S("gamescene"));
        EditorSceneManager.SaveScene(scene, GamePath);
    }
}
