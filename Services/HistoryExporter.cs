using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SafeAlert.Models;

namespace SafeAlert.Services;

public static class HistoryExporter
{
    public static string ExportTxt(UserAccount account)
    {
        var directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            directory = FileSystem.AppDataDirectory;

        var path = Path.Combine(directory, "Historial_SafeAlert.txt");
        var culture = CultureInfo.GetCultureInfo("es-ES");
        var builder = new StringBuilder();

        builder.AppendLine("============================================================");
        builder.AppendLine("                 SAFEALERT - HISTORIAL DE ALERTAS");
        builder.AppendLine("============================================================");
        builder.AppendLine($"Usuario:        {account.Profile.FullName}");
        builder.AppendLine($"Correo:         {account.Profile.Email}");
        builder.AppendLine($"Teléfono:       {account.Profile.Phone}");
        builder.AppendLine($"ID:             {account.Profile.MemberId}");
        builder.AppendLine($"Exportado:      {DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy, HH:mm", culture)}");
        builder.AppendLine($"Total de alertas: {account.Alerts.Count}");
        builder.AppendLine();

        var index = 1;
        foreach (var alert in account.Alerts.OrderByDescending(a => a.CreatedAt))
        {
            builder.AppendLine($"------------------------------------------------------------");
            builder.AppendLine($"ALERTA #{index} - {alert.TypeTitle.ToUpperInvariant()}");
            builder.AppendLine($"Fecha:       {alert.CreatedAt.ToString("dd 'de' MMMM 'de' yyyy, HH:mm", culture)}");
            builder.AppendLine($"Estado:      {alert.StatusText}");
            builder.AppendLine($"Ubicación:   {alert.Address}");
            builder.AppendLine($"Coordenadas: {alert.CoordinatesText}");
            builder.AppendLine($"Contactos:   {alert.NotifiedContacts} notificados");
            builder.AppendLine($"Respuesta:   {alert.ResponseLine}");
            builder.AppendLine($"Mensaje:     {alert.Message}");

            if (!string.IsNullOrWhiteSpace(alert.Notes))
                builder.AppendLine($"Notas:       {alert.Notes}");

            builder.AppendLine();
            index++;
        }

        builder.AppendLine("============================================================");
        builder.AppendLine("Reporte generado automáticamente por SafeAlert.");
        builder.AppendLine("Datos locales - Sin servidor remoto");
        builder.AppendLine("============================================================");

