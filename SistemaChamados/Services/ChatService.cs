using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace SistemaChamados.Services
{
    public class ChatService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChatService> _logger;
        private readonly string _groqApiKey;
        private readonly string _knowledgeBasePath;
        private readonly List<KnowledgeItem> _knowledgeBase;
        private readonly MLContext _mlContext;

        public ChatService(HttpClient httpClient, ILogger<ChatService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _groqApiKey = configuration["Groq:ApiKey"] ?? throw new ArgumentNullException("Groq:ApiKey não configurada");
            _knowledgeBasePath = configuration["KnowledgeBase:Path"] ?? "knowledgebase.csv";
            _knowledgeBase = LoadKnowledgeBase();
            _mlContext = new MLContext(seed: 0);
        }

        private List<KnowledgeItem> LoadKnowledgeBase()
        {
            try
            {
                if (!File.Exists(_knowledgeBasePath))
                {
                    _logger.LogWarning($"Arquivo knowledgebase.csv não encontrado em: {_knowledgeBasePath}");
                    return new List<KnowledgeItem>();
                }

                var lines = File.ReadAllLines(_knowledgeBasePath, Encoding.UTF8);
                var knowledgeBase = new List<KnowledgeItem>();

                for (int i = 1; i < lines.Length; i++) // Pular cabeçalho
                {
                    var parts = lines[i].Split(',');
                    if (parts.Length >= 2)
                    {
                        knowledgeBase.Add(new KnowledgeItem
                        {
                            Question = parts[0].Trim(),
                            Answer = parts[1].Trim()
                        });
                    }
                }

                _logger.LogInformation($"Knowledge base carregada com {knowledgeBase.Count} itens");
                return knowledgeBase;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar knowledge base");
                return new List<KnowledgeItem>();
            }
        }

        public async Task<string> GetResponseAsync(string userInput, string userRole = "cliente")
        {
            try
            {
                _logger.LogInformation($"Processando mensagem do chat - Usuário: {userRole}, Input: {userInput}");
                
                // Se for admin, permite qualquer pergunta
                if (userRole.ToLower() == "admin")
                {
                    _logger.LogInformation("Usuário admin detectado, chamando API Groq diretamente");
                    var response = await CallGroqApiAsync(userInput);
                    _logger.LogInformation($"Resposta admin recebida: {response}");
                    return response;
                }

                // Para clientes, primeiro verifica se a pergunta está na knowledge base
                _logger.LogInformation("Verificando knowledge base para cliente");
                var relevantKnowledge = FindRelevantKnowledge(userInput);
                _logger.LogInformation($"Conhecimento relevante encontrado: {!string.IsNullOrEmpty(relevantKnowledge)}");
                
                if (!string.IsNullOrEmpty(relevantKnowledge))
                {
                    // Se encontrou conhecimento relevante, usa o Groq para formatar a resposta
                    var context = $"Baseado na seguinte informação da knowledge base: {relevantKnowledge}\n\nPergunta do usuário: {userInput}";
                    _logger.LogInformation("Chamando API Groq com contexto da knowledge base");
                    var response = await CallGroqApiAsync(context);
                    _logger.LogInformation($"Resposta com knowledge base recebida: {response}");
                    return response;
                }

                // Se não encontrou na knowledge base, tenta responder com base no contexto geral de suporte
                var generalSupportContext = "Você é um assistente de suporte técnico. O usuário fez a seguinte pergunta que não está em nossa base de conhecimento: " + userInput;
                _logger.LogInformation("Chamando API Groq com contexto geral de suporte");
                var generalResponse = await CallGroqApiAsync(generalSupportContext);
                _logger.LogInformation($"Resposta geral recebida: {generalResponse}");
                return generalResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem do chat");
                return "Desculpe, ocorreu um erro ao processar sua mensagem. Por favor, tente novamente mais tarde.";
            }
        }

        private string FindRelevantKnowledge(string userInput)
        {
            if (!_knowledgeBase.Any())
                return string.Empty;

            // Busca simples por palavras-chave
            var userWords = userInput.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            var bestMatch = _knowledgeBase
                .Select(kb => new
                {
                    Item = kb,
                    Score = CalculateSimilarity(userWords, kb.Question.ToLower())
                })
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            return bestMatch?.Score > 0.1 ? bestMatch.Item.Answer : string.Empty;
        }

        private double CalculateSimilarity(string[] userWords, string knowledgeText)
        {
            var knowledgeWords = knowledgeText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var commonWords = userWords.Intersect(knowledgeWords).Count();
            return (double)commonWords / userWords.Length;
        }

        private async Task<string> CallGroqApiAsync(string message)
        {
            try
            {
                _logger.LogInformation($"Chamando API Groq com mensagem: {message}");
                
                var url = "https://api.groq.com/openai/v1/chat/completions";
                
                var requestBody = new
                {
                    model = "meta-llama/llama-4-scout-17b-16e-instruct",
                    messages = new[]
                    {
                        new { role = "system", content = 
            """ 
                [IDENTIDADE]
                Você é um atendente de Helpdesk corporativo especializado em suporte técnico e orientações internas.

                [OBJETIVO]
                Auxiliar usuários de forma educada, objetiva e eficiente, respondendo com base **exclusivamente** nas informações da base de conhecimento corporativa fornecida como contexto.

                [COMO RESPONDER]
                - Use linguagem simples, profissional e empática.  
                - Dê respostas diretas e curtas (1 a 3 frases).  
                - Sempre ofereça uma solução ou próxima ação clara.  
                - Se houver etapas, liste-as de forma numerada.

                [NÃO FAÇA]
                - Não invente informações fora do contexto.  
                - Não forneça dados pessoais, técnicos ou internos que não constem na base.  
                - Não use jargões técnicos sem explicação.  
                - Não repita mensagens ou se desculpe em excesso.  
                - Se não souber a resposta, diga:  
                > "Não encontrei essa informação na base de conhecimento. Deseja que eu registre um chamado para análise?"

                [FORMATO DE SAÍDA]
                Responda apenas com o texto final para o usuário, sem incluir anotações, raciocínios internos ou metadados.

                [EXEMPLO]
                Usuário: “Meu e-mail não sincroniza no celular.”  
                Assistente: “Verifique se a opção de sincronização está ativada nas configurações do app. Caso o problema continue, reinicie o aplicativo e tente novamente.
                """ },
                        new { role = "user", content = message }
                    },
                    temperature = 0.7,
                    max_tokens = 500
                };

                var json = JsonSerializer.Serialize(requestBody);
                _logger.LogInformation($"Request JSON: {json}");
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_groqApiKey}");

                var response = await _httpClient.PostAsync(url, content);
                _logger.LogInformation($"Status da resposta: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Resposta da API Groq: {responseContent}");
                    
                    // Tentar analisar a estrutura do JSON manualmente
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(responseContent);
                        var choices = jsonDoc.RootElement.GetProperty("choices");
                        _logger.LogInformation($"Número de choices: {choices.GetArrayLength()}");
                        
                        if (choices.GetArrayLength() > 0)
                        {
                            var firstChoice = choices[0];
                            var messageElement = firstChoice.GetProperty("message");
                            var contentText = messageElement.GetProperty("content").GetString();
                            _logger.LogInformation($"Conteúdo extraído manualmente: {contentText}");
                            
                            if (!string.IsNullOrEmpty(contentText))
                            {
                                return contentText;
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogError(parseEx, "Erro ao analisar JSON manualmente");
                    }
                    
                    // Tentar deserialização automática como fallback
                    var result = JsonSerializer.Deserialize<GroqResponse>(responseContent);
                    _logger.LogInformation($"Objeto deserializado: {result != null}");
                    
                    var choice = result?.Choices?.FirstOrDefault();
                    _logger.LogInformation($"Choice encontrada: {choice != null}");
                    
                    var responseText = choice?.Message?.Content;
                    _logger.LogInformation($"Texto da resposta: {responseText}");
                    
                    if (string.IsNullOrEmpty(responseText))
                    {
                        _logger.LogWarning("Conteúdo da resposta está vazio");
                        return "Desculpe, não consegui gerar uma resposta apropriada. Por favor, tente reformular sua pergunta.";
                    }
                    
                    return responseText;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Erro na API Groq: Status {response.StatusCode}, Erro: {error}");
                    return "Desculpe, não consegui processar sua pergunta no momento.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao chamar API Groq");
                return "Desculpe, ocorreu um erro ao processar sua pergunta. Por favor, tente novamente mais tarde.";
            }
        }

        public bool ShouldStopResponding(string userInput)
        {
            var stopWords = new[] { "resolvido", "solucionado", "obrigado", "agradeço", "tudo bem", "até logo" };
            return stopWords.Any(word => userInput.ToLower().Contains(word));
        }
    }

    public class KnowledgeItem
    {
        public string Question { get; set; }
        public string Answer { get; set; }
    }

    public class GroqResponse
    {
        public List<Choice> Choices { get; set; }
    }

    public class Choice
    {
        public Message Message { get; set; }
    }

    public class Message
    {
        public string Content { get; set; }
    }
}