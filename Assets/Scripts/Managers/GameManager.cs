using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public int m_NumRoundsToWin = 5; //Número de rondas que un jugador debe ga nar para ganar el juego
    public float m_StartDelay = 3f; //Delay entre las fases de RoundStarting y RoundPlaying
    public float m_EndDelay = 3f; //Delay entre las fases de RoundPlaying y R oundEnding
    public CameraControl m_CameraControl; //Referencia al sccript de CameraControl
    public TMP_Text m_MessageText; //Referencia al texto para mostrar mensajes
    public TMP_Text m_TimeText; //Referencia al texto para mostrar mensajes

    public GameObject m_TankPrefab; //Referencia al Prefab del Tanque
    public TankManager[] m_Tanks; //Array de TankManagers para controlar cada tanque
    private int m_RoundNumber; //Número de ronda
    private WaitForSeconds m_StartWait; //Delay hasta que la ronda empieza
    private WaitForSeconds m_EndWait; //Delay hasta que la ronda acaba
    private TankManager m_RoundWinner; //Referencia al ganador de la ronda para anunciar quién ha ganado
    private TankManager m_GameWinner; //Referencia al ganador del juego para anunciar quién ha ganado
    private float m_GameTimeInSeconds; // Tiempo jugado
    private float m_RoundTimeInSeconds; // Tiempo de ronda
    private float m_RoundTimeLimit = 60f; // Limite de tiempo

    private void Start()
    {
        //Creamos los delays para que solo se apliquen una vez
        m_StartWait = new WaitForSeconds(m_StartDelay);
        m_EndWait = new WaitForSeconds(m_EndDelay);
        SpawnAllTanks(); //Generar tanques
        SetCameraTargets(); //Ajustar cámara
        StartCoroutine(GameLoop()); //Iniciar juego
    }

    private void SpawnAllTanks()
    {
        //Recorro los tanques...
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            //...los creo, ajusto el número de jugador y ls referencias necesarias para controlarlo
            m_Tanks[i].m_Instance =
                Instantiate(
                    m_TankPrefab,
                    m_Tanks[i].m_SpawnPoint.position,
                    m_Tanks[i].m_SpawnPoint.rotation
                ) as GameObject;
            m_Tanks[i].m_PlayerNumber = i + 1;
            m_Tanks[i].Setup();
        }
    }

    private void SetCameraTargets()
    {
        //Creo un array de Transforms del mismo tamaño que el número de tanques
        Transform[] targets = new Transform[m_Tanks.Length];
        //Recorro los Transforms...
        for (int i = 0; i < targets.Length; i++)
        {
            //...lo ajusto al transform del tanque apropiado
            targets[i] = m_Tanks[i].m_Instance.transform;
        }
        //Estos son los targets que la cámara debe seguir
        m_CameraControl.m_Targets = targets;
    }

    //llamado al principio y en cada fase del juego después de otra
    private IEnumerator GameLoop()
    {
        // Reiniciar temporizador
        m_GameTimeInSeconds = 0f;
        //Empiezo con la corutina RoundStarting y no retorno hasta que finalice
        yield return StartCoroutine(RoundStarting());
        //Cuando finalice RoundStarting, empiezo con RoundPlaying y no retornohasta que finalice
        yield return StartCoroutine(RoundPlaying());
        //Cuando finalice RoundPlaying, empiezo con RoundEnding y no retorno hasta que finalice
        yield return StartCoroutine(RoundEnding());
        //Si aún no ha ganado ninguno
        if (m_GameWinner != null)
        {
            //Si hay un ganador, reinicio el nivel
            SceneManager.LoadScene(0);
        }
        else
        {
            //Si no, reinicio lsa corutinas para que continúe el bucle
            //En este caso sin yiend, de modo que esta versión del GameLoop finalizará siempre
            StartCoroutine(GameLoop());
        }
    }

    private IEnumerator RoundStarting()
    {
        // Reinicio tiempo;
        m_RoundTimeInSeconds = 0f;
        // Cuando empiece la ronda reseteo los tanques e impido que se muevan.
        ResetAllTanks();
        DisableTankControl();
        // Ajusto la cámara a los tanques resteteados.
        m_CameraControl.SetStartPositionAndSize();
        // Incremento la ronda y muestro el texto informativo.
        m_RoundNumber++;
        m_MessageText.text = "ROUND " + m_RoundNumber;
        // Espero a que pase el tiempo de espera antes de volver al bucle.
        yield return m_StartWait;
    }

    private IEnumerator RoundPlaying()
    {
        // Cuando empiece la ronda dejo que los tanques se muevan.
        EnableTankControl();
        // Borro el texto de la pantalla.
        m_MessageText.text = string.Empty;
        // Mientras haya más de un tanque y haya tiempo
        while (!OneTankLeft() && m_RoundTimeInSeconds <= m_RoundTimeLimit)
        {
            // ... vuelvo al frame siguiente e incremento tiempo
             m_RoundTimeInSeconds += Time.deltaTime;
             m_TimeText.text = "Tiempo: " + m_RoundTimeInSeconds.ToString("f2"); 
            yield return null;
        }
        
         // Si no hay tiempo ambos pierden
        if (m_RoundTimeInSeconds > m_RoundTimeLimit)
        {
            Debug.Log("Game over for both");
            for (int i = 0; i < m_Tanks.Length; i++) {
                m_Tanks[i].m_Instance.SetActive(false);
            }
        }

        // Incrementar game time
        m_GameTimeInSeconds += m_RoundTimeInSeconds;
    }

    private IEnumerator RoundEnding()
    {
        // Deshabilito el movimiento de los tanques.
        DisableTankControl();
        // Borro al ganador de la ronda anterior.
        m_RoundWinner = null;
        // Miro si hay un ganador de la ronda.
        m_RoundWinner = GetRoundWinner();
        // Si lo hay, incremento su puntuación.
        if (m_RoundWinner != null)
            m_RoundWinner.m_Wins++;
        // Compruebo si alguien ha ganado el juego.
        m_GameWinner = GetGameWinner();
        // Genero el mensaje según si hay un gaandor del juego o no.
        string message = EndMessage();
        m_MessageText.text = message;
        // Espero a que pase el tiempo de espera antes de volver al bucle.
        yield return m_EndWait;
    }

    // Usado para comprobar si queda más de un tanque.
    private bool OneTankLeft()
    {
        // Contador de tanques.
        int numTanksLeft = 0;
        // recorro los tanques...
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            // ... si está activo, incremento el contador.
            if (m_Tanks[i].m_Instance.activeSelf)
                numTanksLeft++;
        }
        // Devuelvo true si queda 1 o menos, false si queda más de uno.
        return numTanksLeft <= 1;
    }

    // Comprueba si algún tanque ha ganado la ronda (si queda un tanque o menos).
    private TankManager GetRoundWinner()
    {
        // Recorro los tanques...
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            // ... si solo queda uno, es el ganador y lo devuelvo.
            if (m_Tanks[i].m_Instance.activeSelf)
                return m_Tanks[i];
        }
        // SI no hay ninguno activo es un empate, así que devuelvo null.
        return null;
    }

    // Comprueba si hay algún ganador del juegoe.
    private TankManager GetGameWinner()
    {
        // Recorro los tanques...
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            // ... si alguno tiene las rondas necesarias, ha ganado y lo devuelvo.
            if (m_Tanks[i].m_Wins == m_NumRoundsToWin)
                return m_Tanks[i];
        }
        // Si no, devuelvo null.
        return null;
    }

    // Deveulve el texto del mensaje a mostrar al final de cada ronda.
    private string EndMessage()
    {
        // Por defecto no hay ganadores, así que es empate.
        string message = "EMPATE!";
        // Si hay un ganador de ronda cambio el mensaje.
        if (m_RoundWinner != null)
            message = m_RoundWinner.m_ColoredPlayerText + " GANA LA RONDA EN " +  m_RoundTimeInSeconds.ToString("f2") + " SEGUNDOS!";
        // Retornos de carro.
        message += "\n\n\n\n";
        // Recorro los tanques y añado sus puntuaciones.
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            message += m_Tanks[i].m_ColoredPlayerText + ": " + m_Tanks[i].m_Wins + " GANA\n";
        }
        // Si hay un ganador del juego, cambio el mensaje entero para reflejarlo.
        if (m_GameWinner != null)
            message = m_GameWinner.m_ColoredPlayerText + " GANA EL JUEGO EN" +  m_GameTimeInSeconds.ToString("f2") + " SEGUNDOS!";
        return message;
    }

    // Para resetear los tanques (propiedaes, posiciones, etc.).
    private void ResetAllTanks()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].Reset();
        }
    }

    //Habilita el control del tanque
    private void EnableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].EnableControl();
        }
    }

    //Deshabilita el control del tanque
    private void DisableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].DisableControl();
        }
    }
}
