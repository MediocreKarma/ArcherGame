using UnityEngine;

public class EnemyJumpingPoint : MonoBehaviour
{
    [System.Flags]
    public enum Direction
    {
        None = 0,
        Up = 1 << 0,
        Down = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
    }

    public Direction direction = Direction.None;
    public float requiredJumpForce = 0f;
}
