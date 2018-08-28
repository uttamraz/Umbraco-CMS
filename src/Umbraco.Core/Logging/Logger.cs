﻿using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Umbraco.Core.Configuration;
using Umbraco.Core.Diagnostics;
using Serilog;
using Serilog.Events;
using Umbraco.Core.Logging.SerilogExtensions;

namespace Umbraco.Core.Logging
{
    ///<summary>
    /// Implements <see cref="ILogger"/> on top of Serilog.
    ///</summary>
    public class Logger : ILogger
    {
        /// <summary>
        /// Initialize a new instance of the <see cref="Logger"/> class with a configuration file.
        /// </summary>
        /// <param name="logConfigFile"></param>
        public Logger(FileInfo logConfigFile)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings(filePath: AppDomain.CurrentDomain.BaseDirectory + logConfigFile)
                .CreateLogger();
        }

        public Logger(LoggerConfiguration logConfig)
        {
            //Configure Serilog static global logger with config passed in
            Log.Logger = logConfig.CreateLogger();
        }

        /// <summary>
        /// Creates a logger with some pre-definied configuration and remainder from config file
        /// </summary>
        /// <remarks>Used by UmbracoApplicationBase to get its logger.</remarks>
        public static Logger CreateWithDefaultConfiguration()
        {
            var loggerConfig = new LoggerConfiguration();
            loggerConfig
                .MinimalConfiguration()
                .OutputDefaultTextFile(LogEventLevel.Debug)
                .OutputDefaultJsonFile()
                .ReadFromConfigFile()
                .ReadFromUserConfigFile();

            return new Logger(loggerConfig);
        }

        /// <inheritdoc/>
        public void Fatal(Type reporting, Exception exception, string message)
        {
            Fatal(reporting, exception, message, null);
        }

        /// <inheritdoc/>
        public void Fatal(Type reporting, Exception exception)
        {
            Fatal(reporting, exception, string.Empty);
        }

        /// <inheritdoc/>
        public void Fatal(Type reporting, string message)
        {
            //Sometimes we need to throw an error without an ex
            Fatal(reporting, null, message);
        }

        /// <inheritdoc/>
        public void Fatal(Type reporting, string messageTemplate, params object[] propertyValues)
        {
            //Log a structured message WITHOUT an ex
            Fatal(reporting, null, messageTemplate, propertyValues);
        }

        /// <inheritdoc/>
        public void Fatal(Type reporting, Exception exception, string messageTemplate, params object[] propertyValues)
        {
            ErrorOrFatal(Fatal, exception, ref messageTemplate);
            var logger = Log.Logger;
            logger?.ForContext(reporting).Fatal(exception, messageTemplate, propertyValues);
        }

        /// <inheritdoc/>
        public void Error(Type reporting, Exception exception, string message)
        {
            Error(reporting, exception, message, null);
        }

        /// <inheritdoc/>
        public void Error(Type reporting, Exception exception)
        {
            Error(reporting, exception, string.Empty);
        }

        /// <inheritdoc/>
        public void Error(Type reporting, string message)
        {
            //Sometimes we need to throw an error without an ex
            Error(reporting, null, message);
        }

        /// <inheritdoc/>
        public void Error(Type reporting, string messageTemplate, params object[] propertyValues)
        {
            //Log a structured message WITHOUT an ex
            Error(reporting, null, messageTemplate, propertyValues);
        }

        /// <inheritdoc/>
        public void Error(Type reporting, Exception exception, string messageTemplate, params object[] propertyValues)
        {
            ErrorOrFatal(Error, exception, ref messageTemplate);
            var logger = Log.Logger;
            logger?.ForContext(reporting).Error(exception, messageTemplate, propertyValues);
        }

