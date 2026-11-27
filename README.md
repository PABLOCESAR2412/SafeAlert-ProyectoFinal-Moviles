# Safe Alert

Proyecto final de Desarrollo de Aplicaciones Móviles de Pablo César Torres Gilse, Universidad Tecnológica Israel.

[Código fuente en GitHub](https://github.com/PABLOCESAR2412/SafeAlert-ProyectoFinal-Moviles)

## Ejecutar la aplicación

El proyecto usa .NET 11 y las cargas de trabajo de .NET MAUI para Android y Windows. Desde la raíz del proyecto:

```powershell
dotnet workload restore SafeAlert.csproj
dotnet build SafeAlert.csproj -f net11.0-android -c Debug
dotnet build SafeAlert.csproj -f net11.0-windows10.0.19041.0 -c Debug
```

En Android, instala el APK firmado generado en la salida de compilación. En Windows, abre SafeAlert.slnx desde Visual Studio con la carga de trabajo de MAUI y selecciona Windows Machine. La aplicación móvil depende de las capacidades y los permisos del dispositivo. El proyecto tiene destinos Android y Windows.

## Alcance de la demostración

- Inicio separa el marcador del 911 del acceso para preparar un SMS a un contacto.
- Alertas registra el evento y abre Mensajes con el borrador para un contacto y ubicación cuando está disponible. La persona confirma el envío.
- Contactos y ficha médica pueden abrir el marcador con el número guardado.
- Perfil permite activar GPS, acelerómetro y agenda. Empiezan apagados.
- Historial conserva los eventos localmente y los exporta a TXT o PDF. El PDF incluye una huella SHA-256 del contenido.
- La cuenta, el perfil, los contactos y las alertas se guardan en SQLite (safealert.db3). Al abrir una instalación que usaba safealert.json, sus datos se importan automáticamente y el archivo original se conserva.
