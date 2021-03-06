﻿using Caliburn.Micro;
using Bicikelj.ViewModels;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using System.Net;

namespace Bicikelj
{
    //a convenience extension method
    public static class NavigationExtension
    {
        public static void NavigateTo(object targetModel, string context = null)
        {
            IoC.Get<IPhoneService>().State[HostPageViewModel.TARGET_VM_KEY] = targetModel;
            IoC.Get<IPhoneService>().State[HostPageViewModel.TARGET_VM_CTX] = context;
            App.Current.RootVisual.Dispatcher.BeginInvoke(() => {
                IoC.Get<INavigationService>().UriFor<HostPageViewModel>().WithParam<string>(vm => vm.DisplayName, targetModel.GetType().Name).Navigate();
            });
        }

        public static void NavigateTo(string targetModelName, IDictionary<string, string> properties, string context = null)
        {
            var viewModelType = Type.GetType(targetModelName);
            var viewModel = IoC.GetInstance(viewModelType, null);

            foreach (var pair in properties)
            {
                if (string.IsNullOrEmpty(context) && pair.Key == "context")
                    context = pair.Value;

                var property = viewModelType.GetPropertyCaseInsensitive(pair.Key);
                if (property == null)
                    continue;

                property.SetValue(
                    viewModel,
                    MessageBinder.CoerceValue(property.PropertyType, pair.Value, null),
                    null);
            }

            NavigateTo(viewModel, context);
        }
    }

    public static class UriExtension
    {
        private static readonly Regex QueryStringRegex = new Regex(@"[\?&](?<name>[^&=]+)=(?<value>[^&=]+)");

        public static IEnumerable<KeyValuePair<string, string>> ParseQueryString(this Uri uri)
        {
            if (uri == null)
                throw new ArgumentException("uri");

            var matches = QueryStringRegex.Matches(uri.OriginalString);
            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                yield return new KeyValuePair<string, string>(HttpUtility.UrlDecode(match.Groups["name"].Value), HttpUtility.UrlDecode(match.Groups["value"].Value));
            }
        }

        public static IDictionary<string, string> ParseQueryStringEx(this Uri uri)
        {
            if (uri == null)
                throw new ArgumentException("uri");

            var matches = QueryStringRegex.Matches(uri.OriginalString);
            var result = new Dictionary<string, string>();
            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                result.Add(HttpUtility.UrlDecode(match.Groups["name"].Value), HttpUtility.UrlDecode(match.Groups["value"].Value));
            }
            return result;
        }
    }
}