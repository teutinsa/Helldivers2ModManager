using CommunityToolkit.Mvvm.Messaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Helldivers2ModManager.Components
{
	internal sealed class MessageBoxInfoMessage
	{
		public required string Message { get; init; }
	}

	internal sealed class MessageBoxWarningMessage
	{
		public required string Message { get; init; }
	}

	internal sealed class MessageBoxErrorMessage
	{
		public required string Message { get; init; }
	}

	internal sealed class MessageBoxProgressMessage
	{
		public required string Title { get; init; }

		public required string Message { get; init; }
	}

	internal sealed class MessageBoxHideMessage { }

	internal partial class MessageBox : UserControl, IRecipient<MessageBoxInfoMessage>, IRecipient<MessageBoxWarningMessage>, IRecipient<MessageBoxErrorMessage>, IRecipient<MessageBoxProgressMessage>, IRecipient<MessageBoxHideMessage>
	{
		public MessageBox()
		{
			InitializeComponent();

			WeakReferenceMessenger.Default.Register<MessageBoxInfoMessage>(this);
			WeakReferenceMessenger.Default.Register<MessageBoxWarningMessage>(this);
			WeakReferenceMessenger.Default.Register<MessageBoxErrorMessage>(this);
			WeakReferenceMessenger.Default.Register<MessageBoxProgressMessage>(this);
			WeakReferenceMessenger.Default.Register<MessageBoxHideMessage>(this);
		}

		public void Receive(MessageBoxInfoMessage message)
		{
			title.Text = "Info";
			brush.Color = Colors.White;
			this.message.Text = message.Message;
			button.Visibility = Visibility.Visible;
			progress.Visibility = Visibility.Hidden;
			Visibility = Visibility.Visible;
		}

		public void Receive(MessageBoxWarningMessage message)
		{
			title.Text = "Warning";
			brush.Color = Colors.Yellow;
			this.message.Text = message.Message;
			button.Visibility = Visibility.Visible;
			progress.Visibility = Visibility.Hidden;
			Visibility = Visibility.Visible;
		}

		public void Receive(MessageBoxErrorMessage message)
		{
			title.Text = "Error";
			brush.Color = Colors.Red;
			this.message.Text = message.Message;
			button.Visibility = Visibility.Visible;
			progress.Visibility = Visibility.Hidden;
			Visibility = Visibility.Visible;
		}

		public void Receive(MessageBoxProgressMessage message)
		{
			title.Text = message.Title;
			brush.Color = Colors.White;
			this.message.Text = message.Message;
			button.Visibility = Visibility.Hidden;
			progress.Visibility = Visibility.Visible;
			Visibility = Visibility.Visible;
		}

		public void Receive(MessageBoxHideMessage message)
		{
			Visibility = Visibility.Hidden;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Receive(new MessageBoxHideMessage());
		}
	}
}
