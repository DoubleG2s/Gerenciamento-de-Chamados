using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using SistemaChamados.Services;
using Microsoft.AspNetCore.Identity;
using SistemaChamados.Models;
using System.Security.Claims;

namespace SistemaChamados.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chatService;
        private readonly TextToSpeechService _ttsService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            ChatService chatService, 
            TextToSpeechService ttsService, 
            ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _ttsService = ttsService;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
                var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "cliente";
                
                // Verifica se o usuário quer encerrar o chat
                if (_chatService.ShouldStopResponding(request.Message))
                {
                    return Ok(new ChatResponse
                    {
                        Response = "Ótimo! Fico feliz que consegui ajudar. Se precisar de mais alguma coisa, estarei por aqui! 😊",
                        IsResolved = true,
                        AudioBase64 = await _ttsService.GenerateAudioAsync("Ótimo! Fico feliz que consegui ajudar. Se precisar de mais alguma coisa, estarei por aqui!")
                    });
                }

                // Obtém resposta da IA
                var response = await _chatService.GetResponseAsync(request.Message, userRole);
                
                // Gera áudio se solicitado
                string audioBase64 = null;
                if (request.GenerateAudio)
                {
                    audioBase64 = await _ttsService.GenerateAudioAsync(response);
                }

                return Ok(new ChatResponse
                {
                    Response = response,
                    IsResolved = false,
                    AudioBase64 = audioBase64
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem do chat");
                return StatusCode(500, new { error = "Erro interno ao processar mensagem" });
            }
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new { status = "online", timestamp = DateTime.UtcNow });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
        public bool GenerateAudio { get; set; } = false;
    }

    public class ChatResponse
    {
        public string Response { get; set; }
        public bool IsResolved { get; set; }
        public string AudioBase64 { get; set; }
    }
}