using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>Меню та інформаційна панель архітектурної площі.</summary>
public sealed class Lab11Interface : MonoBehaviour
{
    [SerializeField] Camera playerCamera;
    [SerializeField] Canvas mainMenu;
    [SerializeField] Canvas informationPanel;
    [SerializeField] Canvas wallButton;
    [SerializeField] Text informationTitle;
    [SerializeField] Text informationText;
    [SerializeField] AudioSource buttonAudio;
    [SerializeField] AudioClip clickSound;
    readonly Dictionary<XRBaseInteractor, InteractionLayerMask> savedLayers = new();
    readonly List<LocomotionProvider> pausedMovement = new();
    readonly List<XRBaseInteractor> pausedInteractors = new();
    readonly List<InputAction> uiActions = new();
    bool returnToMenu;
    bool blocked;
    bool shuttingDown;

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (mainMenu == null) CreateInterface();
        ApplyVideoRequirements();
        foreach (var ray in FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Include))
        {
            // Лише звичайний промінь керує UI; телепортаційний залишається для підлоги.
            if (ray.name.Contains("Teleport")) continue;
            ray.enableUIInteraction = true;
            ray.uiPressInput = CreatePressInput(ray.transform);
            ray.blockUIOnInteractableSelection = true;
        }
        foreach (var nearFar in FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include))
        {
            nearFar.enableUIInteraction = true;
            nearFar.uiPressInput = CreatePressInput(nearFar.transform);
        }
        informationPanel.gameObject.SetActive(false);
        mainMenu.gameObject.SetActive(true);
        PlaceInFront(mainMenu);
        SetWorldInteractionBlocked(true);
    }

    // Викликається також редактором, щоб усі Canvas були збережені у сцені.
    public void CreateInterface()
    {
        if (mainMenu != null) return;
        if (playerCamera == null) playerCamera = Camera.main;
        buttonAudio = gameObject.AddComponent<AudioSource>();
        buttonAudio.playOnAwake = false;
        buttonAudio.spatialBlend = 0f;
        buttonAudio.volume = .22f;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        mainMenu = NewCanvas("01 • Головне меню — World Space", new Vector2(1000, 860));
        Panel(mainMenu.transform, new Vector2(1000, 860));
        Label(mainMenu.transform, "ВІРТУАЛЬНІ БУДІВЛІ\nТА СПОРУДИ", new Vector2(0, 285), new Vector2(900, 160), 52, font);
        Label(mainMenu.transform, "Архітектурна площа • VR-проєкт", new Vector2(0, 155), new Vector2(900, 70), 30, font);
        AddButton(mainMenu.transform, "Почати", new Vector2(0, 35), new Vector2(680, 115), BeginExploration, font);
        AddButton(mainMenu.transform, "Інфо", new Vector2(0, -110), new Vector2(680, 115), ShowProjectInfo, font);
        AddButton(mainMenu.transform, "Вихід", new Vector2(0, -255), new Vector2(680, 115), ExitApplication, font);

        informationPanel = NewCanvas("02 • Інформаційна панель — World Space", new Vector2(1100, 900));
        Panel(informationPanel.transform, new Vector2(1100, 900));
        informationTitle = Label(informationPanel.transform, "ПАВІЛЬЙОН АРХІТЕКТУРИ", new Vector2(0, 330), new Vector2(1000, 100), 44, font);
        informationText = Label(informationPanel.transform, PavilionDescription, new Vector2(0, 35), new Vector2(950, 450), 33, font);
        informationText.alignment = TextAnchor.UpperLeft;
        AddButton(informationPanel.transform, "Закрити", new Vector2(0, -325), new Vector2(680, 110), CloseInformation, font);
        informationPanel.gameObject.SetActive(false);

        wallButton = NewCanvas("03 • Кнопка інформації на стіні", new Vector2(650, 240));
        wallButton.transform.position = new Vector3(-3f, 1.6f, 17.84f);
        wallButton.transform.rotation = Quaternion.identity;
        AddButton(wallButton.transform, "ІНФОРМАЦІЯ\nПРО ПАВІЛЬЙОН", Vector2.zero, new Vector2(650, 240), ShowPavilionInfo, font);
        ApplyVideoRequirements();
        PlaceInFront(mainMenu);
    }

    // Оновлює наявні об'єкти без перестворення кнопок, шрифтів та їхніх подій.
    public void ApplyVideoRequirements()
    {
        if (mainMenu != null)
        {
            var menuRect = (RectTransform)mainMenu.transform;
            menuRect.sizeDelta = new Vector2(800f, 600f);
            menuRect.localScale = Vector3.one * .001f; // 800×600 пікселів = 80×60 см.
            int labelIndex = 0;
            int buttonIndex = 0;
            foreach (Transform child in mainMenu.transform)
            {
                var rect = (RectTransform)child;
                if (child.name == "Фон панелі")
                {
                    rect.sizeDelta = menuRect.sizeDelta;
                    child.GetComponent<Image>().color = new Color(.1f, .1f, .15f, .95f);
                }
                else if (child.TryGetComponent<Text>(out var label))
                {
                    bool heading = labelIndex++ == 0;
                    rect.anchoredPosition = new Vector2(0f, heading ? 205f : 135f);
                    rect.sizeDelta = new Vector2(740f, heading ? 100f : 45f);
                    label.fontSize = heading ? 36 : 24;
                }
                else if (child.TryGetComponent<Button>(out var button))
                {
                    rect.anchoredPosition = new Vector2(0f, 40f - buttonIndex++ * 120f);
                    rect.sizeDelta = new Vector2(600f, 90f);
                    button.GetComponentInChildren<Text>(true).fontSize = 36;
                }
            }
        }
        foreach (var ray in FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Include))
        {
            // Дальність телепортації з лабораторної №8 залишається окремою.
            if (ray.name.Contains("Teleport")) continue;
            ray.maxRaycastDistance = 10f;
            ray.enableUIInteraction = true;
            var visual = ray.GetComponent<XRInteractorLineVisual>();
            if (visual != null) visual.lineWidth = .015f;
        }
    }

    const string PavilionDescription = "Призначення: виставка архітектури та будівельних матеріалів.\n\nУ павільйоні дві зали: майстерня матеріалів і галерея архітектури.\n\nТут можна оглянути моделі будівель, порівняти поверхні та перенести зразки між різними зонами освітлення.";

    public void SetClickSound(AudioClip clip) { clickSound = clip; }
    public void BeginExploration()
    {
        PlayClick();
        mainMenu.gameObject.SetActive(false);
        informationPanel.gameObject.SetActive(false);
        SetWorldInteractionBlocked(false);
    }
    public void ShowProjectInfo()
    {
        PlayClick();
        returnToMenu = true;
        informationTitle.text = "ПРО ПРОЄКТ";
        informationText.text = "Тема: віртуальні будівлі та споруди.\n\nАрхітектурна площа об’єднує лабораторні роботи 6–11.\n\nМожливості: захоплення предметів, променева взаємодія, сокет, телепортація, плавний рух, освітлення та просторовий звук.\n\nКеруйте кнопками променем контролера.";
        OpenInformation();
    }
    public void ShowPavilionInfo()
    {
        PlayClick();
        returnToMenu = false;
        informationTitle.text = "ПАВІЛЬЙОН АРХІТЕКТУРИ";
        informationText.text = PavilionDescription;
        OpenInformation();
    }
    void OpenInformation()
    {
        mainMenu.gameObject.SetActive(false);
        PlaceInFront(informationPanel);
        informationPanel.gameObject.SetActive(true);
        SetWorldInteractionBlocked(true);
    }
    public void CloseInformation()
    {
        PlayClick();
        informationPanel.gameObject.SetActive(false);
        mainMenu.gameObject.SetActive(returnToMenu);
        if (returnToMenu) PlaceInFront(mainMenu);
        else SetWorldInteractionBlocked(false);
    }
    public void ExitApplication()
    {
        PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    void PlayClick() { if (clickSound != null && buttonAudio != null) buttonAudio.PlayOneShot(clickSound); }

    void PlaceInFront(Canvas canvas)
    {
        if (playerCamera == null) return;
        Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
        Vector3 position = playerCamera.transform.position + forward * 2f;
        if (canvas == mainMenu) position.y = 1.5f;
        canvas.transform.position = position;
        canvas.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    void SetWorldInteractionBlocked(bool value)
    {
        if (blocked == value) return;
        blocked = value;
        if (value)
        {
            foreach (var provider in FindObjectsByType<LocomotionProvider>())
                if (provider.enabled) { pausedMovement.Add(provider); provider.enabled = false; }
            foreach (var interactor in FindObjectsByType<XRBaseInteractor>())
            {
                // Сокет продовжує утримувати вже встановлений предмет.
                if (interactor is XRSocketInteractor) continue;
                if ((interactor is XRRayInteractor || interactor is NearFarInteractor) && !interactor.name.Contains("Teleport"))
                {
                    savedLayers[interactor] = interactor.interactionLayers;
                    interactor.interactionLayers = 0;
                }
                else if (interactor.enabled) { pausedInteractors.Add(interactor); interactor.enabled = false; }
            }
        }
        else
        {
            foreach (var pair in savedLayers) if (pair.Key != null) pair.Key.interactionLayers = pair.Value;
            foreach (var interactor in pausedInteractors) if (interactor != null) interactor.enabled = true;
            foreach (var provider in pausedMovement) if (provider != null) provider.enabled = true;
            savedLayers.Clear(); pausedMovement.Clear(); pausedInteractors.Clear();
        }
    }
    void OnApplicationQuit() { shuttingDown = true; }
    void OnDisable() { if (!shuttingDown && Application.isPlaying) SetWorldInteractionBlocked(false); }
    XRInputButtonReader CreatePressInput(Transform hand)
    {
        bool left = false;
        for (Transform t = hand; t != null; t = t.parent)
            if (t.name.Contains("Left")) { left = true; break; }
        string usage = left ? "LeftHand" : "RightHand";
        var action = new InputAction("Lab11 UI Trigger " + usage, InputActionType.Button, "<XRController>{" + usage + "}/triggerPressed");
        action.AddBinding("<XRController>{" + usage + "}/gripPressed");
        action.Enable(); uiActions.Add(action);
        return new XRInputButtonReader { inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction, inputActionPerformed = action, inputActionValue = action };
    }
    void OnDestroy() { foreach (var action in uiActions) action.Dispose(); }


    Canvas NewCanvas(string name, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
        go.layer = 5;
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * .0015f;
        var rect = (RectTransform)go.transform; rect.sizeDelta = size;
        var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = playerCamera;
        var scaler = go.GetComponent<CanvasScaler>(); scaler.dynamicPixelsPerUnit = 10;
        var raycaster = go.GetComponent<TrackedDeviceGraphicRaycaster>(); raycaster.checkFor3DOcclusion = true;
        return canvas;
    }
    static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }
    static void Panel(Transform parent, Vector2 size)
    {
        var image = Rect("Фон панелі", parent, Vector2.zero, size).gameObject.AddComponent<Image>();
        image.color = new Color(.025f, .045f, .07f, .97f); image.raycastTarget = true;
    }
    static Text Label(Transform parent, string words, Vector2 position, Vector2 size, int fontSize, Font font)
    {
        var text = Rect(words.Split('\n')[0], parent, position, size).gameObject.AddComponent<Text>();
        text.font = font; text.text = words; text.fontSize = fontSize; text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white; text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }
    static void AddButton(Transform parent, string title, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, Font font)
    {
        var rect = Rect("Кнопка • " + title, parent, position, size);
        var image = rect.gameObject.AddComponent<Image>(); image.color = Color.white;
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colours = button.colors;
        colours.normalColor = new Color(.055f, .25f, .34f);
        colours.highlightedColor = new Color(.08f, .65f, .78f);
        colours.selectedColor = colours.highlightedColor;
        colours.pressedColor = new Color(.95f, .58f, .12f);
        colours.fadeDuration = .1f; button.colors = colours;
        var nav = button.navigation; nav.mode = Navigation.Mode.None; button.navigation = nav;
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, action);
        else button.onClick.AddListener(action);
#else
        button.onClick.AddListener(action);
#endif
        var label = Label(rect, title, Vector2.zero, size - new Vector2(40, 10), 42, font);
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.sizeDelta = new Vector2(-40, -10);
    }
}
