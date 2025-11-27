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
        // Pointer (Mouse/Touch/Pen) yoksa çık
        if (Pointer.current == null)
            return;

        // Basıldı -> Drag başlat
        if (Pointer.current.press.wasPressedThisFrame)
            TryStartDragging();

        // Basılı tutuluyor -> Drag devam
        if (dragging && Pointer.current.press.isPressed)
            Dragging();

        // Bırakıldı -> Drag bitir
        if (dragging && Pointer.current.press.wasReleasedThisFrame)
            StopDragging();
    }

    void TryStartDragging()
    {
        Ray ray = cam.ScreenPointToRay(Pointer.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                dragging = true;
                
                // Tıklanan noktanın yerdeki izdüşümünü bul
                // hit.point zaten collider üzerinde. Collider'lar bloklarda.
                // Bizim için önemli olan "Shape Pivot"unun yerdeki karşılığı ile hit.point arasındaki fark.
                
                // Ancak daha basit bir yöntem:
                // Mouse'un yer düzlemindeki pozisyonunu bul.
                Plane plane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
                if (plane.Raycast(ray, out float dist))
                {
                    Vector3 hitPointOnPlane = ray.GetPoint(dist);
                    offset = transform.position - hitPointOnPlane;
                }
                
                // Görsel olarak yukarı kaldır
                transform.position += Vector3.up * 2f;
            }
        }
    }

    void Dragging()
    {
        Ray ray = cam.ScreenPointToRay(Pointer.current.position.ReadValue());
        
        // Yer düzleminde (y=0 değil, objenin orijinal yüksekliği neyse o, ama biz y=0 varsayalım ya da Grid yüksekliği)
        float gridY = Game.Grid.transform.position.y;
        Plane plane = new Plane(Vector3.up, new Vector3(0, gridY, 0));

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 targetPoint = ray.GetPoint(distance) + offset;
            
            // Görsel olarak havada tut
            transform.position = targetPoint + Vector3.up * 2f;
        }
    }

    void StopDragging()
    {
        dragging = false;

        // Mantıksal pozisyon (Görsel pozisyon - Lift miktarı)
        Vector3 logicalPos = transform.position - Vector3.up * 2f;
        
        var (gx, gy) = Game.Grid.WorldToGrid(logicalPos);

        // Fuzzy Placement: Sadece tam tıklanan hücreye değil, etrafındaki hücrelere de bak.
        // Kullanıcı biraz kaydırırsa en yakın geçerli noktayı bulsun.
        
        float bestDistance = float.MaxValue;
        Vector2Int bestCoords = new Vector2Int(-1, -1);
        bool foundValid = false;

        // 3x3'lük bir alanda en iyi (en yakın ve geçerli) noktayı ara
        for (int x = gx - 1; x <= gx + 1; x++)
        {
            for (int y = gy - 1; y <= gy + 1; y++)
            {
                print("x: " + x + " y: " + y);
            
                if (Game.Grid.CanPlace(Data, x, y))
                {
                    print("can place");
                    // Bu hücrenin dünya koordinatını al (Pivot'un olması gereken yer)
                    Vector3 cellWorldPos = Game.Grid.GridToWorld(x, y);
                    
                    // Yüksekliği yoksayarak mesafe ölç (XZ düzleminde)
                    float dist = Vector3.Distance(new Vector3(logicalPos.x, 0, logicalPos.z), new Vector3(cellWorldPos.x, 0, cellWorldPos.z));
                    
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestCoords = new Vector2Int(x, y);
                        foundValid = true;
                        print("best coords: " + bestCoords.x + " " + bestCoords.y);
                    }
                }
            }
        }

        if (foundValid)
        {
            Game.TryPlaceShape(Data, bestCoords.x, bestCoords.y);
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
