using UnityEngine;

public class OutlineTest : MonoBehaviour
{
    [SerializeField] private OutlineTarget target;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            target.SetOutlined(true);

        if (Input.GetKeyDown(KeyCode.E))
            target.SetOutlined(false);
    }
}