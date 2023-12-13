using PlaneFocusLogger.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PlaneFocusLogger.Logging;

[AttributeUsage(AttributeTargets.Class)]
public class AllowScreenLogAttribute : Attribute
{
    public ScreenLogger.Target Target { get; set; }
    public AllowScreenLogAttribute() { }
}

/// <summary>
/// Logs data to a widget visible on-screen
/// </summary>
public class ScreenLogger
{
    public enum Target
    {
        /// <summary>
        /// FOR INTERNAL USE ONLY!
        /// </summary>
        Unknown,
        /// <summary>
        /// outputs only in testing mode; silent in ordinal conditions
        /// </summary>
        DataSource,
        /// <summary>
        /// plane enter/exit events
        /// </summary>
        Parser,
    }

    /// <summary>
    /// Initializatoin procedure, to be calleed once only before calling <see cref="Create(Target?)"/>
    /// </summary>
    /// <param name="output">TextBox to receive logging data</param>
    /// <param name="controls">Parent panel to insert controls to enable/disable logging domains</param>
    /// <exception cref="Exception">Regular exception if the initialization </exception>
    public static void Initialize(TextBox output, Panel? controls = null)
    {
        if (output == null)
        {
            throw new ArgumentException($"Output cannot be null", nameof(output));
        }
        if (_printer != null)
        {
            throw new Exception($"{nameof(ScreenPrinter)} has been initialized already");
        }

        _printer = new ScreenPrinter(output);

        if (controls != null)
        {
            CreateControls(controls);
        }
    }

    /// <summary>
    /// Creates a screen logger
    /// </summary>
    /// <param name="target">If specified, then assigns the loging target (source of events) explicitely, 
    /// otherwise evaluates the target from the attibute of from the caller class name</param>
    /// <returns>screen logger instance</returns>
    public static ScreenLogger? Create(Target? target = null)
    {
        if (target is Target t)
        {
            return new ScreenLogger(t);
        }

        var stackTrace = new StackTrace();
        var cls = stackTrace.GetFrame(1)?.GetMethod()?.DeclaringType;
        if (cls is null)
            return null;

        var attr = (AllowScreenLogAttribute?)Attribute.GetCustomAttribute(cls, typeof(AllowScreenLogAttribute));

        if (attr?.Target != Target.Unknown)
        {
            return new ScreenLogger(attr!.Target);
        }

        var targ = Enum.GetNames(typeof(Target)).FirstOrDefault(t => t == cls.Name);
        if (!string.IsNullOrEmpty(targ))
        {
            return new ScreenLogger((Target)Enum.Parse(typeof(Target), targ));
        }

        return null;
    }

    /// <summary>
    /// Logs data
    /// </summary>
    /// <param name="message">Data to log</param>
    public void Log(object message)
    {
        if (Enabled.Contains(_target))
        {
            _printer?.Print(_target, message);
        }
    }

    // Internal

    static ScreenPrinter? _printer = null;

    readonly Target _target;

    static readonly HashSet<Target> Enabled = new()
    {
        Target.Parser,
    };

    private class ScreenPrinter
    {
        public ScreenPrinter(TextBox output)
        {
            _output = output;
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        public void Print(Target target, object message)
        {
            _dispatcher.Invoke(PrintSafe, target, message);
        }

        // Internal

        readonly TextBox _output;
        readonly Dispatcher _dispatcher;

        private void PrintSafe(Target target, object message)
        {
            _output.Text += $"\n[{target} : {Timestamp.Ms}] {message}";
            _output.ScrollToEnd();
        }
    }

    private ScreenLogger(Target target)
    {
        _target = target;
    }

    private static void CreateControls(Panel parent)
    {
        foreach (var v in Enum.GetNames(typeof(Target)))
        {
            var target = (Target)Enum.Parse(typeof(Target), v);
            if (target == Target.Unknown)
                continue;

            var chk = new CheckBox()
            {
                Content = v,
                Margin = new Thickness(4, 4, 24, 4),
                IsChecked = Enabled.Contains(target),
                IsEnabled = target != Target.DataSource || App.IsDebugging,
            };
            chk.Checked += (s, e) => Enabled.Add(target);
            chk.Unchecked += (s, e) => Enabled.Remove(target);
            parent.Children.Add(chk);
        }
    }
}
