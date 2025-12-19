using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Roulette : MonoBehaviourPunCallbacks
{
    private System.Random rng = new System.Random();
    public float RotatePower;
    public float StopPower;
    public QuizManager quizManager;
    public GameStartManager gameStartManager;
    
    private Rigidbody2D rbody;
    private int inRotate = 0;
    public float minPower = 1400f;
    public float maxPower = 2800f;
    private bool rewardProcessed = false;

    private float spinDuration = 2f;
    private float spinTimer = 0f;
    private bool isSpinning = false;
    
    private void Start()
    {
        rbody = GetComponent<Rigidbody2D>();
    }
    
    float t;
    private void Update()
    {
        // Если колесо вращается, отсчитываем время
        if (isSpinning)
        {
            spinTimer += Time.deltaTime;
            
            // Через 4 секунды останавливаем колесо
            if (spinTimer >= spinDuration)
            {
                StopSpinning();
            }
        }
        
        // Проверяем полную остановку для получения награды
        if (rbody.angularVelocity < 0.01f && inRotate == 1 && !rewardProcessed && !isSpinning) 
        {
            rbody.angularVelocity = 0f;
            t += Time.deltaTime;
            if (t >= 0.5f)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    GetReward();
                }
                inRotate = 0;
                t = 0;
                rewardProcessed = true;
            }
        }
    }

    private void StopSpinning()
    {
        isSpinning = false;
        spinTimer = 0f;
        
        // Синхронизируем остановку на всех клиентах
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("SyncStopSpin", RpcTarget.All);
        }
    }
    
    [PunRPC]
    void SyncStopSpin()
    {
        isSpinning = false;
        spinTimer = 0f;
        
        // Начинаем плавную остановку
        StartCoroutine(SmoothStop());
    }
    
    private IEnumerator SmoothStop()
    {
        float stopDuration = 2f; // Время плавной остановки
        float initialVelocity = rbody.angularVelocity;
        
        for (float elapsed = 0; elapsed < stopDuration; elapsed += Time.deltaTime)
        {
            float progress = elapsed / stopDuration;
            rbody.angularVelocity = Mathf.Lerp(initialVelocity, 0, progress);
            yield return null;
        }
        
        rbody.angularVelocity = 0;
    }

    public void Rotate() 
    {
        if (inRotate == 0)
        {
            rbody.angularVelocity = 0;
            
            if (PhotonNetwork.IsMasterClient)
            {
                float randomPower = (float)rng.NextDouble() * (maxPower - minPower) + minPower;
                photonView.RPC("RotateWithPower", RpcTarget.All, randomPower);
            }
        }
    }

    [PunRPC]
    void RotateWithPower(float power)
    {
        rbody.angularVelocity = 0;
        rbody.AddTorque(power);
        inRotate = 1;
        rewardProcessed = false;
        
        // Запускаем таймер вращения
        isSpinning = true;
        spinTimer = 0f;
        
        // 🎵 Проигрываем звук вращения
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("wheel-of-fortune");
        }
        else
        {
            Debug.LogWarning("AudioManager.Instance не найден для воспроизведения звука 'wheel'");
        }
    }

    public void GetReward()
    {
        float rot = transform.eulerAngles.z;
        int result = 0;
        
        // Исправляем логику определения сектора
        if (rot >= 0 && rot < 90)
        {
            result = 200;
        }
        else if (rot >= 90 && rot < 180)
        {
            result = 300;
        }
        else if (rot >= 180 && rot < 270)
        {
            result = 400;
        }
        else if (rot >= 270 && rot <= 360)
        {
            result = 500;
        }
        
        // Отправляем результат и конечное положение всем клиентам
        photonView.RPC("SyncRouletteResult", RpcTarget.All, result, rot);
    }
    
    [PunRPC]
    void SyncRouletteResult(int result, float finalRotation)
    {
        // Устанавливаем одинаковый конечный поворот для всех клиентов
        transform.rotation = Quaternion.Euler(0, 0, finalRotation);
        
        // Останавливаем физику вращения
        if (rbody != null)
        {
            rbody.angularVelocity = 0;
        }
        
        Debug.Log("Выпал режим: " + result);
        
        // Только мастер обрабатывает результат через GameStartManager
        if (gameStartManager != null && PhotonNetwork.IsMasterClient)
        {
            gameStartManager.HandleRouletteResult(result);
        }
        else if (gameStartManager == null)
        {
            Debug.LogError("gameStartManager не назначен в инспекторе");
        }
    }
    
    public void StartSpin()
    {
        // Добавляем проверку подключения и наличия PhotonView
        if (PhotonNetwork.IsConnected && photonView != null)
        {
            photonView.RPC("StartSpinRpc", RpcTarget.All);
        }
        else
        {
            Debug.LogWarning("Не удалось отправить RPC. Используем локальное вращение.");
            Rotate();
        }
    }
    
    [PunRPC]
    void StartSpinRpc()
    {
        Rotate();
    }
}