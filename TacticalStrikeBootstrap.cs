using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TacticalStrikeBootstrap : MonoBehaviour
{
    CharacterController controller;
    Camera playerCamera;
    float yaw, pitch, verticalVelocity, nextShot;
    Vector2 moveStick;
    bool fireHeld;
    int score;
    Text hud;
    readonly List<GameObject> targets = new List<GameObject>();
    Material sand, stone, dark, red, skin;

    void Start()
    {
        Application.targetFrameRate = 60;
        CreateMaterials();
        BuildArena();
        BuildPlayer();
        BuildUI();
        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (!controller || !playerCamera) return;
        float dt = Time.deltaTime;
        float lookX = 0f, lookY = 0f;

        if (Application.isMobilePlatform)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.position.x > Screen.width * 0.38f && touch.phase == TouchPhase.Moved)
                {
                    lookX += touch.deltaPosition.x * 0.12f;
                    lookY += touch.deltaPosition.y * 0.12f;
                }
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                lookX = Input.GetAxis("Mouse X") * 2.2f;
                lookY = Input.GetAxis("Mouse Y") * 2.2f;
            }
        }

        yaw += lookX;
        pitch = Mathf.Clamp(pitch - lookY, -78f, 78f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        Vector2 input = moveStick;
        if (!Application.isMobilePlatform)
            input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector3 direction = transform.right * input.x + transform.forward * input.y;
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        if (controller.isGrounded) verticalVelocity = -1f;
        else verticalVelocity -= 18f * dt;
        if (!Application.isMobilePlatform && Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
            verticalVelocity = 6f;

        controller.Move((direction * 5.2f + Vector3.up * verticalVelocity) * dt);

        if (!Application.isMobilePlatform && Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.Locked)
            Shoot();
        if (fireHeld && Time.time >= nextShot) Shoot();

        if (hud)
            hud.text = "TACTICAL STRIKE  •  Цели: " + score + "  •  " + (Application.isMobilePlatform ? "ANDROID" : "EDITOR");
    }

    void CreateMaterials()
    {
        sand = MakeMaterial(new Color(0.66f, 0.55f, 0.39f));
        stone = MakeMaterial(new Color(0.76f, 0.68f, 0.53f));
        dark = MakeMaterial(new Color(0.16f, 0.18f, 0.20f));
        red = MakeMaterial(new Color(0.62f, 0.12f, 0.09f));
        skin = MakeMaterial(new Color(0.82f, 0.69f, 0.54f));
    }

    void BuildArena()
    {
        Cube("Ground", new Vector3(0, -0.5f, 0), new Vector3(42, 1, 42), sand);
        Cube("North perimeter", new Vector3(0, 1.5f, 20), new Vector3(42, 4, 1), stone);
        Cube("South perimeter", new Vector3(0, 1.5f, -20), new Vector3(42, 4, 1), stone);
        Cube("East perimeter", new Vector3(20, 1.5f, 0), new Vector3(1, 4, 42), stone);
        Cube("West perimeter", new Vector3(-20, 1.5f, 0), new Vector3(1, 4, 42), stone);

        for (int i = 0; i < 10; i++)
        {
            float x = (i % 5) * 6.2f - 12.4f;
            float z = (i / 5) * 10f - 2f;
            Cube("Stone cover " + i, new Vector3(x, 0.8f, z), new Vector3(2.4f, 1.6f, 2.4f), i % 2 == 0 ? stone : dark);
        }
        Cube("Site A", new Vector3(-13, 0.03f, 13), new Vector3(4, 0.06f, 4), red);
        Cube("Site B", new Vector3(13, 0.03f, -13), new Vector3(4, 0.06f, 4), red);
        for (int i = 0; i < 6; i++)
            SpawnTarget(new Vector3(-10 + (i % 3) * 10, 1f, 8 - (i / 3) * 16));

        RenderSettings.ambientLight = new Color(0.68f, 0.65f, 0.58f);
        GameObject sun = new GameObject("Sun");
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
    }

    void BuildPlayer()
    {
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0, 0, -15);
        controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.32f;
        controller.center = new Vector3(0, 0.9f, 0);

        playerCamera = new GameObject("FPS Camera").AddComponent<Camera>();
        playerCamera.transform.SetParent(player.transform);
        playerCamera.transform.localPosition = new Vector3(0, 1.58f, 0);
        playerCamera.fieldOfView = 78f;

        GameObject receiver = GameObject.CreatePrimitive(PrimitiveType.Cube);
        receiver.name = "Prototype rifle receiver";
        receiver.transform.SetParent(playerCamera.transform);
        receiver.transform.localPosition = new Vector3(0.28f, -0.22f, 0.55f);
        receiver.transform.localScale = new Vector3(0.13f, 0.13f, 0.62f);
        receiver.GetComponent<Renderer>().material = dark;
        Destroy(receiver.GetComponent<Collider>());

        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "Prototype rifle barrel";
        barrel.transform.SetParent(playerCamera.transform);
        barrel.transform.localPosition = new Vector3(0.28f, -0.19f, 0.88f);
        barrel.transform.localRotation = Quaternion.Euler(90, 0, 0);
        barrel.transform.localScale = new Vector3(0.035f, 0.22f, 0.035f);
        barrel.GetComponent<Renderer>().material = dark;
        Destroy(barrel.GetComponent<Collider>());
    }

    void BuildUI()
    {
        GameObject canvasObject = new GameObject("Mobile HUD");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObject.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        hud = AddLabel(canvas.transform, "HUD", "TACTICAL STRIKE", 24,
            new Vector2(0.5f, 0.96f), new Vector2(0.5f, 0.96f),
            Vector2.zero, new Vector2(900, 55), TextAnchor.MiddleCenter);

        Image pad = AddPanel(canvas.transform, new Vector2(0.13f, 0.18f),
            new Vector2(150, 150), new Color(1, 1, 1, 0.20f));
        AddLabel(pad.transform, "MoveLabel", "MOVE", 17, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);

        EventTrigger padTrigger = pad.gameObject.AddComponent<EventTrigger>();
        AddTrigger(padTrigger, EventTriggerType.Drag, data =>
        {
            PointerEventData pointer = (PointerEventData)data;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                pad.rectTransform, pointer.position, pointer.pressEventCamera, out Vector2 local);
            moveStick = Vector2.ClampMagnitude(local / 55f, 1f);
        });
        AddTrigger(padTrigger, EventTriggerType.PointerUp, data => moveStick = Vector2.zero);
        AddTrigger(padTrigger, EventTriggerType.EndDrag, data => moveStick = Vector2.zero);

        Button jump = AddButton(canvas.transform, "ПРЫЖОК",
            new Vector2(0.86f, 0.16f), new Vector2(145, 68));
        jump.onClick.AddListener(() => { if (controller.isGrounded) verticalVelocity = 6f; });

        Button fire = AddButton(canvas.transform, "ОГОНЬ",
            new Vector2(0.86f, 0.30f), new Vector2(145, 80));
        EventTrigger fireTrigger = fire.gameObject.AddComponent<EventTrigger>();
        AddTrigger(fireTrigger, EventTriggerType.PointerDown, data => { fireHeld = true; Shoot(); });
        AddTrigger(fireTrigger, EventTriggerType.PointerUp, data => fireHeld = false);
        AddTrigger(fireTrigger, EventTriggerType.PointerExit, data => fireHeld = false);

        Button reset = AddButton(canvas.transform, "СБРОС ЦЕЛЕЙ",
            new Vector2(0.87f, 0.92f), new Vector2(180, 48));
        reset.onClick.AddListener(ResetTargets);

        AddLabel(canvas.transform, "Hint",
            "Слева — движение  •  Справа — обзор  •  ОГОНЬ — стрельба", 17,
            new Vector2(0.5f, 0.04f), new Vector2(0.5f, 0.04f),
            Vector2.zero, new Vector2(1000, 42), TextAnchor.MiddleCenter);
    }

    void AddTrigger(EventTrigger trigger, EventTriggerType type, System.Action<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(data => callback(data));
        trigger.triggers.Add(entry);
    }

    void Shoot()
    {
        if (Time.time < nextShot || playerCamera == null) return;
        nextShot = Time.time + 0.16f;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, 100f))
        {
            TrainingTarget target = hit.collider.GetComponentInParent<TrainingTarget>();
            if (target)
            {
                GameObject targetObject = target.gameObject;
                targets.Remove(targetObject);
                Destroy(targetObject);
                score++;
            }
        }
    }

    void SpawnTarget(Vector3 position)
    {
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        target.name = "Training Target";
        target.transform.position = position;
        target.GetComponent<Renderer>().material = red;
        target.AddComponent<TrainingTarget>();
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Target Head";
        head.transform.SetParent(target.transform);
        head.transform.localPosition = new Vector3(0, 0.72f, 0);
        head.transform.localScale = new Vector3(0.58f, 0.58f, 0.58f);
        head.GetComponent<Renderer>().material = skin;
        Destroy(head.GetComponent<Collider>());
        targets.Add(target);
    }

    void ResetTargets()
    {
        foreach (GameObject target in targets) if (target) Destroy(target);
        targets.Clear();
        for (int i = 0; i < 6; i++)
            SpawnTarget(new Vector3(-10 + (i % 3) * 10, 1f, 8 - (i / 3) * 16));
    }

    GameObject Cube(string objectName, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = objectName;
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().material = material;
        return obj;
    }

    Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    Image AddPanel(Transform parent, Vector2 anchor, Vector2 size, Color color)
    {
        GameObject obj = new GameObject("Panel");
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    Button AddButton(Transform parent, string text, Vector2 anchor, Vector2 size)
    {
        Image image = AddPanel(parent, anchor, size, new Color(0.06f, 0.09f, 0.12f, 0.82f));
        Button button = image.gameObject.AddComponent<Button>();
        AddLabel(image.transform, "Text", text, 20, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        return button;
    }

    Text AddLabel(Transform parent, string objectName, string text, int fontSize,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, TextAnchor alignment)
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text label = obj.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }
}

public class TrainingTarget : MonoBehaviour { }
