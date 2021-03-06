﻿using System;
using System.Threading.Tasks;

using Abp.Dependency;

using DynamicTranslator.Service.GoogleAnalytics;

namespace DynamicTranslator
{
    public class GoogleAnalyticsTracker : IObserver<long>, ISingletonDependency
    {
        private readonly IGoogleAnalyticsService _googleAnalyticsService;

        public GoogleAnalyticsTracker(IGoogleAnalyticsService googleAnalyticsService)
        {
            _googleAnalyticsService = googleAnalyticsService;
        }

        public void OnCompleted() {}

        public void OnError(Exception error) {}

        public void OnNext(long value)
        {
            Task.Run(() =>
            {
                _googleAnalyticsService.TrackAppScreenAsync("DynamicTranslator",
                    ApplicationVersion.GetCurrentVersion(),
                    "dynamictranslator",
                    "dynamictranslator",
                    "MainWindow");
            });
        }
    }
}
