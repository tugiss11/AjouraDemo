using System;
using Catel.IoC;
using Catel.Logging;
using Catel.Messaging;
using Catel.Services;

namespace ArcGISRuntime.Samples.DesktopViewer.ViewsAndViewModels
{
    public abstract class ViewModelBase : Catel.MVVM.ViewModelBase
    {
        private IUIVisualizerService _vis;
        private IMessageMediator _mes;
        private readonly IDependencyResolver _dependencyResolver;
        protected readonly ILog log = LogManager.GetCurrentClassLogger();
        

        protected ViewModelBase()
        {
            _dependencyResolver = this.GetDependencyResolver();
   
        }
        public IUIVisualizerService VisualizerService
        {
            get
            {
                if (_vis != null) return _vis;
                _vis = _dependencyResolver.Resolve<IUIVisualizerService>();
                return _vis;
            }
        }

        public IMessageMediator MessageMediator
        {
            get
            {
                if (_mes != null) return _mes;
                _mes = _dependencyResolver.Resolve<IMessageMediator>();
                return _mes;
            }
        }

        public bool ShowModalDialog(ViewModelBase viewModel, Type t)
        {
            VisualizerService.Unregister(viewModel.GetType());
            VisualizerService.Register(viewModel.GetType(), t);

            var result = VisualizerService.ShowDialog(viewModel);

            if (!result.HasValue)
            {
                return false;
            }

            return Convert.ToBoolean(result);
        }

        public void ShowModalessDialog(ViewModelBase viewModel, Type t)
        {
            VisualizerService.Unregister(viewModel.GetType());
            VisualizerService.Register(viewModel.GetType(), t);
            VisualizerService.Show(viewModel, OnWindowClose);
        }

        private void OnWindowClose(object sender, UICompletedEventArgs e)
        {
            System.Windows.Application.Current.MainWindow.Focus();
        }
    }
}
