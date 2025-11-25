using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ShapeDragSystem : MonoBehaviour
{
    public ShapeData Data;
    public GameManager Game;

    private Camera cam;
    private bool dragging = false;
    private Vector3 offset;
    private Vector3 startLocalPos;

    void Start()
    {
        cam = Camera.main;
        startLocalPos = transform.localPosition;
    }

    void Update()
    {
        // Mouse yoksa (mobile’da olabilir) null check
        if (Mouse.current == null)
            return;

        // Sol tık basıldı -> Drag başlat
        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryStartDragging();

        // Sol tık hala basılı -> Drag devam
        if (dragging && Mouse.current.leftButton.isPressed)
            Dragging();

        // Sol tık bırakıldı -> Drag bitir
        if (dragging && Mouse.current.leftButton.wasReleasedThisFrame)
            StopDragging();
    }

    void TryStartDragging()
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == transform)
            {
                dragging = true;
                offset = transform.position - hit.point;
            }
        }
    }

    void Dragging()
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        Plane plane = new Plane(Vector3.up, Vector3.zero);

        if (plane.Raycast(ray, out float distance))
        {
            transform.position = ray.GetPoint(distance) + offset;
        }
    }

    void StopDragging()
    {
        dragging = false;

        Vector3 world = transform.position;
        var (gx, gy) = Game.Grid.WorldToGrid(world);

        // Grid kontrol
        if (Game.Grid.CanPlace(Data, gx, gy))
        {
            Game.TryPlaceShape(Data, gx, gy);
            Destroy(gameObject);
        }
        else
        {
            transform.localPosition = startLocalPos;
        }
    }
    // public ShapeData Data;
    // public GameManager Game;

    // private Camera cam;
    // private bool dragging = false;
    // private Vector3 offset;
    // private Vector3 startWorldPos;

    // void Start()
    // {
    //     cam = Camera.main;
    //     startWorldPos = transform.position; // Başlangıç pozisyonunu kaydet
    // }

    // void Update()
    // {
    //     if (Input.touchCount == 0)
    //         return; // dokunma yoksa çık
    //     print("ahahha");
    //     Touch touch = Input.GetTouch(0); // ilk dokunmayı al
    //     print("touch");

    //     Vector3 touchPos = touch.position;

    //     switch (touch.phase)
    //     {
    //         case TouchPhase.Began:
    //             TryStartDragging(touchPos);
    //             break;

    //         case TouchPhase.Moved:
    //         case TouchPhase.Stationary:
    //             if (dragging)
    //                 Dragging(touchPos);
    //             break;

    //         case TouchPhase.Ended:
    //         case TouchPhase.Canceled:
    //             if (dragging)
    //                 StopDragging();
    //             break;
    //     }
    // }

    // void TryStartDragging(Vector3 screenPos)
    // {
    //     Ray ray = cam.ScreenPointToRay(screenPos);
    //     if (Physics.Raycast(ray, out RaycastHit hit))
    //     {
    //         if (hit.transform == transform)
    //         {
    //             dragging = true;
    //             offset = transform.position - hit.point;
    //         }
    //     }
    // }

    // void Dragging(Vector3 screenPos)
    // {
    //     Ray ray = cam.ScreenPointToRay(screenPos);
    //     Plane plane = new Plane(Vector3.up, Vector3.zero);

    //     if (plane.Raycast(ray, out float distance))
    //     {
    //         transform.position = ray.GetPoint(distance) + offset;
    //     }
    // }

    // void StopDragging()
    // {
    //     dragging = false;

    //     Vector3 world = transform.position;
    //     var (gx, gy) = Game.Grid.WorldToGrid(world);

    //     if (Game.Grid.CanPlace(Data, gx, gy))
    //     {
    //         Game.TryPlaceShape(Data, gx, gy);
    //         Destroy(gameObject);
    //     }
    //     else
    //     {
    //         transform.position = startWorldPos;
    //     }
    // }
}
