using MauiMaze.Models;
using MauiMaze.PageModels;

namespace MauiMaze.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}