using CommunityToolkit.Mvvm.Input;
using MauiMaze.Models;

namespace MauiMaze.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}