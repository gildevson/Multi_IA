using Jarvis.Application.DTOs;
using Jarvis.Application.Tools;
using Jarvis.Domain.Entities;
using Jarvis.Domain.ValueObjects;

namespace Jarvis.Application.Abstractions;

/// <summary>
/// Provider de IA usado exclusivamente para análise de imagens.
/// Pode ser diferente do provider principal de chat.
/// </summary>
public interface IVisionAIProvider : IAIProvider { }
