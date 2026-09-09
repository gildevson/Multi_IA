# Arquitetura do Projeto Jarvis

## Visão Geral

O Jarvis é um assistente de IA desktop construído com **Clean Architecture**, **Domain-Driven Design (DDD)** e **CQRS com MediatR**, rodando sobre WPF com o padrão **MVVM**.

---

## Padrões Arquiteturais Utilizados

| Padrão | Onde é aplicado |
|--------|----------------|
| **Clean Architecture** | Separação em camadas com dependências apontando para o centro |
| **DDD (Domain-Driven Design)** | Entidades, Value Objects, Eventos de Domínio |
| **CQRS** | Commands e Queries separados via MediatR |
| **Repository Pattern** | Abstração do acesso a dados (conversas, configurações) |
| **MVVM** | Camada de apresentação WPF |
| **Provider Pattern** | Provedores de IA, Voz e Visão intercambiáveis |
| **Factory Pattern** | `AIProviderFactory` seleciona o provider em runtime |
| **Adapter Pattern** | `VisionAIProviderAdapter` adapta `IAIProvider` para `IVisionAIProvider` |
| **Mediator Pattern** | MediatR desacopla ViewModel dos Handlers |

---

## Estrutura de Camadas

```
src/
├── Core/                          # Núcleo da aplicação (sem dependências externas)
│   ├── Jarvis.Domain              # Regras de negócio puras
│   ├── Jarvis.Application         # Casos de uso, Commands, Queries
│   └── Jarvis.Shared              # Modelos e constantes compartilhados
│
├── Infrastructure/                # Implementações de infraestrutura
│   └── Jarvis.Infrastructure      # Configurações, Repositórios JSON, Eventos
│
├── Providers/                     # Provedores externos (plugáveis)
│   ├── Jarvis.AI                  # Provedores de IA (Claude, OpenAI, Gemini, Ollama)
│   ├── Jarvis.Vision              # Captura de tela e análise de imagem
│   └── Jarvis.Voice               # STT (Whisper local) e TTS (Piper, ElevenLabs)
│
├── Tools/                         # Ferramentas que a IA pode invocar
│   └── Jarvis.Tools               # Sistema, Browser, Desenvolvimento, Visão
│
└── Presentation/                  # Interface do usuário
    └── Jarvis.Desktop             # WPF — Views, ViewModels, Converters
```

---

## Camada Domain (`Jarvis.Domain`)

O centro da arquitetura. Não depende de nada externo.

### Entidades
| Classe | Responsabilidade |
|--------|-----------------|
| `Conversation` | Agrupa mensagens em uma sessão de chat |
| `Message` | Representa uma mensagem (User, Assistant, System, Tool) |
| `ToolExecution` | Registro de uma execução de ferramenta pela IA |

### Value Objects
| Classe | Responsabilidade |
|--------|-----------------|
| `MessageContent` | Conteúdo de uma mensagem (texto, imagem, PDF) |
| `ImageData` | Bytes + MIME type de uma imagem |
| `AudioData` | Bytes + formato de áudio |
| `PdfData` | Bytes + nome de um arquivo PDF |

### Eventos de Domínio
| Evento | Disparado quando |
|--------|-----------------|
| `ConversationStartedEvent` | Uma nova conversa é iniciada |
| `MessageReceivedEvent` | Uma mensagem é adicionada à conversa |
| `ToolExecutedEvent` | A IA executa uma ferramenta |

### Interfaces (contratos)
- `IConversationRepository` — persistência de conversas
- `IDomainEventDispatcher` — disparo de eventos de domínio

---

## Camada Application (`Jarvis.Application`)

Orquestra os casos de uso. Depende apenas do Domain.

### CQRS — Commands
| Command | Ação |
|---------|------|
| `SendMessageCommand` | Usuário envia mensagem (texto, imagem ou PDF) |
| `StartListeningCommand` | Inicia gravação de áudio (STT) |
| `CaptureScreenCommand` | Captura a tela atual |
| `ClearConversationCommand` | Limpa o histórico da conversa |
| `ResumeSessionCommand` | Retoma uma sessão anterior |
| `DeleteSessionCommand` | Exclui uma sessão salva |

