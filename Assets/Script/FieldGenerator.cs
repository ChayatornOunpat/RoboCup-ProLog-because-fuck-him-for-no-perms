using UnityEngine;

public class FieldGenerator : MonoBehaviour
{
    public GameObject tilePrefab;

    int width = 18;
    int height = 10;

    void Start()
    {
        GenerateField();
    }

    void GenerateField()
    {
        for (int x = 1; x <= width; x++)
        {
            for (int y = 1; y <= height; y++)
            {
                GameObject tile = Instantiate(tilePrefab);
                tile.transform.position = new Vector3(x, y, 1);
            }
        }
    }
}