﻿using System;
using Serilog.Formatting;

namespace Serilog.Sinks.Amazon.Kinesis.Common
{
    abstract class KinesisSinkStateBase
    {
        private readonly KinesisSinkOptionsBase _options;
        private readonly ITextFormatter _formatter;
        private readonly ITextFormatter _durableFormatter;
        protected KinesisSinkStateBase(KinesisSinkOptionsBase options)
        {
            if (options == null) throw new ArgumentNullException("options");
            _options = options;
            if (string.IsNullOrWhiteSpace(options.StreamName)) throw new ArgumentException("options.StreamName");
            _formatter = options.CustomDurableFormatter ?? new Serilog.Formatting.Json.JsonFormatter(
                closingDelimiter: string.Empty,
                renderMessage: true,
                formatProvider: options.FormatProvider
                );

            _durableFormatter = options.CustomDurableFormatter ?? new Serilog.Formatting.Json.JsonFormatter(
                closingDelimiter: Environment.NewLine,
                renderMessage: true,
                formatProvider: options.FormatProvider
                );

        }

        public KinesisSinkOptionsBase Options { get { return _options; } }
        public ITextFormatter Formatter { get { return _formatter; } }
        public ITextFormatter DurableFormatter { get { return _durableFormatter; } }


    }
}