        private static void ErrorOrFatal(Action<Type, Exception, string, object[]> logAction, Exception exception, ref string messageTemplate)
        {
            var dump = false;

            if (IsTimeoutThreadAbortException(exception))
            {
                messageTemplate += "\r\nThe thread has been aborted, because the request has timed out.";

                // dump if configured, or if stacktrace contains Monitor.ReliableEnter
                dump = UmbracoConfig.For.CoreDebug().DumpOnTimeoutThreadAbort || IsMonitorEnterThreadAbortException(exception);

                // dump if it is ok to dump (might have a cap on number of dump...)
                dump &= MiniDump.OkToDump();
            }

            if (dump)
            {
                try
                {
                    var dumped = MiniDump.Dump(withException: true);
                    messageTemplate += dumped
                        ? "\r\nA minidump was created in App_Data/MiniDump"
                        : "\r\nFailed to create a minidump";
                }
                catch (Exception ex)
                {
                    //Log a new entry (as opposed to appending to same log entry)
                    logAction(ex.GetType(), ex, "Failed to create a minidump at App_Data/MiniDump ({ExType}: {ExMessage}",
                        new object[]{ ex.GetType().FullName, ex.Message });
                }
            }
        }

        private static bool IsMonitorEnterThreadAbortException(Exception exception)
        {
            if (!(exception is ThreadAbortException abort)) return false;

            var stacktrace = abort.StackTrace;
            return stacktrace.Contains("System.Threading.Monitor.ReliableEnter");
        }

        private static bool IsTimeoutThreadAbortException(Exception exception)
        {
            if (!(exception is ThreadAbortException abort)) return false;

            if (abort.ExceptionState == null) return false;

            var stateType = abort.ExceptionState.GetType();
            if (stateType.FullName != "System.Web.HttpApplication+CancelModuleException") return false;

            var timeoutField = stateType.GetField("_timeout", BindingFlags.Instance | BindingFlags.NonPublic);
            if (timeoutField == null) return false;

            return (bool) timeoutField.GetValue(abort.ExceptionState);
        }

        /// <inheritdoc/>
        public void Warn(Type reporting, string format)
        {
            var logger = Log.Logger;
            logger?.Warning(format);
        }
        
        /// <inheritdoc/>
        public void Warn(Type reporting, string messageTemplate, params object[] propertyValues)
        {
            var logger = Log.Logger;
            logger?.ForContext(reporting).Warning(messageTemplate, propertyValues);
        }

        /// <inheritdoc/>
        public void Warn(Type reporting, Exception exception, string message)
        {
            var logger = Log.Logger;
            logger?.ForContext(reporting).Warning(message, exception);
        }
        
        /// <inheritdoc/>
        public void Warn(Type reporting, Exception exception, string messageTemplate, params object[] propertyValues)
        {
            var logger = Log.Logger;
            logger?.ForContext(reporting).Warning(exception, messageTemplate, propertyValues);
        }

        /// <inheritdoc/>
        public void Info(Type reporting, string message)
        {
            var logger = Log.Logger;
            logger?.ForContext(reporting).Information(message);
        }

        /// <inheritdoc/>
        public void Info(Type reporting, string messageTemplate, params object[] propertyValues)
        {
            var logger = Log.Logger;
            logger?.ForContext(reporting).Information(messageTemplate, propertyValues);
        }

        /// <inheritdoc/>
        public void Debug(Type reporting, string message)
        {
            var logger = Log.Logger;
            logger?.ForContext(reporting).Debug(message);
        }
        
        /// <inheritdoc/>
        public void Debug(Type reporting, string messageTemplate, params object[] propertyValues)
        {
            var logger = Log.Logger;
            logger?.ForContext(reporting).Debug(messageTemplate, propertyValues);
        }

        /// <inheritdoc/>
        public void Verbose(Type reporting, string message)
        {
            var logger = Log.Logger;
            logger?.ForContext(reporting).Verbose(message);
        }

        /// <inheritdoc/>
        public void Verbose(Type reporting, string messageTemplate, params object[] propertyValues)
        {
            var logger = Log.Logger;
            logger?.ForContext(reporting).Verbose(messageTemplate, propertyValues);
        }

        
    }
}
