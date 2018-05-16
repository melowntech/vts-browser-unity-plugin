/**
 * Copyright (c) 2017 Melown Technologies SE
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * *  Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 *
 * *  Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using UnityEngine;

public class FreeFlightController : MonoBehaviour
{
    public float cameraMouseSensitivity = 10;
    public float fastSpeed = 1000000;
    public float normalSpeed = 10000;
    public float slowSpeed = 100;

    private float yaw = 0.0f;
    private float pitch = 0.0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            yaw += Input.GetAxis("Mouse X") * cameraMouseSensitivity;
            pitch += Input.GetAxis("Mouse Y") * cameraMouseSensitivity;
            //pitch = Mathf.Clamp(pitch, -80, 80);
            transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, Vector3.left);
        }
        else
        {
            // change the state if the transformation was modified in the editor
            yaw = transform.rotation.eulerAngles[1];
            pitch = -transform.rotation.eulerAngles[0];
        }

        float speed = normalSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            speed = fastSpeed;
        else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            speed = slowSpeed;
        speed *= Time.deltaTime;

        transform.position += transform.right * speed * Input.GetAxis("Horizontal");
        transform.position += transform.forward * speed * Input.GetAxis("Vertical");

        if (Input.GetKey(KeyCode.E))
            transform.position += transform.up * speed;
        if (Input.GetKey(KeyCode.Q))
            transform.position -= transform.up * speed;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = Cursor.lockState == CursorLockMode.None;
        }
    }
}
