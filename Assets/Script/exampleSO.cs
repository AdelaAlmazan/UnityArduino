using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;  // Para la comunicación serial
using System.IO;       // Para manejar IOException
using System;          // Para UnauthorizedAccessException


public class exampleSO : MonoBehaviour
{
    public float speed = 15.0f;  // Velocidad base
    public float turnSpeed = 50.0f;  // Velocidad de giro controlada por el acelerómetro
    public float horizontalInput;  // Movimiento Horizontal
    public float forwardInput;  // Movimiento Vertical

    // Filtro pasa bajas
    public float smoothingFactor = 0.1f;  // Factor de suavizado para el filtro (0.0 a 1.0)
    private float xTiltFiltered = 0.0f;  // Valor filtrado del eje X
    private float zTiltFiltered = 0.0f;  // Valor filtrado del eje Z

    /// Variables para la cámara
    public Camera mainCamera;
    public Camera hoodCamera;
    public KeyCode switchKey;

    // Variables para multijugador
    public string inputID;

    // Comunicación serial con el Circuit Playground Classic
    SerialPort serialPort;
    public string portName = "COM16";  // Reemplaza COMX por el puerto correcto
    public float threshold = 0.1f;  // Umbral mínimo para el movimiento
    public float maxTilt = 1.0f;    // Máximo ángulo de inclinación considerado para el movimiento
    private float xTilt;  // Almacena el valor del eje X (rotación)
    private float zTilt;  // Almacena el valor del eje Z (movimiento hacia adelante/atrás)

    void Start()
    {
        try
        {
            serialPort = new SerialPort(portName, 9600);
            serialPort.Open();
            System.Threading.Thread.Sleep(100);  // Esperar 100 ms para asegurarse de que el puerto esté listo
            serialPort.DtrEnable = false;  // Deshabilita DTR
            serialPort.RtsEnable = false;  // Deshabilita RTS
            serialPort.ReadTimeout = 1000;
            Debug.Log("Puerto abierto correctamente.");
        }
        catch (IOException ioEx)
        {
            Debug.LogError("IOException: " + ioEx.Message);
        }
        catch (UnauthorizedAccessException uaeEx)
        {
            Debug.LogError("UnauthorizedAccessException: " + uaeEx.Message);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error general al abrir el puerto: " + ex.Message);
        }
    }

    void Update()
    {
        // Leer los datos del acelerómetro del Circuit Playground Classic

       
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string data = serialPort.ReadLine();  // Leer la línea de datos del puerto serial
                string[] values = data.Split(',');    // Separar los valores por coma

                xTilt = float.Parse(values[0]);  // Acelerómetro X (giro)
                zTilt = float.Parse(values[2]);  // Acelerómetro Z (adelante/atrás)
                
               

                // Normalizar los valores entre -1 y 1 (si es necesario) para un mejor control
                xTilt = Mathf.Clamp(xTilt / maxTilt, -1f, 1f);
                zTilt = Mathf.Clamp(zTilt / maxTilt, -1f, 1f);

                // Aplicar el filtro pasa bajas
                xTiltFiltered = ApplyLowPassFilter(xTiltFiltered, xTilt, smoothingFactor);
                zTiltFiltered = ApplyLowPassFilter(zTiltFiltered, zTilt, smoothingFactor);
            }
            catch (System.Exception)
            {
                // Manejo de excepciones si no se puede leer el puerto
            }
        }

        // Ajustar el movimiento con el Circuit Playground Classic
        forwardInput = zTiltFiltered > threshold ? zTiltFiltered : 0;  // Solo avanzar si hay inclinación hacia adelante
        //speed=Math.Abs(zTiltFiltered)*30;
       // turnSpeed = Math.Abs(xTiltFiltered)*30;  
        // Solo permitir girar si hay avance hacia adelante
        if (forwardInput > 0)
        {
                Debug.Log((Math.Abs(horizontalInput -  xTiltFiltered)));
            if(((Math.Abs(horizontalInput -  xTiltFiltered))<0.01 )){
              horizontalInput=0;
  
           }else{
                     horizontalInput = Mathf.Abs(xTiltFiltered) > threshold ? xTiltFiltered : 0;  // Solo mover si pasa el umbral
         
         
            }
       
        }
        else
        {
            horizontalInput = 0
            ;  // No permitir giro si no hay avance
        }

        // Movimiento del player
        transform.Rotate(Vector3.up, turnSpeed * horizontalInput * Time.deltaTime);  // Girar a la izquierda o derecha
        transform.Translate(Vector3.forward * Time.deltaTime * speed * forwardInput);  // Avanzar dependiendo de la inclinación

        // Cambio de cámara
        if (Input.GetKeyDown(switchKey))
        {
            mainCamera.enabled = !mainCamera.enabled;  // Cambiar entre la cámara principal y la secundaria
            hoodCamera.enabled = !hoodCamera.enabled;
        }
    }

    void OnDestroy()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            Debug.Log("Puerto cerrado correctamente.");
        }
    }

    // Función para aplicar el filtro pasa bajas
    private float ApplyLowPassFilter(float previousValue, float newValue, float alpha)
    {
        return alpha * newValue + (1 - alpha) * previousValue;
    }
}
