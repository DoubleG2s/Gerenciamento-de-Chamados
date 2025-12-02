// Serviço de TTS para o Chat usando Web Speech API
class ChatTTS {
    constructor() {
        this.synth = window.speechSynthesis;
        this.isSupported = 'speechSynthesis' in window;
        this.currentVoice = null;
        this.initializeVoice();
    }

    initializeVoice() {
        if (!this.isSupported) {
            console.warn('Web Speech API não é suportada neste navegador');
            return;
        }

        // Aguarda as vozes ficarem disponíveis
        if (this.synth.onvoiceschanged !== undefined) {
            this.synth.onvoiceschanged = () => {
                this.loadPortugueseVoice();
            };
        }

        // Tenta carregar imediatamente também
        setTimeout(() => this.loadPortugueseVoice(), 100);
    }

    loadPortugueseVoice() {
        if (!this.isSupported) return;

        const voices = this.synth.getVoices();
        
        // Procura por vozes em português do Brasil primeiro
        const brazilianVoices = voices.filter(voice => 
            voice.lang === 'pt-BR' || 
            voice.name.toLowerCase().includes('brasil') ||
            voice.name.toLowerCase().includes('brazil')
        );

        // Se não encontrar pt-BR, procura qualquer português
        const portugueseVoices = brazilianVoices.length > 0 ? brazilianVoices : 
            voices.filter(voice => 
                voice.lang.startsWith('pt') || 
                voice.name.toLowerCase().includes('portuguese')
            );

        if (portugueseVoices.length > 0) {
            this.currentVoice = portugueseVoices[0];
            console.log('Voz em português carregada:', this.currentVoice.name, 'Lang:', this.currentVoice.lang);
        } else if (voices.length > 0) {
            // Fallback para a primeira voz disponível
            this.currentVoice = voices[0];
            console.log('Usando voz padrão:', this.currentVoice.name, 'Lang:', this.currentVoice.lang);
        } else {
            console.warn('Nenhuma voz disponível');
        }
    }

    speak(text, options = {}) {
        if (!this.isSupported) {
            console.error('Web Speech API não é suportada');
            return false;
        }

        if (!text || text.trim() === '') {
            console.warn('Texto vazio fornecido');
            return false;
        }

        // Para qualquer fala atual
        this.stop();

        // Cria novo utterance
        const utterance = new SpeechSynthesisUtterance(text);

        // Configura a voz
        if (this.currentVoice) {
            utterance.voice = this.currentVoice;
        }

        // Configurações padrão em português
        utterance.lang = 'pt-BR';
        utterance.rate = options.rate || 1.0; // Velocidade normal
        utterance.pitch = options.pitch || 1.0; // Tom normal
        utterance.volume = options.volume || 0.8; // Volume 80%

        // Event handlers
        utterance.onstart = () => {
            console.log('Fala iniciada:', text.substring(0, 50) + (text.length > 50 ? '...' : ''));
            if (options.onStart) options.onStart();
        };

        utterance.onend = () => {
            console.log('Fala concluída');
            if (options.onEnd) options.onEnd();
        };

        utterance.onerror = (event) => {
            console.error('Erro na fala:', event.error);
            if (options.onError) options.onError(event.error);
        };

        // Inicia a fala
        this.synth.speak(utterance);
        return true;
    }

    stop() {
        if (this.isSupported && this.synth.speaking) {
            this.synth.cancel();
            console.log('Fala interrompida');
        }
    }

    pause() {
        if (this.isSupported && this.synth.speaking && !this.synth.paused) {
            this.synth.pause();
            console.log('Fala pausada');
        }
    }

    resume() {
        if (this.isSupported && this.synth.paused) {
            this.synth.resume();
            console.log('Fala retomada');
        }
    }

    isSpeaking() {
        return this.isSupported && this.synth.speaking;
    }

    isPaused() {
        return this.isSupported && this.synth.paused;
    }

    getVoices() {
        if (!this.isSupported) {
            return [];
        }
        return this.synth.getVoices();
    }

    // Método auxiliar para limpar texto
    cleanText(text) {
        // Remove emojis e caracteres especiais que podem causar problemas
        return text
            .replace(/[\u{1F600}-\u{1F64F}]|[\u{1F300}-\u{1F5FF}]|[\u{1F680}-\u{1F6FF}]|[\u{1F1E0}-\u{1F1FF}]|[\u{2600}-\u{26FF}]|[\u{2700}-\u{27BF}]/gu, '')
            .replace(/\n/g, ' ')
            .replace(/\r/g, ' ')
            .replace(/\s+/g, ' ')
            .trim();
    }
}

// Instância global
window.chatTTS = new ChatTTS();

// Funções globais para fácil uso
function speakChatMessage(text, options = {}) {
    if (window.chatTTS) {
        const cleanText = window.chatTTS.cleanText(text);
        return window.chatTTS.speak(cleanText, options);
    }
    return false;
}

function stopChatTTS() {
    if (window.chatTTS) {
        window.chatTTS.stop();
    }
}

function pauseChatTTS() {
    if (window.chatTTS) {
        window.chatTTS.pause();
    }
}

function resumeChatTTS() {
    if (window.chatTTS) {
        window.chatTTS.resume();
    }
}