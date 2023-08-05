﻿using UnityEngine;
using UnityEngine.UI;

public class TankShooting : MonoBehaviour
{
    public int m_PlayerNumber = 1; //Para identificar a los diferentes jugadores
    public Rigidbody m_Shell; //Prefab de la bomba
    public Transform m_FireTransform; //Hijo del tanque en el que se generarála bomba (desde donde se lanzará)
    public Slider m_AimSlider; //Hijo del tanque que muestra la fuerza de lanzamiento de la bomba
    public AudioSource m_ShootingAudio; //Referencia a la fuenta de audio quese reproducirá al lanzar la bomba
    public AudioClip m_ChargingClip; //Clip de audio que se reproduce cuando se está cargando el disparo
    public AudioClip m_FireClip; //Clip de audio que se reproduce al lanzar la bomba
    public float m_MinLaunchForce = 15f; //Fuerza mínima de dipsaro (si no semantiene presionado el botón de disparo)
    public float m_MaxLaunchForce = 30f; //Fuerza máxima de dipsaro (si se mantiene presionado el botón de disparo hasta la máxima carga)
    public float m_MaxChargeTime = 0.75f; //Tiempo máximo de carga antes de ser lanzado el disparo con máxima fuerza

    private string m_FireButton; //Eje de disparo utilizado para lanzar las bombas
    private float m_CurrentLaunchForce; //Fuerza dada a la bomba cuando se suelta el botón de disparo
    private float m_ChargeSpeed; //Velocidad de carga, basasda en el máximo tiempo de carga
    private bool m_Fired; //Booleano que comprueba si se ha lanzado la bomba

    private void OnEnable()
    {
        //Al crear el tanque, reseteo la fuerza de lanzamiento y la UI
        m_CurrentLaunchForce = m_MinLaunchForce;
        m_AimSlider.value = m_MinLaunchForce;
    }

    private void Start()
    {
        //El eje de disparo basado en el número de jugador
        m_FireButton = "Fire" + m_PlayerNumber;
        //Velocidad de carga, basasda en el máximo tiempo de carga y los valores de carga máximo y mínimo
        m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
    }

    private void Update()
    {
        // Asigno el valor mínimo al slider.
        m_AimSlider.value = m_MinLaunchForce;
        //Si llego al valor máximo y no lo he lanzado...
        if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
        {
            // ... uso el valor máximo y disparo.
            m_CurrentLaunchForce = m_MaxLaunchForce;
            Fire();
        }
        // Si no, si ya he pulsado el botón de disparo...
        else if (Input.GetButtonDown(m_FireButton))
        {
            // ... reseteo el booleano de dipsaro y la fuerza de disparo.
            m_Fired = false;
            m_CurrentLaunchForce = m_MinLaunchForce;
            // Cambio el clip de audio al de cargando y lo reproduzco.
            m_ShootingAudio.clip = m_ChargingClip;
            m_ShootingAudio.Play();
        }
        //Si no, si estoy manteniendo presionado el botón de disparo y aún no he disparado...
        else if (Input.GetButton(m_FireButton) && !m_Fired)
        {
            // Incremento la fuerza de disparo y actualizo el slider.
            m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
            m_AimSlider.value = m_CurrentLaunchForce;
        }
        //Si no, si ya he soltado el botón de disparo y aún no he lanzado...
        else if (Input.GetButtonUp(m_FireButton) && !m_Fired)
        {
            // ... disparo.
            Fire();
        }
    }

    private void Fire()
    {
        // Ajusto el booleano a true para que solo se lance una vez.
        m_Fired = true;
        // Creo una instancia de la bomba y guardo una referencia en su Rigidbody.
        Rigidbody shellInstance =
            Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;
        // Ajusto la velocidad de la bomba en la dirección de disparo.
        shellInstance.velocity = m_CurrentLaunchForce * m_FireTransform.forward;
        // Cambio el audio al de disparo y lo reproduzco.
        m_ShootingAudio.clip = m_FireClip;
        m_ShootingAudio.Play();
        //Reseteo la fuerza de lanzamiento como precaución ante posibles eventos de botón "perdidos".
        m_CurrentLaunchForce = m_MinLaunchForce;
    }
}
