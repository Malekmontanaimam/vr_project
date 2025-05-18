using UnityEngine;
using Physics;

public class DragBall : MonoBehaviour
{
    private Vector3 offset;
    private float zCoord;
    private bool isDragging = false;
    private BallController ballController;
    private Vector3 dragVelocity = Vector3.zero;
    private Vector3 lastPosition;

    void Start()
    {
        ballController = GetComponent<BallController>();
        lastPosition = transform.position;
    }

    void OnMouseDown()
    {
        Debug.Log("Mouse Down!");
        zCoord = Camera.main.WorldToScreenPoint(transform.position).z;
        offset = transform.position - GetMouseWorldPos();
        isDragging = true;
        lastPosition = transform.position;
    }

    void OnMouseDrag()
    {
        if (isDragging && ballController != null)
        {
            Vector3 newPosition = GetMouseWorldPos() + offset;
            transform.position = newPosition;
            
            // حساب السرعة بناءً على حركة الماوس
            dragVelocity = (newPosition - lastPosition) / Time.deltaTime;
            lastPosition = newPosition;

            // تطبيق قوة سحب على الكرة
            Vector3 dragForce = dragVelocity * ballController.mass;
            ballController.ApplyForce(dragForce);
        }
    }

    void OnMouseUp()
    {
        if (isDragging && ballController != null)
        {
            isDragging = false;
            
            // تطبيق قوة نهائية عند إطلاق الكرة
            Vector3 releaseForce = dragVelocity * ballController.mass;
            ballController.ApplyForce(releaseForce);
            
            // إعادة تعيين السرعة
            dragVelocity = Vector3.zero;
        }
    }

    Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = zCoord;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
}