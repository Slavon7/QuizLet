using UnityEngine;
using Photon.Pun;
using System.Collections;
using TMPro;
using DG.Tweening;

public class GameStartManager : MonoBehaviourPun 
{
    public CanvasGroup fadePanel;
    public GameObject rouletteUI;
    public Roulette roulette;
    public GameObject countdown3;
    public GameObject countdown2;
    public GameObject countdown1;
    public GameObject countdownGo;
    public GameObject backgroundPanel;
    public ParticleSystem countdownParticles;
    public QuizManager quizManager;
    public Animator rouletteAnimator;
    public BlurController blurController;
    
    // Элементы для отсчета
    public TMP_Text countdownText;
    public GameObject countdownPanel;
    
    private void Start()
    {

        Random.InitState(System.Environment.TickCount + System.DateTime.Now.Millisecond);
        Debug.Log($"GameStartManager Random test: {Random.Range(0, 1000)}");

        if (rouletteUI != null)
            rouletteUI.SetActive(false);

        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);  // Активна всегда
            fadePanel.alpha = 1f;
            fadePanel.blocksRaycasts = true;
            fadePanel.interactable = true;

            fadePanel.DOFade(0f, 1.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // Просто делаем её "невидимой и не мешающей"
                    fadePanel.blocksRaycasts = false;
                    fadePanel.interactable = false;

                    if (PhotonNetwork.IsMasterClient)
                    {
                        photonView.RPC("StartCountdown", RpcTarget.All);
                    }
                });
        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("StartCountdown", RpcTarget.All);
            }
        }
    }
    
    [PunRPC]
    public void StartCountdown()
    {
        // Запускаем корутину для отсчета
        StartCoroutine(CountdownRoutine());
    }
    
    private IEnumerator CountdownRoutine()
    {
        if (countdownPanel != null)
            countdownPanel.SetActive(true);

        yield return PlayCountdownStep(countdown3, "countdown");
        yield return PlayCountdownStep(countdown2, "countdown");
        yield return PlayCountdownStep(countdown1, "countdown");
        yield return PlayCountdownStep(countdownGo, "go");

        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        // Запускаем рулетку с анимацией появления
        photonView.RPC("ShowRoulette", RpcTarget.All);
    }

    private IEnumerator PlayCountdownStep(GameObject stepObject, string soundName)
    {
        if (stepObject != null)
        {
            stepObject.SetActive(true);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(soundName);
            else
                Debug.LogWarning($"AudioManager.Instance не найден для воспроизведения звука '{soundName}'");

            if (countdownParticles != null)
                countdownParticles.Play();

            yield return new WaitForSeconds(1f);
            stepObject.SetActive(false);
        }
    }
    
    [PunRPC]
    public void ShowRoulette()
    {
        // Активируем объект рулетки
        rouletteUI.SetActive(true);
        
        // Запускаем анимацию появления рулетки
        if (rouletteAnimator != null)
        {
            rouletteAnimator.SetTrigger("Show");
            // Ждем завершения анимации появления, затем запускаем вращение
            StartCoroutine(WaitForShowAnimationThenSpin());
        }
        else
        {
            // Если аниматор отсутствует, просто запускаем вращение после небольшой задержки
            StartCoroutine(StartSpinAfterDelay(0.5f));
        }
    }
    
    private IEnumerator WaitForShowAnimationThenSpin()
    {
        // Ждем пока проиграется анимация появления
        // Вам может потребоваться настроить время в зависимости от длительности анимации
        yield return new WaitForSeconds(rouletteAnimator.GetCurrentAnimatorStateInfo(0).length);
        
        // Запускаем вращение рулетки
        StartSpin();
    }
    
    private void StartSpin()
    {
        // Небольшая пауза перед запуском вращения
        StartCoroutine(StartSpinAfterDelay(0.1f));
    }
    
    private IEnumerator StartSpinAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        roulette.StartSpin(); // визуально запускает вращение
    }

    public void OnRouletteDisappearComplete()
    {
        rouletteUI.SetActive(false);

        if (blurController != null)
        {
            blurController.StartFadeOut();
        }

        // Добавляем задержку 0.5 секунды перед запуском квиза
        StartCoroutine(DelayedStartQuiz(0.5f));
    }

    private IEnumerator DelayedStartQuiz(float delay)
    {
        // Ждем указанное время
        yield return new WaitForSeconds(delay);
        
        // Запускаем квиз после задержки
        quizManager.StartQuiz();
        quizManager.SetRouletteFinished();
    }

    public void HandleRouletteResult(int result)
    {
        photonView.RPC("OnRouletteFinished", RpcTarget.All, result);
    }
    
    [PunRPC]
    public void OnRouletteFinished(int result)
    {
        QuizMode selectedMode = QuizMode.Normal;
        
        switch (result)
        {
            case 200:
                selectedMode = QuizMode.Bomb;
                break;
            case 300:
                selectedMode = QuizMode.Normal;
                break;
            case 400:
                selectedMode = QuizMode.ShortTime;
                break;
            case 500:
                selectedMode = QuizMode.DoublePoints;
                break;
        }
        
        StartCoroutine(TransitionToQuiz(selectedMode));
    }
    
    private IEnumerator TransitionToQuiz(QuizMode mode)
    {
        // Пауза 1 секунда чтобы игрок мог увидеть результат рулетки
        yield return new WaitForSeconds(1f);

        // Устанавливаем режим
        quizManager.SetMode(mode);  

        if (rouletteAnimator != null)
        {
            rouletteAnimator.SetTrigger("Hide");
            
            // Ждем завершения анимации скрытия
            yield return new WaitForSeconds(rouletteAnimator.GetCurrentAnimatorStateInfo(0).length);
            
            // Вызываем метод после завершения анимации
            OnRouletteDisappearComplete();
        }
        else
        {
            // Если аниматора нет, просто вызываем метод напрямую
            OnRouletteDisappearComplete();
        }
    }
}