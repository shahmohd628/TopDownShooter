using UnityEngine;

public class Crosshairs : MonoBehaviour
{
    public float rotateSpeed = 40f;
    public LayerMask targetMask;
    public SpriteRenderer dot;
    public Color dotHighlightColor;
    Color originalColor;
    void Start()
    {
        Cursor.visible = false;
        originalColor = dot.color;
    }
    void Update()
    {
        transform.Rotate(Vector3.forward * Time.deltaTime * -rotateSpeed);
    }

    public void DetectTargets(Ray ray)
    {
        if(Physics.Raycast(ray,100,targetMask))
        {
            dot.color = dotHighlightColor;
        }
        else
        {
            dot.color = originalColor;
        }
    }
}
