using UnityEngine;

public class SortingSprite : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;//精灵渲染器引用
    [SerializeField] private int frontSortingOrder = 2;//前景排序层级
    [SerializeField] private int backSortingOrder = 0;//背景排序层级
    [SerializeField] private float angeThreshold = 0f;//角度阈值
    private WeaponsRoation weaponRotation;//武器旋转组件引用

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        weaponRotation = GetComponent<WeaponsRoation>();
    }
     
    // Update is called once per frame
    void Update()
    {
        AdjustSortingOrder();
    }
    void AdjustSortingOrder()
    {
        float rotationZ = transform.rotation.eulerAngles.z;
        if(rotationZ > 180f)
        {
            rotationZ -= 360f;
        }
        if(rotationZ > angeThreshold)
        {
            //枪械在前景
            spriteRenderer.sortingOrder = frontSortingOrder;
        }
        else
        {
            spriteRenderer.sortingOrder = backSortingOrder;
        }
    }
}
