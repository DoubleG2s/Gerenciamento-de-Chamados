using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;
using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace SistemaChamados.Services
{
#if WINDOWS
    [SupportedOSPlatform("windows")]
#endif
    public class TextToSpeechService
    {
        private readonly ILogger<TextToSpeechService> _logger;
        private readonly HttpClient _httpClient;
#pragma warning disable CS0414 // O campo é atribuído mas seu valor nunca é usado
#if WINDOWS
        private dynamic? _synthesizer;
#else
        private object? _synthesizer;
#endif
#pragma warning restore CS0414

        public TextToSpeechService(ILogger<TextToSpeechService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            InitializeSynthesizer();
        }

        private void InitializeSynthesizer()
        {
#if WINDOWS
            try
            {
                var speechType = Type.GetType("System.Speech.Synthesis.SpeechSynthesizer, System.Speech");
                if (speechType != null)
                {
                    _synthesizer = Activator.CreateInstance(speechType);
                    ConfigureSynthesizer();
                }
                else
                {
                    _synthesizer = null;
                    _logger.LogWarning("System.Speech não disponível. Usando TTS alternativo.");
                }
            }
            catch (Exception ex)
            {
                _synthesizer = null;
                _logger.LogWarning(ex, "Erro ao inicializar sintetizador de voz. Usando TTS alternativo.");
            }
#else
            _synthesizer = null;
            _logger.LogInformation("Sintetizador de voz nativo não disponível. Usando TTS alternativo.");
#endif
        }

        private void ConfigureSynthesizer()
        {
#if WINDOWS
            if (_synthesizer == null) return;
            
            try
            {
                // Configura a voz para português do Brasil usando reflection
                var voiceGenderType = Type.GetType("System.Speech.Synthesis.VoiceGender, System.Speech");
                var voiceAgeType = Type.GetType("System.Speech.Synthesis.VoiceAge, System.Speech");
                var cultureInfoType = Type.GetType("System.Globalization.CultureInfo, System.Private.CoreLib");
                
                if (voiceGenderType != null && voiceAgeType != null && cultureInfoType != null)
                {
                    var femaleGender = Enum.Parse(voiceGenderType, "Female");
                    var adultAge = Enum.Parse(voiceAgeType, "Adult");
                    var ptBRCulture = Activator.CreateInstance(cultureInfoType, new object[] { "pt-BR" });
                    
                    var selectVoiceMethod = _synthesizer.GetType().GetMethod("SelectVoiceByHints", 
                        new Type[] { voiceGenderType, voiceAgeType, typeof(int), cultureInfoType });
                    
                    if (selectVoiceMethod != null)
                    {
                        selectVoiceMethod.Invoke(_synthesizer, new object[] { femaleGender, adultAge, 0, ptBRCulture });
                    }
                    
                    // Configura velocidade
                    var rateProperty = _synthesizer.GetType().GetProperty("Rate");
                    if (rateProperty != null)
                    {
                        rateProperty.SetValue(_synthesizer, 0); // Velocidade normal
                    }
                    
                    // Configura volume
                    var volumeProperty = _synthesizer.GetType().GetProperty("Volume");
                    if (volumeProperty != null)
                    {
                        volumeProperty.SetValue(_synthesizer, 100); // Volume máximo
                    }
                }
                
                _logger.LogInformation("Sintetizador de voz configurado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Não foi possível configurar voz específica para pt-BR. Usando voz padrão.");
            }
#endif
        }

        public async Task<string> GenerateAudioAsync(string text)
        {
#if WINDOWS
            if (_synthesizer != null)
            {
                return await GenerateWindowsAudioAsync(text);
            }
#endif
            
            // Fallback para TTS via navegador/API
            return await GenerateWebAudioAsync(text);
        }

        private async Task<string> GenerateWindowsAudioAsync(string text)
        {
            try
            {
                var cleanText = CleanTextForTTS(text);
                
                // Gera áudio em memória
                using var memoryStream = new MemoryStream();
                
                // Usa reflection para chamar métodos
                var setOutputMethod = _synthesizer.GetType().GetMethod("SetOutputToWaveStream", new Type[] { typeof(Stream) });
                var speakMethod = _synthesizer.GetType().GetMethod("Speak", new Type[] { typeof(string) });
                var setOutputNullMethod = _synthesizer.GetType().GetMethod("SetOutputToNull");
                
                if (setOutputMethod != null && speakMethod != null && setOutputNullMethod != null)
                {
                    // Usa Task.Run para operações síncronas que podem demorar
                    await Task.Run(() =>
                    {
                        setOutputMethod.Invoke(_synthesizer, new object[] { memoryStream });
                        speakMethod.Invoke(_synthesizer, new object[] { cleanText });
                        setOutputNullMethod.Invoke(_synthesizer, null);
                    });
                    
                    // Converte para base64
                    var audioBytes = memoryStream.ToArray();
                    var base64Audio = Convert.ToBase64String(audioBytes);
                    
                    _logger.LogInformation($"Áudio Windows gerado com sucesso. Tamanho: {audioBytes.Length} bytes");
                    return base64Audio;
                }
                else
                {
                    _logger.LogError("Métodos necessários não encontrados no sintetizador");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar áudio Windows");
                return null;
            }
        }

        private async Task<string> GenerateWebAudioAsync(string text)
        {
            try
            {
                var cleanText = CleanTextForTTS(text);
                
                // Gera arquivo de áudio base64 que pode ser reproduzido via navegador
                // Usa Web Speech API sintaxe para compatibilidade
                var audioData = GenerateWebCompatibleAudio(cleanText);
                
                _logger.LogInformation($"Áudio web gerado com sucesso. Tamanho: {audioData.Length} bytes");
                return audioData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar áudio web");
                return null;
            }
        }

        private string GenerateWebCompatibleAudio(string text)
        {
            // Gera um placeholder de áudio que pode ser usado com Web Speech API
            // Em produção, isso seria substituído por uma chamada real a um serviço TTS
            var ttsConfig = new
            {
                text = text,
                lang = "pt-BR",
                rate = 1.0,
                pitch = 1.0,
                volume = 1.0
            };
            
            // Converte para base64 para compatibilidade com o frontend
            var configJson = JsonSerializer.Serialize(ttsConfig);
            var bytes = Encoding.UTF8.GetBytes(configJson);
            return Convert.ToBase64String(bytes);
        }

        public void Dispose()
        {
#if WINDOWS
            if (_synthesizer != null)
            {
                try
                {
                    var disposeMethod = _synthesizer.GetType().GetMethod("Dispose");
                    disposeMethod?.Invoke(_synthesizer, null);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao liberar recursos do sintetizador");
                }
                _synthesizer = null;
            }
#endif
            _httpClient?.Dispose();
        }

        private string CleanTextForTTS(string text)
        {
            // Remove caracteres que podem causar problemas
            var cleaned = text.Replace("\"", "")
                            .Replace("'", "")
                            .Replace("\n", " ")
                            .Replace("\r", " ")
                            .Trim();
            
            // Limita tamanho do texto
            if (cleaned.Length > 1000)
                cleaned = cleaned.Substring(0, 1000) + "...";
            
            return cleaned;
        }
    }
}