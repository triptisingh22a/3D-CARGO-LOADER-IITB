using UnityEngine;

public class TruckViewController : MonoBehaviour
{
    [Header("Truck and View Positions")]
    [SerializeField] private GameObject truck; 
    [SerializeField] private Transform rightViewCube; 
    [SerializeField] private Transform leftViewCube;
    [SerializeField] private Transform topViewCube;


    public void ShowRightView()
    {
        MoveTruckToView(rightViewCube);
    }

 
    public void ShowLeftView()
    {
        MoveTruckToView(leftViewCube);
    }

    public void ShowTopView()
    {
        MoveTruckToView(topViewCube);
    }

   
    private void MoveTruckToView(Transform viewTransform)
    {
        truck.transform.position = viewTransform.position;
        truck.transform.rotation = viewTransform.rotation;
    }
}
