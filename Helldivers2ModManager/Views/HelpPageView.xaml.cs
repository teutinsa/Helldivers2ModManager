using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Helldivers2ModManager.Views
{
	internal partial class HelpPageView : Page
	{
		public HelpPageView()
		{
			InitializeComponent();
		}

		private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			if (sender is Hyperlink link)
			{
				Process.Start(new ProcessStartInfo(link.NavigateUri.AbsoluteUri)
				{
					UseShellExecute = true
				});
			}
		}
	}
}
