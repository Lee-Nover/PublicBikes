using Caliburn.Micro;
using Coding4Fun.Toolkit.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PublicBikes.Tools
{
    public static class UIHelpers
    {
        public static void ShowToast(string title, string message, System.Action<bool> onComplete, Orientation textOrientation = Orientation.Horizontal, int timeoutMs = 10000)
        {
            Execute.OnUIThread(() =>
            {
                ToastPrompt toast = new ToastPrompt();
                toast.MillisecondsUntilHidden = timeoutMs;
                toast.Title = title;
                toast.Message = message;
                //toast.ImageSource = new BitmapImage(new Uri("/Images/PublicBikeLogo62.png", UriKind.RelativeOrAbsolute));
                toast.TextOrientation = textOrientation;
                if (onComplete != null)
                {
                    EventHandler<PopUpEventArgs<string, PopUpResult>> completed = (sender, e) =>
                    {
                        onComplete(e.PopUpResult == PopUpResult.Ok);
                    };
                    toast.Completed += completed;
                }
                toast.Show();
            });
        }
    }
}
