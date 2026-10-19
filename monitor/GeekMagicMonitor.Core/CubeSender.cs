using System.Text.Json;

namespace GeekMagicMonitor.Core;

public sealed class CubeSender(HttpClient client)
{
    private ScreenFrame? previous;
    private int failures;
    private DateTimeOffset retryAt;
    private DateTimeOffset lastSuccess;
    private int generation;
    private Uri? capabilityAddress;
    private bool supportsCyrillic;
    private DateTimeOffset capabilityExpires;
    public void Reset() { generation++; previous = null; failures = 0; retryAt = default; capabilityExpires=default; }
    public async Task<string> SendAsync(Uri address, ScreenFrame frame, DateTimeOffset now, CancellationToken token,string language="ru")
    {
        string T(string text)=>Localization.Text(text,language);
        if (now < retryAt) return T("Повтор через ")+Math.Ceiling((retryAt-now).TotalSeconds)+T(" с");
        int requestGeneration = generation;
        var commands = Frames.Commands(frame, now - lastSuccess > TimeSpan.FromSeconds(10) ? null : previous);
        if (commands.Count == 0) return T("Куб: экран актуален");
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));
            if(frame.Lines.Any(line=>line.Text.Any(c=>c is >= '\u0400' and <= '\u04ff')))
            {
                if(capabilityAddress!=address || now>=capabilityExpires)
                {
                    using var device=JsonDocument.Parse(await client.GetStringAsync(new Uri(address,"api/v1/device"),timeout.Token));
                    supportsCyrillic=device.RootElement.TryGetProperty("application",out var app) && app.GetString()=="GeekMagicMonitor" &&
                        device.RootElement.TryGetProperty("cyrillicText",out var supported) && supported.ValueKind==JsonValueKind.True;
                    capabilityAddress=address;capabilityExpires=now.AddSeconds(supportsCyrillic?300:10);
                }
                if(!supportsCyrillic)return T("Для русского экрана обновите firmware.bin: текущая прошивка не поддерживает кириллицу.");
            }
            if(requestGeneration!=generation)return T("Настройки изменены; ожидается новый кадр");
            // ESP8266WebServer requires Content-Length and cannot decode a chunked JSON body.
            // JsonContent/PostAsJsonAsync streams by default; pre-serialize a bounded frame.
            using var content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(new {commands}));
            content.Headers.ContentType = new("application/json") {CharSet="utf-8"};
            using var response = await client.PostAsync(new Uri(address,"api/v1/draw/batch"), content, timeout.Token);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            if (json.RootElement.GetProperty("status").GetString() != "ok" || json.RootElement.GetProperty("processed").GetInt32() != commands.Count)
                throw new InvalidDataException(T("Неполное подтверждение кадра"));
            if (requestGeneration != generation) return T("Настройки изменены; ожидается новый кадр");
            previous = frame;
            failures = 0;
            lastSuccess = now;
            return T("Куб: кадр принят");
        }
        catch (Exception ex) when (!token.IsCancellationRequested && ex is HttpRequestException or OperationCanceledException or JsonException or InvalidDataException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            if (requestGeneration != generation) return T("Настройки изменены; ожидается новый кадр");
            previous = null;
            failures = Math.Min(failures+1,5);
            retryAt = now.AddSeconds(Math.Min(30,Math.Pow(2,failures)));
            return T("Куб недоступен: ") + ex.GetType().Name;
        }
    }
    public static Uri Address(string host,string language="ru")
    {
        if (!Uri.TryCreate("http://"+host.Trim()+"/",UriKind.Absolute,out var uri) ||
            string.IsNullOrWhiteSpace(uri.Host) || uri.UserInfo.Length > 0 || uri.AbsolutePath != "/" || uri.Query.Length > 0 || uri.Fragment.Length > 0)
            throw new ArgumentException(Localization.Text("Введите IP или имя куба без http:// и пути",language));
        return uri;
    }
}
