﻿using System;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

using Abp.Dependency;
using Abp.Runtime.Caching;

using DynamicTranslator.Constants;
using DynamicTranslator.Orchestrators;
using DynamicTranslator.Orchestrators.Detector;
using DynamicTranslator.Orchestrators.Event;
using DynamicTranslator.Orchestrators.Finder;
using DynamicTranslator.Orchestrators.Model;
using DynamicTranslator.Orchestrators.Organizer;
using DynamicTranslator.Service.GoogleAnalytics;
using DynamicTranslator.Wpf.ViewModel.Model;

namespace DynamicTranslator.Wpf.Orchestrators.Observers
{
    public class Finder : IObserver<EventPattern<WhenClipboardContainsTextEventArgs>>, ISingletonDependency
    {
        private readonly ITypedCache<string, TranslateResult[]> cache;
        private readonly ICacheManager cacheManager;
        private readonly IGoogleAnalyticsService googleAnalytics;
        private readonly ILanguageDetector languageDetector;
        private readonly IMeanFinderFactory meanFinderFactory;
        private readonly INotifier notifier;
        private readonly IResultOrganizer resultOrganizer;

        private string previousString;

        public Finder(INotifier notifier,
            IMeanFinderFactory meanFinderFactory,
            IResultOrganizer resultOrganizer,
            ICacheManager cacheManager,
            IGoogleAnalyticsService googleAnalytics,
            ILanguageDetector languageDetector
            )
        {
            if (notifier == null)
                throw new ArgumentNullException(nameof(notifier));

            if (meanFinderFactory == null)
                throw new ArgumentNullException(nameof(meanFinderFactory));

            if (resultOrganizer == null)
                throw new ArgumentNullException(nameof(resultOrganizer));

            if (cacheManager == null)
                throw new ArgumentNullException(nameof(cacheManager));

            if (languageDetector == null)
                throw new ArgumentNullException(nameof(languageDetector));

            if (googleAnalytics == null)
                throw new ArgumentNullException(nameof(googleAnalytics));

            this.notifier = notifier;
            this.meanFinderFactory = meanFinderFactory;
            this.resultOrganizer = resultOrganizer;
            this.cacheManager = cacheManager;
            this.googleAnalytics = googleAnalytics;
            this.languageDetector = languageDetector;
            cache = this.cacheManager.GetCache<string, TranslateResult[]>(CacheNames.MeanCache);
        }

        public void OnCompleted() {}

        public void OnError(System.Exception error) {}

        public async void OnNext(EventPattern<WhenClipboardContainsTextEventArgs> value)
        {
            await Task.Run(async () =>
            {
                var currentString = value.EventArgs.CurrentString;

                if (previousString == currentString)
                    return;

                previousString = currentString;

                var fromLanguageExtension = await languageDetector.DetectLanguage(currentString);

                var results = await cache.GetAsync(currentString,
                    async () => await Task.WhenAll(meanFinderFactory.GetFinders().Select(t => t.Find(new TranslateRequest(currentString, fromLanguageExtension)))))
                                         .ConfigureAwait(false);

                var findedMeans = await resultOrganizer.OrganizeResult(results, currentString).ConfigureAwait(false);

                await notifier.AddNotificationAsync(currentString, ImageUrls.NotificationUrl, findedMeans.DefaultIfEmpty(string.Empty).First()).ConfigureAwait(false);

                await googleAnalytics.TrackEventAsync("DynamicTranslator", "Translate", currentString, null).ConfigureAwait(false);

                await googleAnalytics.TrackAppScreenAsync("DynamicTranslator",
                    ApplicationVersion.GetCurrentVersion(),
                    "dynamictranslator",
                    "dynamictranslator",
                    "notification").ConfigureAwait(false);
            });
        }
    }
}