using UnityEngine;
using System.Collections.Generic;
using Physics;

public class BallManager : MonoBehaviour
{
    public List<BallController> balls = new List<BallController>();

    void Start()
    {
        balls.AddRange(Object.FindObjectsByType<BallController>(FindObjectsSortMode.None));
    }

    void Update()
    {
        if (balls.Count == 0)
            return;

        // تحديث كل الكرات
        foreach (var ball in balls)
        {
            if (ball != null && ball.myBall != null)
                ball.myBall.physics.UpdatePhysics(Time.deltaTime, ball.myBall.springs);
        }

        // تصادم الكرات
        for (int i = 0; i < balls.Count; i++)
        {
            for (int j = i + 1; j < balls.Count; j++)
            {
                if (balls[i] != null && balls[j] != null && balls[i].myBall != null && balls[j].myBall != null)
                    BallCollision.ResolveBallCollision(balls[i].myBall, balls[j].myBall);
            }
        }
    }
}