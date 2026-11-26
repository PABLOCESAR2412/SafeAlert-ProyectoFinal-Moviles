# SafeAlert — app de seguridad y alerta de emergencia

Proyecto final de **Desarrollo de Aplicaciones Móviles** (2026), desarrollado en **.NET MAUI** (TFM `net11.0-android`). La aplicación funciona de forma **local en el dispositivo**: guarda cuentas, contactos e historial sin servidor propio. No promete despacho policial ni envío automático: toda salida externa se prepara y el usuario la confirma en la app nativa correspondiente.

## Pantallas (más de cinco, con contenido dinámico)

1. **Login / Registro** — cuenta local, contraseña y acceso biométrico real (Android 9+).
2. **Inicio** — ubicación del dispositivo, batería/red, botón SOS y envío de alerta.
3. **Alerta** — mapa dinámico centrado en la ubicación leída, lista de destinatarios y llamada manual al 911.
4. **Contactos** — agenda local, selector de contactos del teléfono, formulario de alta/edición.
5. **Historial** — alertas guardadas, cambios de estado y exportación TXT/PDF.
6. **Perfil** — datos personales y médicos, preferencias, privacidad, términos y actualizaciones.
7. **Privacidad y Términos** — política completa dentro de la app.

## Plataformas

- Android físico (probado en Samsung SM-G780G, Android 13).
- Windows Machine (interfaz y lógica compartida; sin sensores físicos).

## Ejecución

### Windows

1. Abrir `SafeAlert.slnx` en Visual Studio.
2. Seleccionar **Windows Machine** y ejecutar con **F5**.

### Android

1. Activar *Opciones para desarrolladores* y *Depuración USB*.
2. Conectar el teléfono y autorizar la depuración en el diálogo del dispositivo.
3. Instalar el APK firmado:

   ```powershell
   & "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" install -r bin\Debug\net11.0-android\com.uisrael.safealert-Signed.apk
   ```

4. Compilar desde cero si se desea:

   ```powershell
   dotnet build SafeAlert.csproj -f net11.0-android -c Debug -v minimal
   ```

Versión instalada: **1.1** (`versionCode 2`). Paquete: `com.uisrael.safealert`.

Cuenta de demostración: `elena.rosales@safealert.app` / `123456`. Si el equipo no tiene huella configurada, el acceso biométrico ofrece entrar con esta cuenta demo o con contraseña.

## Permisos Android solicitados

| Permiso | Para qué se usa | Cuándo se pide |
|---|---|---|
| `ACCESS_FINE_LOCATION` / `ACCESS_COARSE_LOCATION` | Leer la ubicación actual y mostrarla en Inicio y en el mapa de Alerta | Al abrir la app y al entrar a Alerta |
| `READ_CONTACTS` | Elegir un contacto desde la agenda sin escribir el teléfono | Al pulsar «Elegir de mis contactos» |
| `USE_BIOMETRIC` | Desbloquear con huella/rostro en el login | Al pulsar «Entrar con huella» |
| `INTERNET` | Imagen del mapa y comprobación opcional de actualizaciones | Automático, sin diálogo |
| `VIBRATE` | Respuesta háptica al activar SOS | Automático |

El acelerómetro **no requiere permiso** en Android; al activar la detección se muestra un aviso que lo explica.

## Implementado

- Permisos reales en tiempo de ejecución con `Permissions.RequestAsync`.
- GPS real con dirección aproximada y respaldo demostrativo (Quito) si falla.
- Mapa estático de OpenStreetMap centrado en la misma coordenada del GPS.
- Biometría real con `BiometricPrompt` (API 28+) y verificación de disponibilidad; reintento tras huella no reconocida.
- Selector de contactos del teléfono (`ActionPick`) con puente `MainActivity` → `AndroidActivityResult`.
- Envío **sin API de WhatsApp y sin costo**: WhatsApp con enlace de Google Maps al contacto principal, SMS (un solo intent) para los demás; lo envía el usuario en la app nativa.
- Llamada manual al 911 mediante `PhoneDialer` (el usuario confirma la llamada).
- Política de privacidad, términos, retención, permisos, derechos y seguridad dentro de la app.
- Aviso de actualización no intrusivo: `update.json` remoto + vista previa local desde Perfil → Actualizaciones.
- Icono propio (`Resources/AppIcon/appicon.png`) aplicado al manifiesto Android.
- Diseño con Inter + Material Symbols, colores definidos en `App.xaml`, tarjetas y estados consistentes.
- Persistencia local (`safealert.json`), historial y exportación TXT/PDF.
- Sensor de acelerómetro para sacudidas (umbral 2.2 g, cooldown 2 s, `SensorSpeed.Game`) y simulación para equipos sin sensor.
- Control de versiones en GitHub y plan de pruebas en `TESTING_ANDROID.md`.

## Pendiente / limitaciones declaradas

- Backend con base de datos y autenticación remota; hoy los datos se guardan solo en el teléfono.
- API key de Google Maps/Geocoding (actualmente se usa el mapa estático de OpenStreetMap y geocodificación inversa del dispositivo).
- Envío 100 % automático, cifrado de extremo a extremo, notificaciones push y monitoreo en segundo plano.
- Detección automática de caídas con cuenta regresiva (hoy solo sacudidas confirmadas por la usuaria).
- Pruebas con usuarios reales, artículo científico y manual de capturas ampliado.
- No se contacta a ECU911 ni a ninguna central; el botón 911 solo abre el marcador.

## Estructura

- `Services/` — permisos, biometría, contactos, envío, ubicación, sensores, actualizaciones, persistencia.
- `Views/` — pantallas y navegación.
- `Models/` — cuenta, contacto, alerta y preferencias.
- `Platforms/Android/` — manifiesto, actividad y puente de resultados.
- `Resources/` — icono, fuentes e imágenes.
- `Helpers/` — utilidades de interfaz (alertas, navegación, iconos).
