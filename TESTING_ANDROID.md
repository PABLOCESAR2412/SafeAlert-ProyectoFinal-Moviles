# Prueba de SafeAlert en Android físico

## Alcance de la demo SMS

El registro P01–P20 corresponde al historial de pruebas del flujo anterior. Para la demo actual, SafeAlert prepara un SMS al contacto principal y deja la confirmación en Mensajes. El enlace de mapa se añade cuando se obtuvo una posición para la alerta. WhatsApp y la entrega de SMS no forman parte de los resultados certificados.

| Caso nuevo | Resultado esperado | Estado |
| --- | --- | --- |
| SMS-D1 | Con un contacto de prueba, Alerta queda en Historial y Mensajes muestra el destinatario y el texto. Salir sin tocar Enviar. | Pendiente en Android físico |
| SMS-D2 | Con GPS no disponible, el texto explica la ausencia de coordenadas. | Pendiente en Android físico |
| SMS-D3 | Con agenda vacía, se registra la alerta y se pide agregar un contacto. | Pendiente en Android físico |
| SMS-D4 | Al regresar de Mensajes sin enviar, el historial mantiene la entrega sin confirmar. | Pendiente en Android físico |

## Objetivo histórico

Validar permisos reales, biometría, GPS, contactos, envío WhatsApp/SMS, sensores, persistencia local, privacidad y aviso de actualización en un teléfono Android real.

## Entorno verificado

| Dato | Valor |
|---|---|
| Dispositivo | Samsung SM-G780G (`RF8T204PRXM`) |
| Android | 13 (SDK 33) |
| Paquete | `com.uisrael.safealert` |
| Versión | 1.1 (código 2) |
| TFM | `net11.0-android` |
| APK | `bin/Debug/net11.0-android/com.uisrael.safealert-Signed.apk` |

## Cuenta demo

- Correo: `elena.rosales@safealert.app`
- Contraseña: `123456`

## Preparación

1. Activar *Opciones para desarrolladores* y *Depuración USB*.
2. Conectar el teléfono por USB y aceptar «Permitir depuración USB» en el dispositivo.
3. Instalar el APK:

   ```powershell
   & "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" install -r bin\Debug\net11.0-android\com.uisrael.safealert-Signed.apk
   ```

4. Abrir la app:

   ```powershell
   & "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" shell am start -n com.uisrael.safealert/crc64ad7a7cd0b243135e.MainActivity
   ```

## Plan de pruebas

| ID | Prueba | Procedimiento | Resultado esperado | Estado |
|---|---|---|---|---|
| P01 | Permiso de ubicación | Abrir la app por primera vez | Aparece diálogo de Android y, al aceptar, Inicio muestra dirección real | Aprobado |
| P02 | GPS automático | Entrar a Inicio sin pulsar nada | Dirección y coordenadas se leen solas | Aprobado |
| P03 | Mapa consistente | Entrar a Alerta | El mapa se centra en la misma coordenada de Inicio | Aprobado |
| P04 | Contactos desde agenda | Contactos → «Elegir de mis contactos» | Pide `READ_CONTACTS`, abre la agenda y rellena nombre/teléfono | Aprobado |
| P05 | Permiso de contactos | Primera vez que se elige contacto | Diálogo del sistema «Permitir acceso a contactos» | Aprobado |
| P06 | Biometría | Login → «Entrar con huella» | Se abre `BiometricPrompt` del sistema | Aprobado (diálogo; sin huella enrolada, usa cuenta demo) |
| P07 | Sin huella enrolada | Pulsar huella sin sensor configurado | Aviso honesto y opción de cuenta demo/contraseña | Aprobado |
| P08 | SOS | Pulsar SOS en Inicio | Prepara WhatsApp con ubicación y SMS para los demás | Aprobado |
| P09 | Alerta médica | Seleccionar tipo médico y enviar | Se crea registro en historial | Aprobado |
| P10 | Historial | Abrir historial | Aparece la alerta nueva | Aprobado |
| P11 | Cambiar estado | Marcar resuelta / falsa alarma | Estado actualizado | Aprobado |
| P12 | Perfil médico | Editar datos y guardar | Persisten tras cerrar la app | Aprobado |
| P13 | Exportación | Exportar TXT y PDF | Se generan archivos válidos | Aprobado |
| P14 | Acelerómetro | Activar detección y sacudir el teléfono | Evento de sacudida; se ofrece enviar SOS | Aprobado |
| P15 | Sin sensor | Ejecutar en Windows | Explica que no hay acelerómetro y ofrece simulación | Aprobado |
| P16 | Privacidad | Perfil → «Privacidad y Términos» | Se abre la política completa dentro de la app | Aprobado |
| P17 | Actualizaciones | Perfil → «Actualizaciones» → «Ver aviso de ejemplo» | Aparece banner discreto «Nueva versión disponible» | Aprobado |
| P18 | Icono | Ver app en ajustes/lanzador | Icono propio (escudo de seguridad) | Aprobado |
| P19 | Llamada 911 | Alerta → «Llamar al 911» | Abre marcador con 911; la llama el usuario | Aprobado |
| P20 | Persistencia | Cerrar y reabrir | Mantiene sesión, contactos e historial | Aprobado |

## Registros de prueba

| Usuario | Modelo | Android | Fecha | Pruebas aprobadas | Observaciones |
|---|---|---|---|---|---|
| Prueba interna | SM-G780G | 13 | 26/11/2026 | P01–P20 | Dispositivo sin huella enrolada; biometría verificada hasta el diálogo del sistema |

> Las pruebas con usuarios externos, pruebas de carga y el artículo científico siguen pendientes y se declaran así en la defensa.

## Evidencias

Las capturas se guardan en `C:\Users\pablo\Downloads\safealert_capturas\` y se incluyen en el PDF de defensa: inicio con ubicación real, mapa de Alerta, permiso de contactos, agenda del teléfono, contacto autocompletado, biometría, banner de actualización, privacidad y términos, icono y perfil.

## Nota histórica sobre el flujo anterior

SafeAlert **no** envía datos a ECU911 ni usa la API de WhatsApp: abre WhatsApp con un enlace de Google Maps para el contacto principal y prepara un SMS para los demás. La llamada al 911 abre el marcador y la confirma la usuaria. Los datos se guardan en el dispositivo; no hay backend ni cifrado de extremo a extremo en esta versión.

