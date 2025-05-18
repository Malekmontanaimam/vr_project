/*using UnityEngine;

public class CustomBall : MonoBehaviour
{
    public float mass = 4f;
    public Vector3 velocity;
    public float radius = 0.5f;

    void Start()
    {
        
        transform.localScale = Vector3.one * radius * 2f;
    }

    void Update()
    {
       
        velocity += Physics.gravity * Time.deltaTime;

        
        transform.position += velocity * Time.deltaTime;

        
        if (transform.position.y - radius < 0f)
        {
            transform.position = new Vector3(transform.position.x, radius, transform.position.z);
            velocity.y *= -0.7f; 
        }
    }
}*/
