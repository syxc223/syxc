using UnityEngine;

public class EnemyScale : MonoBehaviour
{
    [SerializeField] private float minscale = 2f;//最小缩放
    [SerializeField] private float maxScale = 3f;//最大缩放
    public float CurrentScale { get; private set; }//当前缩放
    public float MinScale => minscale;//最小缩放属性
    public float MaxScale => maxScale;
    // Start is called before the first frame update

    void Start()
    {//random scale between min and max
        CurrentScale = Random.Range(minscale, maxScale);//设置当前缩放
        transform.localScale = new Vector3(CurrentScale, CurrentScale, 1f);//应用缩放
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}