using UnityEngine;

public class WalkingEnemyAI : EnemyAI
{

    new void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!isAlive)
        {
            return;
        }
        if (!enableMovement && isAggressive)
        {
            return;
        }
        rb.linearVelocityX = (GetTargetPosition().x - transform.position.x < 0 ? -1 : 1) * speed;
    }
}
