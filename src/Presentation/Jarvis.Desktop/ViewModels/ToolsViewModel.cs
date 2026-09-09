using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jarvis.Application.DTOs;
using Jarvis.Application.Queries;
using MediatR;
using System.Collections.ObjectModel;

namespace Jarvis.Desktop.ViewModels;

public partial class ToolsViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<ToolDto> Tools { get; } = new();

    public ToolsViewModel(IMediator mediator)
    {
        _mediator = mediator;
        _ = LoadToolsAsync();
    }

    [RelayCommand]
    private async Task LoadToolsAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _mediator.Send(new GetAvailableToolsQuery());
            if (result.IsSuccess)
            {
                Tools.Clear();
                foreach (var tool in result.Value)
                    Tools.Add(tool);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
