using SafeAlert.Models;

namespace SafeAlert.Helpers;

public static class EmergencyCatalog
{
    public static string Title(EmergencyType type) => type switch
    {
        EmergencyType.Panic => "Riesgo Inminente",
        EmergencyType.Medical => "Accidente Médico",
        EmergencyType.Harassment => "Acoso / Sospecha",
        EmergencyType.Robbery => "Aviso de robo observado",
        EmergencyType.Police => "Seguridad Policial",
        EmergencyType.Silent => "Alerta Silenciosa",
        EmergencyType.WalkSafe => "Camina Seguro",
        EmergencyType.Test => "Prueba de Sistema y Sirena",
        _ => "Alerta"
    };

    public static string Subtitle(EmergencyType type) => type switch
    {
        EmergencyType.Panic => "Peligro vital inmediato",
        EmergencyType.Medical => "Salud / Ambulancia",
        EmergencyType.Harassment => "Seguimiento hostil",
        EmergencyType.Robbery => "Incidente observado a terceros",
        EmergencyType.Police => "Envío a patrullas",
        EmergencyType.Silent => "Sin sonido ni luces",
        EmergencyType.WalkSafe => "Ruta en vivo",
        EmergencyType.Test => "Simulación preventiva",
        _ => string.Empty
    };

    public static string DefaultMessage(EmergencyType type) => type switch
    {
        EmergencyType.Medical => "Emergencia médica. Necesito asistencia de una ambulancia en mi ubicación. Por favor acudan lo antes posible.",
        EmergencyType.Harassment => "Me siento en peligro. Alguien me está siguiendo o acosando. Por favor envíen ayuda a mi ubicación.",
        EmergencyType.Robbery => "Observé un posible robo que afecta a otra persona. Te aviso para que tomes precauciones. Revisa la ubicación si está adjunta.",
        EmergencyType.Police => "Solicito presencia policial inmediata en mi ubicación.",
        EmergencyType.Silent => "Alerta silenciosa activada. Compartan mi ubicación con mis contactos de confianza.",
        EmergencyType.WalkSafe => "Estoy realizando una ruta segura y necesito seguimiento de mi ubicación.",
        EmergencyType.Test => "Prueba del sistema SafeAlert. No es una emergencia real.",
        _ => "¡Necesito ayuda urgente! Me encuentro en esta ubicación y estoy en una situación de riesgo. Por favor envíen apoyo y llamen a las autoridades."
    };

    public static string HistoryTitle(EmergencyType type) => type switch
    {
        EmergencyType.Panic => "Pánico / Alerta SOS",
        EmergencyType.Medical => "Alerta Médica SOS",
        EmergencyType.Harassment => "Acoso / Sospecha",
        EmergencyType.Robbery => "Aviso de robo observado",
        EmergencyType.Police => "Seguridad Policial",
        EmergencyType.Silent => "Pánico / Alerta Silenciosa",
        EmergencyType.WalkSafe => "Camina Seguro",
        EmergencyType.Test => "Prueba de Sistema y Sirena",
        _ => "Alerta"
    };

    public static string Icon(EmergencyType type) => type switch
    {
        EmergencyType.Medical => Icons.Medical,
        EmergencyType.Harassment => Icons.Visibility,
        EmergencyType.Robbery => Icons.Police,
        EmergencyType.Police => Icons.Police,
        EmergencyType.Silent => Icons.VisibilityOff,
        EmergencyType.WalkSafe => Icons.Walk,
        EmergencyType.Test => Icons.NotificationsActive,
        _ => Icons.Emergency
    };
}
