using System;
using System.Windows.Media.Imaging;

namespace DesignFactory.WebMatrix.Executer
{
    public class TaskItem
    {
        private static BitmapImage _errorIcon = new BitmapImage(new Uri("pack://application:,,,/DesignFactory.WebMatrix.Executer;component/Images/error.gif", UriKind.Absolute));
        private static BitmapImage _warningIcon = new BitmapImage(new Uri("pack://application:,,,/DesignFactory.WebMatrix.Executer;component/Images/warning.gif", UriKind.Absolute));
        private static BitmapImage _informationIcon = new BitmapImage(new Uri("pack://application:,,,/DesignFactory.WebMatrix.Executer;component/Images/information.gif", UriKind.Absolute));
        private static BitmapImage _helpIcon = new BitmapImage(new Uri("pack://application:,,,/DesignFactory.WebMatrix.Executer;component/Images/help.gif", UriKind.Absolute));

        public string TaskSource { get; set; }
        public TaskCategory Category { set; get; }
        public string Text { get; set; }
        public bool HasHelpLink { get; set; }
        public string HelpLink { get; set; }
        public string Code { get; set; }
        public string Filename { get; set; }
        public string WorkspaceRelativeFilename { get; set; }
        public int Linenumber { get; set; }
        public int Column { get; set; }

        public BitmapImage CategoryIcon
        {
            get
            {
                if (Category == TaskCategory.Error)
                {
                    return _errorIcon;
                }
                else if (Category == TaskCategory.Warning)
                {
                    return _warningIcon;
                }
                else
                {
                    return _informationIcon;
                }
            }
        }

        public BitmapImage HelpIcon
        {
            get
            {
                return HelpLink != null? _helpIcon : null;
            }
        }
    }
}