        File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
        return path;
    }

    public static string ExportPdf(UserAccount account)
    {
        var directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            directory = FileSystem.AppDataDirectory;

        var path = Path.Combine(directory, "Historial_SafeAlert.pdf");
        var culture = CultureInfo.GetCultureInfo("es-ES");
        var rawLines = new List<string>
        {
            "SAFEALERT - HISTORIAL CERTIFICADO DE ALERTAS",
            "============================================================",
            $"Usuario:          {account.Profile.FullName}",
            $"Correo:           {account.Profile.Email}",
            $"Telefono:         {account.Profile.Phone}",
            $"ID de miembro:    {account.Profile.MemberId}",
            $"Exportado:        {DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy, HH:mm", culture)}",
            $"Total de alertas: {account.Alerts.Count}",
            "------------------------------------------------------------"
        };

        var index = 1;
        foreach (var alert in account.Alerts.OrderByDescending(alert => alert.CreatedAt))
        {
            rawLines.Add($"ALERTA #{index} - {alert.TypeTitle.ToUpperInvariant()}");
            rawLines.Add($"Fecha:       {alert.CreatedAt.ToString("dd 'de' MMMM 'de' yyyy, HH:mm", culture)}");
            rawLines.Add($"Estado:      {alert.StatusText}");
            rawLines.Add($"Ubicacion:   {alert.Address}");
            rawLines.Add($"Coordenadas: {alert.CoordinatesText}");
            rawLines.Add($"Contactos:   {alert.NotifiedContacts} notificados");
            rawLines.Add($"Respuesta:   {alert.ResponseLine}");
            rawLines.Add($"Mensaje:     {alert.Message}");

            if (!string.IsNullOrWhiteSpace(alert.Notes))
                rawLines.Add($"Notas:       {alert.Notes}");

            rawLines.Add("------------------------------------------------------------");
            index++;
        }

        var fingerprint = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", rawLines))));
        rawLines.Add($"Firma digital SHA-256: {fingerprint}");
        rawLines.Add("Reporte generado automaticamente por SafeAlert.");
        rawLines.Add("Datos locales - Sin servidor remoto");

        var lines = rawLines.SelectMany(line => Wrap(line, 92)).ToList();
        if (lines.Count == 0)
            lines.Add("Sin alertas registradas.");

        const int linesPerPage = 48;
        var pages = lines
            .Chunk(linesPerPage)
            .Select(chunk => chunk.ToList())
            .ToList();

        if (pages.Count == 0)
            pages.Add(new List<string>());

        var pageCount = pages.Count;
        var fontObject = 3 + (pageCount * 2);
        var totalObjects = fontObject;
        var offsets = new int[totalObjects + 1];
        var pdf = new StringBuilder();

        pdf.Append("%PDF-1.4\n");

        offsets[1] = pdf.Length;
        pdf.Append("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        offsets[2] = pdf.Length;
        var kids = string.Join(" ", Enumerable.Range(0, pageCount)
            .Select(i => $"{3 + (i * 2)} 0 R"));
        pdf.Append($"2 0 obj\n<< /Type /Pages /Kids [ {kids} ] /Count {pageCount} >>\nendobj\n");

        for (var i = 0; i < pageCount; i++)
        {
            var pageObject = 3 + (i * 2);
            var contentObject = pageObject + 1;

            offsets[pageObject] = pdf.Length;
            pdf.Append($"{pageObject} 0 obj\n");
            pdf.Append("<< /Type /Page /Parent 2 0 R /MediaBox [ 0 0 595 842 ] ");
            pdf.Append($"/Resources << /Font << /F1 {fontObject} 0 R >> >> ");
            pdf.Append($"/Contents {contentObject} 0 R >>\nendobj\n");

            var content = new StringBuilder();
            content.Append("BT\n/F1 10 Tf\n50 800 Td\n14 TL\n");
            foreach (var line in pages[i])
                content.Append($"({EscapePdf(line)}) Tj\nT*\n");
            content.Append("ET");

            offsets[contentObject] = pdf.Length;
            pdf.Append($"{contentObject} 0 obj\n");
            pdf.Append($"<< /Length {content.Length} >>\nstream\n");
            pdf.Append(content);
            pdf.Append("\nendstream\nendobj\n");
        }

        offsets[fontObject] = pdf.Length;
        pdf.Append($"{fontObject} 0 obj\n");
        pdf.Append("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\n");
        pdf.Append("endobj\n");

        var xrefPosition = pdf.Length;
        pdf.Append("xref\n");
        pdf.Append($"0 {totalObjects + 1}\n");
        pdf.Append("0000000000 65535 f \n");

        for (var i = 1; i <= totalObjects; i++)
            pdf.Append($"{offsets[i]:0000000000} 00000 n \n");

        pdf.Append("trailer\n");
        pdf.Append($"<< /Size {totalObjects + 1} /Root 1 0 R /Info << /Title (Historial certificado SafeAlert) /Author (SafeAlert) >> >>\n");
        pdf.Append("startxref\n");
        pdf.Append(xrefPosition);
        pdf.Append("\n%%EOF");

        File.WriteAllText(path, pdf.ToString(), Encoding.ASCII);
        return path;
    }

    private static IEnumerable<string> Wrap(string text, int width)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield return string.Empty;
            yield break;
        }

        if (text.Length <= width)
        {
            yield return text;
            yield break;
        }

        var line = new StringBuilder();
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + word.Length + 1 > width)
            {
                yield return line.ToString();
                line.Clear();
            }

            if (line.Length > 0)
                line.Append(' ');

            line.Append(word);
        }

        if (line.Length > 0)
            yield return line.ToString();
    }

    private static string EscapePdf(string text)
    {
        var ascii = new StringBuilder(text.Length);

        foreach (var character in text)
        {
            var mapped = character switch
            {
                'á' or 'à' or 'ä' or 'â' => 'a',
                'é' or 'è' or 'ë' or 'ê' => 'e',
                'í' or 'ì' or 'ï' or 'î' => 'i',
                'ó' or 'ò' or 'ö' or 'ô' => 'o',
                'ú' or 'ù' or 'ü' or 'û' => 'u',
                'Á' or 'À' or 'Ä' or 'Â' => 'A',
                'É' or 'È' or 'Ë' or 'Ê' => 'E',
                'Í' or 'Ì' or 'Ï' or 'Î' => 'I',
                'Ó' or 'Ò' or 'Ö' or 'Ô' => 'O',
                'Ú' or 'Ù' or 'Ü' or 'Û' => 'U',
                'ñ' => 'n',
                'Ñ' => 'N',
                '¿' => '?',
                '¡' => '!',
                '•' or '–' or '—' => '-',
                '“' or '”' => '"',
                '‘' or '’' => '\'',
                '…' => '.',
                _ => character is >= ' ' and <= '~' ? character : '?'
            };

            ascii.Append(mapped);
        }

        return ascii
            .ToString()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }
}
