using Jarvis.Application.DTOs;
using Jarvis.Domain.ValueObjects;
using Jarvis.Shared.Models;
using MediatR;

namespace Jarvis.Application.Commands;

public record SendMessageCommand(
    string UserInput,
    bool UseVoiceResponse = false,
    ImageData? Image = null,
    PdfData? Pdf = null,
    string? AiContextPrompt = null) : IRequest<Result<AIResponse>>;
