using System;
using System.ComponentModel;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;

namespace AutomatedCodeReview
{
    public class TeamExplorerSection : ITeamExplorerSection
    {
        private IServiceProvider ServiceProvider { get; set; }

        private string m_title;
        private bool m_isExpanded = true;
        private bool m_isVisible = true;
        private object m_sectionContent;

        public string Title
        {
            get { return m_title; }

            protected set
            {
                m_title = value;
                RaisePropertyChanged(@"Title");
            }
        }

        public object SectionContent
        {
            get { return m_sectionContent; }

            protected set
            {
                m_sectionContent = value;
                RaisePropertyChanged(@"SectionContent");
            }
        }

        public bool IsVisible
        {
            get { return m_isVisible; }

            set
            {
                m_isVisible = value;
                RaisePropertyChanged(@"IsVisible");
            }
        }

        public bool IsExpanded
        {
            get { return m_isExpanded; }

            set
            {
                m_isExpanded = value;
                RaisePropertyChanged(@"IsExpanded");
            }
        }

        public bool IsBusy { get; }

        public void Initialize(object sender, SectionInitializeEventArgs e)
        {
            ServiceProvider = e.ServiceProvider;
        }

        public void SaveContext(object sender, SectionSaveContextEventArgs e)
        {
        }

        public virtual void Loaded(object sender, SectionLoadedEventArgs e)
        {
        }

        public void Refresh()
        {
        }

        public void Cancel()
        {
        }

        public object GetExtensibilityService(Type serviceType)
        {
            return null;
        }

        public void Dispose()
        {
        }

        protected ITeamFoundationContext CurrentContext => GetService<ITeamFoundationContextManager>()?.CurrentContext;

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected T GetService<T>()
        {
            return (T) ServiceProvider?.GetService(typeof(T));
        }
    }
}