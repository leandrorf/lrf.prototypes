using Microsoft.AspNetCore.Mvc;
using lrfdev.auth.web.Models;
using QRCoder;
using System.Net.Http.Headers;
using System.Text.Json;

namespace lrfdev.auth.web.Controllers;

public sealed class DeviceController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<DeviceController> logger) : Controller
{
    private const string GrantTypeDeviceCode = "urn:ietf:params:oauth:grant-type:device_code";

    [HttpGet]
    public IActionResult Authorize(string? user_code)
    {
        var model = new DeviceAuthorizeViewModel();
        if (!string.IsNullOrWhiteSpace(user_code))
        {
            model.UserCode = user_code.Trim();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Authorize(DeviceAuthorizeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var client = httpClientFactory.CreateClient("auth-api");
        var form = new Dictionary<string, string>
        {
            ["user_code"] = model.UserCode.Trim(),
            ["username"] = model.Username.Trim(),
            ["password"] = model.Password
        };

        using var content = new FormUrlEncodedContent(form);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync("/connect/device", content);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao chamar API de autorização do dispositivo.");
            model.ErrorMessage = "Não foi possível contatar o servidor de identidade.";
            model.Password = string.Empty;
            return View(model);
        }

        var body = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            using var doc = JsonDocument.Parse(body);
            var msg = doc.RootElement.TryGetProperty("message", out var m)
                ? m.GetString()
                : "Dispositivo autorizado.";
            model.SuccessMessage = msg;
            model.Password = string.Empty;
            return View(model);
        }

        model.ErrorMessage = TryReadOAuthError(body) ?? $"Erro HTTP {(int)response.StatusCode}.";
        model.Password = string.Empty;
        return View(model);
    }

    /// <summary>Simula uma TV: inicia device flow e exibe QR + polling.</summary>
    [HttpGet]
    public IActionResult SimulateTv()
    {
        return View(new SimulatedTvViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartSimulatedTv()
    {
        var tvClientId = configuration["DeviceFlow:TvClientId"] ?? "lrfdev.tv.device";
        var client = httpClientFactory.CreateClient("auth-api");
        var form = new Dictionary<string, string>
        {
            ["client_id"] = tvClientId
        };

        using var content = new FormUrlEncodedContent(form);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync("/connect/deviceauthorization", content);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao iniciar device authorization.");
            return View("SimulateTv", new SimulatedTvViewModel
            {
                ErrorMessage = "Não foi possível contatar o servidor de identidade."
            });
        }

        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            return View("SimulateTv", new SimulatedTvViewModel
            {
                ErrorMessage = TryReadOAuthError(body) ?? $"Erro HTTP {(int)response.StatusCode}."
            });
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var deviceCode = root.GetProperty("device_code").GetString() ?? string.Empty;
        var userCode = root.GetProperty("user_code").GetString() ?? string.Empty;
        var verificationUri = root.GetProperty("verification_uri").GetString() ?? string.Empty;
        var verificationComplete = root.GetProperty("verification_uri_complete").GetString() ?? string.Empty;
        var interval = root.TryGetProperty("interval", out var iv) ? iv.GetInt32() : 5;

        HttpContext.Session.SetString("device_flow_device_code", deviceCode);
        HttpContext.Session.SetString("device_flow_client_id", tvClientId);
        HttpContext.Session.SetString("device_flow_user_code", userCode);

        var qrDataUrl = BuildQrDataUrl(verificationComplete);

        return View("SimulateTvDisplay", new SimulatedTvViewModel
        {
            UserCode = userCode,
            VerificationUri = verificationUri,
            VerificationUriComplete = verificationComplete,
            QrDataUrl = qrDataUrl,
            PollIntervalSeconds = Math.Max(2, interval)
        });
    }

    /// <summary>Usado pelo JavaScript da TV simulada para poll do token.</summary>
    [HttpGet]
    public async Task<IActionResult> PollToken()
    {
        var deviceCode = HttpContext.Session.GetString("device_flow_device_code");
        var clientId = HttpContext.Session.GetString("device_flow_client_id");
        if (string.IsNullOrEmpty(deviceCode) || string.IsNullOrEmpty(clientId))
        {
            return Content(
                """{"error":"session_expired","error_description":"Inicie o fluxo novamente na TV simulada."}""",
                "application/json",
                System.Text.Encoding.UTF8);
        }

        var client = httpClientFactory.CreateClient("auth-api");
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = GrantTypeDeviceCode,
            ["device_code"] = deviceCode,
            ["client_id"] = clientId
        };

        using var content = new FormUrlEncodedContent(form);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var response = await client.PostAsync("/connect/token", content);
        var body = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode &&
            body.Contains("access_token", StringComparison.Ordinal))
        {
            HttpContext.Session.Remove("device_flow_device_code");
        }

        return new ContentResult
        {
            Content = body,
            ContentType = "application/json; charset=utf-8",
            StatusCode = (int)response.StatusCode
        };
    }

    private static string BuildQrDataUrl(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(20);
        return "data:image/png;base64," + Convert.ToBase64String(bytes);
    }

    private static string? TryReadOAuthError(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("error", out var err))
            {
                return null;
            }

            var code = err.GetString();
            if (doc.RootElement.TryGetProperty("error_description", out var d))
            {
                return $"{code}: {d.GetString()}";
            }

            return code;
        }
        catch
        {
            return null;
        }
    }
}