### CQRS — Queries
| Query | Retorno |
|-------|---------|
| `GetConversationHistoryQuery` | Histórico de mensagens |
| `GetSessionsQuery` | Lista de sessões salvas |
| `GetAvailableToolsQuery` | Ferramentas disponíveis para a IA |

### Abstrações (interfaces dos providers)
| Interface | Implementações |
|-----------|---------------|
| `IAIProvider` | Claude, OpenAI, Gemini, Ollama |
| `IVisionAIProvider` | Adapter sobre `IAIProvider` |
| `ISpeechToTextProvider` | Windows (Whisper local), Whisper API |
| `ITextToSpeechProvider` | Windows TTS, Piper, ElevenLabs |
| `IScreenCaptureProvider` | Windows Forms screen capture |
| `IImageAnalysisProvider` | Multimodal via `IAIProvider` |
| `IToolRegistry` | Registro de ferramentas disponíveis |

### Serviços de aplicação
| Serviço | Responsabilidade |
|---------|-----------------|
| `AssistantOrchestrator` | Orquestra o fluxo completo: mensagem → IA → ferramentas → resposta |
| `ConversationService` | Gerencia o histórico da conversa ativa |
| `ToolDispatcher` | Despacha a execução das ferramentas solicitadas pela IA |

### Pipeline de Behaviors (MediatR)
- `LoggingBehavior` — loga entrada/saída de cada command/query
- `ValidationBehavior` — valida os requests antes de processar

---

## Camada Infrastructure (`Jarvis.Infrastructure`)

Implementações concretas de infraestrutura.

| Componente | Responsabilidade |
|------------|-----------------|
| `JsonConversationRepository` | Persiste conversas em arquivos JSON locais |
| `DomainEventDispatcher` | Dispara eventos de domínio para os handlers |
| `JarvisSettings` | Configurações globais + `GetEffectiveSystemPrompt()` |
| `AISettings` | Configurações dos providers de IA |
| `VoiceSettings` | Configurações de STT/TTS |

---

## Camada Providers

### Jarvis.AI — Provedores de IA
Todos implementam `IAIProvider`:

| Provider | Modelo padrão | Suporta ferramentas | Suporta visão | Suporta PDF |
|----------|--------------|---------------------|---------------|-------------|
| `OllamaProvider` | qwen3:8b | Não | Sim (minicpm-v) | Sim |
| `ClaudeProvider` | claude-opus-4-6 | Sim | Sim | Sim |
| `OpenAIProvider` | gpt-4o | Sim | Sim | Não |
| `GeminiProvider` | gemini-2.0-flash | Não | Sim | Sim |

O `AIProviderFactory` seleciona o provider em runtime com base nas configurações.

### Jarvis.Vision — Visão
| Componente | Responsabilidade |
|------------|-----------------|
| `ScreenCaptureProvider` | Captura tela via `System.Drawing` |
| `MultimodalImageAnalysisProvider` | Delega análise ao `IAIProvider` |

### Jarvis.Voice — Voz
| Provider | STT / TTS | Descrição |
|----------|-----------|-----------|
| `WindowsSpeechToTextProvider` | STT | Whisper.net local + NAudio |
| `WhisperSpeechToTextProvider` | STT | OpenAI Whisper API |
| `WindowsTextToSpeechProvider` | TTS | Windows Speech Synthesis |
| `PiperTextToSpeechProvider` | TTS | Piper offline PT-BR |
| `ElevenLabsTextToSpeechProvider` | TTS | ElevenLabs API |

---

## Camada Tools

Ferramentas que a IA pode invocar durante uma conversa.

