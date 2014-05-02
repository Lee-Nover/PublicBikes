using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Caliburn.Micro.Contrib.Dialogs
{
	public class DialogResult<TResponse> : IResult
	{
		private Func<IDialogViewModel<TResponse>> _locateVM =
			() => new DialogViewModel<TResponse>();

		public DialogResult(Dialog<TResponse> dialog)
		{
			Dialog = dialog;
		}

		public Dialog<TResponse> Dialog { get; private set; }

		public event EventHandler<ResultCompletionEventArgs> Completed;

		public void Execute(ActionExecutionContext context)
		{
			IDialogViewModel<TResponse> vm = _locateVM();
			vm.Dialog = Dialog;
			Micro.Execute.OnUIThread(() =>
				{
					IoC.Get<IWindowManager>().ShowDialog(vm);
					var deactivated = vm as IDeactivate;
					if (deactivated == null)
						Completed(this, new ResultCompletionEventArgs());
					else
					{
						deactivated.Deactivated += (o, e) =>
						{
							if (e.WasClosed)
							{
								Completed(this, new ResultCompletionEventArgs());
							}
						};
					}
				}
			);
		}

		public DialogResult<TResponse> In(IDialogViewModel<TResponse> dialogViewModel)
		{
			_locateVM = () => dialogViewModel;
			return this;
		}

		public DialogResult<TResponse> In<TDialogViewModel>()
			where TDialogViewModel : IDialogViewModel<TResponse>
		{
			_locateVM = () => IoC.Get<TDialogViewModel>();
			return this;
		}
	}

	public static class DialogExtension
	{
		public static DialogResult<TResponse> AsResult<TResponse>(this Dialog<TResponse> dialog)
		{
			return new DialogResult<TResponse>(dialog);
		}
	}
}
