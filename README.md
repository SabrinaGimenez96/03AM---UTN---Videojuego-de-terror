# Videojuego 03:00 A.M - Parcial Motores 1

![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-black?style=flat-square&logo=unity)
![C#](https://img.shields.io/badge/C%23-Scripting-blue?style=flat-square&logo=c-sharp)
![Status](https://img.shields.io/badge/Estado-Completado-brightgreen?style=flat-square)

**03:00 A.M** es un juego de terror psicológico en primera persona desarrollado como proyecto práctico para la evaluación de Motores 1. El objetivo principal es sobrevivir en una habitación desde las 03:00 AM hasta el amanecer (06:00 AM), gestionando el miedo y la cordura a través de la interacción (y negación) de los eventos paranormales que suceden alrededor.

---

## 🎮 Gameplay y Mecánicas

El juego escapa del clásico enfoque de combate o escondite físico. El jugador debe usar su **razonamiento y gestión de estrés** para convencer a su mente de que lo que ve no es real.

* **Sistema de Estados Mentales:** La UI no usa barras numéricas invasivas. El jugador transiciona dinámicamente entre estados de *Calma*, *Inestable* y *Crisis*, lo que afecta las reglas del entorno.
* **Eventos RNG y Audio Espacial:** El juego cuenta con un sistema de probabilidad matemática que genera estática en la TV, notificaciones en el celular, ruidos en la puerta y apariciones de sombras. El audio espacial (3D) es crucial para ubicar la amenaza.
* **Lámpara (Zona Segura Dinámica):** Encender la luz disipa a las criaturas y frena los eventos, pero gasta batería. Si la batería llega a 0%, la lámpara estallará dejando al jugador expuesto permanentemente.
* **Anclaje Mental (Celular):** Mirar fotos en el teléfono recupera la cordura. Sin embargo, abusar del teléfono (*spam*) desencadena un evento de castigo severo.
* **Mecánica de Exposición (Pasillo):** Abrir la puerta principal brinda una falsa sensación de paz. Si el jugador decide "acampar" afuera de su habitación aprovechando la luz, el juego detectará la trampa a los 10 segundos, encerrándolo en la oscuridad y finalizando la partida.

## ⚙️ Características Técnicas

* **Arquitectura Singleton:** `GameManager`, `HorrorEventManager`, `AudioManager` y `UIManager` centralizan la lógica para evitar dependencias cruzadas (Spaghetti Code).
* **Domain-Driven Folder Structure:** Los *scripts* están estrictamente aislados en la carpeta `_Project` y separados por dominio de responsabilidad (Core, Player, Enemy, UI, Interaction).
* **Inteligencia Artificial (NavMesh):** El enemigo (Araña/Sombra) utiliza `NavMeshAgent` para evadir obstáculos de la habitación y cazar al jugador, sincronizando su velocidad de desplazamiento con un `Animator Controller`.
* **Corrutinas y Feedback Reactivo:** Se emplean `IEnumerator` para manejar el flujo del tiempo, animaciones de luces titilantes, y *delays* cinemáticos durante las pantallas de victoria y derrota.
* **Audio Dinámico:** El ritmo cardíaco del jugador (Pitch y Volumen) se calcula de forma procedural utilizando `Mathf.Lerp` en base a la variable global de Miedo.

## 🕹️ Controles

* **Mouse:** Mirar alrededor. 
* **Barra Espaciadora:** Interactuar con el objeto resaltado por el punto de mira (Lámpara, Celular, TV, Puerta) y acostarse (Acción de *Ignorar* para evadir sustos directos).
* **W, A, S, D:** Caminar / Movimiento.


---
*Desarrollado para la cátedra de Motores de desarrollo 1 - UTN.*
