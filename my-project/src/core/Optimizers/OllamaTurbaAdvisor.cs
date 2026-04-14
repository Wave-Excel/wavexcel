using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace Optimizers.PSOFlowPathNozzle
{
    /// <summary>
    /// Proposes the next five DAT parameters via local Ollama.
    /// Uses <c>/api/chat</c> unless <c>Ollama:ApiMode</c> is <c>generate</c>. When <c>ApiMode</c> is <c>auto</c> (default), a 404 from <c>/api/chat</c> triggers <c>/api/generate</c>.
    /// Uses absolute URLs for each request (avoids HttpClient BaseAddress combining issues). On errors, logs the response body when present.
    /// Prerequisite: Ollama running and model pulled, e.g. <c>ollama pull deepseek-r1:1.5b</c>.
    /// </summary>
    public sealed class OllamaTurbaAdvisor : IDisposable
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _model;
        private readonly bool _useJsonFormat;
        private readonly string _apiMode;
        private readonly Action<string> _log;

        public OllamaTurbaAdvisor(IConfiguration configuration, Action<string> log)
        {
            _log = log ?? (_ => { });
            _baseUrl = (configuration.GetValue<string>("Ollama:BaseUrl") ?? "http://127.0.0.1:11434").TrimEnd('/');
            _model = configuration.GetValue<string>("Ollama:Model") ?? "deepseek-r1:1.5b";
            var timeoutSec = configuration.GetValue<int>("Ollama:TimeoutSeconds");
            if (timeoutSec <= 0) timeoutSec = 120;
            _useJsonFormat = configuration.GetValue<bool>("Ollama:UseJsonFormat");
            _apiMode = (configuration.GetValue<string>("Ollama:ApiMode") ?? "auto").Trim().ToLowerInvariant();

            _http = new HttpClient();
            _http.Timeout = TimeSpan.FromSeconds(timeoutSec);
        }

        /// <summary>
        /// Calls Ollama with a JSON snapshot of the last Turba run. Returns five proposed values or null if the response is unusable.
        /// Caller must clamp and snap to engineering bounds.
        /// </summary>
        public double[] ProposeNextParameters(string snapshotJson, out string rawAssistantContent)
        {
            rawAssistantContent = null;
            const string systemPrompt =
                "You are an engineering assistant for a steam turbine nozzle / flow-path optimization loop. " +
                "Each iteration: five values are written into the TURBA DAT template (TURBATURBAE1-style), Turba.exe is run, and an ERG-derived snapshot is scored with a penalty. " +
                "Your job is to propose the NEXT set of five DAT knobs only. " +
                "Parameter keys (must appear exactly once in JSON): \"B\" = BEAUFSCHL (admission), \"R\" = RADKAMMER (wheel chamber pressure), " +
                "\"D\" = DRUCKZIFFERN (stage count; same sign convention as in the snapshot), \"I\" = INNENDURCHMESSER (shaft diameter), \"A\" = AUSGLEICHSKOLBEN (balance piston diameter). " +
                "The user JSON snapshot includes: parameterLimits with lowerLimit, upperLimit, and step for B,R,D,I,A; a one-line limitsSummary repeating those bounds; " +
                "outputLimits and outputLimitsSummary (BCD-specific feasible bands for ERG outputs—HOEHE, FMIN1, DELTA_T, wheel chamber T ceiling, GBC_Length, thrust per LP, PSI/Lang—aligned with the penalty checks); " +
                "checkType (e.g. BCD 1120 vs 1190), current B/R/D/I/A, bounds array (same limits as min/max aliases), penalty, efficiency, " +
                "hard-check flags (Check_HOEHE, Check_FMIN1, Check_DELTA_T, Check_Wheel_Chamber_Temperature, Check_GBC_Length, Check_PSI, Check_Lang, Check_Thrust), " +
                "and key outputs (HOEHE, FMIN1, DELTA_T, wheel chamber P/T, GBC_Length, thrust per load point). " +
                "Never propose a value below lowerLimit or above upperLimit for any key; prefer small moves that stay on the step grid. " +
                "Use failing checks and numeric outputs to choose feasible adjustments. " +
                "Reply with ONE JSON object only: no markdown fences, no commentary, no keys other than B, R, D, I, A. " +
                "Keep reasoning minimal so the answer stays short. Lets think step by step and make a plan before you propose the next values.";

            var userContent =
                "Current Turba optimization snapshot (JSON). Read parameterLimits and limitsSummary for lowerLimit/upperLimit/step per key; read outputLimits and outputLimitsSummary to see how far ERG outputs may move for the active BCD.\n\n" + snapshotJson +
                "\n\nOutput exactly one JSON object, every value within lowerLimit..upperLimit: {\"B\":<number>,\"R\":<number>,\"D\":<number>,\"I\":<number>,\"A\":<number>}";

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            string assistantText = null;

            if (_apiMode == "generate")
            {
                assistantText = CallGenerateApi(systemPrompt, userContent, jsonOptions);
            }
            else if (_apiMode == "chat")
            {
                assistantText = CallChatApi(systemPrompt, userContent, jsonOptions, out _);
            }
            else
            {
                assistantText = CallChatApi(systemPrompt, userContent, jsonOptions, out var chatNotFound);
                if (assistantText == null && chatNotFound)
                {
                    _log("Ollama: POST /api/chat returned 404; retrying with /api/generate.");
                    assistantText = CallGenerateApi(systemPrompt, userContent, jsonOptions);
                }
            }

            if (string.IsNullOrEmpty(assistantText))
            {
                _log("Ollama: empty assistant content after chat/generate.");
                return null;
            }

            rawAssistantContent = assistantText;
            var cleaned = StripReasoningBlocks(rawAssistantContent);
            var objectJson = ExtractJsonObject(cleaned);
            if (objectJson == null)
            {
                _log("Could not extract JSON object from model output.");
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(objectJson);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return null;
                var b = GetDouble(root, "B");
                var r = GetDouble(root, "R");
                var d = GetDouble(root, "D");
                var i = GetDouble(root, "I");
                var a = GetDouble(root, "A");
                if (!b.HasValue || !r.HasValue || !d.HasValue || !i.HasValue || !a.HasValue)
                    return null;
                return new[] { b.Value, r.Value, d.Value, i.Value, a.Value };
            }
            catch (Exception ex)
            {
                _log($"Proposed parameters JSON parse failed: {ex.Message}");
                return null;
            }
        }

        private string CallChatApi(string systemPrompt, string userContent, JsonSerializerOptions jsonOptions, out bool chatReturned404)
        {
            chatReturned404 = false;
            var request = new OllamaChatRequest
            {
                Model = _model,
                Stream = false,
                Messages = new List<OllamaChatMessage>
                {
                    new OllamaChatMessage { Role = "system", Content = systemPrompt },
                    new OllamaChatMessage { Role = "user", Content = userContent }
                },
                Format = _useJsonFormat ? "json" : null
            };

            var body = JsonSerializer.Serialize(request, jsonOptions);
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            var url = $"{_baseUrl}/api/chat";

            HttpResponseMessage response;
            try
            {
                response = _http.PostAsync(url, content).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log($"Ollama POST {_baseUrl}/api/chat failed: {ex.Message}");
                return null;
            }

            var responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                chatReturned404 = true;
                LogOllamaFailure("chat", (int)response.StatusCode, responseText);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                LogOllamaFailure("chat", (int)response.StatusCode, responseText);
                return null;
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<OllamaChatResponse>(responseText, jsonOptions);
                return parsed?.Message?.Content;
            }
            catch (Exception ex)
            {
                _log($"Ollama chat response JSON parse failed: {ex.Message}");
                return null;
            }
        }

        private string CallGenerateApi(string systemPrompt, string userContent, JsonSerializerOptions jsonOptions)
        {
            var request = new OllamaGenerateRequest
            {
                Model = _model,
                System = systemPrompt,
                Prompt = userContent,
                Stream = false,
                Format = _useJsonFormat ? "json" : null
            };

            var body = JsonSerializer.Serialize(request, jsonOptions);
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            var url = $"{_baseUrl}/api/generate";

            HttpResponseMessage response;
            try
            {
                response = _http.PostAsync(url, content).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log($"Ollama POST {_baseUrl}/api/generate failed: {ex.Message}");
                return null;
            }

            var responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                LogOllamaFailure("generate", (int)response.StatusCode, responseText);
                return null;
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<OllamaGenerateResponse>(responseText, jsonOptions);
                if (!string.IsNullOrEmpty(parsed?.Error))
                    _log($"Ollama generate error field: {parsed.Error}");
                return parsed?.Response;
            }
            catch (Exception ex)
            {
                _log($"Ollama generate response JSON parse failed: {ex.Message}");
                return null;
            }
        }

        private void LogOllamaFailure(string api, int statusCode, string body)
        {
            var snippet = string.IsNullOrEmpty(body)
                ? "(empty body)"
                : (body.Length > 600 ? body.Substring(0, 600) + "..." : body);
            _log($"Ollama HTTP {statusCode} on /api/{api}. Body: {snippet}");
        }

        private static double? GetDouble(JsonElement root, string name)
        {
            foreach (var prop in root.EnumerateObject())
            {
                if (!string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                    continue;
                var el = prop.Value;
                return el.ValueKind switch
                {
                    JsonValueKind.Number => el.GetDouble(),
                    JsonValueKind.String when double.TryParse(el.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) => v,
                    _ => null
                };
            }
            return null;
        }

        internal static string StripReasoningBlocks(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var s = text;
            const string start1 = "<think>";
            const string end1 = "</think>";
            while (true)
            {
                int a = s.IndexOf(start1, StringComparison.OrdinalIgnoreCase);
                if (a < 0) break;
                int b = s.IndexOf(end1, a, StringComparison.OrdinalIgnoreCase);
                if (b < 0) break;
                s = s.Remove(a, b - a + end1.Length);
            }
            return s.Trim();
        }

        internal static string ExtractJsonObject(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            int start = text.IndexOf('{');
            int end = text.LastIndexOf('}');
            if (start < 0 || end <= start) return null;
            return text.Substring(start, end - start + 1);
        }

        public void Dispose() => _http.Dispose();

        private sealed class OllamaChatRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; }

            [JsonPropertyName("messages")]
            public List<OllamaChatMessage> Messages { get; set; }

            [JsonPropertyName("stream")]
            public bool Stream { get; set; }

            [JsonPropertyName("format")]
            public string Format { get; set; }
        }

        private sealed class OllamaGenerateRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; }

            [JsonPropertyName("system")]
            public string System { get; set; }

            [JsonPropertyName("prompt")]
            public string Prompt { get; set; }

            [JsonPropertyName("stream")]
            public bool Stream { get; set; }

            [JsonPropertyName("format")]
            public string Format { get; set; }
        }

        private sealed class OllamaChatMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; }

            [JsonPropertyName("content")]
            public string Content { get; set; }
        }

        private sealed class OllamaChatResponse
        {
            [JsonPropertyName("message")]
            public OllamaChatResponseMessage Message { get; set; }
        }

        private sealed class OllamaChatResponseMessage
        {
            [JsonPropertyName("content")]
            public string Content { get; set; }
        }

        private sealed class OllamaGenerateResponse
        {
            [JsonPropertyName("response")]
            public string Response { get; set; }

            [JsonPropertyName("error")]
            public string Error { get; set; }
        }
    }
}
