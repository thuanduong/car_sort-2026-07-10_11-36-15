using UnityEngine;
using UnityEngine.UIElements;

public class ScrollViewDragManipulator : PointerManipulator
{
    private ScrollView scrollView;
    private Vector2 startPointerPosition;
    private Vector2 startScrollOffset;
    private bool isDragging;
    private bool hasMoved;

    private readonly float dragThreshold = 5f; 

    public ScrollViewDragManipulator(ScrollView scrollView)
    {
        this.scrollView = scrollView;
        
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (CanStartManipulation(evt))
        {
            startPointerPosition = evt.position;
            startScrollOffset = scrollView.scrollOffset;
            isDragging = true;
            hasMoved = false;
        }
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!isDragging) return;

        Vector2 delta = (Vector2)evt.position - startPointerPosition;

        // Nếu quãng đường rê chuột vượt qua ngưỡng, bắt đầu tính là thao tác cuộn (Drag)
        if (!hasMoved && delta.magnitude > dragThreshold)
        {
            hasMoved = true;
            target.CapturePointer(evt.pointerId);
        }

        // Thực hiện cuộn màn hình
        if (hasMoved && target.HasPointerCapture(evt.pointerId))
        {
            scrollView.scrollOffset = startScrollOffset - delta;
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!isDragging) return;
        
        isDragging = false;

        if (target.HasPointerCapture(evt.pointerId))
        {
            target.ReleasePointer(evt.pointerId);
        }

        if (hasMoved)
        {
            evt.StopPropagation();
        }
    }

    private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
    {
        if (isDragging)
        {
            isDragging = false;
        }
    }
}