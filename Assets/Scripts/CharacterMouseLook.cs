using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMouseLook : MonoBehaviour {

    float sensitivityX = 1f;
    float minX = -60f;
    float maxX = 60f;
    float rotationX = 0f;

    void Update() {
        rotationX -= Input.GetAxis("Mouse Y") * sensitivityX;
        rotationX = Mathf.Clamp(rotationX, minX, maxX);
        transform.localEulerAngles = new Vector3(rotationX, transform.localEulerAngles.y, 0);
    } 

}