| Ferramenta | Categoria | O que faz |
|------------|-----------|-----------|
| `OpenApplicationTool` | Sistema | Abre um aplicativo |
| `ReadFileTool` | Sistema | Lê conteúdo de arquivo |
| `WriteFileTool` | Sistema | Escreve em arquivo |
| `ExecutePowerShellTool` | Sistema | Executa comandos PowerShell |
| `WebSearchTool` | Browser | Busca na web (DuckDuckGo/Google/Brave) |
| `WebFetchSearchTool` | Browser | Busca + fetcha conteúdo da página |
| `OpenChromeTool` | Browser | Abre URL no Chrome |
| `GitTool` | Desenvolvimento | Executa comandos Git |
| `OpenVisualStudioTool` | Desenvolvimento | Abre solução no Visual Studio |
| `CaptureScreenTool` | Visão | Captura e analisa a tela |

O `ToolRegistry` registra todas as ferramentas. O `ToolDispatcher` despacha a execução.

---

## Camada Presentation (`Jarvis.Desktop`)

WPF com padrão **MVVM** usando **CommunityToolkit.Mvvm**.

| ViewModel | View | Responsabilidade |
|-----------|------|-----------------|
| `ChatViewModel` | `MainWindow` | Chat principal, envio de mensagens, áudio, imagem |
| `SettingsViewModel` | `SettingsView` | Configurações de IA, voz, busca, aparência |

Comunicação entre ViewModels via `WeakReferenceMessenger` (MVVM Toolkit).

---

## Fluxo de uma Mensagem

```
Usuário digita/fala
        ↓
ChatViewModel.SendMessageAsync()
        ↓
MediatR → SendMessageCommand
        ↓
SendMessageCommandHandler
        ↓
AssistantOrchestrator.ProcessUserInputAsync()
        ↓
IAIProvider.SendMessageWithToolsAsync()   ← qwen3:8b
        ↓
    [IA solicita ferramenta?]
    Sim → ToolDispatcher.DispatchAsync()
         → Executa a ferramenta (ex: WebSearch)
         → Devolve resultado para a IA
         → IA gera resposta final
    Não → Resposta direta
        ↓
ConversationService.AddAssistantMessageAsync()
        ↓
[VoiceEnabled?] → TTS fala a resposta
        ↓
ChatViewModel exibe a resposta
```

---

## Fluxo de Análise de Imagem

```
Usuário anexa imagem + texto
        ↓
ChatViewModel → SendMessageCommand (com ImageData)
        ↓
SendMessageCommandHandler → ProcessUserInputWithImageAsync()
        ↓
IVisionAIProvider.SendMessageWithVisionAsync()   ← minicpm-v:latest
        ↓
OllamaProvider → POST /api/generate
  { model, prompt, images: [base64], options: { num_ctx: 8192 } }
        ↓
Resposta em português (instruída pelo system prompt)
```

---

## Configuração em Runtime

As configurações são carregadas de dois arquivos:
- `appsettings.json` — valores padrão
- `appsettings.user.json` — valores do usuário (sobrescreve os padrões)

O `JarvisSettings.GetEffectiveSystemPrompt()` monta o system prompt final combinando:
1. Instrução de idioma (ex: `REGRA ABSOLUTA: Responda SEMPRE em português do Brasil`)
2. Nome do usuário
3. System prompt personalizado definido nas configurações

---

## Tecnologias Utilizadas

| Tecnologia | Uso |
|------------|-----|
| **.NET 9 / C#** | Plataforma principal |
| **WPF** | Interface desktop |
| **MediatR** | CQRS e Mediator |
| **CommunityToolkit.Mvvm** | MVVM (ObservableProperty, RelayCommand) |
| **Ollama** | Modelos de IA locais (qwen3:8b, minicpm-v) |
| **Whisper.net** | Transcrição de áudio offline |
| **NAudio** | Captura de áudio do microfone |
| **Piper** | TTS offline em português |
| **System.Text.Json** | Serialização JSON |
| **Microsoft.Extensions.DependencyInjection** | Injeção de dependência |
| **Microsoft.Extensions.Logging** | Logs estruturados |
